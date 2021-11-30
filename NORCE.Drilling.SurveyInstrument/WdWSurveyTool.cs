using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Std;

namespace NORCE.Drilling.SurveyInstrument
{
    public class WdWSurveyTool : SurveyInstrument
    {
        //private static Random rnd_ = null;
        private static List<WdWSurveyTool> defaultList_ = null;
        private double? relDepthError_ = null;
        private double? misalignment_ = null;
        private double? trueInclination_ = null;
        private double? referenceError_ = null;
        private double? drillStringMag_ = null;
        private double? gyroCompassError_ = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static WdWSurveyTool GetTool(List<WdWSurveyTool> list, string name)
        {
            foreach (WdWSurveyTool tool in list)
            {
                if (!string.IsNullOrEmpty(tool.Name) && tool.Name.Equals(name))
                {
                    return tool;
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static List<WdWSurveyTool> DefaultList
        {
            get
            {
                if (defaultList_ == null)
                {
                    defaultList_ = new List<WdWSurveyTool>();
                    defaultList_.Add(PoorMag);
                    defaultList_.Add(GoodMag);
                    defaultList_.Add(PoorGyro);
                    defaultList_.Add(GoodGyro);
                }
                return defaultList_;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly WdWSurveyTool PoorMag = new WdWSurveyTool
        {
            Name = "Poor-mag",
            relDepthError_ = 0.002,
            misalignment_ = 0.3 * Numeric.PI / 180.0,
            trueInclination_ = 1.0 * Numeric.PI / 180.0,
            referenceError_ = 1.5 * Numeric.PI / 180.0,
            drillStringMag_ = 5.0 * Numeric.PI / 180.0,
            gyroCompassError_ = null
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly WdWSurveyTool GoodMag = new WdWSurveyTool
        {
            Name = "Good-mag",
            relDepthError_ = 0.001,
            misalignment_ = 0.1 * Numeric.PI / 180.0,
            trueInclination_ = 0.5 * Numeric.PI / 180.0,
            referenceError_ = 1.5 * Numeric.PI / 180.0,
            drillStringMag_ = 0.25 * Numeric.PI / 180.0,
            gyroCompassError_ = null
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly WdWSurveyTool PoorGyro = new WdWSurveyTool
        {
            Name = "Poor-gyro",
            relDepthError_ = 0.002,
            misalignment_ = 0.2 * Numeric.PI / 180.0,
            trueInclination_ = 0.5 * Numeric.PI / 180.0,
            referenceError_ = 1.0 * Numeric.PI / 180.0,
            drillStringMag_ = Numeric.UNDEF_DOUBLE,
            gyroCompassError_ = 2.5 * Numeric.PI / 180.0
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly WdWSurveyTool GoodGyro = new WdWSurveyTool
        {
            Name = "Good-gyro",
            relDepthError_ = 0.0005,
            misalignment_ = 0.203 * Numeric.PI / 180.0,
            trueInclination_ = 0.2 * Numeric.PI / 180.0,
            referenceError_ = 0.1 * Numeric.PI / 180.0,
            drillStringMag_ = Numeric.UNDEF_DOUBLE,
            gyroCompassError_ = 0.5 * Numeric.PI / 180.0
        };

        /// <summary>
        /// Default constructor
        /// </summary>
        public WdWSurveyTool()
        {
            ID = rnd_.Next();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="value"></param>
        public WdWSurveyTool(WdWSurveyTool value)
        {           
            if (value != null)
            {
                ID = value.ID;
                Name = value.Name;
                relDepthError_ = value.relDepthError_;
                misalignment_ = value.misalignment_;
                trueInclination_ = value.trueInclination_;
                referenceError_ = value.referenceError_;
                drillStringMag_ = value.drillStringMag_;
                gyroCompassError_ = value.gyroCompassError_;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? RelativeDepthError
        {
            get
            {
                return relDepthError_;
            }
            set
            {
                relDepthError_ = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? Misalignment
        {
            get
            {
                return misalignment_;
            }
            set
            {
                misalignment_ = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? TrueInclination
        {
            get
            {
                return trueInclination_;
            }
            set
            {
                trueInclination_ = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? ReferenceError
        {
            get
            {
                return referenceError_;
            }
            set
            {
                referenceError_ = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? DrillStringMagnetisation
        {
            get
            {
                return drillStringMag_;
            }
            set
            {
                drillStringMag_ = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double? GyroCompassError
        {
            get
            {
                return gyroCompassError_;
            }
            set
            {
                gyroCompassError_ = value;
            }
        }

        #region ICopyable<WdWSurveyTool> Members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Copy(ref WdWSurveyTool item)
        {
            if (item != null)
            {
                SurveyInstrument st = (SurveyInstrument)item;
                CopyFrom(st);
                item.relDepthError_ = relDepthError_;
                item.misalignment_ = misalignment_;
                item.trueInclination_ = trueInclination_;
                item.referenceError_ = referenceError_;
                item.drillStringMag_ = drillStringMag_;
                item.gyroCompassError_ = gyroCompassError_;
            }
        }

        #endregion

        #region IEquatable<WdWSurveyTool> Members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WdWSurveyTool other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Equals((SurveyInstrument)other) &&
                       Numeric.EQ(relDepthError_, other.relDepthError_) &&
                       Numeric.EQ(misalignment_, other.misalignment_) &&
                       Numeric.EQ(trueInclination_, other.trueInclination_) &&
                       Numeric.EQ(referenceError_, other.referenceError_) &&
                       Numeric.EQ(drillStringMag_, other.drillStringMag_) &&
                       Numeric.EQ(gyroCompassError_, other.gyroCompassError_);
            }
        }

        #endregion

        #region IUndefinable Members
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public override bool IsUndefined()
        //{
        //    return base.IsUndefined() ||
        //           Numeric.IsUndefined(relDepthError_) ||
        //           Numeric.IsUndefined(misalignment_) ||
        //           Numeric.IsUndefined(trueInclination_) ||
        //           Numeric.IsUndefined(referenceError_) ||
        //           Numeric.IsUndefined(drillStringMag_) ||
        //           Numeric.IsUndefined(gyroCompassError_);
        //}

        /// <summary>
        /// 
        /// </summary>
        //public override void SetUndefined()
        //{
        //    base.SetUndefined();
        //    relDepthError_ = Numeric.UNDEF_FLOAT;
        //    misalignment_ = Numeric.UNDEF_FLOAT;
        //    trueInclination_ = Numeric.UNDEF_FLOAT;
        //    referenceError_ = Numeric.UNDEF_FLOAT;
        //    drillStringMag_ = Numeric.UNDEF_FLOAT;
        //    gyroCompassError_ = Numeric.UNDEF_FLOAT;
        //}

        #endregion

        #region ICloneable Members
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public override object Clone()
        //{
        //    return new WdWSurveyTool(this);
        //}

        #endregion
    }
}
