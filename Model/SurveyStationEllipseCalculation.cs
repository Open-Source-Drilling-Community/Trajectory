using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.DataManagement;
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
        public string? CalculationMessage { get; private set; }

        public bool Calculate()
        {
            if (!Numeric.IsDefined(ConfidenceFactor) || !Numeric.GT(ConfidenceFactor, 0.0) || !Numeric.LT(ConfidenceFactor, 1.0) ||
                SurveyStationList is not { Count: > 0 } surveyStations)
            {
                CalculationMessage = "Confidence factor must be between 0 and 1 and at least one survey station is required.";
                SurveyStationEllipseResultList = null;
                return false;
            }

            if (!EnsureCovariance(surveyStations))
            {
                SurveyStationEllipseResultList = null;
                return false;
            }

            SurveyStationEllipseResultList = surveyStations
                .OrderBy(station => station.MD ?? station.Abscissa ?? double.MaxValue)
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

            bool hasResult = SurveyStationEllipseResultList.Any(result =>
                result.HorizontalEllipse != null ||
                result.VerticalEllipse != null ||
                result.PerpendicularEllipse != null);
            if (!hasResult)
            {
                CalculationMessage = "No ellipse could be calculated from the survey station covariance matrices.";
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
