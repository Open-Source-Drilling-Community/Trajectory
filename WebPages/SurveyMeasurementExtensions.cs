using NORCE.Drilling.Trajectory.ModelShared;

namespace NORCE.Drilling.Trajectory.ModelShared
{
    public partial class SurveyMeasurement
    {
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
