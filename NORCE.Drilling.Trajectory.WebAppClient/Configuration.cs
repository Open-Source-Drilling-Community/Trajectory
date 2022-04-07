using System;
using Newtonsoft.Json;

namespace NORCE.Drilling.Trajectory.WebApp.Client
{
    public class Configuration
    {
        public static string TrajectoryHostURL { get; set; } = "https://app.DigiWells.no/";
        public static string  WellBoreHostURL { get; set; }
        public static string WellHostURL { get; set; }
        public static string FieldHostURL { get; set; }
        public static string ClusterHostURL { get; set; }
        public static string SurveyInstrumentHostURL { get; set; }
        public static string SurveyProgramHostURL { get; set; }

        public static int InternalTrajectoryHTTPPortNumber { get; set; } = 10002;
		public static int InternalHTTPPortNumberWellBore { get; set; } = 6002;
		public static int InternalHTTPPortNumberWell { get; set; } = 4002;
		public static int InternalHTTPPortNumberField { get; set; } = 1002;
		public static int InternalHTTPPortNumberCluster { get; set; } = 2002;
        public static int InternalHTTPPortNumberSurveyInstrument { get; set; } = 50002;
        public static int InternalHTTPPortNumberSurveyProgram { get; set; } = 20002;

    }
}
