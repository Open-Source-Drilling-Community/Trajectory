using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.General.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OSDC.Drilling.Trajectory.Model;

public static class TargetLandingCalculator
{
    internal const int MaximumAdaptiveDepth = 4;
    internal const int EllipsePointCount = 72;
    internal const int CalculationAlgorithmVersion = 7;
    internal const double BoundaryPositionTolerance = 0.25;
    internal const double PositionTolerance = 0.05;
    internal const double SamplingInterval = 10.0;

    public static List<string> Validate(TargetLandingCase? value)
    {
        List<string> errors = [];
        if (value?.MetaInfo?.ID is not Guid id || id == Guid.Empty) errors.Add("A non-empty case UUID is required.");
        if (value?.SourceTrajectoryID == Guid.Empty) errors.Add("A source trajectory is required.");
        if (value == null) return errors;
        if (!Finite(value.LeadLength) || value.LeadLength < 0.0) errors.Add("Lead length must be finite and non-negative.");
        if (!Finite(value.ConfidenceFactor) || value.ConfidenceFactor <= 0.0 || value.ConfidenceFactor > SurveyStationEllipseCalculation.MaximumConfidenceFactor)
            errors.Add("Confidence factor must be greater than zero and no greater than 0.999.");
        if (value.MaximumLandingCurvature is double maximum && (!Finite(maximum) || maximum <= 0.0))
            errors.Add("Maximum Landing Curvature must be positive when specified.");
        if (!TryCreateFrame(value.Target, out _, out string? frameError)) errors.Add(frameError!);
        ValidatePolygon(value.Target?.Polygon, errors);
        return errors;
    }

