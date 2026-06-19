using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryMinimumDistanceCalculation : TrajectoryMinimumDistanceCalculationLight
    {
        public double? MaximumChordArcDistance { get; set; }
        public bool AccountForBoreholeRadius { get; set; } = true;
        public int OctreeMaximumDepth { get; set; } = 8;
        public int OctreeMaximumSegmentCountPerLeaf { get; set; } = 32;
        public MinimumDistanceAdaptiveRefinementSettings? AdaptiveRefinementSettings { get; set; }
        public double? GlobalMinimumCenterToCenterDistance { get; set; }
        public double? GlobalMinimumClearanceDistance { get; set; }
        public double? GlobalMinimumReferenceMD { get; set; }
        public Guid? GlobalMinimumComparisonTrajectoryID { get; set; }
        public double? GlobalMinimumComparisonMD { get; set; }
        public double? GlobalMinimumToolface { get; set; }
        public bool GlobalMinimumIsGravity { get; set; }
        public List<MinimumDistanceReferenceInterval>? ReferenceIntervalList { get; set; }
        public List<TrajectoryMinimumDistanceResult>? ResultList { get; set; }
        public List<TrajectoryMinimumDistanceIntervalResult>? IntervalResultList { get; set; }

        public TrajectoryMinimumDistanceCalculation()
        {
        }
    }
}
