using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using Geometry = OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    internal static class TrajectoryAggregationCalculator
    {
        private const int ClosestApproachIterations = 48;

        public static bool Calculate(
            Trajectory trajectory,
            TrajectoryAggregation aggregation,
            double epsilonL,
            double epsilonKappa,
            double alpha,
            double interpolationInterval,
            double distanceReferenceCoarseningThreshold,
            Action<double, string?>? progress = null)
        {
            aggregation.SectionList = null;
            aggregation.AggregatedSurveyPointList = null;
            aggregation.CoarsenedReferenceTrajectory = null;
            aggregation.DistanceResultList = null;

            List<SurveyPoint> sourcePoints = BuildSourcePoints(trajectory);
            aggregation.OriginalReferenceStationCount = trajectory.SurveyStationList?.Count ?? 0;
            if (sourcePoints.Count < 2)
            {
                aggregation.CalculationMessage = "The trajectory must contain at least two usable survey stations.";
                return false;
            }

            progress?.Invoke(0.05, "Fitting section candidates");
            SectionFitter fitter = new() { Alpha = alpha };
            foreach (SurveyPoint point in sourcePoints)
            {
                fitter.MDs.Add((point.MD ?? point.Abscissa)!.Value);
                fitter.Inclinations.Add(point.Inclination!.Value);
                fitter.Azimuths.Add(point.Azimuth!.Value);
            }

            fitter.Evaluate(epsilonL, epsilonKappa);
            List<FittedSection> fittedSections = fitter.BuildFittedSections(sourcePoints[0], sourcePoints[^1]);
            if (fittedSections.Count == 0)
            {
                aggregation.CalculationMessage = "No aggregation section could be fitted.";
                return false;
            }

            progress?.Invoke(0.25, "Building section chain");
            List<SectionRuntime> sectionChain = BuildSectionChain(sourcePoints[0], fittedSections, out string? sectionMessage);
            if (sectionChain.Count == 0)
            {
                aggregation.CalculationMessage = sectionMessage ?? "The fitted section chain could not be calculated.";
                return false;
            }

            aggregation.SectionList = sectionChain.Select(runtime => runtime.Model).ToList();
            aggregation.SectionCount = aggregation.SectionList.Count;

            progress?.Invoke(0.45, "Interpolating aggregation sections");
            aggregation.AggregatedSurveyPointList = InterpolateSectionChain(sectionChain, interpolationInterval);
            aggregation.AggregatedSurveyPointCount = aggregation.AggregatedSurveyPointList.Count;
            if (aggregation.AggregatedSurveyPointList.Count < 2)
            {
                aggregation.CalculationMessage = "The aggregation section chain produced too few survey points.";
                return false;
            }

            progress?.Invoke(0.6, "Coarsening reference trajectory");
            aggregation.CoarsenedReferenceTrajectory = CoarsenReferenceTrajectory(sourcePoints, distanceReferenceCoarseningThreshold);
            aggregation.CoarsenedReferencePointCount = aggregation.CoarsenedReferenceTrajectory.Count;

            progress?.Invoke(0.75, "Calculating closest approach distances");
            aggregation.DistanceResultList = CalculateDistances(aggregation.CoarsenedReferenceTrajectory, sectionChain);
            aggregation.DistanceResultCount = aggregation.DistanceResultList.Count;

            progress?.Invoke(1.0, null);
            return true;
        }

        private static List<SurveyPoint> BuildSourcePoints(Trajectory trajectory)
        {
            List<SurveyStation> stations = trajectory.SurveyStationList?
                .Where(station => station != null)
                .OrderBy(station => station.MD ?? station.Abscissa ?? double.MaxValue)
                .ToList() ?? [];

            List<SurveyPoint> points = [];
            foreach (SurveyStation station in stations)
            {
                double? md = station.MD ?? station.Abscissa;
                double? north = station.RiemannianNorth ?? station.X;
                double? east = station.RiemannianEast ?? station.Y;
                double? tvd = station.TVD ?? station.Z;
                if (!IsDefined(md) || !IsDefined(station.Inclination) || !IsDefined(station.Azimuth) ||
                    !IsDefined(north) || !IsDefined(east) || !IsDefined(tvd))
                {
                    continue;
                }

                points.Add(new SurveyPoint
                {
                    MD = md,
                    Abscissa = md,
                    Inclination = station.Inclination,
                    Azimuth = station.Azimuth,
                    TVD = tvd,
                    RiemannianNorth = north,
                    RiemannianEast = east,
                    X = north,
                    Y = east,
                    Z = tvd,
                    VerticalSection = station.VerticalSection
                });
            }

            return points;
        }

        private static List<SectionRuntime> BuildSectionChain(SurveyPoint firstPoint, List<FittedSection> fittedSections, out string? message)
        {
            message = null;
            List<SectionRuntime> result = [];
            Geometry.CurvilinearPoint3D current = ToCurvilinearPoint(firstPoint);
            for (int i = 0; i < fittedSections.Count; i++)
            {
                FittedSection fitted = fittedSections[i];
                if (fitted.EndMD <= fitted.StartMD)
                {
                    continue;
                }

                current.Abscissa = fitted.StartMD;
                ArcSection? arc = CreateArcSection(current, fitted);
                if (arc == null || !arc.Calculate())
                {
                    message = $"Section {i + 1} could not be calculated.";
                    return [];
                }

                TrajectoryAggregationSection model = ToSectionModel(i, fitted, current);
                result.Add(new SectionRuntime(i, fitted.Type, fitted.StartMD, fitted.EndMD, arc, model));
                Geometry.CurvilinearPoint3D? end = arc.InterpolateAtMD(fitted.EndMD);
                if (end == null)
                {
                    message = $"Section {i + 1} end point could not be interpolated.";
                    return [];
                }

                current = new Geometry.CurvilinearPoint3D(end);
            }

            return result;
        }

        private static ArcSection? CreateArcSection(Geometry.CurvilinearPoint3D start, FittedSection fitted)
        {
            Geometry.CurvilinearPoint3D end = new()
            {
                Abscissa = fitted.EndMD
            };

            switch (fitted.Type)
            {
                case TrajectoryAggregationSectionType.CircularArc:
                    return new CircularArcSection
                    {
                        Start = new Geometry.CurvilinearPoint3D(start),
                        End = end,
                        Circle =
                        {
                            Curvature = fitted.FirstParameter,
                            ReferenceToolface = fitted.SecondParameter
                        }
                    };
                case TrajectoryAggregationSectionType.ConstantBuildAndTurn:
                    return new BuildAndTurnArcSection
                    {
                        Start = new Geometry.CurvilinearPoint3D(start),
                        End = end,
                        BuildAndTurn =
                        {
                            BUR = fitted.FirstParameter,
                            TR = fitted.SecondParameter
                        }
                    };
                case TrajectoryAggregationSectionType.ConstantCurvatureAndToolface:
                    return new ConstantCurvatureAndToolfaceArcSection
                    {
                        Start = new Geometry.CurvilinearPoint3D(start),
                        End = end,
                        CTCCurve =
                        {
                            Curvature = fitted.FirstParameter,
                            Toolface = fitted.SecondParameter
                        }
                    };
                default:
                    return null;
            }
        }

        private static TrajectoryAggregationSection ToSectionModel(int index, FittedSection fitted, Geometry.CurvilinearPoint3D start)
        {
            TrajectoryAggregationSection section = new()
            {
                SectionIndex = index,
                SectionType = fitted.Type,
                StartMD = fitted.StartMD,
                EndMD = fitted.EndMD,
                StartInclination = start.Inclination,
                StartAzimuth = start.Azimuth,
                StartTVD = start.Z,
                StartNorth = start.X,
                StartEast = start.Y
            };

            switch (fitted.Type)
            {
                case TrajectoryAggregationSectionType.CircularArc:
                    section.CircularArcCurvature = fitted.FirstParameter;
                    section.CircularArcStartToolface = fitted.SecondParameter;
                    break;
                case TrajectoryAggregationSectionType.ConstantBuildAndTurn:
                    section.BuildRate = fitted.FirstParameter;
                    section.TurnRate = fitted.SecondParameter;
                    break;
                case TrajectoryAggregationSectionType.ConstantCurvatureAndToolface:
                    section.ConstantCurvature = fitted.FirstParameter;
                    section.ConstantToolface = fitted.SecondParameter;
                    break;
            }

            return section;
        }

        private static List<SurveyPoint> InterpolateSectionChain(List<SectionRuntime> sectionChain, double interpolationInterval)
        {
            List<SurveyPoint> points = [];
            for (int sectionIndex = 0; sectionIndex < sectionChain.Count; sectionIndex++)
            {
                SectionRuntime runtime = sectionChain[sectionIndex];
                double md = sectionIndex == 0 ? runtime.StartMD : runtime.StartMD + interpolationInterval;
                for (; md < runtime.EndMD; md += interpolationInterval)
                {
                    AddInterpolatedPoint(points, runtime, md);
                }

                AddInterpolatedPoint(points, runtime, runtime.EndMD);
            }

            return points
                .Where(point => IsDefined(point.MD) && IsDefined(point.RiemannianNorth) && IsDefined(point.RiemannianEast) && IsDefined(point.TVD))
                .OrderBy(point => point.MD)
                .ToList();
        }

        private static void AddInterpolatedPoint(List<SurveyPoint> points, SectionRuntime runtime, double md)
        {
            if (points.Count > 0 && Numeric.EQ(points[^1].MD, md, 1e-6))
            {
                return;
            }

            Geometry.CurvilinearPoint3D? point = runtime.Section.InterpolateAtMD(md);
            if (point == null)
            {
                return;
            }

            points.Add(ToSurveyPoint(point));
        }

        private static List<SurveyPoint> CoarsenReferenceTrajectory(List<SurveyPoint> sourcePoints, double threshold)
        {
            if (sourcePoints.Count <= 2 || threshold <= 0.0)
            {
                return sourcePoints.Select(CloneSurveyPoint).ToList();
            }

            List<SurveyPoint> result = [CloneSurveyPoint(sourcePoints[0])];
            int startIndex = 0;
            while (startIndex < sourcePoints.Count - 1)
            {
                int acceptedEndIndex = startIndex + 1;
                for (int candidateEndIndex = startIndex + 2; candidateEndIndex < sourcePoints.Count; candidateEndIndex++)
                {
                    if (CircularArcWithinTolerance(sourcePoints, startIndex, candidateEndIndex, threshold))
                    {
                        acceptedEndIndex = candidateEndIndex;
                    }
                    else
                    {
                        break;
                    }
                }

                result.Add(CloneSurveyPoint(sourcePoints[acceptedEndIndex]));
                startIndex = acceptedEndIndex;
            }

            return result;
        }

        private static bool CircularArcWithinTolerance(List<SurveyPoint> points, int startIndex, int endIndex, double threshold)
        {
            CircularArcSection? arc = CreateReferenceCircularArc(points[startIndex], points[endIndex]);
            if (arc == null || !arc.Calculate())
            {
                return false;
            }

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (!TryGetCoordinates(points[i], out double x, out double y, out double z))
                {
                    return false;
                }

                ClosestPoint closest = ClosestPointOnSection(arc, points[startIndex].MD!.Value, points[endIndex].MD!.Value, x, y, z);
                if (!IsDefined(closest.Distance) || closest.Distance > threshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static CircularArcSection? CreateReferenceCircularArc(SurveyPoint start, SurveyPoint end)
        {
            Geometry.CurvilinearPoint3D startPoint = ToCurvilinearPoint(start);
            Geometry.CurvilinearPoint3D endPoint = ToCurvilinearPoint(end);
            CircularArcSection section = new(startPoint, endPoint);
            return section;
        }

        private static List<TrajectoryAggregationDistanceResult> CalculateDistances(List<SurveyPoint> referencePoints, List<SectionRuntime> sectionChain)
        {
            List<TrajectoryAggregationDistanceResult> results = [];
            foreach (SurveyPoint referencePoint in referencePoints)
            {
                if (!TryGetCoordinates(referencePoint, out double x, out double y, out double z))
                {
                    continue;
                }

                ClosestRuntimePoint? closest = null;
                foreach (SectionRuntime runtime in sectionChain)
                {
                    ClosestPoint candidate = ClosestPointOnSection(runtime.Section, runtime.StartMD, runtime.EndMD, x, y, z);
                    if (!IsDefined(candidate.Distance))
                    {
                        continue;
                    }

                    if (closest == null || candidate.Distance < closest.Value.Point.Distance)
                    {
                        closest = new ClosestRuntimePoint(runtime, candidate);
                    }
                }

                if (closest is not { } best || best.Point.CurvilinearPoint == null)
                {
                    continue;
                }

                results.Add(new TrajectoryAggregationDistanceResult
                {
                    ReferenceMD = referencePoint.MD ?? referencePoint.Abscissa,
                    ReferenceTVD = referencePoint.TVD ?? referencePoint.Z,
                    ReferenceNorth = referencePoint.RiemannianNorth ?? referencePoint.X,
                    ReferenceEast = referencePoint.RiemannianEast ?? referencePoint.Y,
                    ClosestMD = best.Point.MD,
                    ClosestTVD = best.Point.CurvilinearPoint.Z,
                    ClosestNorth = best.Point.CurvilinearPoint.X,
                    ClosestEast = best.Point.CurvilinearPoint.Y,
                    CenterToCenterDistance = best.Point.Distance,
                    ClosestSectionIndex = best.Runtime.Index,
                    ClosestSectionType = best.Runtime.Type,
                    SectionParameter = (best.Point.MD - best.Runtime.StartMD) / (best.Runtime.EndMD - best.Runtime.StartMD)
                });
            }

            return results;
        }

        private static ClosestPoint ClosestPointOnSection(ArcSection section, double startMD, double endMD, double x, double y, double z)
        {
            double left = startMD;
            double right = endMD;
            double gr = (Math.Sqrt(5.0) - 1.0) / 2.0;
            double c = right - gr * (right - left);
            double d = left + gr * (right - left);
            for (int i = 0; i < ClosestApproachIterations; i++)
            {
                if (DistanceAt(section, c, x, y, z) > DistanceAt(section, d, x, y, z))
                {
                    left = c;
                    c = d;
                    d = left + gr * (right - left);
                }
                else
                {
                    right = d;
                    d = c;
                    c = right - gr * (right - left);
                }
            }

            double md = 0.5 * (left + right);
            Geometry.CurvilinearPoint3D? point = section.InterpolateAtMD(md);
            double distance = point == null ? double.NaN : Distance(point, x, y, z);
            return new ClosestPoint(md, distance, point);
        }

        private static double DistanceAt(ArcSection section, double md, double x, double y, double z)
        {
            Geometry.CurvilinearPoint3D? point = section.InterpolateAtMD(md);
            return point == null ? double.PositiveInfinity : Distance(point, x, y, z);
        }

        private static double Distance(Geometry.CurvilinearPoint3D point, double x, double y, double z)
        {
            if (point.X is not double px || point.Y is not double py || point.Z is not double pz)
            {
                return double.PositiveInfinity;
            }

            return Math.Sqrt(Square(px - x) + Square(py - y) + Square(pz - z));
        }

        private static Geometry.CurvilinearPoint3D ToCurvilinearPoint(SurveyPoint point)
        {
            return new Geometry.CurvilinearPoint3D
            {
                X = point.RiemannianNorth ?? point.X,
                Y = point.RiemannianEast ?? point.Y,
                Z = point.TVD ?? point.Z,
                Abscissa = point.MD ?? point.Abscissa,
                Inclination = point.Inclination,
                Azimuth = point.Azimuth
            };
        }

        private static SurveyPoint ToSurveyPoint(Geometry.CurvilinearPoint3D point)
        {
            return new SurveyPoint
            {
                MD = point.Abscissa,
                Abscissa = point.Abscissa,
                Inclination = point.Inclination,
                Azimuth = point.Azimuth,
                TVD = point.Z,
                RiemannianNorth = point.X,
                RiemannianEast = point.Y,
                X = point.X,
                Y = point.Y,
                Z = point.Z
            };
        }

        private static SurveyPoint CloneSurveyPoint(SurveyPoint point) => new()
        {
            MD = point.MD,
            Abscissa = point.Abscissa,
            Inclination = point.Inclination,
            Azimuth = point.Azimuth,
            TVD = point.TVD,
            RiemannianNorth = point.RiemannianNorth,
            RiemannianEast = point.RiemannianEast,
            X = point.X,
            Y = point.Y,
            Z = point.Z,
            VerticalSection = point.VerticalSection,
            Curvature = point.Curvature,
            Toolface = point.Toolface,
            BUR = point.BUR,
            TUR = point.TUR
        };

        private static bool TryGetCoordinates(SurveyPoint point, out double x, out double y, out double z)
        {
            x = point.RiemannianNorth ?? point.X ?? double.NaN;
            y = point.RiemannianEast ?? point.Y ?? double.NaN;
            z = point.TVD ?? point.Z ?? double.NaN;
            return IsDefined(x) && IsDefined(y) && IsDefined(z);
        }

        private static bool IsDefined(double? value) => value is double defined && IsDefined(defined);
        private static bool IsDefined(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double Square(double value) => value * value;

        private readonly record struct SectionRuntime(int Index, TrajectoryAggregationSectionType Type, double StartMD, double EndMD, ArcSection Section, TrajectoryAggregationSection Model);
        private readonly record struct FittedSection(TrajectoryAggregationSectionType Type, double StartMD, double EndMD, double FirstParameter, double SecondParameter);
        private readonly record struct ClosestPoint(double MD, double Distance, Geometry.CurvilinearPoint3D? CurvilinearPoint);
        private readonly record struct ClosestRuntimePoint(SectionRuntime Runtime, ClosestPoint Point);

        private sealed class SectionFitter
        {
            public List<double> MDs { get; } = [];
            public List<double> Inclinations { get; } = [];
            public List<double> Azimuths { get; } = [];
            public double Alpha { get; set; } = 0.9;

            private readonly List<double> dls_ = [];
            private readonly List<double> bur_ = [];
            private readonly List<double> tr_ = [];
            private readonly List<double?> xc_ = [];
            private readonly List<double?> yc_ = [];
            private readonly List<double?> zc_ = [];
            private readonly List<double> smoothDls_ = [];
            private readonly List<double> smoothBur_ = [];
            private readonly List<double> smoothTr_ = [];
            private readonly List<double> smoothXc_ = [];
            private readonly List<double> smoothYc_ = [];
            private readonly List<double> smoothZc_ = [];
            private readonly List<FittedSection> fittedSections_ = [];

            public void Evaluate(double epsilonL, double epsilonKappa)
            {
                fittedSections_.Clear();
                if (MDs.Count < 2 || Inclinations.Count != MDs.Count || Azimuths.Count != MDs.Count)
                {
                    return;
                }

                CalculateOsculatingCentresAndCurvatures();
                Smooth();
                List<(int Start, int End, double Mean)> dlsPeriods = FindConstantPeriods(smoothDls_, epsilonKappa);
                List<(int Start, int End, double Mean)> burPeriods = FindConstantPeriods(smoothBur_, epsilonKappa);
                List<(int Start, int End, double Mean)> trPeriods = FindConstantPeriods(smoothTr_, epsilonKappa);
                List<(int Start, int End, double Mean)> xcPeriods = FindConstantPeriods(smoothXc_, epsilonL);
                List<(int Start, int End, double Mean)> ycPeriods = FindConstantPeriods(smoothYc_, epsilonL);
                List<(int Start, int End, double Mean)> zcPeriods = FindConstantPeriods(smoothZc_, epsilonL);

                List<int> dlsEq = EstimateEquivalents(dlsPeriods, smoothDls_);
                List<int> burEq = EstimateEquivalents(burPeriods, smoothBur_);
                List<int> trEq = EstimateEquivalents(trPeriods, smoothTr_);
                List<int> xcEq = EstimateEquivalents(xcPeriods, smoothXc_);
                List<int> ycEq = EstimateEquivalents(ycPeriods, smoothYc_);
                List<int> zcEq = EstimateEquivalents(zcPeriods, smoothZc_);

                List<int> ca = [];
                List<int> cbt = [];
                List<int> ctc = [];
                int caIndex = 0;
                int cbtIndex = 0;
                int ctcIndex = 0;
                ca.Add(caIndex);
                cbt.Add(cbtIndex);
                ctc.Add(ctcIndex);
                for (int i = 1; i < dlsEq.Count; i++)
                {
                    if (dlsEq[i] != dlsEq[i - 1] || xcEq[i] != xcEq[i - 1] || ycEq[i] != ycEq[i - 1] || zcEq[i] != zcEq[i - 1])
                    {
                        caIndex++;
                    }
                    ca.Add(caIndex);

                    if (burEq[i] != burEq[i - 1] || trEq[i] != trEq[i - 1])
                    {
                        cbtIndex++;
                    }
                    cbt.Add(cbtIndex);

                    if (dlsEq[i] != dlsEq[i - 1] || burEq[i] != burEq[i - 1])
                    {
                        ctcIndex++;
                    }
                    ctc.Add(ctcIndex);
                }

                List<int>? shortestPath = ShortestPath(ca, cbt, ctc);
                if (shortestPath == null)
                {
                    return;
                }

                double?[] curvatures = FillArray(dls_.Count, dlsPeriods);
                double?[] buildups = FillArray(bur_.Count, burPeriods);
                double?[] turnrates = FillArray(tr_.Count, trPeriods);
                double?[] xcentres = FillArray(xc_.Count, xcPeriods);
                double?[] ycentres = FillArray(yc_.Count, ycPeriods);
                double?[] zcentres = FillArray(zc_.Count, zcPeriods);

                List<(TrajectoryAggregationSectionType Type, double MD, double First, double Second, double Third, double Fourth)> raw = [];
                for (int i = 1; i < shortestPath.Count - 1; i++)
                {
                    int codeEnd = shortestPath[i + 1];
                    int curveType = (codeEnd - 1) % 3;
                    AddRawSection(curveType, i - 1, raw, curvatures, buildups, turnrates, xcentres, ycentres, zcentres);
                }

                int lastCurveType = (shortestPath[^1] - 1) % 3;
                AddRawSection(lastCurveType, curvatures.Length - 1, raw, curvatures, buildups, turnrates, xcentres, ycentres, zcentres);
                BuildFittedSectionsFromRaw(raw);
            }

            public List<FittedSection> BuildFittedSections(SurveyPoint firstPoint, SurveyPoint lastPoint)
            {
                if (fittedSections_.Count > 0)
                {
                    return fittedSections_;
                }

                double startMd = (firstPoint.MD ?? firstPoint.Abscissa)!.Value;
                double endMd = (lastPoint.MD ?? lastPoint.Abscissa)!.Value;
                return [new FittedSection(TrajectoryAggregationSectionType.CircularArc, startMd, endMd, 0.0, 0.0)];
            }

            private void BuildFittedSectionsFromRaw(List<(TrajectoryAggregationSectionType Type, double MD, double First, double Second, double Third, double Fourth)> raw)
            {
                fittedSections_.Clear();
                if (raw.Count == 0)
                {
                    return;
                }

                List<(TrajectoryAggregationSectionType Type, double MD, double First, double Second, double Third, double Fourth)> compact = [raw[0]];
                for (int i = 1; i < raw.Count; i++)
                {
                    var previous = compact[^1];
                    var current = raw[i];
                    if (current.Type != previous.Type ||
                        !Numeric.EQ(current.First, previous.First) ||
                        !Numeric.EQ(current.Second, previous.Second) ||
                        !Numeric.EQ(current.Third, previous.Third) ||
                        !Numeric.EQ(current.Fourth, previous.Fourth))
                    {
                        compact.Add(current);
                    }
                }

                for (int i = 0; i < compact.Count; i++)
                {
                    double startMd = compact[i].MD;
                    double endMd = i + 1 < compact.Count ? compact[i + 1].MD : MDs[^1];
                    if (endMd <= startMd)
                    {
                        continue;
                    }

                    double first = compact[i].First;
                    double second = compact[i].Second;
                    if (compact[i].Type == TrajectoryAggregationSectionType.CircularArc)
                    {
                        second = EstimateCircularArcStartToolface(i, startMd, first, compact[i].Second, compact[i].Third, compact[i].Fourth);
                    }

                    fittedSections_.Add(new FittedSection(compact[i].Type, startMd, endMd, first, second));
                }
            }

            private double EstimateCircularArcStartToolface(int compactIndex, double startMd, double curvature, double xc, double yc, double zc)
            {
                if (Math.Abs(curvature) < 1e-12)
                {
                    return 0.0;
                }

                int sourceIndex = 0;
                for (int i = 0; i < MDs.Count; i++)
                {
                    if (MDs[i] <= startMd)
                    {
                        sourceIndex = i;
                    }
                }

                Geometry.CurvilinearPoint3D start = new(MDs[sourceIndex], Inclinations[sourceIndex], Azimuths[sourceIndex])
                {
                    X = sourceIndex < smoothXc_.Count ? 0.0 : null,
                    Y = sourceIndex < smoothYc_.Count ? 0.0 : null,
                    Z = MDs[sourceIndex]
                };
                Geometry.CurvilinearPoint3D next = new(MDs[Math.Min(sourceIndex + 1, MDs.Count - 1)], Inclinations[Math.Min(sourceIndex + 1, MDs.Count - 1)], Azimuths[Math.Min(sourceIndex + 1, MDs.Count - 1)]);
                return start.GetToolface(next) is double toolface ? NormalizeAnglePositive(toolface) : 0.0;
            }

            private void AddRawSection(
                int curveType,
                int idx,
                List<(TrajectoryAggregationSectionType Type, double MD, double First, double Second, double Third, double Fourth)> raw,
                double?[] curvatures,
                double?[] buildups,
                double?[] turnrates,
                double?[] xcentres,
                double?[] ycentres,
                double?[] zcentres)
            {
                int idxMD = Math.Clamp(idx, 0, MDs.Count - 1);
                idx = Math.Clamp(idx, 0, curvatures.Length - 1);
                if (curveType == 0 && curvatures[idx] is double curvature && xcentres[idx] is double xc && ycentres[idx] is double yc && zcentres[idx] is double zc)
                {
                    raw.Add((TrajectoryAggregationSectionType.CircularArc, MDs[idxMD], curvature, xc, yc, zc));
                }
                else if (curveType == 1 && buildups[idx] is double bur && turnrates[idx] is double tr)
                {
                    raw.Add((TrajectoryAggregationSectionType.ConstantBuildAndTurn, MDs[idxMD], bur, tr, 0.0, 0.0));
                }
                else if (curveType == 2 && curvatures[idx] is double ctcCurvature && buildups[idx] is double buildup)
                {
                    raw.Add((TrajectoryAggregationSectionType.ConstantCurvatureAndToolface, MDs[idxMD], ctcCurvature, EstimateToolface(idx, ctcCurvature, buildup), 0.0, 0.0));
                }
            }

            private double EstimateToolface(int idx, double curvature, double buildup)
            {
                if (Math.Abs(curvature) < 1e-12)
                {
                    return 0.0;
                }

                double ratio = Math.Clamp(buildup / curvature, -1.0, 1.0);
                double toolface = Math.Acos(ratio);
                if (idx > 0 && idx < Azimuths.Count && Math.Abs(Azimuths[idx] - Azimuths[idx - 1]) < 1e-12 && buildup < 0.0)
                {
                    toolface = Math.PI;
                }

                return NormalizeAnglePositive(toolface);
            }

            private void CalculateOsculatingCentresAndCurvatures()
            {
                dls_.Clear();
                bur_.Clear();
                tr_.Clear();
                xc_.Clear();
                yc_.Clear();
                zc_.Clear();

                double x1 = 0.0;
                double y1 = 0.0;
                double z1 = MDs[0];
                for (int i = 0; i < MDs.Count - 1; i++)
                {
                    double ds = MDs[i + 1] - MDs[i];
                    if (ds <= 0.0)
                    {
                        continue;
                    }

                    double incl1 = Inclinations[i];
                    double incl2 = Inclinations[i + 1];
                    double az1 = Azimuths[i];
                    double az2 = Azimuths[i + 1];
                    MinimumCurvature(incl1, az1, incl2, az2, ds, out double dx, out double dy, out double dz, out double dls);
                    dls_.Add(dls);
                    bur_.Add((incl2 - incl1) / ds);
                    tr_.Add(NormalizeAngleSigned(az2 - az1) / ds);

                    double x2 = x1 + dx;
                    double y2 = y1 + dy;
                    double z2 = z1 + dz;
                    double xm = x1 + 0.5 * dx;
                    double ym = y1 + 0.5 * dy;
                    double zm = z1 + 0.5 * dz;
                    double t1x = Math.Cos(az1) * Math.Sin(incl1);
                    double t1y = Math.Sin(az1) * Math.Sin(incl1);
                    double t1z = Math.Cos(incl1);
                    double t2x = Math.Cos(az2) * Math.Sin(incl2);
                    double t2y = Math.Sin(az2) * Math.Sin(incl2);
                    double t2z = Math.Cos(incl2);
                    double nx = t2x - t1x;
                    double ny = t2y - t1y;
                    double nz = t2z - t1z;
                    double norm = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (!Numeric.EQ(norm, 0.0) && !Numeric.EQ(dls, 0.0))
                    {
                        nx /= norm;
                        ny /= norm;
                        nz /= norm;
                        double radius = 1.0 / dls;
                        xc_.Add(xm + nx * radius);
                        yc_.Add(ym + ny * radius);
                        zc_.Add(zm + nz * radius);
                    }
                    else
                    {
                        xc_.Add(null);
                        yc_.Add(null);
                        zc_.Add(null);
                    }

                    x1 = x2;
                    y1 = y2;
                    z1 = z2;
                }
            }

            private void Smooth()
            {
                smoothDls_.Clear();
                smoothBur_.Clear();
                smoothTr_.Clear();
                smoothXc_.Clear();
                smoothYc_.Clear();
                smoothZc_.Clear();
                if (dls_.Count == 0)
                {
                    return;
                }

                smoothDls_.Add(dls_[0]);
                smoothBur_.Add(bur_[0]);
                smoothTr_.Add(tr_[0]);
                smoothXc_.Add(xc_[0] ?? 0.0);
                smoothYc_.Add(yc_[0] ?? 0.0);
                smoothZc_.Add(zc_[0] ?? 0.0);
                for (int i = 1; i < dls_.Count; i++)
                {
                    smoothDls_.Add(Alpha * dls_[i] + (1.0 - Alpha) * smoothDls_[^1]);
                    smoothBur_.Add(Alpha * bur_[i] + (1.0 - Alpha) * smoothBur_[^1]);
                    smoothTr_.Add(Alpha * tr_[i] + (1.0 - Alpha) * smoothTr_[^1]);
                    smoothXc_.Add(Alpha * (xc_[i] ?? 0.0) + (1.0 - Alpha) * smoothXc_[^1]);
                    smoothYc_.Add(Alpha * (yc_[i] ?? 0.0) + (1.0 - Alpha) * smoothYc_[^1]);
                    smoothZc_.Add(Alpha * (zc_[i] ?? 0.0) + (1.0 - Alpha) * smoothZc_[^1]);
                }
            }

            private static List<(int Start, int End, double Mean)> FindConstantPeriods(List<double> values, double maxStandardDeviation)
            {
                List<(int Start, int End, double Mean)> result = [];
                if (values.Count == 0 || maxStandardDeviation < 0.0)
                {
                    return result;
                }

                FindConstantPeriods(values, maxStandardDeviation * maxStandardDeviation, 0, values.Sum(), result);
                return result;
            }

            private static void FindConstantPeriods(List<double> values, double maxVariance, int startIndex, double sum, List<(int Start, int End, double Mean)> result)
            {
                if (startIndex >= values.Count - 1)
                {
                    return;
                }

                int lastIndex = -1;
                double mean = 0.0;
                double runningSum = sum;
                for (int i = values.Count - 1; i > startIndex; i--)
                {
                    if (IsConstantPeriod(values, maxVariance, startIndex, i, runningSum, out mean))
                    {
                        lastIndex = i;
                        break;
                    }
                    runningSum -= values[i];
                }

                if (lastIndex >= 0)
                {
                    result.Add((startIndex, lastIndex, mean));
                    double nextSum = 0.0;
                    for (int i = lastIndex; i < values.Count; i++)
                    {
                        nextSum += values[i];
                    }
                    FindConstantPeriods(values, maxVariance, lastIndex, nextSum, result);
                }
                else
                {
                    double nextSum = 0.0;
                    for (int i = startIndex + 1; i < values.Count; i++)
                    {
                        nextSum += values[i];
                    }
                    FindConstantPeriods(values, maxVariance, startIndex + 1, nextSum, result);
                }
            }

            private static bool IsConstantPeriod(List<double> values, double maxVariance, int startIndex, int lastIndex, double sum, out double mean)
            {
                mean = sum / (lastIndex - startIndex + 1);
                double variance = 0.0;
                for (int i = startIndex; i <= lastIndex; i++)
                {
                    variance += Square(values[i] - mean);
                }

                variance /= lastIndex - startIndex + 1;
                return variance < maxVariance;
            }

            private static List<int> EstimateEquivalents(List<(int Start, int End, double Mean)> periods, List<double> values)
            {
                List<int> result = [];
                int valueIndex = 0;
                int idx = 0;
                foreach ((int start, int end, _) in periods)
                {
                    while (idx < start)
                    {
                        result.Add(valueIndex++);
                        idx++;
                    }

                    for (int j = start; j <= end; j++)
                    {
                        result.Add(valueIndex);
                        idx++;
                    }
                    valueIndex++;
                }

                while (idx < values.Count)
                {
                    result.Add(valueIndex++);
                    idx++;
                }

                return result;
            }

            private static double?[] FillArray(int length, List<(int Start, int End, double Mean)> periods)
            {
                double?[] values = new double?[length];
                foreach ((int start, int end, double mean) in periods)
                {
                    for (int i = Math.Max(0, start); i <= Math.Min(length - 1, end); i++)
                    {
                        values[i] = mean;
                    }
                }

                return values;
            }

            private static List<int>? ShortestPath(List<int> ca, List<int> cbt, List<int> ctc)
            {
                Dictionary<int, List<(int Node, int Weight)>> graph = [];
                graph[0] = [(1, 1), (2, 1), (3, 1)];
                for (int i = 1; i < ctc.Count; i++)
                {
                    int ca0 = 3 * (i - 1) + 1;
                    int cbt0 = 3 * (i - 1) + 2;
                    int ctc0 = 3 * (i - 1) + 3;
                    int ca1 = 3 * i + 1;
                    int cbt1 = 3 * i + 2;
                    int ctc1 = 3 * i + 3;
                    graph[ca0] = [(ca1, ca[i - 1] != ca[i] ? 1 : 0), (cbt1, 1), (ctc1, 1)];
                    graph[cbt0] = [(ca1, 1), (cbt1, cbt[i - 1] != cbt[i] ? 1 : 0), (ctc1, 1)];
                    graph[ctc0] = [(ca1, 1), (cbt1, 1), (ctc1, ctc[i - 1] != ctc[i] ? 1 : 0)];
                }

                int lastCa = 3 * (ctc.Count - 1) + 1;
                int lastCbt = 3 * (ctc.Count - 1) + 2;
                int lastCtc = 3 * (ctc.Count - 1) + 3;
                graph.TryAdd(lastCa, []);
                graph.TryAdd(lastCbt, []);
                graph.TryAdd(lastCtc, []);

                Dictionary<int, int> distances = graph.Keys.ToDictionary(node => node, _ => int.MaxValue);
                Dictionary<int, int> previous = [];
                SortedSet<(int Distance, int Node)> queue = [];
                distances[0] = 0;
                queue.Add((0, 0));
                while (queue.Count > 0)
                {
                    (int distance, int node) = queue.Min;
                    queue.Remove(queue.Min);
                    foreach ((int neighbor, int weight) in graph[node])
                    {
                        int newDistance = distance + weight;
                        if (newDistance < distances[neighbor])
                        {
                            queue.Remove((distances[neighbor], neighbor));
                            distances[neighbor] = newDistance;
                            previous[neighbor] = node;
                            queue.Add((newDistance, neighbor));
                        }
                    }
                }

                int target = new[] { lastCa, lastCbt, lastCtc }.MinBy(node => distances[node]);
                if (distances[target] == int.MaxValue)
                {
                    return null;
                }

                List<int> path = [];
                int current = target;
                path.Insert(0, current);
                while (previous.TryGetValue(current, out int prev))
                {
                    current = prev;
                    path.Insert(0, current);
                }

                return path;
            }

            private static void MinimumCurvature(double incl1, double az1, double incl2, double az2, double ds, out double dx, out double dy, out double dz, out double dls)
            {
                double ci1 = Math.Cos(incl1);
                double si1 = Math.Sin(incl1);
                double ca1 = Math.Cos(az1);
                double sa1 = Math.Sin(az1);
                double ci2 = Math.Cos(incl2);
                double si2 = Math.Sin(incl2);
                double ca2 = Math.Cos(az2);
                double sa2 = Math.Sin(az2);
                double si12 = Math.Sin((incl2 - incl1) / 2.0);
                double sa12 = Math.Sin((az2 - az1) / 2.0);
                double dl = 2.0 * Math.Asin(Math.Sqrt(si12 * si12 + si1 * si2 * sa12 * sa12));
                dls = dl / ds;
                double rf;
                if (Numeric.EQ(dl, 0.0, 0.02))
                {
                    double dl2 = dl * dl;
                    rf = 1.0 + (dl2 / 12.0) * (1.0 + (dl2 / 10.0) * (1.0 + (dl2 / 168.0) * (1.0 + 31.0 * dl2 / 18.0)));
                }
                else
                {
                    rf = (2.0 / dl) * Math.Tan(dl / 2.0);
                }

                dx = 0.5 * ds * rf * (si1 * ca1 + si2 * ca2);
                dy = 0.5 * ds * rf * (si1 * sa1 + si2 * sa2);
                dz = 0.5 * ds * rf * (ci1 + ci2);
            }
        }

        private static double NormalizeAnglePositive(double angle)
        {
            double result = angle % (2.0 * Math.PI);
            return result < 0.0 ? result + 2.0 * Math.PI : result;
        }

        private static double NormalizeAngleSigned(double angle)
        {
            double result = NormalizeAnglePositive(angle);
            return result > Math.PI ? result - 2.0 * Math.PI : result;
        }
    }
}
