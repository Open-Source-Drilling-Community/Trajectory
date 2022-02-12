using System;
using Newtonsoft.Json;

namespace NORCE.Drilling.Trajectory.WebApp.Client
{
    public class Configuration
    {
        public string HostURL { get; set; } = "https://app.DigiWells.no/";
        public int InternalHTTPPortNumber { get; set; } = 10002;
        public int InternalHTTPPortNumberWellBore { get; set; } = 6002;
        public int InternalHTTPPortNumberWell { get; set; } = 4002;
        public int InternalHTTPPortNumberField { get; set; } = 1002;
        public int InternalHTTPPortNumberCluster { get; set; } = 2002;

        /// <summary>
        /// Serialize a Configuration to Json
        /// </summary>
        /// <returns></returns>
        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// deserialize a string that is expected to be in Json into an instance of the configuration object
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Configuration FromJson(string str)
        {
            Configuration values = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    values = JsonConvert.DeserializeObject<Configuration>(str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return values;
        }

    }
}
