namespace NORCE.Drilling.Trajectory.ModelShared
{
    public partial class SurveyPoint
    {
        [System.Text.Json.Serialization.JsonPropertyName("Annotation")]
        public string? Annotation { get; set; }
    }

    public partial class SurveyStation
    {
        [System.Text.Json.Serialization.JsonPropertyName("Annotation")]
        public string? Annotation { get; set; }
    }
}
