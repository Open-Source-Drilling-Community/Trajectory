using System;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyRunMinimumDistanceIntervalResult
    {
        public Guid IntervalID { get; set; }
        public string? IntervalName { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }
        public Guid? ComparisonSurveyRunID { get; set; }
        public int SampleCount { get; set; }
        public double? AverageCenterToCenterDistance { get; set; }
        public double? StandardDeviationCenterToCenterDistance { get; set; }
        public double? AverageClearanceDistance { get; set; }
        public double? StandardDeviationClearanceDistance { get; set; }

        public SurveyRunMinimumDistanceIntervalResult()
        {
        }
    }
}
