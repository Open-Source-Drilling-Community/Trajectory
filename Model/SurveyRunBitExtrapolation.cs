using System.Text.Json.Serialization;

namespace OSDC.Drilling.Trajectory.Model
{
    public enum SurveyRunBitExtrapolationMode
    {
        CalculateFromLastMeasurement,
        LastStationAlreadyExtrapolated
    }

    /// <summary>
    /// Optional terminal extrapolation from the measurement tool to the bit. The distance is a
    /// positive distance-to-bit elevation of the measurement tool relative to the bit front face,
    /// in canonical SI metres and frozen with the survey run. It is used as the positive along-hole
    /// MD increment from the final tool station to the bit.
    /// </summary>
    public sealed class SurveyRunBitExtrapolation
    {
        [JsonRequired]
        public SurveyRunBitExtrapolationMode Mode { get; set; }

        /// <summary>
        /// Distance-to-bit elevation of the measurement tool relative to the bit front face, positive
        /// upward and in metres; used as the positive along-hole MD increment to the bit.
        /// </summary>
        public double MeasurementToolToBitDistance { get; set; }
    }
}
