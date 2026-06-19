using System;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyRunMinimumDistanceResult
    {
        public double? ReferenceMD { get; set; }
        public double? ReferenceTVD { get; set; }
        public double? ReferenceNorth { get; set; }
        public double? ReferenceEast { get; set; }
        public double? ReferenceBoreholeDiameter { get; set; }
        public Guid? ComparisonSurveyRunID { get; set; }
        public double? ComparisonMD { get; set; }
        public double? ComparisonTVD { get; set; }
        public double? ComparisonNorth { get; set; }
        public double? ComparisonEast { get; set; }
        public double? ComparisonBoreholeDiameter { get; set; }
        public double? CenterToCenterDistance { get; set; }
        public double? ClearanceDistance { get; set; }
        public double? Toolface { get; set; }
        public bool IsGravity { get; set; }
        public bool IsAdaptiveRefinementSample { get; set; }
        public int RefinementLevel { get; set; }
    }
}
