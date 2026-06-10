using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryMinimumDistanceCalculationLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        public Guid ReferenceTrajectoryID { get; set; }
        public List<Guid>? ComparisonTrajectoryIDList { get; set; }
        public CalculationState CalculationState { get; set; } = CalculationState.Completed;
        public double CalculationProgress { get; set; } = 1.0;
        public string? CalculationMessage { get; set; }
        public int ResultCount { get; set; }
        public int IntervalResultCount { get; set; }

        public TrajectoryMinimumDistanceCalculationLight()
        {
        }
    }
}
