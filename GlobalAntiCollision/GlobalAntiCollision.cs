using System;
using System.Collections.Generic;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public class GlobalAntiCollision : ICloneable
    {
        /// <summary>
        /// an ID for the GlobalAntiCollision
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public double ConfidenceFactor { get; set; }

        /// <summary>
        /// This is the ID for the reference well path (which we obtain interpolated from the WellPath Service). 
        /// </summary>
        public Guid ReferenceWellPathID { get; set; } = Guid.Empty;

        /// <summary>
        /// This is the ID for the reference trajectory. This is only relevant when working with trajectories which are already in the database. For other wells this can be "undefined"
        /// </summary>
        public Guid ReferenceTrajectoryID { get; set; } = Guid.Empty;

        /// <summary>
        /// 
        /// </summary>
        public List<Guid> ComparisonTrajectoryIDs { get; set; } = [];

        /// <summary>
        /// <summary>
        /// the set of Results associated with the investigated trajectory
        /// </summary>
        public List<SeparationFactorResult> SeparationFactorResults { get; set; } = [];
        
        /// <summary>
        /// default constructor
        /// </summary>
        public GlobalAntiCollision(): base()
        {

        }
        
        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public GlobalAntiCollision(GlobalAntiCollision? src) : base()
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
        public bool Copy(GlobalAntiCollision? dest)
        {
            if (dest != null)
            {
                dest.ID = ID;
                dest.ReferenceWellPathID = ReferenceWellPathID;
                dest.ReferenceTrajectoryID = ReferenceTrajectoryID;
                dest.ComparisonTrajectoryIDs = [.. ComparisonTrajectoryIDs];
                dest.ConfidenceFactor = ConfidenceFactor;
                dest.SeparationFactorResults ??= [];
                dest.SeparationFactorResults.Clear();
                foreach (SeparationFactorResult sf in SeparationFactorResults)
                {
                    dest.SeparationFactorResults.Add(new SeparationFactorResult(sf));
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
            GlobalAntiCollision copy = new GlobalAntiCollision(this);
            copy.ID = ID;
            return copy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Calculate(List<SurveyStation>? referenceSurveyList, List<List<SurveyStation>>? comparisonSurveyLists)
        {
            if (comparisonSurveyLists != null && referenceSurveyList != null)
            {
                for (int i = 0; i < comparisonSurveyLists.Count; i++)
                {
                    List<SurveyStation> surveysCmp = comparisonSurveyLists[i];
                    SeparationFactorResult sfr = new SeparationFactorResult((Guid)ComparisonTrajectoryIDs[i]);
                    for (int k = 0; k < referenceSurveyList.Count; k++)
                    {
                        List<SeparationFactorPoint> safetyFactorResults = SeparationFactorCalculations.CalculateSeparationFactor(referenceSurveyList, surveysCmp, k, ConfidenceFactor);
                        sfr.SeparationFactorProfile.AddRange(safetyFactorResults);
                    }
                    SeparationFactorResults.Add(sfr);
                }
            }
        }
    }
}
