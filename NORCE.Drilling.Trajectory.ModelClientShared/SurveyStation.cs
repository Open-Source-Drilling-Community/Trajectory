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
                dest.Az = Az;
                dest.Incl = Incl;
                dest.MD = MD;
                dest.X = X;
                dest.Y = Y;
                dest.Z = Z;
                dest.SurveyTool = SurveyTool;
                return true;
            }
            else
            {
                return false;
            }
        }

        
    }
}
