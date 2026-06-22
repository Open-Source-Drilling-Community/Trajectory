namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryAggregationDistanceResult
    {
        public double? ReferenceMD { get; set; }
        public double? ReferenceTVD { get; set; }
        public double? ReferenceNorth { get; set; }
        public double? ReferenceEast { get; set; }
        public double? ClosestMD { get; set; }
        public double? ClosestTVD { get; set; }
        public double? ClosestNorth { get; set; }
        public double? ClosestEast { get; set; }
        public double? CenterToCenterDistance { get; set; }
        public int? ClosestSectionIndex { get; set; }
        public TrajectoryAggregationSectionType? ClosestSectionType { get; set; }
        public double? SectionParameter { get; set; }
    }
}
