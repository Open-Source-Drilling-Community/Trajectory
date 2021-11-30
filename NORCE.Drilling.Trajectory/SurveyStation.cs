using System;
using NORCE.General.Std;
using NORCE.General.Math;

namespace NORCE.Drilling.Trajectory
{
    /// <summary>
    /// class representing a survey station
    /// </summary>
    public class SurveyStation : CurvilinearPoint3D
    {
        public double MD { 
            get {
                if (Abscissa == null)
                {
                    return Numeric.UNDEF_DOUBLE;
                }
                else
                {
                    return (double)Abscissa;
                }
            }
            set {
                if (Numeric.IsUndefined(value))
                {
                    Abscissa = null;
                } else
                {
                    Abscissa = value;
                }
            }
        }

        /// <summary>
        ///  accessor to the survey station uncertainty
        /// </summary>
        public SurveyStationUncertainty Uncertainty { get; set; }
    }
}