    public static bool Calculate(TargetLandingCase value, Trajectory source,
        Action<double, string>? progress = null)
    {
        List<string> errors = Validate(value);
        if (errors.Count > 0) return Fail(value, string.Join(" ", errors));
        if (!TrajectoryExtrapolationCalculator.TryGetOrderedCompleteStations(source, out List<SurveyStation> sourceStations))
            return Fail(value, "The source trajectory has no complete calculated station list.");
        if (!TryCreateFrame(value.Target, out PlaneFrame frame, out string? frameError))
            return Fail(value, frameError ?? "The target plane is invalid.");

        value.CalculationState = CalculationState.Running;
        value.CalculationProgress = 0.02;
        value.CalculationMessage = "Preparing target landing calculation";
        value.SourceTrajectoryRevision = source.LastModificationDate;
        value.SampleList = [];
        value.MeshTriangleList = [];
        value.GeologicalTargetBoundary = value.Target.Polygon.Select(Copy).ToList();
        value.DrillerTargetBoundary = [];
        value.ReachableTargetBoundary = [];
        value.DrillerTargetContourList = [];
        value.ReachableTargetContourList = [];
        progress?.Invoke(0.02, value.CalculationMessage);

        SurveyStation sourceEnd = TrajectoryExtrapolationCalculator.PrepareStartStation(sourceStations, source.CalculationType);
        sourceEnd.SurveyTool ??= sourceStations.AsEnumerable().Reverse().Select(x => x.SurveyTool).FirstOrDefault(x => x != null);
        value.SourceEndStation = new SurveyStation(sourceEnd);
        if (!TryApplyLead(sourceStations, source.CalculationType, sourceEnd, value.LeadLength,
                out SurveyStation steeringStart, out List<SurveyStation> leadStations))
            return Fail(value, "The final source trend could not be continued through the requested lead length.");
        TrajectoryExtrapolationCalculator.ContinueSourceUncertainty(leadStations, sourceEnd, sourceStations);
        steeringStart = new SurveyStation(leadStations[^1]);
        value.LeadSurveyStationList = leadStations.Select(station => new SurveyStation(station)).ToList();
        value.SteeringStartStation = new SurveyStation(steeringStart);

        Dictionary<string, TargetLandingSample> samples = [];
        TargetPlanePoint centroid = PolygonCentroid(value.Target.Polygon);
        TargetLandingSample center = Evaluate(centroid, value, frame, sourceEnd, steeringStart,
            leadStations, sourceStations);
        samples.Add(Key(centroid), center);
        for (int index = 0; index < value.Target.Polygon.Count; index++)
        {
            TargetPlanePoint first = value.Target.Polygon[index];
            TargetPlanePoint second = value.Target.Polygon[(index + 1) % value.Target.Polygon.Count];
            TargetLandingSample a = GetOrEvaluate(first);
            TargetLandingSample b = GetOrEvaluate(second);
            Refine(center, a, b, 0);
            value.CalculationProgress = 0.08 + 0.72 * (index + 1.0) / value.Target.Polygon.Count;
            value.CalculationMessage = $"Sampling target region edge {index + 1} of {value.Target.Polygon.Count}";
            progress?.Invoke(value.CalculationProgress, value.CalculationMessage);
        }

        value.CalculationProgress = 0.84;
        value.CalculationMessage = "Refining uncertainty-safe target boundary";
        progress?.Invoke(value.CalculationProgress, value.CalculationMessage);
        value.DrillerTargetContourList = value.TargetType == TargetLandingTargetType.DrillerTarget
            ? [value.Target.Polygon.Select(Copy).ToList()]
            : ExtractContours(samples.Values.ToList(), value.MeshTriangleList,
                x => x.IsUncertaintySafe == true, FindBoundaryPoint);
        value.CalculationProgress = 0.90;
        value.CalculationMessage = "Refining curvature and geometry reachability boundary";
        progress?.Invoke(value.CalculationProgress, value.CalculationMessage);
        value.ReachableTargetContourList = ExtractContours(samples.Values.ToList(), value.MeshTriangleList,
            x => x.State == TargetLandingSampleState.Reachable, FindBoundaryPoint);
        value.CalculationProgress = 0.98;
        value.CalculationMessage = "Finalizing target landing result";
        progress?.Invoke(value.CalculationProgress, value.CalculationMessage);
        value.SampleList = samples.Values.OrderBy(x => x.PlaneY).ThenBy(x => x.PlaneX).ToList();
        value.DrillerTargetBoundary = LargestContour(value.DrillerTargetContourList);
        value.ReachableTargetBoundary = LargestContour(value.ReachableTargetContourList);
        value.CalculationFingerprint = ComputeFingerprint(value, source);
        value.CalculationState = CalculationState.Completed;
        value.CalculationProgress = 1.0;
        value.CalculationMessage = value.SampleList.Any(x => x.State == TargetLandingSampleState.Reachable)
            ? null
            : "No sampled part of the target is reachable with the selected curve and Maximum Landing Curvature.";
        value.IsStale = false;
        progress?.Invoke(1.0, value.CalculationMessage ?? "Target landing calculation completed");
        return true;

        TargetLandingSample GetOrEvaluate(TargetPlanePoint point)
        {
            string key = Key(point);
            if (!samples.TryGetValue(key, out TargetLandingSample? sample))
            {
                sample = Evaluate(point, value, frame, sourceEnd, steeringStart,
                    leadStations, sourceStations);
                samples.Add(key, sample);
            }
            return sample;
        }

        TargetPlanePoint FindBoundaryPoint(TargetLandingSample first, TargetLandingSample second,
            Func<TargetLandingSample, bool> included) =>
            BisectBoundary(first, second, GetOrEvaluate, included, BoundaryPositionTolerance);

        void Refine(TargetLandingSample a, TargetLandingSample b, TargetLandingSample c, int depth)
        {
            double longest = Math.Max(Distance(a, b), Math.Max(Distance(b, c), Distance(c, a)));
            double scale = Math.Max(CharacteristicLength(value.Target.Polygon), PositionTolerance);
            bool mixed = a.State != b.State || b.State != c.State;
            bool refine = depth < 2 || (depth < MaximumAdaptiveDepth && (mixed || longest > scale / 5.0));
            if (!refine)
            {
                value.MeshTriangleList!.Add(new TargetLandingMeshTriangle
                {
                    FirstSampleID = a.SampleID,
                    SecondSampleID = b.SampleID,
                    ThirdSampleID = c.SampleID
                });
                return;
            }
            TargetLandingSample ab = GetOrEvaluate(Midpoint(a, b));
            TargetLandingSample bc = GetOrEvaluate(Midpoint(b, c));
            TargetLandingSample ca = GetOrEvaluate(Midpoint(c, a));
            Refine(a, ab, ca, depth + 1);
            Refine(ab, b, bc, depth + 1);
            Refine(ca, bc, c, depth + 1);
            Refine(ab, bc, ca, depth + 1);
        }
    }

