using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryRealizationCaseLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        public Guid TrajectoryID { get; set; }
        public int RealizationCount { get; set; } = 100;
        public double CoarseningMaximumDistance { get; set; } = 0.1;
        public int? RandomSeed { get; set; }
        public int? ReferenceStationCount { get; set; }
        public int? CoarsenedStationCount { get; set; }
        public CalculationState CalculationState { get; set; } = CalculationState.Completed;
        public double CalculationProgress { get; set; } = 1.0;
        public string? CalculationMessage { get; set; }

        public TrajectoryRealizationCaseLight()
        {
        }
    }
}
