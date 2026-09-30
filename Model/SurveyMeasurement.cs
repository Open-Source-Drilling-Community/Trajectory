using OSDC.DotnetLibraries.Drilling.Surveying;
using System;

namespace OSDC.Drilling.Trajectory.Model
{
    public class SurveyMeasurement
    {
        /// <summary>Stable identity used to address this measurement independently of its list position.</summary>
        public Guid MeasurementID { get; set; } = Guid.NewGuid();
        public double? MD { get; set; }
        /// <summary>Canonical inclination from the local WGS84 geodetic-down axis, in SI radians.</summary>
        public double? Inclination { get; set; }
        /// <summary>Canonical clockwise azimuth from geodetic true north, in SI radians.</summary>
        public double? Azimuth { get; set; }
        /// <summary>Original observed inclination before reference correction, in SI radians.</summary>
        public double? ObservedInclination { get; set; }
        /// <summary>Original observed azimuth before reference correction, in SI radians.</summary>
        public double? ObservedAzimuth { get; set; }
        /// <summary>UTC measurement instant when known for this station.</summary>
        public DateTimeOffset? MeasurementTimeUtc { get; set; }
        public SurveyInclinationReference InclinationReference { get; set; } = SurveyInclinationReference.InheritRun;
        public SurveyAzimuthReference AzimuthReference { get; set; } = SurveyAzimuthReference.InheritRun;
        public SurveyMeasurementCorrection? Correction { get; set; }
        public string? Annotation { get; set; }

        public SurveyStation ToSurveyStation()
        {
            return new SurveyStation
            {
                MD = MD,
                Abscissa = MD,
                Inclination = Inclination,
                Azimuth = Azimuth,
                Annotation = Annotation
            };
        }

        public static SurveyMeasurement FromSurveyStation(SurveyStation station)
        {
            return new SurveyMeasurement
            {
                MD = station.MD ?? station.Abscissa,
                Inclination = station.Inclination,
                Azimuth = station.Azimuth,
                Annotation = station.Annotation
            };
        }
    }
}
