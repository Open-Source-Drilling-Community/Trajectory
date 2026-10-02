using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model
{
    public class SurveyStationEllipseCalculation
    {
        public const double MaximumConfidenceFactor = 0.999;

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

        public void SetCalculationMessage(string message) => CalculationMessage = message;

        public bool Calculate()
        {
            if (!Numeric.IsDefined(ConfidenceFactor) || !Numeric.GT(ConfidenceFactor, 0.0) || !Numeric.LE(ConfidenceFactor, MaximumConfidenceFactor) ||
                SurveyStationList is not { Count: > 0 } surveyStations)
            {
                CalculationMessage = $"Confidence factor must be greater than 0 and no greater than {MaximumConfidenceFactor.ToString(CultureInfo.InvariantCulture)} and at least one survey station is required.";
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
            double? verticalSectionAzimuth = ResolveVerticalSectionAzimuth(orderedStations);

            SurveyStationEllipseResultList = orderedStations
                .Select(station =>
                {
                    SurveyStationEllipseResult result = new()
                    {
                        MD = station.MD ?? station.Abscissa,
                        HorizontalEllipse = CalculateEllipse(station, EllipseProjection.Horizontal),
                        VerticalEllipse = CalculateEllipse(station, EllipseProjection.Vertical, verticalSectionAzimuth),
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
            if (surveyStations.All(HasUsableCovariance))
            {
                CalculationMessage = null;
                return true;
            }

            SurveyInstrument? lastDefinedTool = null;
            foreach (SurveyStation station in surveyStations)
            {
                lastDefinedTool = station.SurveyTool ?? lastDefinedTool;
                station.SurveyTool ??= lastDefinedTool;
            }
            SurveyInstrument? surveyTool = surveyStations
                .Select(station => station.SurveyTool)
                .FirstOrDefault(tool => tool != null);
            SurveyStation? baseline = surveyStations.FirstOrDefault(HasUsableCovariance);
            if (baseline != null && surveyTool?.ModelType is
                (SurveyInstrumentModelType.MWD_WolffDeWardt or SurveyInstrumentModelType.Gyro_WolffDeWardt))
            {
                CalculationMessage = "A partial Wolff-de Wardt result cannot be continued from covariance alone; provide the complete propagation history or use a resource-specific ellipse endpoint.";
                return false;
            }
            if ((lastDefinedTool?.ModelType is
                    SurveyInstrumentModelType.MWD_WolffDeWardt or SurveyInstrumentModelType.Gyro_WolffDeWardt) &&
                surveyStations.All(station => station.SurveyTool != null))
            {
                try
                {
                    if (CovarianceCalculatorWolffDeWardt.Calculate(surveyStations))
                    {
                        CalculationMessage = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    CalculationMessage = $"Wolff-de Wardt covariance continuation failed: {ex.Message}";
                    return false;
                }
            }

            if (baseline != null && surveyTool != null && ReferenceEquals(baseline, surveyStations[0]))
            {
                baseline.SurveyTool ??= surveyTool;
                TrajectoryExtrapolationCalculator.ContinueSourceUncertainty(surveyStations, baseline);
                if (surveyStations.All(HasUsableCovariance))
                {
                    CalculationMessage = null;
                    return true;
                }
            }

            if (baseline != null)
            {
                // Preserve the established behavior for a genuinely partial uncertainty result
                // when there is no defensible instrument with which to extend it.
                CalculationMessage = null;
                return true;
            }

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

        private static double? ResolveVerticalSectionAzimuth(IReadOnlyList<SurveyStation> stations)
        {
            SurveyStation? origin = stations.FirstOrDefault(HasHorizontalPosition);
            if (origin == null)
            {
                return stations.Select(station => station.Azimuth).FirstOrDefault(azimuth => azimuth.HasValue);
            }

            double originNorth = (origin.X ?? origin.RiemannianNorth)!.Value;
            double originEast = (origin.Y ?? origin.RiemannianEast)!.Value;
            for (int index = stations.Count - 1; index >= 0; index--)
            {
                SurveyStation end = stations[index];
                if (!HasHorizontalPosition(end))
                {
                    continue;
                }

                double deltaNorth = (end.X ?? end.RiemannianNorth)!.Value - originNorth;
                double deltaEast = (end.Y ?? end.RiemannianEast)!.Value - originEast;
                if (deltaNorth * deltaNorth + deltaEast * deltaEast <= 1e-18)
                {
                    continue;
                }

                double azimuth = System.Math.Atan2(deltaEast, deltaNorth);
                if (end.VerticalSection is double endVerticalSection &&
                    origin.VerticalSection is double originVerticalSection &&
                    endVerticalSection < originVerticalSection)
                {
                    azimuth += Numeric.PI;
                }
                return NormalizeAzimuth(azimuth);
            }

            return stations.Select(station => station.Azimuth).FirstOrDefault(azimuth => azimuth.HasValue);
        }

        private static bool HasHorizontalPosition(SurveyStation station) =>
            (station.X ?? station.RiemannianNorth).HasValue &&
            (station.Y ?? station.RiemannianEast).HasValue;

        private static double NormalizeAzimuth(double azimuth)
        {
            double normalized = azimuth % (2.0 * Numeric.PI);
            return normalized < 0.0 ? normalized + 2.0 * Numeric.PI : normalized;
        }

        private SurveyStationEllipse? CalculateEllipse(
            SurveyStation station,
            EllipseProjection projection,
            double? verticalSectionAzimuth = null)
        {
            if (station.Covariance == null)
            {
                return null;
            }

            // A vertical projection must use one stable curtain for the complete calculation.
            // The station azimuth is ill-conditioned close to vertical and can otherwise rotate
            // the projection plane sharply between adjacent stations while covariance stays smooth.
            SurveyStation projectionStation = projection == EllipseProjection.Vertical && verticalSectionAzimuth.HasValue
                ? new SurveyStation
                {
                    Covariance = station.Covariance,
                    BoreholeRadius = station.BoreholeRadius,
                    Azimuth = verticalSectionAzimuth,
                    Inclination = station.Inclination,
                    X = station.X,
                    Y = station.Y,
                    Z = station.Z,
                    RiemannianNorth = station.RiemannianNorth,
                    RiemannianEast = station.RiemannianEast,
                    TVD = station.TVD
                }
                : station;

            UncertaintyEllipsoid ellipsoid = new()
            {
                EllipsoidSurveyStation = projectionStation,
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
