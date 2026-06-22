namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryAggregationSection
    {
        public int SectionIndex { get; set; }
        public TrajectoryAggregationSectionType SectionType { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }
        public double? StartInclination { get; set; }
        public double? StartAzimuth { get; set; }
        public double? StartTVD { get; set; }
        public double? StartNorth { get; set; }
        public double? StartEast { get; set; }
        public double? CircularArcCurvature { get; set; }
        public double? CircularArcStartToolface { get; set; }
        public double? ConstantCurvature { get; set; }
        public double? ConstantToolface { get; set; }
        public double? BuildRate { get; set; }
        public double? TurnRate { get; set; }
    }
}
