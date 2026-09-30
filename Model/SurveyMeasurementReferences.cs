using System;

namespace OSDC.Drilling.Trajectory.Model
{
    public enum SurveyInclinationReference { GeodeticVertical, GravityVertical, InheritRun }
    public enum SurveyAzimuthReference { TrueNorth, MagneticNorth, InheritRun }
    public enum SurveyCorrectionTimeMethod { NotRequired, StationMeasurementTime, RunAcquisitionMidpoint }
    public enum SurveyCorrectionSource { None, Computed, Supplied, ManualOverride }
    public enum SurveyCorrectionStatus { NotRequired, Pending, Completed, Failed }
    public enum SurveyGeomagneticModel { Automatic, WMM2025, IGRF14 }

    /// <summary>Frozen correction result and upstream model provenance for one survey measurement.</summary>
    public class SurveyMeasurementCorrection
    {
        public SurveyCorrectionSource Source { get; set; }
        public SurveyCorrectionStatus Status { get; set; } = SurveyCorrectionStatus.Pending;
        public string? Message { get; set; }
        public double? AppliedInclinationCorrection { get; set; }
        public double? AppliedAzimuthCorrection { get; set; }
        public double? MagneticDeclination { get; set; }
        public double? GravityNorth { get; set; }
        public double? GravityEast { get; set; }
        public double? GravityDown { get; set; }
        public double? EvaluatedLatitude { get; set; }
        public double? EvaluatedLongitude { get; set; }
        public double? EvaluatedDepthWgs84 { get; set; }
        public DateTimeOffset? EvaluationTimeUtc { get; set; }
        public SurveyCorrectionTimeMethod TimeMethod { get; set; }
        public string? GravityModelID { get; set; }
        public string? GravityModelVersion { get; set; }
        public string? GravityCoefficientSHA256 { get; set; }
        public string? GeomagneticModelID { get; set; }
        public string? GeomagneticMetadataSHA256 { get; set; }
        public string? GeomagneticCoefficientSHA256 { get; set; }
        public string AlgorithmVersion { get; set; } = "1";
    }
}
