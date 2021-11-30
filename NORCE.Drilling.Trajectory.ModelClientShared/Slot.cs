using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NORCE.Drilling.Trajectory.ModelClientShared
{
    public partial class Slot
    {
        /// <summary>
        /// copy everything except the ID
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public bool Copy(Slot dest)
        {
            if (dest != null)
            {
                dest.Name = Name;
                dest.LatitudeWGS84 = LatitudeWGS84;
                dest.LongitudeWGS84 = LongitudeWGS84;
                dest.TVDWGS84 = TVDWGS84;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Serialize a Trajectory to Json
        /// </summary>
        /// <returns></returns>
        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// deserialize a string that is expected to be in Json into an instance of Slot
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Slot FromJson(string str)
        {
            Slot value = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    value = JsonConvert.DeserializeObject<Slot>(str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return value;
        }
    }
}

