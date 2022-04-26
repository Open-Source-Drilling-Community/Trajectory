using System;
using NORCE.General.Std;
using NORCE.General.Math;

namespace NORCE.Drilling.Trajectory
{
    /// <summary>
    /// class representing a survey station
    /// </summary>
    public class SurveyStation : GlobalCoordinatePoint3D
    {      
        /// <summary>
        ///  accessor to the survey station uncertainty
        /// </summary>
        public SurveyStationUncertainty Uncertainty { get; set; }
        /// <summary>
        ///  accessor to the survey station uncertainty
        /// </summary>
        public SurveyInstrument.Model.SurveyInstrument SurveyTool { get; set; }
    }
}
