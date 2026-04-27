using OSDC.DotnetLibraries.Drilling.Surveying;
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
                MeshSectorCount = meshSectorCount,
                MeshLongitudinalCount = meshLongitudinalCount,
                MeshLongitudinalLength = meshLongitudinalLength ?? 0.0,
            };

            bool ok = uncertaintyEnvelope.Calculate();
            surveyPrepared = surveyPrepared || ok;
            meshedEllipseList = ok ? uncertaintyEnvelope.MeshedEllipseList : null;
            return meshedEllipseList is { Count: > 0 };
        }
    }
}
