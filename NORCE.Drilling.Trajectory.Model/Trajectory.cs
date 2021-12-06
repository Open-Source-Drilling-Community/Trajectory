using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class Trajectory : ICloneable
    {
        /// <summary>
        /// an ID for the trajectory
        /// </summary>
        public int ID { get; set; } = -1;
        /// <summary>
        /// a name for the trajectory
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// a description for the trajectory
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        ///  the ID of the wellbore in which this trajectory belongs to
        /// </summary>
        public int WellboreID { get; set; } = -1;      
        /// <summary>
        /// the set of SurveyList associated with this trajectory
        /// </summary>
        public SurveyList SurveyList { get; set; } = new SurveyList();
        /// <summary>
        /// IsPlanned
        /// </summary>
        public bool IsPlanned { get; set; } = false;
        /// <summary>
        /// IsMeasured
        /// </summary>
        public bool IsMeasured { get; set; } = true;
        /// <summary>
        /// IsDefinite
        /// </summary>
        public bool IsDefinitive { get; set; } = true;
        /// <summary>
        /// Survey Instrument
        /// </summary>
        //public string SurveyInstrument
        /// <summary>
        /// default constructor
        /// </summary>
        public Trajectory(): base()
        {

        }
        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public Trajectory(Trajectory src) : base()
        {
            if (src != null)
            {
                src.Copy(this);
            }
        }
        /// <summary>
        /// copy everything except the ID
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public bool Copy(Trajectory dest)
        {
            if (dest != null)
            {
                dest.ID = ID;
                dest.Name = Name;
                dest.Description = Description;
                dest.WellboreID = WellboreID;
                dest.IsDefinitive = IsDefinitive;
                dest.IsMeasured = IsMeasured;
                dest.IsPlanned = IsPlanned;

                if (dest.SurveyList == null)
                {
                    dest.SurveyList = new SurveyList();
                }
                if (SurveyList != null)
                {
                    dest.SurveyList = SurveyList;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// cloning function (including the ID)
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            Trajectory copy = new Trajectory(this);
            copy.ID = ID;
            return copy;
        }
    }
}
