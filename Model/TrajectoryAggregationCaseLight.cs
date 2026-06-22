using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryAggregationCaseLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        public CalculationState CalculationState { get; set; } = CalculationState.Queued;
        public double CalculationProgress { get; set; }
        public string? CalculationMessage { get; set; }
        public double? EpsilonL { get; set; } = TrajectoryAggregationCase.DefaultEpsilonL;
        public double? EpsilonKappa { get; set; } = TrajectoryAggregationCase.DefaultEpsilonKappa;
        public double? Alpha { get; set; } = TrajectoryAggregationCase.DefaultAlpha;
        public double? InterpolationInterval { get; set; } = TrajectoryAggregationCase.DefaultInterpolationInterval;
        public double? DistanceReferenceCoarseningThreshold { get; set; } = TrajectoryAggregationCase.DefaultDistanceReferenceCoarseningThreshold;
        public List<TrajectoryAggregation>? TrajectoryAggregationList { get; set; }

        public TrajectoryAggregationCaseLight()
        {
        }
    }
}
