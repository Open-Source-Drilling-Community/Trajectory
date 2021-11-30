using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NORCE.Drilling.Trajectory.ModelClientShared
{
    public partial class Trajectory
    {

        /// <summary>
        /// copy everything except the ID
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public bool Copy(Trajectory dest)
        {
            if (dest != null)
            {
                dest.Name = Name;
                dest.Description = Description;
                dest.WellboreID = WellboreID;
                dest.ReferenceLatitudeWGS84 = ReferenceLatitudeWGS84;
                dest.ReferenceLongitudeWGS84 = ReferenceLongitudeWGS84;
                dest.ReferenceTVDWGS84 = ReferenceTVDWGS84;
                if(dest.SurveyList == null )
				{
                    dest.SurveyList = new SurveyList();
                    if (dest.SurveyList.Surveys == null)
                    {
                        dest.SurveyList.Surveys = new List<SurveyStation>();
                    }
                }
                dest.SurveyList.Surveys.Clear();
                if (SurveyList != null)
                {
                    if (SurveyList.Surveys != null)
                    {
                        foreach (SurveyStation surveyStation in SurveyList.Surveys)
                        {
                            dest.SurveyList.Surveys.Add(surveyStation);
                        }
                    }
                }
                //if (dest.Slots == null)
                //{
                //    dest.Slots = new List<Slot>();
                //}
                //dest.Slots.Clear();
                //if (Slots != null)
                //{
                //    foreach (Slot slot in Slots)
                //    {
                //        Slot copy = new Slot();
                //        slot.Copy(copy);
                //        dest.Slots.Add(copy);
                //    }
                //}
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
        /// deserialize a string that is expected to be in Json into an instance of Trajectory
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Trajectory FromJson(string str)
        {
            Trajectory values = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    values = JsonConvert.DeserializeObject<Trajectory>(str);
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
