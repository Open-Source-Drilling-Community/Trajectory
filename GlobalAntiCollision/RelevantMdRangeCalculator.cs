using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public static class RelevantMdRangeCalculator
    {
        private const double DefaultConfidenceFactor = 0.999;
        private const int MeshSectorCount = 36;

        public static bool TryGetRelevantMdRanges(
            List<SurveyStation>? referenceSurveyList,
            List<SurveyStation>? comparisonSurveyList,
            double confidenceFactor,
            out MeasuredDepthRange? referenceRange,
            out MeasuredDepthRange? comparisonRange)
        {
            referenceRange = null;
            comparisonRange = null;

            if (referenceSurveyList is not { Count: >= 2 } || comparisonSurveyList is not { Count: >= 2 })
            {
                return false;
            }

            if (!TryCreateEnvelope(referenceSurveyList, confidenceFactor, out List<UncertaintyEllipse>? referenceEllipses) ||
                !TryCreateEnvelope(comparisonSurveyList, confidenceFactor, out List<UncertaintyEllipse>? comparisonEllipses))
            {
                return false;
            }
            if (referenceEllipses == null || comparisonEllipses == null)
            {
                return false;
            }

            double? referenceMinMD = null;
            double? referenceMaxMD = null;
            double? comparisonMinMD = null;
            double? comparisonMaxMD = null;

            for (int referenceIndex = 0; referenceIndex < referenceEllipses.Count; referenceIndex++)
            {
                UncertaintyEllipse referenceEllipse = referenceEllipses[referenceIndex];
                if (referenceEllipse.BoundingBox == null || !TryGetMD(referenceEllipse, out double referenceMD))
                {
                    continue;
                }

                for (int comparisonIndex = 0; comparisonIndex < comparisonEllipses.Count; comparisonIndex++)
                {
                    UncertaintyEllipse comparisonEllipse = comparisonEllipses[comparisonIndex];
                    if (comparisonEllipse.BoundingBox == null)
                    {
                        continue;
                    }
                    if (!Intersects(referenceEllipse.BoundingBox, comparisonEllipse.BoundingBox))
                    {
                        continue;
                    }
                    if (!TryGetMD(comparisonEllipse, out double comparisonMD))
                    {
                        continue;
                    }

                    UpdateRange(referenceMD, ref referenceMinMD, ref referenceMaxMD);
                    UpdateRange(comparisonMD, ref comparisonMinMD, ref comparisonMaxMD);
                }

                for (int comparisonIndex = 0; comparisonIndex < comparisonEllipses.Count - 1; comparisonIndex++)
                {
                    Bounds? comparisonSegmentBounds = TryGetJoinedBounds(comparisonEllipses[comparisonIndex], comparisonEllipses[comparisonIndex + 1]);
                    if (comparisonSegmentBounds == null ||
                        !Intersects(referenceEllipse.BoundingBox, comparisonSegmentBounds) ||
                        !TryGetMD(comparisonEllipses[comparisonIndex], out double comparisonStartMD) ||
                        !TryGetMD(comparisonEllipses[comparisonIndex + 1], out double comparisonEndMD))
                    {
                        continue;
                    }

                    UpdateRange(referenceMD, ref referenceMinMD, ref referenceMaxMD);
                    UpdateRange(comparisonStartMD, ref comparisonMinMD, ref comparisonMaxMD);
                    UpdateRange(comparisonEndMD, ref comparisonMinMD, ref comparisonMaxMD);
                }
            }

            for (int referenceIndex = 0; referenceIndex < referenceEllipses.Count - 1; referenceIndex++)
            {
                Bounds? referenceSegmentBounds = TryGetJoinedBounds(referenceEllipses[referenceIndex], referenceEllipses[referenceIndex + 1]);
                if (referenceSegmentBounds == null ||
                    !TryGetMD(referenceEllipses[referenceIndex], out double referenceStartMD) ||
                    !TryGetMD(referenceEllipses[referenceIndex + 1], out double referenceEndMD))
                {
                    continue;
                }

                for (int comparisonIndex = 0; comparisonIndex < comparisonEllipses.Count; comparisonIndex++)
                {
                    UncertaintyEllipse comparisonEllipse = comparisonEllipses[comparisonIndex];
                    if (comparisonEllipse.BoundingBox == null ||
                        !Intersects(referenceSegmentBounds, comparisonEllipse.BoundingBox) ||
                        !TryGetMD(comparisonEllipse, out double comparisonMD))
                    {
                        continue;
                    }

                    UpdateRange(referenceStartMD, ref referenceMinMD, ref referenceMaxMD);
                    UpdateRange(referenceEndMD, ref referenceMinMD, ref referenceMaxMD);
                    UpdateRange(comparisonMD, ref comparisonMinMD, ref comparisonMaxMD);
                }

                for (int comparisonIndex = 0; comparisonIndex < comparisonEllipses.Count - 1; comparisonIndex++)
                {
                    Bounds? comparisonSegmentBounds = TryGetJoinedBounds(comparisonEllipses[comparisonIndex], comparisonEllipses[comparisonIndex + 1]);
                    if (comparisonSegmentBounds == null ||
                        !Intersects(referenceSegmentBounds, comparisonSegmentBounds) ||
                        !TryGetMD(comparisonEllipses[comparisonIndex], out double comparisonStartMD) ||
                        !TryGetMD(comparisonEllipses[comparisonIndex + 1], out double comparisonEndMD))
                    {
                        continue;
                    }

                    UpdateRange(referenceStartMD, ref referenceMinMD, ref referenceMaxMD);
                    UpdateRange(referenceEndMD, ref referenceMinMD, ref referenceMaxMD);
                    UpdateRange(comparisonStartMD, ref comparisonMinMD, ref comparisonMaxMD);
                    UpdateRange(comparisonEndMD, ref comparisonMinMD, ref comparisonMaxMD);
                }
            }

            if (!referenceMinMD.HasValue || !referenceMaxMD.HasValue || !comparisonMinMD.HasValue || !comparisonMaxMD.HasValue)
            {
                return false;
            }

            referenceRange = ClampRangeToSurvey(
                new MeasuredDepthRange(referenceMinMD.Value, referenceMaxMD.Value),
                referenceSurveyList);
            comparisonRange = ClampRangeToSurvey(
                new MeasuredDepthRange(comparisonMinMD.Value, comparisonMaxMD.Value),
                comparisonSurveyList);
            return referenceRange != null && comparisonRange != null;
        }

        public static MeasuredDepthRange? GetSurveyMdRange(List<SurveyStation>? surveyStations)
        {
            if (surveyStations is not { Count: > 0 } ||
                !TryGetMD(surveyStations[0], out double startMD) ||
                !TryGetMD(surveyStations[^1], out double endMD))
            {
                return null;
            }

            return new MeasuredDepthRange(Math.Min(startMD, endMD), Math.Max(startMD, endMD));
        }

        private static bool TryCreateEnvelope(List<SurveyStation> surveyStations, double confidenceFactor, out List<UncertaintyEllipse>? ellipses)
        {
            ellipses = null;
            double clampedConfidenceFactor =
                Numeric.IsUndefined(confidenceFactor) || confidenceFactor <= 0 || confidenceFactor > DefaultConfidenceFactor
                    ? DefaultConfidenceFactor
                    : confidenceFactor;

            double? minimumDeltaMD = MinimumMDBetweenSurveyStations(surveyStations);
            if (minimumDeltaMD.HasValue && minimumDeltaMD.Value > 0)
            {
                return PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseList(
                    surveyStations,
                    UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                    clampedConfidenceFactor,
                    SeparationFactorCalculations.MaxSeparationFactor,
                    MeshSectorCount,
                    null,
                    minimumDeltaMD.Value / SeparationFactorCalculations.MinNumberInterpolations,
                    out ellipses);
            }

            return PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseList(
                surveyStations,
                UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                clampedConfidenceFactor,
                SeparationFactorCalculations.MaxSeparationFactor,
                MeshSectorCount,
                SeparationFactorCalculations.MinNumberInterpolations,
                null,
                out ellipses);
        }

        private static MeasuredDepthRange? ClampRangeToSurvey(MeasuredDepthRange range, List<SurveyStation> surveyStations)
        {
            MeasuredDepthRange? surveyRange = GetSurveyMdRange(surveyStations);
            if (surveyRange == null)
            {
                return null;
            }

            double startMD = Math.Max(surveyRange.StartMD, Math.Min(range.StartMD, range.EndMD));
            double endMD = Math.Min(surveyRange.EndMD, Math.Max(range.StartMD, range.EndMD));
            if (startMD > endMD)
            {
                return surveyRange;
            }

            return new MeasuredDepthRange(startMD, endMD);
        }

        private static void UpdateRange(double md, ref double? minMD, ref double? maxMD)
        {
            if (!Numeric.IsDefined(md))
            {
                return;
            }

            if (!minMD.HasValue || md < minMD.Value)
            {
                minMD = md;
            }
            if (!maxMD.HasValue || md > maxMD.Value)
            {
                maxMD = md;
            }
        }

        private static bool Intersects(OSDC.DotnetLibraries.General.Math.BoundingBox3D left, OSDC.DotnetLibraries.General.Math.BoundingBox3D right)
        {
            return left.MinX <= right.MaxX &&
                left.MaxX >= right.MinX &&
                left.MinY <= right.MaxY &&
                left.MaxY >= right.MinY &&
                left.MinZ <= right.MaxZ &&
                left.MaxZ >= right.MinZ;
        }

        private static bool Intersects(OSDC.DotnetLibraries.General.Math.BoundingBox3D left, Bounds right)
        {
            return left.MinX <= right.MaxX &&
                left.MaxX >= right.MinX &&
                left.MinY <= right.MaxY &&
                left.MaxY >= right.MinY &&
                left.MinZ <= right.MaxZ &&
                left.MaxZ >= right.MinZ;
        }

        private static bool Intersects(Bounds left, OSDC.DotnetLibraries.General.Math.BoundingBox3D right)
        {
            return left.MinX <= right.MaxX &&
                left.MaxX >= right.MinX &&
                left.MinY <= right.MaxY &&
                left.MaxY >= right.MinY &&
                left.MinZ <= right.MaxZ &&
                left.MaxZ >= right.MinZ;
        }

        private static bool Intersects(Bounds left, Bounds right)
        {
            return left.MinX <= right.MaxX &&
                left.MaxX >= right.MinX &&
                left.MinY <= right.MaxY &&
                left.MaxY >= right.MinY &&
                left.MinZ <= right.MaxZ &&
                left.MaxZ >= right.MinZ;
        }

        private static Bounds? TryGetJoinedBounds(UncertaintyEllipse first, UncertaintyEllipse second)
        {
            if (first.BoundingBox == null || second.BoundingBox == null)
            {
                return null;
            }

            return Bounds.Join(first.BoundingBox, second.BoundingBox);
        }

        private static double? MinimumMDBetweenSurveyStations(List<SurveyStation>? listOfSurveyStations)
        {
            double? minDeltaMD = null;
            if (listOfSurveyStations != null && listOfSurveyStations.Count > 1)
            {
                for (int i = 0; i < listOfSurveyStations.Count - 1; i++)
                {
                    double? deltaMD = listOfSurveyStations[i + 1].MD - listOfSurveyStations[i].MD;
                    if (Numeric.IsDefined(deltaMD) && (minDeltaMD == null || Numeric.LT(deltaMD, minDeltaMD)))
                    {
                        minDeltaMD = deltaMD;
                    }
                }
            }
            return minDeltaMD;
        }

        private static bool TryGetMD(UncertaintyEllipse ellipse, out double md)
        {
            return TryGetCoordinate(ellipse.EllipseCenter, static point => point.MD, out md);
        }

        private static bool TryGetMD(SurveyStation surveyStation, out double md)
        {
            return TryGetCoordinate(surveyStation, static point => point.MD, out md);
        }

        private static bool TryGetCoordinate(SurveyPoint? surveyPoint, Func<SurveyPoint, double?> selector, out double value)
        {
            value = 0;
            if (surveyPoint == null)
            {
                return false;
            }

            double? candidate = selector(surveyPoint);
            if (!candidate.HasValue || !Numeric.IsDefined(candidate.Value))
            {
                return false;
            }

            value = candidate.Value;
            return true;
        }
    }
}
