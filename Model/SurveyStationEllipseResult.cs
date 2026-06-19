namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyStationEllipseResult
    {
        public double? MD { get; set; }
        public SurveyStationEllipse? HorizontalEllipse { get; set; }
        public SurveyStationEllipse? VerticalEllipse { get; set; }
        public SurveyStationEllipse? PerpendicularEllipse { get; set; }
    }
}
