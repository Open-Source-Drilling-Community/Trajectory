using OSDC.DotnetLibraries.General.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model
{
    public static class SurveyRunBitExtrapolationValidation
    {
        public static List<string> Validate(SurveyRun? surveyRun, IReadOnlyList<SurveyMeasurement>? measurements = null)
        {
            List<string> errors = [];
            if (surveyRun == null)
            {
                errors.Add("A survey run is required.");
                return errors;
            }

            IReadOnlyList<SurveyMeasurement> rows;
            if (measurements != null)
            {
                rows = measurements;
            }
            else if (surveyRun.SurveyMeasurementList != null)
            {
                rows = surveyRun.SurveyMeasurementList;
            }
            else
            {
                List<SurveyMeasurement> converted = (surveyRun.SurveyStationList ?? [])
                    .Select(SurveyMeasurement.FromSurveyStation)
                    .OrderBy(value => value.MD)
                    .ToList();
                if (surveyRun.BitExtrapolation?.Mode == SurveyRunBitExtrapolationMode.LastStationAlreadyExtrapolated && converted.Count > 0)
                    converted[^1].Origin = SurveyMeasurementOrigin.Extrapolated;
                rows = converted;
            }
            List<int> extrapolatedIndexes = rows
                .Select((measurement, index) => (measurement, index))
                .Where(value => value.measurement?.Origin == SurveyMeasurementOrigin.Extrapolated)
                .Select(value => value.index)
                .ToList();
            if (rows.Any(measurement => measurement == null || !Enum.IsDefined(measurement.Origin)))
                errors.Add("Every survey row must have a valid Measured or Extrapolated origin.");

            if (surveyRun.BitExtrapolation == null)
            {
                if (extrapolatedIndexes.Count > 0)
                    errors.Add("An extrapolated survey row requires BitExtrapolation configuration.");
                return errors;
            }

            double distance = surveyRun.BitExtrapolation.MeasurementToolToBitDistance;
            if (!Numeric.IsDefined(distance) || !Numeric.GT(distance, 0.0))
                errors.Add("MeasurementToolToBitDistance must be a finite positive SI-metre value.");

            if (surveyRun.BitExtrapolation.Mode == SurveyRunBitExtrapolationMode.CalculateFromLastMeasurement)
            {
                if (extrapolatedIndexes.Count > 0)
                    errors.Add("CalculateFromLastMeasurement does not accept a caller-supplied extrapolated row.");
                if (rows.Count == 0)
                    errors.Add("CalculateFromLastMeasurement requires at least one measured station.");
                return errors;
            }

            if (surveyRun.BitExtrapolation.Mode != SurveyRunBitExtrapolationMode.LastStationAlreadyExtrapolated)
            {
                errors.Add("The bit extrapolation mode is invalid.");
                return errors;
            }

            if (rows.Count < 2)
                errors.Add("LastStationAlreadyExtrapolated requires at least one measured row followed by the extrapolated row.");
            if (extrapolatedIndexes.Count != 1 || extrapolatedIndexes.SingleOrDefault(-1) != rows.Count - 1)
                errors.Add("Exactly the final survey row must be marked Extrapolated.");

            if (rows.Count >= 2 && rows[^2]?.MD is double measuredMd && rows[^1]?.MD is double extrapolatedMd &&
                Numeric.IsDefined(measuredMd) && Numeric.IsDefined(extrapolatedMd) && Numeric.IsDefined(distance))
            {
                double tolerance = Math.Max(1e-6, Math.Abs(distance) * 1e-8);
                if (Math.Abs((extrapolatedMd - measuredMd) - distance) > tolerance)
                    errors.Add("The final MD minus the last measured MD must equal MeasurementToolToBitDistance.");
            }

            return errors;
        }

        public static void ClearNonMeasurementMetadata(SurveyMeasurement measurement)
        {
            if (measurement.Origin != SurveyMeasurementOrigin.Extrapolated) return;
            measurement.ObservedInclination = null;
            measurement.ObservedAzimuth = null;
            measurement.MeasurementTimeUtc = null;
            measurement.InclinationReference = SurveyInclinationReference.InheritRun;
            measurement.AzimuthReference = SurveyAzimuthReference.InheritRun;
            measurement.Correction = null;
        }
    }
}
