using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Trajectory.Model
{
    /// <summary>
    /// Light-weight version of a survey run.
    /// </summary>
    public class SurveyRunLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        /// <summary>Earliest known UTC acquisition time for this survey run.</summary>
        public DateTimeOffset? AcquisitionStartUtc { get; set; }
        /// <summary>Latest known UTC acquisition time for this survey run.</summary>
        public DateTimeOffset? AcquisitionEndUtc { get; set; }
        public Guid? FieldID { get; set; }
        public Guid? ClusterID { get; set; }
        public Guid? WellID { get; set; }
        public Guid WellBoreID { get; set; }
        public Guid SurveyInstrumentID { get; set; }
        public SurveyRunType SurveyRunType { get; set; } = SurveyRunType.Actual;
        public TrajectoryCalculationType CalculationType { get; set; } = TrajectoryCalculationType.MinimumCurvatureMethod;
        public SurveyInclinationReference DefaultInclinationReference { get; set; } = SurveyInclinationReference.GeodeticVertical;
        public SurveyAzimuthReference DefaultAzimuthReference { get; set; } = SurveyAzimuthReference.TrueNorth;
        public SurveyGeomagneticModel GeomagneticModel { get; set; } = SurveyGeomagneticModel.Automatic;
        public Guid? ParentSurveyRunID { get; set; }
        public CalculationState CalculationState { get; set; } = CalculationState.Completed;
        public double CalculationProgress { get; set; } = 1.0;
        public string? CalculationMessage { get; set; }

        public SurveyRunLight() : base()
        {
        }
    }
}
