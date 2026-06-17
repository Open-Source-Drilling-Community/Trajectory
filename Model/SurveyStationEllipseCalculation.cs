using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyStationEllipseCalculation
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        public double ConfidenceFactor { get; set; } = 0.95;
        public Guid? SurveyInstrumentID { get; set; }
        public List<SurveyStation>? SurveyStationList { get; set; }
        public List<SurveyStationEllipseResult>? SurveyStationEllipseResultList { get; set; }
        public List<SurveyPoint>? HighestTvdSurveyPointList { get; set; }
        public List<SurveyPoint>? LowestTvdSurveyPointList { get; set; }
        public string? CalculationMessage { get; private set; }

        public bool Calculate()
        {
            if (!Numeric.IsDefined(ConfidenceFactor) || !Numeric.GT(ConfidenceFactor, 0.0) || !Numeric.LT(ConfidenceFactor, 1.0) ||
                SurveyStationList is not { Count: > 0 } surveyStations)
            {
                CalculationMessage = "Confidence factor must be between 0 and 1 and at least one survey station is required.";
                SurveyStationEllipseResultList = null;
                HighestTvdSurveyPointList = null;
                LowestTvdSurveyPointList = null;
                return false;
            }

            if (!EnsureCovariance(surveyStations))
            {
                SurveyStationEllipseResultList = null;
                HighestTvdSurveyPointList = null;
                LowestTvdSurveyPointList = null;
                return false;
            }

            List<SurveyStation> orderedStations = surveyStations
                .OrderBy(station => station.MD ?? station.Abscissa ?? double.MaxValue)
                .ToList();

            SurveyStationEllipseResultList = orderedStations
                .Select(station =>
                {
                    SurveyStationEllipseResult result = new()
                    {
                        MD = station.MD ?? station.Abscissa,
                        HorizontalEllipse = CalculateEllipse(station, EllipseProjection.Horizontal),
                        VerticalEllipse = CalculateEllipse(station, EllipseProjection.Vertical),
                        PerpendicularEllipse = CalculateEllipse(station, EllipseProjection.Perpendicular)
                    };
                    return result;
                })
                .ToList();
            CalculateExtremeTvdPaths(orderedStations);

            bool hasResult = SurveyStationEllipseResultList.Any(result =>
                result.HorizontalEllipse != null ||
                result.VerticalEllipse != null ||
                result.PerpendicularEllipse != null) ||
                HighestTvdSurveyPointList is { Count: > 1 } ||
                LowestTvdSurveyPointList is { Count: > 1 };
            if (!hasResult)
            {
                CalculationMessage = "No uncertainty result could be calculated from the survey station covariance matrices.";
            }
            return hasResult;
        }

        private bool EnsureCovariance(List<SurveyStation> surveyStations)
        {
            if (surveyStations.Any(HasUsableCovariance))
            {
                CalculationMessage = null;
                return true;
            }

            SurveyInstrument? surveyTool = surveyStations
                .Select(station => station.SurveyTool)
                .FirstOrDefault(tool => tool != null);
            if (surveyTool == null)
            {
                CalculationMessage = "No usable covariance matrix or survey instrument was provided with the survey stations.";
                return false;
            }

            foreach (SurveyStation station in surveyStations)
            {
                station.SurveyTool ??= surveyTool;
            }

            try
            {
                bool success = surveyTool.ModelType switch
                {
                    SurveyInstrumentModelType.MWD_WolffDeWardt or SurveyInstrumentModelType.Gyro_WolffDeWardt =>
                        CovarianceCalculatorWolffDeWardt.Calculate(surveyStations),
                    SurveyInstrumentModelType.MWD_ISCWSA or SurveyInstrumentModelType.Gyro_ISCWSA =>
                        CovarianceCalculatorISCWSA.Calculate(surveyStations),
                    _ => false
                };

                CalculationMessage = success
                    ? null
                    : $"Covariance calculation failed for survey instrument model {surveyTool.ModelType}.";
                return success;
            }
            catch (Exception ex)
            {
                CalculationMessage = $"Covariance calculation failed for survey instrument model {surveyTool.ModelType}: {ex.Message}";
                return false;
            }
        }

        private static bool HasUsableCovariance(SurveyStation station)
        {
            if (station.Covariance == null)
            {
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (station.Covariance[i, i] is double value && Numeric.IsDefined(value))
                {
                    return true;
                }
            }
            return false;
        }

        private void CalculateExtremeTvdPaths(List<SurveyStation> orderedStations)
        {
            HighestTvdSurveyPointList = [];
            LowestTvdSurveyPointList = [];
            if (orderedStations.Count == 0)
            {
                return;
            }

            SurveyPoint? first = CreateSurveyPoint(orderedStations[0]);
            if (first == null)
            {
                return;
            }

            HighestTvdSurveyPointList.Add(new SurveyPoint(first));
            LowestTvdSurveyPointList.Add(new SurveyPoint(first));
            SurveyPoint previousHighest = new(first);
            SurveyPoint previousLowest = new(first);

            for (int i = 1; i < orderedStations.Count; i++)
            {
                SurveyStation station = orderedStations[i];
                UncertaintyEllipsoid ellipsoid = new()
                {
                    EllipsoidSurveyStation = station,
                    ConfidenceFactor = ConfidenceFactor,
                    ScalingFactor = 1.0,
                    CalculateHorizontalEllipse = false,
                    CalculateVerticalEllipse = false,
                    CalculatePerpendicularEllipse = false
                };

                if (!ellipsoid.CalculateExactExtremumsInDepth() ||
                    ellipsoid.PointAtHighestTVD == null ||
                    ellipsoid.PointAtLowestTVD == null)
                {
                    continue;
                }

                SurveyPoint? highest = CreateCompletedPoint(previousHighest, ellipsoid.PointAtHighestTVD);
                if (highest != null)
                {
                    HighestTvdSurveyPointList.Add(highest);
                    previousHighest = highest;
                }

                SurveyPoint? lowest = CreateCompletedPoint(previousLowest, ellipsoid.PointAtLowestTVD);
                if (lowest != null)
                {
                    LowestTvdSurveyPointList.Add(lowest);
                    previousLowest = lowest;
                }
            }
        }

        private static SurveyPoint? CreateSurveyPoint(SurveyStation station)
        {
            if ((station.X ?? station.RiemannianNorth) is not double x ||
                (station.Y ?? station.RiemannianEast) is not double y ||
                (station.Z ?? station.TVD) is not double z ||
                (station.Abscissa ?? station.MD) is not double abscissa ||
                station.Inclination is not double inclination ||
                station.Azimuth is not double azimuth)
            {
                return null;
            }

            return new SurveyPoint
            {
                X = x,
                Y = y,
                Z = z,
                Abscissa = abscissa,
                Inclination = inclination,
                Azimuth = azimuth,
                VerticalSection = station.VerticalSection ?? 0.0
            };
        }

        private static SurveyPoint? CreateCompletedPoint(SurveyPoint previous, Point3D point)
        {
            if (point.X is not double x ||
                point.Y is not double y ||
                point.Z is not double z)
            {
                return null;
            }

            SurveyPoint target = new()
            {
                X = x,
                Y = y,
                Z = z
            };

            return previous.CompleteFromXYZ(target) ? target : null;
        }

        private SurveyStationEllipse? CalculateEllipse(SurveyStation station, EllipseProjection projection)
        {
            if (station.Covariance == null)
            {
                return null;
            }

            UncertaintyEllipsoid ellipsoid = new()
            {
                EllipsoidSurveyStation = station,
                ConfidenceFactor = ConfidenceFactor,
                ScalingFactor = 1.0,
                CalculateHorizontalEllipse = projection == EllipseProjection.Horizontal,
                CalculateVerticalEllipse = projection == EllipseProjection.Vertical,
                CalculatePerpendicularEllipse = projection == EllipseProjection.Perpendicular
            };

            bool success = projection switch
            {
                EllipseProjection.Horizontal => ellipsoid.CalculateHorizontalEllipseParameters(),
                EllipseProjection.Vertical => ellipsoid.CalculateVerticalEllipseParameters(),
                EllipseProjection.Perpendicular => ellipsoid.CalculatePerpendicularEllipseParameters(),
                _ => false
            };

            if (!success)
            {
                return null;
            }

            UncertaintyEllipse? ellipse = projection switch
            {
                EllipseProjection.Horizontal => ellipsoid.HorizontalEllipse,
                EllipseProjection.Vertical => ellipsoid.VerticalEllipse,
                EllipseProjection.Perpendicular => ellipsoid.PerpendicularEllipse,
                _ => null
            };

            if (ellipse?.EllipseRadii == null)
            {
                return null;
            }

            return new SurveyStationEllipse
            {
                SemiMajorAxis = ellipse.EllipseRadii.X,
                SemiMinorAxis = ellipse.EllipseRadii.Y,
                OrientationAngle = ellipse.EllipseOrientationAngle
            };
        }

        private enum EllipseProjection
        {
            Horizontal,
            Vertical,
            Perpendicular
        }
    }
}
