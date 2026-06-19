using OSDC.DotnetLibraries.Drilling.Surveying;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyMeasurement
    {
        public double? MD { get; set; }
        public double? Inclination { get; set; }
        public double? Azimuth { get; set; }
        public string? Annotation { get; set; }

        public SurveyStation ToSurveyStation()
        {
            return new SurveyStation
            {
                MD = MD,
                Abscissa = MD,
                Inclination = Inclination,
                Azimuth = Azimuth,
                Annotation = Annotation
            };
        }

        public static SurveyMeasurement FromSurveyStation(SurveyStation station)
        {
            return new SurveyMeasurement
            {
                MD = station.MD ?? station.Abscissa,
                Inclination = station.Inclination,
                Azimuth = station.Azimuth,
                Annotation = station.Annotation
            };
        }
    }
}
