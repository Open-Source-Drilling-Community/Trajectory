using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Statistics;
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
            if (ok)
            {
                ok = TryAppendDownholeTerminalHalfEllipsoid(
                    uncertaintyEnvelope.MeshedEllipseList,
                    surveyStations,
                    confidenceFactor,
                    scalingFactor,
                    meshSectorCount);
            }
            surveyPrepared = surveyPrepared || ok;
            meshedEllipseList = ok ? uncertaintyEnvelope.MeshedEllipseList : null;
            return meshedEllipseList is { Count: > 0 };
        }

        private static bool TryAppendDownholeTerminalHalfEllipsoid(
            List<UncertaintyEllipse>? meshedEllipseList,
            List<SurveyStation> surveyStations,
            double confidenceFactor,
            double scalingFactor,
            int meshSectorCount)
        {
            if (meshedEllipseList is not { Count: > 0 } ||
                surveyStations is not { Count: > 0 } ||
                meshedEllipseList[^1] is not { } terminalEllipse ||
                terminalEllipse.EllipseCenter is not { } terminalCenter ||
                terminalEllipse.EllipseRadii is not { } terminalRadii ||
                terminalRadii[0] is not double firstRadius ||
                terminalRadii[1] is not double secondRadius ||
                surveyStations[^1] is not { } terminalStation ||
                terminalCenter.Inclination is not double inclination ||
                terminalCenter.Azimuth is not double azimuth ||
                terminalCenter.X is not double x ||
                terminalCenter.Y is not double y ||
                terminalCenter.Z is not double z ||
                terminalCenter.MD is not double md)
            {
                return true;
            }

            if (!TryCalculateTangentSemiAxis(terminalStation, confidenceFactor, scalingFactor, out double tangentSemiAxis) ||
                tangentSemiAxis < 0.0)
            {
                tangentSemiAxis = 0.0;
            }

            int capSliceCount = Math.Max(2, SeparationFactorCalculations.MinNumberInterpolations);
            double sinInclination = Math.Sin(inclination);
            double tangentX = sinInclination * Math.Cos(azimuth);
            double tangentY = sinInclination * Math.Sin(azimuth);
            double tangentZ = Math.Cos(inclination);

            for (int i = 1; i <= capSliceCount; i++)
            {
                double normalizedOffset = (double)i / capSliceCount;
                double radiusScale = Math.Sqrt(Math.Max(0.0, 1.0 - normalizedOffset * normalizedOffset));
                double tangentOffset = tangentSemiAxis * normalizedOffset;
                UncertaintyEllipse capEllipse = new()
                {
                    EllipseCenter = new SurveyPoint
                    {
                        Inclination = inclination,
                        Azimuth = azimuth,
                        X = x + tangentOffset * tangentX,
                        Y = y + tangentOffset * tangentY,
                        Z = z + tangentOffset * tangentZ,
                        MD = md + tangentOffset
                    },
                    EllipseOrientationAngle = terminalEllipse.EllipseOrientationAngle,
                    EllipseRadii = new()
                    {
                        X = firstRadius * radiusScale,
                        Y = secondRadius * radiusScale
                    }
                };

                if (!capEllipse.DiscretizeEllipse(meshSectorCount))
                {
                    return false;
                }

                meshedEllipseList.Add(capEllipse);
            }

            return true;
        }

        private static bool TryCalculateTangentSemiAxis(
            SurveyStation station,
            double confidenceFactor,
            double scalingFactor,
            out double tangentSemiAxis)
        {
            tangentSemiAxis = 0.0;
            if (station.Inclination is not double inclination ||
                station.Azimuth is not double azimuth ||
                station.Covariance is not { } covariance ||
                covariance[0, 0] is not double c00 ||
                covariance[0, 1] is not double c01 ||
                covariance[0, 2] is not double c02 ||
                covariance[1, 1] is not double c11 ||
                covariance[1, 2] is not double c12 ||
                covariance[2, 2] is not double c22)
            {
                return false;
            }

            double sinInclination = Math.Sin(inclination);
            double tangentX = sinInclination * Math.Cos(azimuth);
            double tangentY = sinInclination * Math.Sin(azimuth);
            double tangentZ = Math.Cos(inclination);
            double tangentVariance =
                tangentX * tangentX * c00 +
                tangentY * tangentY * c11 +
                tangentZ * tangentZ * c22 +
                2.0 * tangentX * tangentY * c01 +
                2.0 * tangentX * tangentZ * c02 +
                2.0 * tangentY * tangentZ * c12;
            if (!double.IsFinite(tangentVariance))
            {
                return false;
            }

            tangentVariance = Math.Max(0.0, tangentVariance);
            double chiSquare = Statistics.GetChiSquare3D(confidenceFactor);
            double boreholeRadius = station.BoreholeRadius ?? 0.0;
            tangentSemiAxis = scalingFactor * Math.Sqrt(chiSquare * tangentVariance) + boreholeRadius;
            return double.IsFinite(tangentSemiAxis);
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
