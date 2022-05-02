using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NORCE.Drilling.Trajectory.ModelClientShared
{
    public partial class SurveyStation
    {

        /// <summary>
        /// copy everything except the ID
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public bool Copy(SurveyStation dest)
        {
            if (dest != null)
            {
                dest.AzWGS84 = AzWGS84;
                dest.Incl = Incl;
                dest.MdWGS84 = MdWGS84;
                dest.NorthOfWellHead = NorthOfWellHead;
                dest.EastOfWellHead = EastOfWellHead;
                dest.TvdWGS84 = TvdWGS84;
                dest.SurveyTool = SurveyTool;
                dest.Az = Az;
                dest.LatitudeWGS84 = LatitudeWGS84;
                dest.LongitudeWGS84 = LongitudeWGS84;
                return true;
            }
            else
            {
                return false;
            }
        }

        
    }
}
