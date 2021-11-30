using System;
using System.Collections.Generic;
using System.Text;

namespace NORCE.Drilling.Trajectory.Model
{
    public class Slot : ICloneable
    {
        /// <summary>
        /// 
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double LatitudeWGS84 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double LongitudeWGS84 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double TVDWGS84 { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        public Slot() : base()
        {

        }
        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public Slot(Slot src) : base()
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
        /// cloning (including the ID)
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            Slot slot = new Slot(this);
            slot.ID = ID;
            return slot;
        }
    }
}
