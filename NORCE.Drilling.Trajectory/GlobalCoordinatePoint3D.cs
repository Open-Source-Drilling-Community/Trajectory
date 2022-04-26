using System;
using NORCE.General.Std;
using NORCE.General.Math;

namespace NORCE.Drilling.Trajectory
{
    /// <summary>
    /// class representing a survey station
    /// </summary>
    public class GlobalCoordinatePoint3D : CurvilinearPoint3D
    {
        public GlobalCoordinatePoint3D()
        {

        }
        /// <summary>
        /// constructor with initialization
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public GlobalCoordinatePoint3D(double x, double y, double z) : base(x,y,z)
        {
           
        }
        /// <summary>
        ///  accessor to the MD with respect to WGS84 Coordinates
        /// </summary>
        public double? MdWGS84
        { 
            get {
                if (Abscissa == null)
                {
                    return null;
                }
                else
                {
                    return Abscissa;
                }
            }
            set {
                if (value == null)
                {
                    Abscissa = null;
                } else
                {
                    Abscissa = value;
                }
            }
        }
        /// <summary>
        ///  accessor to the TVD in WGS84 Coordinates
        /// </summary>
        public double? TvdWGS84
        {
            get
            {
                if (Z == null)
                {
                    return null;
                }
                else
                {
                    return Z;
                }
            }
            set
            {
                if (Numeric.IsUndefined(value))
                {
                    Z = null;
                }
                else
                {
                    Z = value;
                }
            }
        }
        /// <summary>
        ///  accessor to the distance north of wellhead
        /// </summary>
        public double? NorthOfWellHead
        {
            get
            {
                if (X == null)
                {
                    return null;
                }
                else
                {
                    return X;
                }
            }
            set
            {
                if (value == null)
                {
                    X = null;
                }
                else
                {
                    X = value;
                }
            }
        }
        /// <summary>
        ///  accessor to the distance east of wellhead
        /// </summary>
        public double? EastOfWellHead
        {
            get
            {
                if (Y == null)
                {
                    return null;
                }
                else
                {
                    return Y;
                }
            }
            set
            {
                if (value == null)
                {
                    Y = null;
                }
                else
                {
                    Y = value;
                }
            }
        }
        /// <summary>
        ///  accessor to the Azimuth with respect to WGS84 coordinates
        /// </summary>
        public double? AzWGS84
        {
            get
            {
                if (Az == null)
                {
                    return null;
                }
                else
                {
                    return Az;
                }
            }
            set
            {
                if (value == null)
                {
                    Az = null;
                }
                else
                {
                    Az = value;
                }
            }
        }
        /// <summary>
        ///  accessor to the Latitude in WGS84 coordinates
        /// </summary>
        public double? LatitudeWGS84  { get; set; }
        /// <summary>
        ///  accessor to the Longitude in WGS84 coordinates
        /// </summary>
        public double? LongitudeWGS84 { get; set; }
       
    }
}
