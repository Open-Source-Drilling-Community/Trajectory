using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryAggregation
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public Guid TrajectoryID { get; set; }
        public CalculationState CalculationState { get; set; } = CalculationState.Queued;
        public double CalculationProgress { get; set; }
        public string? CalculationMessage { get; set; }
        public int OriginalReferenceStationCount { get; set; }
        public int CoarsenedReferencePointCount { get; set; }
        public int SectionCount { get; set; }
        public int AggregatedSurveyPointCount { get; set; }
        public int DistanceResultCount { get; set; }
        public List<TrajectoryAggregationSection>? SectionList { get; set; }
        public List<SurveyPoint>? AggregatedSurveyPointList { get; set; }
        public List<SurveyPoint>? CoarsenedReferenceTrajectory { get; set; }
        public List<TrajectoryAggregationDistanceResult>? DistanceResultList { get; set; }
    }
}
