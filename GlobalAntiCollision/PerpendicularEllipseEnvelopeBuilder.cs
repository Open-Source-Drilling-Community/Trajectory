using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public static class PerpendicularEllipseEnvelopeBuilder
    {
        public static int DefaultMeshSectorCount { get; } = new UncertaintyEnvelope().MeshSectorCount ?? 32;

        public static bool TryBuildMeshedEllipseList(
            List<SurveyStation> surveyStations,
            UncertaintyEnvelope.ErrorModelType errorModelType,
            double confidenceFactor,
            double scalingFactor,
            int meshSectorCount,
            int? meshLongitudinalCount,
            double? meshLongitudinalLength,
            out List<UncertaintyEllipse>? meshedEllipseList)
        {
            bool surveyPrepared = false;
            return TryBuildMeshedEllipseList(
                surveyStations,
                errorModelType,
                ref surveyPrepared,
                confidenceFactor,
                scalingFactor,
                meshSectorCount,
                meshLongitudinalCount,
                meshLongitudinalLength,
                out meshedEllipseList);
        }

        public static bool TryBuildMeshedEllipseListWithAdaptiveSectorCount(
            List<SurveyStation> surveyStations,
            UncertaintyEnvelope.ErrorModelType errorModelType,
            double confidenceFactor,
            double scalingFactor,
            double targetPointSpacing,
            int minMeshSectorCount,
            int maxMeshSectorCount,
            int? meshLongitudinalCount,
            double? meshLongitudinalLength,
            out List<UncertaintyEllipse>? meshedEllipseList,
            out int meshSectorCount)
        {
            meshSectorCount = DefaultMeshSectorCount;
            int initialMeshSectorCount = Math.Clamp(DefaultMeshSectorCount, Math.Max(4, minMeshSectorCount), Math.Max(4, maxMeshSectorCount));
            if (!TryBuildMeshedEllipseList(
                surveyStations,
                errorModelType,
                confidenceFactor,
                scalingFactor,
                initialMeshSectorCount,
                meshLongitudinalCount,
                meshLongitudinalLength,
                out meshedEllipseList) ||
                meshedEllipseList == null ||
                !TryCalculateMeshSectorCountFromMaxSemiAxis(
                    meshedEllipseList,
                    targetPointSpacing,
                    minMeshSectorCount,
                    maxMeshSectorCount,
                    out meshSectorCount,
                    out _))
            {
                return false;
            }

            return RediscretizeEllipses(meshedEllipseList, meshSectorCount);
        }

        public static bool TryBuildMeshedEllipseList(
            List<SurveyStation> surveyStations,
            UncertaintyEnvelope.ErrorModelType errorModelType,
            ref bool surveyPrepared,
            double confidenceFactor,
            double scalingFactor,
            int meshSectorCount,
            int? meshLongitudinalCount,
            double? meshLongitudinalLength,
            out List<UncertaintyEllipse>? meshedEllipseList)
        {
            UncertaintyEnvelope uncertaintyEnvelope = new()
            {
                ErrorModel = errorModelType,
                SurveyStationList = surveyStations,
                ConfidenceFactor = confidenceFactor,
                ScalingFactor = scalingFactor,
                CalculateHorizontalEllipse = false,
                CalculateVerticalEllipse = false,
                CalculatePerpendicularEllipse = true,
                //SurveyStationsPrepared = surveyPrepared,
                MeshSectorCount = meshSectorCount,
                MeshLongitudinalCount = meshLongitudinalCount,
                MeshLongitudinalLength = meshLongitudinalLength ?? 3.0,
            };

            bool ok = uncertaintyEnvelope.Calculate();
            surveyPrepared = surveyPrepared || ok;
            meshedEllipseList = ok ? uncertaintyEnvelope.MeshedEllipseList : null;
            return meshedEllipseList is { Count: > 0 };
        }

        public static bool TryCalculateMeshSectorCountFromMaxSemiAxis(
            IReadOnlyList<UncertaintyEllipse> meshedEllipseList,
            double targetPointSpacing,
            int minMeshSectorCount,
            int maxMeshSectorCount,
            out int meshSectorCount,
            out double maxSemiAxis)
        {
            meshSectorCount = DefaultMeshSectorCount;
            maxSemiAxis = 0.0;
            if (meshedEllipseList == null ||
                !double.IsFinite(targetPointSpacing) ||
                targetPointSpacing <= 0.0 ||
                maxMeshSectorCount < minMeshSectorCount)
            {
                return false;
            }

            foreach (UncertaintyEllipse ellipse in meshedEllipseList)
            {
                if (ellipse.EllipseRadii == null)
                {
                    continue;
                }

                if (ellipse.EllipseRadii[0] is not double firstSemiAxis ||
                    ellipse.EllipseRadii[1] is not double secondSemiAxis)
                {
                    continue;
                }

                double semiAxis = Math.Max(firstSemiAxis, secondSemiAxis);
                if (double.IsFinite(semiAxis) && semiAxis > maxSemiAxis)
                {
                    maxSemiAxis = semiAxis;
                }
            }

            double circumferenceUpperBound = 2.0 * Math.PI * maxSemiAxis;
            int requestedSectorCount = circumferenceUpperBound > 0.0
                ? (int)Math.Ceiling(circumferenceUpperBound / targetPointSpacing)
                : minMeshSectorCount;
            meshSectorCount = Math.Clamp(requestedSectorCount, minMeshSectorCount, maxMeshSectorCount);
            return true;
        }

        private static bool RediscretizeEllipses(List<UncertaintyEllipse> meshedEllipseList, int meshSectorCount)
        {
            foreach (UncertaintyEllipse ellipse in meshedEllipseList)
            {
                ellipse.EllipseVertices = null;
                ellipse.BoundingBox = null;
                if (!ellipse.DiscretizeEllipse(meshSectorCount))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
