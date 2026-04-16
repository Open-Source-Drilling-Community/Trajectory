using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public class SeparationFactorResult : ICloneable
    {
        /// <summary>
        ///
        /// </summary>
        public Guid ComparisonTrajectoryID { get; set; } = Guid.Empty;

        /// <summary>
        /// Tuple order: Reference MD, comparison MD, separation factor.
        /// </summary>
        [JsonConverter(typeof(SeparationFactorProfileJsonConverter))]
        public List<SeparationFactorPoint> SeparationFactorProfile { get; set; } = [];

        /// <summary>
        /// Default Constructor - required for deserialization
        /// </summary>
        public SeparationFactorResult() : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SeparationFactorResult(Guid comparisonTrajectoryID) : base()
        {
            ComparisonTrajectoryID = comparisonTrajectoryID;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public SeparationFactorResult(SeparationFactorResult? src) : base()
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
        public bool Copy(SeparationFactorResult? dest)
        {
            if (dest != null)
            {
                dest.ComparisonTrajectoryID = ComparisonTrajectoryID;
                dest.SeparationFactorProfile ??= [];
                dest.SeparationFactorProfile.Clear();

                if (SeparationFactorProfile != null)
                {
                    foreach (SeparationFactorPoint point in SeparationFactorProfile)
                    {
                        dest.SeparationFactorProfile.Add(new SeparationFactorPoint(point.ReferenceMD, point.ComparisonMD, point.SeparationFactor));
                    }
                }
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
            SeparationFactorResult separationFactorResult = new SeparationFactorResult(this);
            separationFactorResult.ComparisonTrajectoryID = ComparisonTrajectoryID;
            return separationFactorResult;
        }
    }
}