    public static string ComputeFingerprint(TargetLandingCase value, Trajectory source)
    {
        var input = new
        {
            SourceRevision = source.LastModificationDate,
            Stations = source.SurveyStationList?.Select(x => new { x.MD, x.Inclination, x.Azimuth, x.RiemannianNorth, x.RiemannianEast, x.TVD }),
            value.TargetType,
            value.CurveType,
            value.AttitudeMode,
            value.LeadLength,
            value.ConfidenceFactor,
            value.MaximumLandingCurvature,
            value.Target,
            AlgorithmVersion = CalculationAlgorithmVersion
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
    }

    private static TargetLandingSample Evaluate(TargetPlanePoint point, TargetLandingCase value,
        PlaneFrame frame, SurveyStation sourceEnd, SurveyStation steeringStart,
        IReadOnlyList<SurveyStation> leadStations, IReadOnlyList<SurveyStation> sourceHistory)
    {
        (double north, double east, double tvd) = frame.ToNed(point.X, point.Y);
        TargetLandingSample result = new()
        {
            PlaneX = point.X,
            PlaneY = point.Y,
            PolarRadius = Math.Sqrt(point.X * point.X + point.Y * point.Y),
            PolarAngle = NormalizeAngle(Math.Atan2(point.X, point.Y)),
            North = north,
            East = east,
            TVD = tvd,
            State = TargetLandingSampleState.NoGeometricSolution
        };
        TargetAxisPath path = new()
        {
            Start = TrajectoryExtrapolationCalculator.ToPoint(steeringStart),
            CurveType = ToSectionCurveType(value.CurveType)
        };
        TargetAxis target = value.AttitudeMode == TargetLandingAttitudeMode.PerpendicularToTargetPlane
            ? new TargetAxis(north, east, tvd, value.Target.Plane.Inclination!.Value, value.Target.Plane.Azimuth!.Value)
            : new TargetAxis(north, east, tvd);
        path.Targets.Add(target);
        if (!path.Calculate() || path.Sections.Count == 0)
        {
            result.Message = path.FailureDescription;
            return result;
        }

        List<ArcSection> sections = path.Sections.Where(x => SectionLength(x) > PositionTolerance).ToList();
        if (sections.Count == 0 || sections.Any(x => SectionLength(x) <= 0.0))
        {
            result.Message = "The landing solution does not have positive forward section lengths.";
            return result;
        }
        result.TotalLandingLength = sections.Sum(SectionLength);
        result.PeakLandingCurvature = sections.Max(PeakCurvature);
        bool exceedsMaximumCurvature = value.MaximumLandingCurvature is double maximum &&
            result.PeakLandingCurvature > maximum + 1e-12;

        // Uncertainty must be replayed from the actual source endpoint through the lead-in and
        // landing sections as one chain. This is essential for Wolff-de Wardt, whose propagated
        // A matrix cannot be reconstructed from the covariance at steeringStart alone.
        List<SurveyStation> uncertaintyStations = leadStations.Select(x => new SurveyStation(x)).ToList();
        List<SurveyStation> stations = [new SurveyStation(steeringStart)];
        List<TrajectoryExtrapolationSolvedSection> solved = [];
        for (int index = 0; index < sections.Count; index++)
        {
            ArcSection section = sections[index];
            double startMd = section.Start.Abscissa!.Value;
            double endMd = section.End.Abscissa!.Value;
            TrajectoryExtrapolationCalculator.AddSamples(stations, section, startMd, endMd, SamplingInterval);
            solved.Add(ToSolvedSection(section, index));
        }
        foreach (SurveyStation station in stations.Skip(1)) uncertaintyStations.Add(new SurveyStation(station));
        TrajectoryExtrapolationCalculator.ContinueSourceUncertainty(uncertaintyStations, sourceEnd, sourceHistory);

        int leadStationCount = uncertaintyStations.Count - stations.Count + 1;
        List<SurveyStation> landingStations = uncertaintyStations.Skip(leadStationCount - 1)
            .Select(x => new SurveyStation(x)).ToList();
        SurveyStation landing = landingStations[^1];
        result.SurveyStationList = landingStations;
        result.SolvedSectionList = solved;
        result.LandingStation = new SurveyStation(landing);

        if (TryProjectedEllipse(landing, frame, value.ConfidenceFactor, out SurveyStationEllipse ellipse))
        {
            result.LandingEllipseInTargetPlane = ellipse;
            result.IsUncertaintySafe = value.TargetType == TargetLandingTargetType.DrillerTarget ||
                EllipseInsidePolygon(point, ellipse, value.Target.Polygon);
            if (result.IsUncertaintySafe == false)
            {
                result.State = TargetLandingSampleState.OutsideUncertaintySafeTarget;
                result.Message = "The landing confidence ellipse is not fully contained by the geological target.";
                return result;
            }
        }
        else if (value.TargetType == TargetLandingTargetType.GeologicalTarget)
        {
            result.IsUncertaintySafe = false;
            result.State = TargetLandingSampleState.OutsideUncertaintySafeTarget;
            result.Message = "No usable landing covariance was available for geological-target containment.";
            return result;
        }
        else result.IsUncertaintySafe = true;

        if (exceedsMaximumCurvature)
        {
            result.State = TargetLandingSampleState.ExceedsMaximumLandingCurvature;
            result.Message = "The shortest forward solution exceeds Maximum Landing Curvature.";
            return result;
        }

        result.State = TargetLandingSampleState.Reachable;
        result.Message = null;
        return result;
    }

    private static bool TryApplyLead(List<SurveyStation> sourceStations, TrajectoryCalculationType calculationType,
        SurveyStation sourceEnd, double leadLength, out SurveyStation steeringStart, out List<SurveyStation> stations)
    {
        steeringStart = new SurveyStation(sourceEnd);
        stations = [new SurveyStation(sourceEnd)];
        if (leadLength <= PositionTolerance) return true;
        if (sourceStations.Count == 1)
        {
            double inclination = sourceEnd.Inclination!.Value;
            double azimuth = sourceEnd.Azimuth!.Value;
            steeringStart.MD += leadLength;
            steeringStart.RiemannianNorth += leadLength * Math.Sin(inclination) * Math.Cos(azimuth);
            steeringStart.RiemannianEast += leadLength * Math.Sin(inclination) * Math.Sin(azimuth);
            steeringStart.TVD += leadLength * Math.Cos(inclination);
            steeringStart.Curvature = steeringStart.BUR = steeringStart.TUR = 0.0;
            stations.Add(new SurveyStation(steeringStart));
            return true;
        }
        SurveyStation previous = sourceStations[^2];
        ArcSection? section = calculationType switch
        {
            TrajectoryCalculationType.ConstantBuildAndTurnMethod => TrajectoryExtrapolationCalculator.ExtendBuildAndTurn(previous, sourceEnd, leadLength),
            TrajectoryCalculationType.ConstantCurvatureAndToolfaceMethod => TrajectoryExtrapolationCalculator.ExtendCurvatureAndToolface(previous, sourceEnd, leadLength),
            _ => TrajectoryExtrapolationCalculator.ExtendCircularArc(previous, sourceEnd, leadLength)
        };
        if (section == null) return false;
        TrajectoryExtrapolationCalculator.AddSamples(stations, section, sourceEnd.MD!.Value, section.End.Abscissa!.Value, SamplingInterval);
        steeringStart = new SurveyStation(stations[^1]);
        return true;
    }

    private static TrajectoryExtrapolationSolvedSection ToSolvedSection(ArcSection section, int index)
    {
        TrajectoryExtrapolationSolvedSection result = new()
        {
            SectionID = Guid.NewGuid(),
            SectionIndex = index,
            Role = index == 0 ? TrajectoryExtrapolationSectionRole.UpstreamSteeringSection : TrajectoryExtrapolationSectionRole.DownstreamSteeringSection,
            CurveType = section switch
            {
                BuildAndTurnArcSection => ExtrapolationCurveType.ConstantBuildAndTurn,
                ConstantCurvatureAndToolfaceArcSection => ExtrapolationCurveType.ConstantCurvatureAndToolface,
                _ => ExtrapolationCurveType.CircularArc
            },
            StartMD = section.Start.Abscissa!.Value,
            EndMD = section.End.Abscissa!.Value,
            Length = SectionLength(section),
            Start = TrajectoryExtrapolationCalculator.FromPoint(section.Start),
            End = TrajectoryExtrapolationCalculator.FromPoint(section.End)
        };
        switch (section)
        {
            case CircularArcSection circular:
                result.CircularArcCurvature = circular.Circle.Curvature;
                result.CircularArcStartToolface = circular.Circle.ReferenceToolface;
                break;
            case BuildAndTurnArcSection bt:
                result.ConstantBuildRate = bt.BuildAndTurn.BUR;
                result.ConstantTurnRate = bt.BuildAndTurn.TR;
                break;
            case ConstantCurvatureAndToolfaceArcSection ctc:
                result.ConstantCurvature = ctc.CTCCurve.Curvature;
                result.ConstantToolface = ctc.CTCCurve.Toolface;
                break;
        }
        return result;
    }

    private static double PeakCurvature(ArcSection section)
    {
        return section switch
        {
            CircularArcSection circular => Math.Abs(circular.Circle.Curvature ?? double.PositiveInfinity),
            ConstantCurvatureAndToolfaceArcSection ctc => Math.Abs(ctc.CTCCurve.Curvature ?? double.PositiveInfinity),
            BuildAndTurnArcSection bt => PeakBuildTurnCurvature(bt),
            _ => double.PositiveInfinity
        };
    }

    private static double PeakBuildTurnCurvature(BuildAndTurnArcSection section)
    {
        if (section.BuildAndTurn.BUR is not double build || section.BuildAndTurn.TR is not double turn ||
            section.Start.Inclination is not double start || section.End.Inclination is not double end)
            return double.PositiveInfinity;
        double maximumSin = Math.Max(Math.Abs(Math.Sin(start)), Math.Abs(Math.Sin(end)));
        double lower = Math.Min(start, end);
        double upper = Math.Max(start, end);
        int firstPeak = (int)Math.Ceiling((lower - Math.PI / 2.0) / Math.PI);
        if (Math.PI / 2.0 + firstPeak * Math.PI <= upper + 1e-12) maximumSin = 1.0;
        return Math.Sqrt(build * build + turn * turn * maximumSin * maximumSin);
    }

    private static bool TryProjectedEllipse(SurveyStation station, PlaneFrame frame, double confidence,
        out SurveyStationEllipse ellipse)
    {
        ellipse = new SurveyStationEllipse();
        if (station.Covariance is not { } covariance) return false;
        double[,] c = new double[3, 3];
        for (int row = 0; row < 3; row++)
        for (int column = 0; column < 3; column++)
        {
            double? item = covariance[row, column];
            if (!item.HasValue || !Finite(item.Value)) return false;
            c[row, column] = item.Value;
        }
        double a = Quadratic(frame.XAxis, c, frame.XAxis);
        double b = Quadratic(frame.XAxis, c, frame.YAxis);
        double d = Quadratic(frame.YAxis, c, frame.YAxis);
        double trace = a + d;
        double root = Math.Sqrt(Math.Max(0.0, (a - d) * (a - d) + 4.0 * b * b));
        double majorVariance = Math.Max(0.0, 0.5 * (trace + root));
        double minorVariance = Math.Max(0.0, 0.5 * (trace - root));
        double angle = 0.5 * Math.Atan2(2.0 * b, a - d);
        double scale = Math.Sqrt(Statistics.GetChiSquare3D(confidence));
        double boreholeRadius = station.BoreholeRadius ?? 0.0;
        ellipse.SemiMajorAxis = scale * Math.Sqrt(majorVariance) + boreholeRadius;
        ellipse.SemiMinorAxis = scale * Math.Sqrt(minorVariance) + boreholeRadius;
        ellipse.OrientationAngle = NormalizeAngle(angle);
        return Finite(ellipse.SemiMajorAxis ?? double.NaN) && Finite(ellipse.SemiMinorAxis ?? double.NaN);
    }

    private static bool EllipseInsidePolygon(TargetPlanePoint center, SurveyStationEllipse ellipse,
        IReadOnlyList<TargetPlanePoint> polygon)
    {
        double major = ellipse.SemiMajorAxis ?? 0.0;
        double minor = ellipse.SemiMinorAxis ?? 0.0;
        double angle = ellipse.OrientationAngle ?? 0.0;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        for (int index = 0; index < EllipsePointCount; index++)
        {
            double parameter = 2.0 * Math.PI * index / EllipsePointCount;
            double ex = major * Math.Cos(parameter);
            double ey = minor * Math.Sin(parameter);
            TargetPlanePoint point = new()
            {
                X = center.X + ex * cos - ey * sin,
                Y = center.Y + ex * sin + ey * cos
            };
            if (!PointInConvexPolygon(point, polygon)) return false;
        }
        return true;
    }

    private static void ValidatePolygon(IReadOnlyList<TargetPlanePoint>? polygon, List<string> errors)
    {
        if (polygon is not { Count: >= 3 }) { errors.Add("The target polygon requires at least three vertices."); return; }
        if (polygon.Any(x => !Finite(x.X) || !Finite(x.Y))) { errors.Add("Target polygon coordinates must be finite."); return; }
        double sign = 0.0;
        for (int i = 0; i < polygon.Count; i++)
        {
            TargetPlanePoint a = polygon[i];
            TargetPlanePoint b = polygon[(i + 1) % polygon.Count];
            TargetPlanePoint c = polygon[(i + 2) % polygon.Count];
            double cross = Cross(a, b, c);
            if (Math.Abs(cross) <= 1e-10) continue;
            if (sign == 0.0) sign = Math.Sign(cross);
            else if (Math.Sign(cross) != sign) { errors.Add("The target polygon must be convex and consistently ordered."); return; }
        }
        if (sign == 0.0) errors.Add("The target polygon must have non-zero area.");
    }

    private static bool TryCreateFrame(TargetPlaneDefinition? target, out PlaneFrame frame, out string? error)
    {
        frame = default;
        error = null;
        CurvilinearPoint3D? plane = target?.Plane;
        double? north = plane?.RiemannianNorth ?? plane?.X;
        double? east = plane?.RiemannianEast ?? plane?.Y;
        double? tvd = plane?.TVD ?? plane?.Z;
        if (plane != null && (!north.HasValue || !east.HasValue) &&
            plane.Latitude is double suppliedLatitude && plane.Longitude is double suppliedLongitude)
        {
            Point3DGlobalCoordinates geographic = new() { Latitude = suppliedLatitude, Longitude = suppliedLongitude, TVD = tvd };
            north = geographic.RiemannianNorth;
            east = geographic.RiemannianEast;
        }
        if (plane == null || north is not double originNorth || east is not double originEast || tvd is not double originTvd ||
            plane.Inclination is not double normalInclination || plane.Azimuth is not double normalAzimuth ||
            !Finite(originNorth) || !Finite(originEast) || !Finite(originTvd) || !Finite(normalInclination) || !Finite(normalAzimuth))
        { error = "The target plane origin and oriented normal must be finite."; return false; }
        Point3DGlobalCoordinates canonical = new(originNorth, originEast, originTvd);
        if (plane.Latitude is double latitude && Math.Abs(latitude - canonical.Latitude!.Value) > 1e-9 ||
            plane.Longitude is double longitude && Math.Abs(NormalizeSigned(longitude - canonical.Longitude!.Value)) > 1e-9)
        { error = "The target plane's Riemannian and WGS84 geographic coordinates are inconsistent."; return false; }
        plane.RiemannianNorth = originNorth;
        plane.RiemannianEast = originEast;
        plane.TVD = originTvd;
        plane.X = originNorth;
        plane.Y = originEast;
        plane.Z = originTvd;
        plane.Latitude = canonical.Latitude;
        plane.Longitude = canonical.Longitude;
        Vec3 normal = new(Math.Sin(normalInclination) * Math.Cos(normalAzimuth),
            Math.Sin(normalInclination) * Math.Sin(normalAzimuth), Math.Cos(normalInclination));
        Vec3 up = new(0.0, 0.0, -1.0);
        Vec3 projectedUp = up - normal * Dot(up, normal);
        Vec3 xAxis;
        Vec3 yAxis;
        if (Norm(projectedUp) <= 1e-8)
        {
            xAxis = new Vec3(1.0, 0.0, 0.0);
            yAxis = new Vec3(0.0, 1.0, 0.0);
        }
        else
        {
            yAxis = Normalize(projectedUp);
            xAxis = Normalize(Cross(normal, yAxis));
        }
        frame = new PlaneFrame(new Vec3(originNorth, originEast, originTvd), normal, xAxis, yAxis);
        return true;
    }

    private static bool PointInConvexPolygon(TargetPlanePoint point, IReadOnlyList<TargetPlanePoint> polygon)
    {
        double sign = 0.0;
        for (int i = 0; i < polygon.Count; i++)
        {
            double cross = Cross(polygon[i], polygon[(i + 1) % polygon.Count], point);
            if (Math.Abs(cross) <= PositionTolerance * 1e-3) continue;
            if (sign == 0.0) sign = Math.Sign(cross);
            else if (Math.Sign(cross) != sign) return false;
        }
        return true;
    }

    private static List<List<TargetPlanePoint>> ExtractContours(
        IReadOnlyList<TargetLandingSample> samples,
        IReadOnlyList<TargetLandingMeshTriangle> triangles,
        Func<TargetLandingSample, bool> included,
        Func<TargetLandingSample, TargetLandingSample, Func<TargetLandingSample, bool>, TargetPlanePoint> findBoundary)
    {
        Dictionary<Guid, TargetLandingSample> byId = samples.ToDictionary(x => x.SampleID);
        Dictionary<string, (TargetLandingSample A, TargetLandingSample B, int Count)> edges = [];
        List<PlaneSegment> segments = [];

        foreach (TargetLandingMeshTriangle triangle in triangles)
        {
            if (!byId.TryGetValue(triangle.FirstSampleID, out TargetLandingSample? a) ||
                !byId.TryGetValue(triangle.SecondSampleID, out TargetLandingSample? b) ||
                !byId.TryGetValue(triangle.ThirdSampleID, out TargetLandingSample? c)) continue;
            TargetLandingSample[] vertices = [a, b, c];
            List<TargetPlanePoint> crossings = [];
            for (int index = 0; index < 3; index++)
            {
                TargetLandingSample first = vertices[index];
                TargetLandingSample second = vertices[(index + 1) % 3];
                string edgeKey = EdgeKey(first.SampleID, second.SampleID);
                edges[edgeKey] = edges.TryGetValue(edgeKey, out var edge)
                    ? (edge.A, edge.B, edge.Count + 1)
                    : (first, second, 1);
                if (included(first) != included(second)) crossings.Add(findBoundary(first, second, included));
            }
            if (crossings.Count == 2) segments.Add(new PlaneSegment(crossings[0], crossings[1]));
        }

        // Retain the part of the original target boundary that belongs to the selected zone.
        foreach ((TargetLandingSample a, TargetLandingSample b, int count) in edges.Values.Where(x => x.Count == 1))
        {
            bool includeA = included(a);
            bool includeB = included(b);
            if (includeA && includeB) segments.Add(new PlaneSegment(ToPlanePoint(a), ToPlanePoint(b)));
            else if (includeA != includeB)
            {
                TargetPlanePoint midpoint = findBoundary(a, b, included);
                segments.Add(includeA
                    ? new PlaneSegment(ToPlanePoint(a), midpoint)
                    : new PlaneSegment(midpoint, ToPlanePoint(b)));
            }
        }

        return StitchSegments(segments);
    }

    internal static TargetPlanePoint BisectBoundary(
        TargetLandingSample first,
        TargetLandingSample second,
        Func<TargetPlanePoint, TargetLandingSample> evaluate,
        Func<TargetLandingSample, bool> included,
        double positionTolerance)
    {
        TargetLandingSample sameAsFirst = first;
        TargetLandingSample sameAsSecond = second;
        bool firstIncluded = included(first);
        if (firstIncluded == included(second)) return Midpoint(first, second);

        double tolerance = Math.Max(positionTolerance, 1e-9);
        for (int iteration = 0; iteration < 64 && Distance(sameAsFirst, sameAsSecond) > tolerance; iteration++)
        {
            TargetLandingSample middle = evaluate(Midpoint(sameAsFirst, sameAsSecond));
            if (included(middle) == firstIncluded) sameAsFirst = middle;
            else sameAsSecond = middle;
        }
        return Midpoint(sameAsFirst, sameAsSecond);
    }

    private static List<List<TargetPlanePoint>> StitchSegments(List<PlaneSegment> segments) =>
        TraceBoundaryLoops(segments.Select(segment => (segment.A, segment.B)));

    /// <summary>
    /// Traces the exterior face of each connected segment graph. Adaptive target meshes can leave
    /// several contour segments meeting at one coordinate. A greedy chain then follows an arbitrary
    /// branch and creates self-intersecting chords. Walking planar graph faces keeps those internal
    /// branches out of the returned zone boundary.
    /// </summary>
    internal static List<List<TargetPlanePoint>> TraceBoundaryLoops(
        IEnumerable<(TargetPlanePoint A, TargetPlanePoint B)> segments)
    {
        Dictionary<string, TargetPlanePoint> points = [];
        Dictionary<string, HashSet<string>> neighbours = [];
        HashSet<string> uniqueEdges = [];

        foreach ((TargetPlanePoint a, TargetPlanePoint b) in segments)
        {
            string aKey = Key(a);
            string bKey = Key(b);
            if (aKey == bKey) continue;
            string edgeKey = string.CompareOrdinal(aKey, bKey) < 0 ? $"{aKey}>{bKey}" : $"{bKey}>{aKey}";
            if (!uniqueEdges.Add(edgeKey)) continue;

            points.TryAdd(aKey, Copy(a));
            points.TryAdd(bKey, Copy(b));
            if (!neighbours.TryGetValue(aKey, out HashSet<string>? aNeighbours))
                neighbours[aKey] = aNeighbours = [];
            if (!neighbours.TryGetValue(bKey, out HashSet<string>? bNeighbours))
                neighbours[bKey] = bNeighbours = [];
            aNeighbours.Add(bKey);
            bNeighbours.Add(aKey);
        }

        List<List<TargetPlanePoint>> contours = [];
        HashSet<string> unassigned = [.. neighbours.Keys];
        while (unassigned.Count > 0)
        {
            string seed = unassigned.Order(StringComparer.Ordinal).First();
            HashSet<string> component = [];
            Queue<string> pending = new();
            pending.Enqueue(seed);
            unassigned.Remove(seed);
            while (pending.TryDequeue(out string? current))
            {
                component.Add(current);
                foreach (string next in neighbours[current])
                {
                    if (unassigned.Remove(next)) pending.Enqueue(next);
                }
            }

            List<(List<TargetPlanePoint> Points, double Area)> faces = [];
            HashSet<(string From, string To)> visitedDirectedEdges = [];
            foreach (string from in component.Order(StringComparer.Ordinal))
            {
                foreach (string to in neighbours[from].Order(StringComparer.Ordinal))
                {
                    if (visitedDirectedEdges.Contains((from, to))) continue;
                    List<TargetPlanePoint>? face = TraceFace(from, to, component.Count + uniqueEdges.Count);
                    if (face == null || face.Count < 3) continue;
                    double area = PolygonArea(face);
                    if (Math.Abs(area) > PositionTolerance * PositionTolerance) faces.Add((face, area));
                }
            }

            // With the face on the left of every directed edge, the exterior face is clockwise.
            // Fall back to the largest simple face for degenerate orientation or legacy data.
            List<(List<TargetPlanePoint> Points, double Area)> clockwiseFaces = faces
                .Where(face => face.Area < 0.0)
                .OrderBy(face => face.Area)
                .ToList();
            (List<TargetPlanePoint> Points, double Area)? exterior = clockwiseFaces.Count > 0
                ? clockwiseFaces[0]
                : faces.Count > 0
                    ? faces.OrderByDescending(face => Math.Abs(face.Area)).First()
                    : null;
            if (exterior is { } selected)
            {
                if (selected.Area < 0.0) selected.Points.Reverse();
                contours.Add(selected.Points);
            }

            List<TargetPlanePoint>? TraceFace(string startFrom, string startTo, int maximumSteps)
            {
                (string From, string To) start = (startFrom, startTo);
                (string From, string To) edge = start;
                List<string> vertexKeys = [];
                for (int step = 0; step <= maximumSteps * 2; step++)
                {
                    if (!visitedDirectedEdges.Add(edge)) return null;
                    vertexKeys.Add(edge.From);

                    List<string> orderedNeighbours = neighbours[edge.To]
                        .OrderBy(key => Math.Atan2(points[key].Y - points[edge.To].Y, points[key].X - points[edge.To].X))
                        .ThenBy(key => key, StringComparer.Ordinal)
                        .ToList();
                    int incomingIndex = orderedNeighbours.IndexOf(edge.From);
                    if (incomingIndex < 0) return null;
                    string next = orderedNeighbours[(incomingIndex - 1 + orderedNeighbours.Count) % orderedNeighbours.Count];
                    edge = (edge.To, next);
                    if (edge == start)
                    {
                        if (vertexKeys.Distinct(StringComparer.Ordinal).Count() != vertexKeys.Count) return null;
                        return vertexKeys.Select(key => Copy(points[key])).ToList();
                    }
                }
                return null;
            }
        }

        return contours.OrderByDescending(contour => Math.Abs(PolygonArea(contour))).ToList();
    }

    private static List<TargetPlanePoint> LargestContour(IReadOnlyList<List<TargetPlanePoint>>? contours) =>
        contours is { Count: > 0 } ? contours.OrderByDescending(x => Math.Abs(PolygonArea(x))).First().Select(Copy).ToList() : [];

    private static double PolygonArea(IReadOnlyList<TargetPlanePoint> polygon)
    {
        double area = 0.0;
        for (int index = 0; index < polygon.Count; index++)
            area += polygon[index].X * polygon[(index + 1) % polygon.Count].Y - polygon[(index + 1) % polygon.Count].X * polygon[index].Y;
        return 0.5 * area;
    }

    private static string EdgeKey(Guid first, Guid second) => string.CompareOrdinal(first.ToString(), second.ToString()) <= 0
        ? $"{first:N}|{second:N}" : $"{second:N}|{first:N}";
    private static bool SamePoint(TargetPlanePoint first, TargetPlanePoint second) => Key(first) == Key(second);

    private static TargetPlanePoint PolygonCentroid(IReadOnlyList<TargetPlanePoint> polygon) => new()
    {
        X = polygon.Average(x => x.X),
        Y = polygon.Average(x => x.Y)
    };
    private static TargetPlanePoint Midpoint(TargetLandingSample a, TargetLandingSample b) => new()
    { X = 0.5 * (a.PlaneX + b.PlaneX), Y = 0.5 * (a.PlaneY + b.PlaneY) };
    private static TargetPlanePoint ToPlanePoint(TargetLandingSample value) => new() { X = value.PlaneX, Y = value.PlaneY };
    private static TargetPlanePoint Copy(TargetPlanePoint value) => new() { X = value.X, Y = value.Y };
    private static double Distance(TargetLandingSample a, TargetLandingSample b) =>
        Math.Sqrt((a.PlaneX - b.PlaneX) * (a.PlaneX - b.PlaneX) + (a.PlaneY - b.PlaneY) * (a.PlaneY - b.PlaneY));
    private static double CharacteristicLength(IReadOnlyList<TargetPlanePoint> polygon) =>
        Math.Sqrt(Math.Max(1e-12, polygon.Max(x => x.X) - polygon.Min(x => x.X)) * Math.Max(1e-12, polygon.Max(x => x.X) - polygon.Min(x => x.X)) +
                  Math.Max(1e-12, polygon.Max(x => x.Y) - polygon.Min(x => x.Y)) * Math.Max(1e-12, polygon.Max(x => x.Y) - polygon.Min(x => x.Y)));
    private static double SectionLength(ArcSection section) => section.End.Abscissa!.Value - section.Start.Abscissa!.Value;
    private static SectionCurveType ToSectionCurveType(ExtrapolationCurveType value) => value switch
    {
        ExtrapolationCurveType.ConstantBuildAndTurn => SectionCurveType.ConstantBuildAndTurn,
        ExtrapolationCurveType.ConstantCurvatureAndToolface => SectionCurveType.ConstantCurvatureAndToolface,
        _ => SectionCurveType.CircularArc
    };
    private static string Key(TargetPlanePoint point) => $"{Math.Round(point.X, 8):R}|{Math.Round(point.Y, 8):R}";
    private static double Cross(TargetPlanePoint a, TargetPlanePoint b, TargetPlanePoint c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    private static double NormalizeAngle(double value)
    {
        double result = value % (2.0 * Math.PI);
        return result < 0.0 ? result + 2.0 * Math.PI : result;
    }
    private static double NormalizeSigned(double value)
    {
        double result = value % (2.0 * Math.PI);
        if (result > Math.PI) result -= 2.0 * Math.PI;
        if (result < -Math.PI) result += 2.0 * Math.PI;
        return result;
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static bool Fail(TargetLandingCase value, string message)
    {
        value.CalculationState = CalculationState.Failed;
        value.CalculationProgress = 1.0;
        value.CalculationMessage = message;
        return false;
    }

    private readonly record struct Vec3(double N, double E, double D)
    {
        public static Vec3 operator +(Vec3 left, Vec3 right) => new(left.N + right.N, left.E + right.E, left.D + right.D);
        public static Vec3 operator -(Vec3 left, Vec3 right) => new(left.N - right.N, left.E - right.E, left.D - right.D);
        public static Vec3 operator *(Vec3 value, double scale) => new(value.N * scale, value.E * scale, value.D * scale);
    }
    private readonly record struct PlaneSegment(TargetPlanePoint A, TargetPlanePoint B);
    private readonly record struct PlaneFrame(Vec3 Origin, Vec3 Normal, Vec3 XAxis, Vec3 YAxis)
    {
        public (double North, double East, double TVD) ToNed(double x, double y)
        {
            Vec3 point = Origin + XAxis * x + YAxis * y;
            return (point.N, point.E, point.D);
        }
    }
    private static double Dot(Vec3 a, Vec3 b) => a.N * b.N + a.E * b.E + a.D * b.D;
    private static double Norm(Vec3 value) => Math.Sqrt(Dot(value, value));
    private static Vec3 Normalize(Vec3 value) => value * (1.0 / Norm(value));
    private static Vec3 Cross(Vec3 a, Vec3 b) => new(a.E * b.D - a.D * b.E, a.D * b.N - a.N * b.D, a.N * b.E - a.E * b.N);
    private static double Quadratic(Vec3 left, double[,] matrix, Vec3 right)
    {
        double[] l = [left.N, left.E, left.D];
        double[] r = [right.N, right.E, right.D];
        double result = 0.0;
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) result += l[i] * matrix[i, j] * r[j];
        return result;
    }
}
