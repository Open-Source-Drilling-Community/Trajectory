using OSDC.DotnetLibraries.Drilling.Surveying;
using Geometry = OSDC.DotnetLibraries.General.Math;
using NORCE.Drilling.Trajectory.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    internal static class CenterlineMinimumDistanceEngine
    {
        public static List<CenterlineMinimumDistanceResult> Calculate(
            List<SurveyStation> referenceStations,
            IEnumerable<CenterlineComparisonSource> comparisonSources,
            bool accountForBoreholeRadius,
            int octreeMaximumDepth,
            int octreeMaximumSegmentCountPerLeaf,
            MinimumDistanceAdaptiveRefinementSettings? adaptiveRefinementSettings = null,
            Action<int, int>? progressCallback = null)
        {
            List<ReferenceSegment> referenceSegments = BuildReferenceSegments(referenceStations);
            if (referenceSegments.Count == 0)
            {
                return [];
            }

            List<ComparisonSegmentGroup> comparisonSegmentGroups = [];
            foreach (CenterlineComparisonSource comparisonSource in comparisonSources)
            {
                List<ComparisonSegment> comparisonSegments = BuildComparisonSegments(comparisonSource.SourceID, comparisonSource.StationList);
                if (comparisonSegments.Count > 0)
                {
                    SegmentSpatialIndex spatialIndex = new(comparisonSegments, Math.Clamp(octreeMaximumDepth, 1, 12), Math.Max(4, octreeMaximumSegmentCountPerLeaf));
                    comparisonSegmentGroups.Add(new ComparisonSegmentGroup(comparisonSource.SourceID, comparisonSegments, spatialIndex));
                }
            }

            if (comparisonSegmentGroups.Count == 0)
            {
                return [];
            }

            List<CenterlineMinimumDistanceResult> results = [];
            int totalComparisons = referenceSegments.Count * comparisonSegmentGroups.Count;
            int completedComparisons = 0;
            foreach (ComparisonSegmentGroup comparisonGroup in comparisonSegmentGroups)
            {
                List<CenterlineMinimumDistanceResult> comparisonResults = [];
                foreach (ReferenceSegment referenceSegment in referenceSegments)
                {
                    CenterlineMinimumDistanceResult? result = CalculateClosestApproach(referenceSegment, comparisonGroup.Segments, comparisonGroup.SpatialIndex, accountForBoreholeRadius);
                    if (result != null)
                    {
                        comparisonResults.Add(result);
                    }

                    completedComparisons++;
                    progressCallback?.Invoke(completedComparisons, totalComparisons);
                }

                if (adaptiveRefinementSettings?.Enabled == true)
                {
                    comparisonResults = RefinePolarCurve(
                        referenceStations,
                        comparisonResults,
                        comparisonGroup,
                        accountForBoreholeRadius,
                        adaptiveRefinementSettings);
                }

                results.AddRange(comparisonResults.OrderBy(result => result.ReferenceMD));
            }

            return results;
        }

        public static List<SurveyStation> PrepareStationList(List<SurveyStation>? stationList, TrajectoryCalculationType calculationType, double? maximumChordArcDistance)
        {
            List<SurveyStation> source = stationList?.Where(IsUsableStation).OrderBy(GetMD).ToList() ?? [];
            if (source.Count < 2 || !IsDefinedPositive(maximumChordArcDistance))
            {
                return source;
            }

            List<SurveyPoint>? interpolated = SurveyPoint.Interpolate(source, null, null, calculationType, maximumChordArcDistance, null);
            if (interpolated is not { Count: > 0 })
            {
                return source;
            }

            return interpolated
                .Select(point =>
                {
                    SurveyStation station = new(point);
                    if ((point.MD ?? point.Abscissa) is { } md &&
                        SurveyStation.InterpolateAtAbscissa(source, md, out SurveyStation? interpolatedStation, calculationType) &&
                        interpolatedStation != null)
                    {
                        station = interpolatedStation;
                    }

                    station.VerticalSection ??= point.VerticalSection;
                    station.Annotation = point.Annotation;
                    return station;
                })
                .Where(IsUsableStation)
                .OrderBy(GetMD)
                .ToList();
        }

        private static List<ReferenceSegment> BuildReferenceSegments(List<SurveyStation> stations)
        {
            List<ReferenceSegment> segments = [];
            for (int i = 1; i < stations.Count; i++)
            {
                if (TryCreateSegment(stations[i - 1], stations[i], out SegmentData segment))
                {
                    segments.Add(new ReferenceSegment(segment));
                }
            }

            return segments;
        }

        private static List<ComparisonSegment> BuildComparisonSegments(Guid sourceId, List<SurveyStation> stations)
        {
            List<ComparisonSegment> segments = [];
            for (int i = 1; i < stations.Count; i++)
            {
                if (TryCreateSegment(stations[i - 1], stations[i], out SegmentData segment))
                {
                    segments.Add(new ComparisonSegment(sourceId, segment));
                }
            }

            return segments;
        }

        private static bool TryCreateSegment(SurveyStation start, SurveyStation end, out SegmentData segment)
        {
            segment = default;
            if (!TryGetPoint(start, out Geometry.Point3D startPoint) || !TryGetPoint(end, out Geometry.Point3D endPoint))
            {
                return false;
            }

            double startMd = GetMD(start);
            double endMd = GetMD(end);
            if (!IsDefined(startMd) || !IsDefined(endMd) || endMd <= startMd)
            {
                return false;
            }

            Geometry.Segment3D geometry = new(startPoint, endPoint);
            segment = new SegmentData(start, end, startMd, endMd, geometry, BoundingBox.FromPoints(startPoint, endPoint));
            return true;
        }

        private static CenterlineMinimumDistanceResult? CalculateClosestApproach(
            ReferenceSegment referenceSegment,
            List<ComparisonSegment> comparisonSegments,
            SegmentSpatialIndex spatialIndex,
            bool accountForBoreholeRadius)
        {
            List<int> candidates = spatialIndex.Query(referenceSegment.Data.Bounds, referenceSegment.Data.Bounds.DiagonalLength * 0.25);
            if (candidates.Count == 0)
            {
                candidates = Enumerable.Range(0, comparisonSegments.Count).ToList();
            }

            ClosestApproach? closest = null;
            foreach (int index in candidates)
            {
                UpdateClosest(referenceSegment, comparisonSegments[index], ref closest);
            }

            if (closest == null)
            {
                return null;
            }

            double verificationLimit = closest.CenterDistance ?? double.PositiveInfinity;
            for (int i = 0; i < comparisonSegments.Count; i++)
            {
                if (referenceSegment.Data.Bounds.DistanceTo(comparisonSegments[i].Data.Bounds) <= verificationLimit)
                {
                    UpdateClosest(referenceSegment, comparisonSegments[i], ref closest);
                    verificationLimit = closest?.CenterDistance ?? verificationLimit;
                }
            }

            if (closest == null)
            {
                return null;
            }

            SurveyStation referenceStation = InterpolateStation(referenceSegment.Data, closest.ReferenceParameter ?? 0.0);
            SurveyStation comparisonStation = InterpolateStation(closest.ComparisonSegment.Data, closest.ComparisonParameter ?? 0.0);
            double referenceRadius = accountForBoreholeRadius ? referenceStation.BoreholeRadius ?? 0.0 : 0.0;
            double comparisonRadius = accountForBoreholeRadius ? comparisonStation.BoreholeRadius ?? 0.0 : 0.0;

            return new CenterlineMinimumDistanceResult
            {
                SourceID = closest.ComparisonSegment.SourceId,
                ReferenceSegmentStartMD = referenceSegment.Data.StartMD,
                ReferenceSegmentEndMD = referenceSegment.Data.EndMD,
                ReferenceMD = referenceStation.MD ?? referenceStation.Abscissa,
                ReferenceTVD = referenceStation.TVD,
                ReferenceNorth = referenceStation.RiemannianNorth,
                ReferenceEast = referenceStation.RiemannianEast,
                ReferenceBoreholeDiameter = accountForBoreholeRadius ? 2.0 * referenceRadius : null,
                ComparisonMD = comparisonStation.MD ?? comparisonStation.Abscissa,
                ComparisonTVD = comparisonStation.TVD,
                ComparisonNorth = comparisonStation.RiemannianNorth,
                ComparisonEast = comparisonStation.RiemannianEast,
                ComparisonBoreholeDiameter = accountForBoreholeRadius ? 2.0 * comparisonRadius : null,
                CenterToCenterDistance = closest.CenterDistance,
                ClearanceDistance = closest.CenterDistance - referenceRadius - comparisonRadius,
                Toolface = closest.Toolface,
                IsGravity = closest.IsGravity
            };
        }

        private static List<CenterlineMinimumDistanceResult> RefinePolarCurve(
            List<SurveyStation> referenceStations,
            List<CenterlineMinimumDistanceResult> baseResults,
            ComparisonSegmentGroup comparisonGroup,
            bool accountForBoreholeRadius,
            MinimumDistanceAdaptiveRefinementSettings settings)
        {
            List<CenterlineMinimumDistanceResult> sortedResults = baseResults
                .Where(result => IsDefined(result.ReferenceMD))
                .OrderBy(result => result.ReferenceMD)
                .ToList();
            if (sortedResults.Count < 2)
            {
                return sortedResults;
            }

            RefinementOptions options = RefinementOptions.From(settings);
            List<CenterlineMinimumDistanceResult> refinedResults = [];
            int remainingExtraSamples = options.MaximumExtraSamplesPerComparison;
            for (int i = 0; i < sortedResults.Count - 1; i++)
            {
                CenterlineMinimumDistanceResult start = sortedResults[i];
                CenterlineMinimumDistanceResult end = sortedResults[i + 1];
                refinedResults.Add(start);
                if (remainingExtraSamples <= 0)
                {
                    continue;
                }

                List<CenterlineMinimumDistanceResult> inserted = [];
                RefineInterval(
                    referenceStations,
                    comparisonGroup,
                    accountForBoreholeRadius,
                    options,
                    start,
                    end,
                    1,
                    inserted,
                    ref remainingExtraSamples);
                refinedResults.AddRange(inserted.OrderBy(result => result.ReferenceMD));
            }

            refinedResults.Add(sortedResults[^1]);
            return refinedResults
                .Where(result => IsDefined(result.ReferenceMD))
                .OrderBy(result => result.ReferenceMD)
                .ThenBy(result => result.RefinementLevel)
                .ToList();
        }

        private static void RefineInterval(
            List<SurveyStation> referenceStations,
            ComparisonSegmentGroup comparisonGroup,
            bool accountForBoreholeRadius,
            RefinementOptions options,
            CenterlineMinimumDistanceResult start,
            CenterlineMinimumDistanceResult end,
            int refinementLevel,
            List<CenterlineMinimumDistanceResult> inserted,
            ref int remainingExtraSamples)
        {
            if (remainingExtraSamples <= 0 ||
                refinementLevel > options.MaximumDepth ||
                start.ReferenceMD is not double startMd ||
                end.ReferenceMD is not double endMd ||
                !IsDefined(startMd) ||
                !IsDefined(endMd) ||
                endMd <= startMd ||
                endMd - startMd <= options.MinimumMDStep)
            {
                return;
            }

            double midMd = 0.5 * (startMd + endMd);
            CenterlineMinimumDistanceResult? mid = CalculateClosestApproachAtReferenceMD(
                referenceStations,
                midMd,
                comparisonGroup,
                accountForBoreholeRadius,
                refinementLevel);
            if (mid == null)
            {
                return;
            }

            if (!NeedsRefinement(start, mid, end, options))
            {
                return;
            }

            inserted.Add(mid);
            remainingExtraSamples--;
            RefineInterval(referenceStations, comparisonGroup, accountForBoreholeRadius, options, start, mid, refinementLevel + 1, inserted, ref remainingExtraSamples);
            RefineInterval(referenceStations, comparisonGroup, accountForBoreholeRadius, options, mid, end, refinementLevel + 1, inserted, ref remainingExtraSamples);
        }

        private static CenterlineMinimumDistanceResult? CalculateClosestApproachAtReferenceMD(
            List<SurveyStation> referenceStations,
            double md,
            ComparisonSegmentGroup comparisonGroup,
            bool accountForBoreholeRadius,
            int refinementLevel)
        {
            if (!TryCreateTinyReferenceSegment(referenceStations, md, out ReferenceSegment referenceSegment))
            {
                return null;
            }

            CenterlineMinimumDistanceResult? result = CalculateClosestApproach(referenceSegment, comparisonGroup.Segments, comparisonGroup.SpatialIndex, accountForBoreholeRadius);
            if (result == null)
            {
                return null;
            }

            return new CenterlineMinimumDistanceResult
            {
                SourceID = result.SourceID,
                ReferenceSegmentStartMD = result.ReferenceSegmentStartMD,
                ReferenceSegmentEndMD = result.ReferenceSegmentEndMD,
                ReferenceMD = result.ReferenceMD,
                ReferenceTVD = result.ReferenceTVD,
                ReferenceNorth = result.ReferenceNorth,
                ReferenceEast = result.ReferenceEast,
                ReferenceBoreholeDiameter = result.ReferenceBoreholeDiameter,
                ComparisonMD = result.ComparisonMD,
                ComparisonTVD = result.ComparisonTVD,
                ComparisonNorth = result.ComparisonNorth,
                ComparisonEast = result.ComparisonEast,
                ComparisonBoreholeDiameter = result.ComparisonBoreholeDiameter,
                CenterToCenterDistance = result.CenterToCenterDistance,
                ClearanceDistance = result.ClearanceDistance,
                Toolface = result.Toolface,
                IsGravity = result.IsGravity,
                IsAdaptiveRefinementSample = true,
                RefinementLevel = refinementLevel
            };
        }

        private static bool TryCreateTinyReferenceSegment(List<SurveyStation> referenceStations, double md, out ReferenceSegment referenceSegment)
        {
            referenceSegment = default;
            if (referenceStations.Count < 2 || !IsDefined(md))
            {
                return false;
            }

            double firstMd = GetMD(referenceStations[0]);
            double lastMd = GetMD(referenceStations[^1]);
            if (md <= firstMd || md >= lastMd)
            {
                return false;
            }

            double halfStep = Math.Min(0.005, Math.Max((lastMd - firstMd) * 1e-9, 1e-6));
            double startMd = Math.Max(firstMd, md - halfStep);
            double endMd = Math.Min(lastMd, md + halfStep);
            if (endMd <= startMd)
            {
                return false;
            }

            SurveyStation? start = InterpolateStationAtMD(referenceStations, startMd);
            SurveyStation? end = InterpolateStationAtMD(referenceStations, endMd);
            if (start == null || end == null || !TryCreateSegment(start, end, out SegmentData segment))
            {
                return false;
            }

            referenceSegment = new ReferenceSegment(segment);
            return true;
        }

        private static SurveyStation? InterpolateStationAtMD(List<SurveyStation> referenceStations, double md)
        {
            if (!IsDefined(md) || referenceStations.Count == 0)
            {
                return null;
            }

            if (SurveyStation.InterpolateAtAbscissa(referenceStations, md, out SurveyStation? station, TrajectoryCalculationType.MinimumCurvatureMethod) &&
                station != null)
            {
                return station;
            }

            for (int i = 1; i < referenceStations.Count; i++)
            {
                double startMd = GetMD(referenceStations[i - 1]);
                double endMd = GetMD(referenceStations[i]);
                if (md >= startMd && md <= endMd && endMd > startMd)
                {
                    double ratio = (md - startMd) / (endMd - startMd);
                    return new SurveyStation
                    {
                        MD = md,
                        Abscissa = md,
                        TVD = Interpolate(referenceStations[i - 1].TVD, referenceStations[i].TVD, ratio),
                        RiemannianNorth = Interpolate(referenceStations[i - 1].RiemannianNorth, referenceStations[i].RiemannianNorth, ratio),
                        RiemannianEast = Interpolate(referenceStations[i - 1].RiemannianEast, referenceStations[i].RiemannianEast, ratio),
                        BoreholeRadius = Interpolate(referenceStations[i - 1].BoreholeRadius, referenceStations[i].BoreholeRadius, ratio)
                    };
                }
            }

            return null;
        }

        private static bool NeedsRefinement(
            CenterlineMinimumDistanceResult start,
            CenterlineMinimumDistanceResult mid,
            CenterlineMinimumDistanceResult end,
            RefinementOptions options)
        {
            if (!TryGetPolarCartesian(start, out PolarPoint startPoint) ||
                !TryGetPolarCartesian(mid, out PolarPoint midPoint) ||
                !TryGetPolarCartesian(end, out PolarPoint endPoint) ||
                start.ReferenceMD is not double startMd ||
                mid.ReferenceMD is not double midMd ||
                end.ReferenceMD is not double endMd ||
                endMd <= startMd)
            {
                return false;
            }

            double ratio = Math.Clamp((midMd - startMd) / (endMd - startMd), 0.0, 1.0);
            double chordX = startPoint.X + ratio * (endPoint.X - startPoint.X);
            double chordY = startPoint.Y + ratio * (endPoint.Y - startPoint.Y);
            double deviation = Math.Sqrt(Math.Pow(midPoint.X - chordX, 2.0) + Math.Pow(midPoint.Y - chordY, 2.0));
            if (deviation > options.PolarDeviationTolerance)
            {
                return true;
            }

            double expectedAngle = Math.Atan2(chordX, chordY);
            double angularDeviation = Math.Abs(NormalizeAngle(midPoint.Angle - expectedAngle));
            return angularDeviation > options.PolarAngularTolerance &&
                Math.Abs(midPoint.Radius - Math.Sqrt(chordX * chordX + chordY * chordY)) > 0.1 * options.PolarDeviationTolerance;
        }

        private static bool TryGetPolarCartesian(CenterlineMinimumDistanceResult result, out PolarPoint point)
        {
            point = default;
            double? radius = result.CenterToCenterDistance ?? result.ClearanceDistance;
            if (radius is not double definedRadius ||
                result.Toolface is not double angle ||
                !IsDefined(definedRadius) ||
                !IsDefined(angle))
            {
                return false;
            }

            point = new PolarPoint(
                definedRadius * Math.Sin(angle),
                definedRadius * Math.Cos(angle),
                definedRadius,
                angle);
            return true;
        }

        private static void UpdateClosest(ReferenceSegment referenceSegment, ComparisonSegment comparisonSegment, ref ClosestApproach? closest)
        {
            double? distance = referenceSegment.Data.Geometry.GetDistance(
                comparisonSegment.Data.Geometry,
                out double? referenceParameter,
                out double? comparisonParameter,
                out double? toolface,
                out bool isGravity);

            if (distance is not { } definedDistance || !IsDefined(definedDistance))
            {
                return;
            }

            if (closest == null || definedDistance < closest.CenterDistance)
            {
                closest = new ClosestApproach(comparisonSegment, definedDistance, Clamp01(referenceParameter), Clamp01(comparisonParameter), toolface, isGravity);
            }
        }

        private static SurveyStation InterpolateStation(SegmentData segment, double parameter)
        {
            double ratio = Clamp01(parameter);
            double md = segment.StartMD + ratio * (segment.EndMD - segment.StartMD);
            if (SurveyStation.InterpolateAtAbscissa([segment.Start, segment.End], md, out SurveyStation? station, TrajectoryCalculationType.MinimumCurvatureMethod) && station != null)
            {
                station.BoreholeRadius = Interpolate(segment.Start.BoreholeRadius, segment.End.BoreholeRadius, ratio);
                return station;
            }

            return new SurveyStation
            {
                MD = md,
                Abscissa = md,
                TVD = Interpolate(segment.Start.TVD, segment.End.TVD, ratio),
                RiemannianNorth = Interpolate(segment.Start.RiemannianNorth, segment.End.RiemannianNorth, ratio),
                RiemannianEast = Interpolate(segment.Start.RiemannianEast, segment.End.RiemannianEast, ratio),
                BoreholeRadius = Interpolate(segment.Start.BoreholeRadius, segment.End.BoreholeRadius, ratio)
            };
        }

        private static bool TryGetPoint(SurveyStation station, out Geometry.Point3D point)
        {
            point = new Geometry.Point3D();
            if (station.RiemannianEast is not { } east ||
                station.RiemannianNorth is not { } north ||
                station.TVD is not { } tvd ||
                !IsDefined(east) || !IsDefined(north) || !IsDefined(tvd))
            {
                return false;
            }

            point = new Geometry.Point3D(north, east, tvd);
            return true;
        }

        private static bool IsUsableStation(SurveyStation station) =>
            IsDefined(GetMD(station)) &&
            IsDefined(station.RiemannianEast) &&
            IsDefined(station.RiemannianNorth) &&
            IsDefined(station.TVD);

        private static double GetMD(SurveyStation station) => station.MD ?? station.Abscissa ?? double.NaN;
        private static double? Interpolate(double? start, double? end, double ratio) =>
            start.HasValue && end.HasValue ? start.Value + ratio * (end.Value - start.Value) : start ?? end;
        private static bool IsDefinedPositive(double? value) => value is { } defined && IsDefined(defined) && defined > 0.0;
        private static bool IsDefined(double? value) => value is { } defined && IsDefined(defined);
        private static bool IsDefined(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double Clamp01(double? value) => Math.Clamp(value ?? 0.0, 0.0, 1.0);
        private static double NormalizeAngle(double angle)
        {
            while (angle > Math.PI)
            {
                angle -= 2.0 * Math.PI;
            }

            while (angle <= -Math.PI)
            {
                angle += 2.0 * Math.PI;
            }

            return angle;
        }

        private readonly record struct SegmentData(SurveyStation Start, SurveyStation End, double StartMD, double EndMD, Geometry.Segment3D Geometry, BoundingBox Bounds);
        private readonly record struct ReferenceSegment(SegmentData Data);
        private readonly record struct ComparisonSegment(Guid SourceId, SegmentData Data);
        private sealed record ComparisonSegmentGroup(Guid SourceId, List<ComparisonSegment> Segments, SegmentSpatialIndex SpatialIndex);
        private sealed record ClosestApproach(ComparisonSegment ComparisonSegment, double? CenterDistance, double? ReferenceParameter, double? ComparisonParameter, double? Toolface, bool IsGravity);
        private readonly record struct PolarPoint(double X, double Y, double Radius, double Angle);
        private readonly record struct RefinementOptions(double PolarDeviationTolerance, double PolarAngularTolerance, double MinimumMDStep, int MaximumDepth, int MaximumExtraSamplesPerComparison)
        {
            public static RefinementOptions From(MinimumDistanceAdaptiveRefinementSettings settings) => new(
                PositiveOrDefault(settings.PolarDeviationTolerance, 0.5),
                PositiveOrDefault(settings.PolarAngularTolerance, Math.PI / 12.0),
                PositiveOrDefault(settings.MinimumMDStep, 1.0),
                Math.Clamp(settings.MaximumDepth, 1, 12),
                Math.Clamp(settings.MaximumExtraSamplesPerComparison, 0, 100000));

            private static double PositiveOrDefault(double? value, double defaultValue) =>
                value is double defined && IsDefined(defined) && defined > 0.0 ? defined : defaultValue;
        }

        private sealed class SegmentSpatialIndex
        {
            private readonly Node root_;

            public SegmentSpatialIndex(List<ComparisonSegment> segments, int maxDepth, int maxSegmentsPerLeaf)
            {
                BoundingBox bounds = BoundingBox.FromBoxes(segments.Select(segment => segment.Data.Bounds));
                root_ = new Node(bounds.Expanded(bounds.DiagonalLength * 0.01 + 1.0), 0, maxDepth, maxSegmentsPerLeaf);
                for (int i = 0; i < segments.Count; i++)
                {
                    root_.Insert(i, segments[i].Data.Bounds);
                }
            }

            public List<int> Query(BoundingBox bounds, double initialExpansion)
            {
                double expansion = Math.Max(initialExpansion, 1.0);
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    HashSet<int> result = [];
                    root_.Query(bounds.Expanded(expansion), result);
                    if (result.Count > 0)
                    {
                        return result.ToList();
                    }

                    expansion *= 2.0;
                }

                return [];
            }

            private sealed class Node
            {
                private readonly BoundingBox bounds_;
                private readonly int depth_;
                private readonly int maxDepth_;
                private readonly int maxSegmentsPerLeaf_;
                private readonly List<(int Index, BoundingBox Bounds)> items_ = [];
                private Node[]? children_;

                public Node(BoundingBox bounds, int depth, int maxDepth, int maxSegmentsPerLeaf)
                {
                    bounds_ = bounds;
                    depth_ = depth;
                    maxDepth_ = maxDepth;
                    maxSegmentsPerLeaf_ = maxSegmentsPerLeaf;
                }

                public void Insert(int index, BoundingBox itemBounds)
                {
                    if (children_ != null && TryGetContainingChild(itemBounds, out Node? child))
                    {
                        child!.Insert(index, itemBounds);
                        return;
                    }

                    items_.Add((index, itemBounds));
                    if (children_ == null && depth_ < maxDepth_ && items_.Count > maxSegmentsPerLeaf_)
                    {
                        Split();
                    }
                }

                public void Query(BoundingBox searchBounds, HashSet<int> result)
                {
                    if (!bounds_.Intersects(searchBounds))
                    {
                        return;
                    }

                    foreach ((int index, BoundingBox itemBounds) in items_)
                    {
                        if (itemBounds.Intersects(searchBounds))
                        {
                            result.Add(index);
                        }
                    }

                    if (children_ == null)
                    {
                        return;
                    }

                    foreach (Node child in children_)
                    {
                        child.Query(searchBounds, result);
                    }
                }

                private void Split()
                {
                    children_ = bounds_.SplitOctants().Select(childBounds => new Node(childBounds, depth_ + 1, maxDepth_, maxSegmentsPerLeaf_)).ToArray();
                    List<(int Index, BoundingBox Bounds)> retained = [];
                    foreach ((int index, BoundingBox itemBounds) in items_)
                    {
                        if (TryGetContainingChild(itemBounds, out Node? child))
                        {
                            child!.Insert(index, itemBounds);
                        }
                        else
                        {
                            retained.Add((index, itemBounds));
                        }
                    }

                    items_.Clear();
                    items_.AddRange(retained);
                }

                private bool TryGetContainingChild(BoundingBox itemBounds, out Node? child)
                {
                    child = null;
                    if (children_ == null)
                    {
                        return false;
                    }

                    foreach (Node candidate in children_)
                    {
                        if (candidate.bounds_.Contains(itemBounds))
                        {
                            child = candidate;
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        private readonly record struct BoundingBox(double MinX, double MaxX, double MinY, double MaxY, double MinZ, double MaxZ)
        {
            public double DiagonalLength => Math.Sqrt(Math.Pow(MaxX - MinX, 2.0) + Math.Pow(MaxY - MinY, 2.0) + Math.Pow(MaxZ - MinZ, 2.0));

            public static BoundingBox FromPoints(Geometry.Point3D start, Geometry.Point3D end) => new(
                Math.Min(start.X!.Value, end.X!.Value),
                Math.Max(start.X!.Value, end.X!.Value),
                Math.Min(start.Y!.Value, end.Y!.Value),
                Math.Max(start.Y!.Value, end.Y!.Value),
                Math.Min(start.Z!.Value, end.Z!.Value),
                Math.Max(start.Z!.Value, end.Z!.Value));

            public static BoundingBox FromBoxes(IEnumerable<BoundingBox> boxes)
            {
                List<BoundingBox> list = boxes.ToList();
                return new BoundingBox(
                    list.Min(box => box.MinX),
                    list.Max(box => box.MaxX),
                    list.Min(box => box.MinY),
                    list.Max(box => box.MaxY),
                    list.Min(box => box.MinZ),
                    list.Max(box => box.MaxZ));
            }

            public BoundingBox Expanded(double value) => new(MinX - value, MaxX + value, MinY - value, MaxY + value, MinZ - value, MaxZ + value);

            public bool Intersects(BoundingBox other) =>
                MinX <= other.MaxX && MaxX >= other.MinX &&
                MinY <= other.MaxY && MaxY >= other.MinY &&
                MinZ <= other.MaxZ && MaxZ >= other.MinZ;

            public bool Contains(BoundingBox other) =>
                MinX <= other.MinX && MaxX >= other.MaxX &&
                MinY <= other.MinY && MaxY >= other.MaxY &&
                MinZ <= other.MinZ && MaxZ >= other.MaxZ;

            public double DistanceTo(BoundingBox other)
            {
                double dx = AxisDistance(MinX, MaxX, other.MinX, other.MaxX);
                double dy = AxisDistance(MinY, MaxY, other.MinY, other.MaxY);
                double dz = AxisDistance(MinZ, MaxZ, other.MinZ, other.MaxZ);
                return Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            public IEnumerable<BoundingBox> SplitOctants()
            {
                double midX = 0.5 * (MinX + MaxX);
                double midY = 0.5 * (MinY + MaxY);
                double midZ = 0.5 * (MinZ + MaxZ);
                for (int ix = 0; ix < 2; ix++)
                {
                    for (int iy = 0; iy < 2; iy++)
                    {
                        for (int iz = 0; iz < 2; iz++)
                        {
                            yield return new BoundingBox(
                                ix == 0 ? MinX : midX,
                                ix == 0 ? midX : MaxX,
                                iy == 0 ? MinY : midY,
                                iy == 0 ? midY : MaxY,
                                iz == 0 ? MinZ : midZ,
                                iz == 0 ? midZ : MaxZ);
                        }
                    }
                }
            }

            private static double AxisDistance(double aMin, double aMax, double bMin, double bMax)
            {
                if (aMax < bMin)
                {
                    return bMin - aMax;
                }

                if (bMax < aMin)
                {
                    return aMin - bMax;
                }

                return 0.0;
            }
        }
    }

    internal sealed record CenterlineComparisonSource(Guid SourceID, List<SurveyStation> StationList);

    internal sealed class CenterlineMinimumDistanceResult
    {
        public Guid SourceID { get; init; }
        public double? ReferenceSegmentStartMD { get; init; }
        public double? ReferenceSegmentEndMD { get; init; }
        public double? ReferenceMD { get; init; }
        public double? ReferenceTVD { get; init; }
        public double? ReferenceNorth { get; init; }
        public double? ReferenceEast { get; init; }
        public double? ReferenceBoreholeDiameter { get; init; }
        public double? ComparisonMD { get; init; }
        public double? ComparisonTVD { get; init; }
        public double? ComparisonNorth { get; init; }
        public double? ComparisonEast { get; init; }
        public double? ComparisonBoreholeDiameter { get; init; }
        public double? CenterToCenterDistance { get; init; }
        public double? ClearanceDistance { get; init; }
        public double? Toolface { get; init; }
        public bool IsGravity { get; init; }
        public bool IsAdaptiveRefinementSample { get; init; }
        public int RefinementLevel { get; init; }
    }
}
