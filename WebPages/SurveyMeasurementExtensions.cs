using OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.ModelShared
{
    public partial class SurveyMeasurement
    {
        public const string CurrentCorrectionAlgorithmVersion = "1";

        public static SurveyMeasurementCorrection CreatePendingCorrection()
        {
            return new SurveyMeasurementCorrection
            {
                Source = SurveyCorrectionSource.None,
                Status = SurveyCorrectionStatus.Pending,
                AlgorithmVersion = CurrentCorrectionAlgorithmVersion
            };
        }

        public void EnsureCorrectionContractDefaults()
        {
            if (Correction != null && string.IsNullOrWhiteSpace(Correction.AlgorithmVersion))
            {
                Correction.AlgorithmVersion = CurrentCorrectionAlgorithmVersion;
            }
        }

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
