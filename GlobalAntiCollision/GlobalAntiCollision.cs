using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public class GlobalAntiCollision : ICloneable
    {
        /// <summary>
        /// If true, each comparison pair is evaluated in both directions and valid reverse-direction points are merged
        /// back into the requested reference/comparison orientation. This is slower, but reduces direction-dependent minima.
        /// </summary>
        public static bool UseSymmetricSeparationFactorCalculation { get; set; } = false;

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
        public void Calculate(
            List<SurveyStation>? referenceSurveyList,
            List<List<SurveyStation>>? comparisonSurveyLists,
            List<MeasuredDepthRange?>? referenceMdRanges = null,
            List<MeasuredDepthRange?>? comparisonMdRanges = null,
            List<double?>? referenceMinimumMDs = null,
            List<double?>? comparisonMinimumMDs = null)
        {
            if (comparisonSurveyLists != null && referenceSurveyList != null)
            {
                SeparationFactorResults.Clear();
                SeparationFactorEnvelopeCache sharedReferenceCache = new(
                    referenceSurveyList,
                    referenceSurveyList,
                    ConfidenceFactor,
                    UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                    UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt);
                for (int i = 0; i < comparisonSurveyLists.Count; i++)
                {
                    double? referenceMinimumMD = referenceMinimumMDs != null && i < referenceMinimumMDs.Count
                        ? referenceMinimumMDs[i]
                        : null;
                    double? comparisonMinimumMD = comparisonMinimumMDs != null && i < comparisonMinimumMDs.Count
                        ? comparisonMinimumMDs[i]
                        : null;
                    List<SurveyStation> surveysRef = GetSurveyWithTieInStation(referenceSurveyList, referenceMinimumMD);
                    List<SurveyStation> surveysCmp = GetSurveyWithTieInStation(comparisonSurveyLists[i], comparisonMinimumMD);

                    if (surveysRef.Count == 0 || surveysCmp.Count == 0)
                    {
                        continue;
                    }

                    SeparationFactorResult sfr = new SeparationFactorResult((Guid)ComparisonTrajectoryIDs[i]);
                    sfr.ReferenceMDRange = ClampRangeMinimum(
                        referenceMdRanges != null && i < referenceMdRanges.Count
                            ? referenceMdRanges[i]
                            : RelevantMdRangeCalculator.GetSurveyMdRange(surveysRef),
                        referenceMinimumMD);
                    sfr.ComparisonMDRange = ClampRangeMinimum(
                        comparisonMdRanges != null && i < comparisonMdRanges.Count
                            ? comparisonMdRanges[i]
                            : RelevantMdRangeCalculator.GetSurveyMdRange(surveysCmp),
                        comparisonMinimumMD);

                    List<SeparationFactorPoint>? directionalProfile = CalculateDirectionalProfile(
                        surveysRef,
                        surveysCmp,
                        sfr.ReferenceMDRange,
                        referenceMinimumMD,
                        comparisonMinimumMD,
                        referenceMinimumMD.HasValue ? null : sharedReferenceCache);
                    if (directionalProfile == null)
                    {
                        continue;
                    }

                    sfr.SeparationFactorProfile.AddRange(directionalProfile);
                    if (UseSymmetricSeparationFactorCalculation)
                    {
                        List<SeparationFactorPoint>? reverseDirectionalProfile = CalculateDirectionalProfile(
                            surveysCmp,
                            surveysRef,
                            sfr.ComparisonMDRange,
                            comparisonMinimumMD,
                            referenceMinimumMD,
                            null);
                        if (reverseDirectionalProfile != null)
                        {
                            AddSwappedValidProfilePoints(sfr.SeparationFactorProfile, reverseDirectionalProfile);
                            SortSeparationFactorProfile(sfr.SeparationFactorProfile);
                        }
                    }

                    ExpandMdRangesToProfile(sfr);

                    SeparationFactorResults.Add(sfr);
                }
            }
        }

        private static List<SurveyStation> GetSurveyWithTieInStation(
            List<SurveyStation> surveyStations,
            double? tieInMD)
        {
            if (!tieInMD.HasValue || !double.IsFinite(tieInMD.Value))
            {
                return surveyStations;
            }

            double threshold = tieInMD.Value;
            int firstStationAtOrAfterTieIn = surveyStations.FindIndex(
                station => station.MD is double md && double.IsFinite(md) && md >= threshold);
            if (firstStationAtOrAfterTieIn < 0)
            {
                return surveyStations;
            }

            if (surveyStations[firstStationAtOrAfterTieIn].MD is double stationMD &&
                Math.Abs(stationMD - threshold) <= 1e-9)
            {
                return surveyStations;
            }

            List<SurveyStation> surveyWithTieIn = [.. surveyStations];
            if (firstStationAtOrAfterTieIn > 0 &&
                SurveyStation.InterpolateAtAbscissa(
                    surveyStations,
                    threshold,
                    out SurveyStation? tieInStation) &&
                tieInStation != null)
            {
                tieInStation.MD = threshold;
                tieInStation.Abscissa = threshold;
                surveyWithTieIn.Insert(firstStationAtOrAfterTieIn, tieInStation);
            }

            return surveyWithTieIn;
        }

        private static MeasuredDepthRange? ClampRangeMinimum(MeasuredDepthRange? range, double? minimumMD)
        {
            if (range == null || !minimumMD.HasValue || !double.IsFinite(minimumMD.Value))
            {
                return range;
            }

            return new MeasuredDepthRange(
                Math.Max(range.StartMD, minimumMD.Value),
                range.EndMD);
        }

        private List<SeparationFactorPoint>? CalculateDirectionalProfile(
            List<SurveyStation> surveysRef,
            List<SurveyStation> surveysCmp,
            MeasuredDepthRange? referenceMDRange,
            double? referenceMinimumMD,
            double? comparisonMinimumMD,
            SeparationFactorEnvelopeCache? sharedReferenceCache)
        {
            SeparationFactorEnvelopeCache envelopeCache = new(
                surveysRef,
                surveysCmp,
                ConfidenceFactor,
                UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                referenceMinimumMD: referenceMinimumMD,
                comparisonMinimumMD: comparisonMinimumMD,
                sharedReferenceCache: sharedReferenceCache);
            if (!envelopeCache.IsValid)
            {
                return null;
            }

            int startIndex = GetSurveyStationStartIndex(surveysRef, referenceMDRange);
            int endIndex = GetSurveyStationEndIndex(surveysRef, referenceMDRange);
            if (startIndex > endIndex)
            {
                return [];
            }

            int stationCount = endIndex - startIndex + 1;
            List<SeparationFactorPoint>[] resultsByStation = new List<SeparationFactorPoint>[stationCount];
            Parallel.For(
                0,
                stationCount,
                new ParallelOptions { MaxDegreeOfParallelism = GetMaxSeparationFactorParallelism(stationCount) },
                offset =>
            {
                int k = startIndex + offset;
                List<SeparationFactorPoint> safetyFactorResults = SeparationFactorCalculations.CalculateSeparationFactor(
                    surveysRef,
                    surveysCmp,
                    k,
                    ConfidenceFactor,
                    UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                    UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt,
                    envelopeCache);
                resultsByStation[offset] = safetyFactorResults;
            });

            List<SeparationFactorPoint> profile = [];
            for (int offset = 0; offset < resultsByStation.Length; offset++)
            {
                profile.AddRange(resultsByStation[offset]);
            }
            return profile;
        }

        private static void AddSwappedValidProfilePoints(
            List<SeparationFactorPoint> targetProfile,
            List<SeparationFactorPoint> reverseDirectionalProfile)
        {
            foreach (SeparationFactorPoint point in reverseDirectionalProfile)
            {
                if (!double.IsFinite(point.ComparisonMD))
                {
                    continue;
                }

                targetProfile.Add(new SeparationFactorPoint(
                    point.ComparisonMD,
                    point.ReferenceMD,
                    point.SeparationFactor));
            }
        }

        private static void SortSeparationFactorProfile(List<SeparationFactorPoint> profile)
        {
            profile.Sort(static (left, right) =>
            {
                int referenceComparison = left.ReferenceMD.CompareTo(right.ReferenceMD);
                if (referenceComparison != 0)
                {
                    return referenceComparison;
                }

                int comparisonComparison = left.ComparisonMD.CompareTo(right.ComparisonMD);
                if (comparisonComparison != 0)
                {
                    return comparisonComparison;
                }

                return left.SeparationFactor.CompareTo(right.SeparationFactor);
            });
        }

        private static void ExpandMdRangesToProfile(SeparationFactorResult result)
        {
            foreach (SeparationFactorPoint point in result.SeparationFactorProfile)
            {
                if (double.IsFinite(point.ReferenceMD))
                {
                    result.ReferenceMDRange = ExpandRange(result.ReferenceMDRange, point.ReferenceMD);
                }
                if (double.IsFinite(point.ComparisonMD) && point.ComparisonMD != -1.0)
                {
                    result.ComparisonMDRange = ExpandRange(result.ComparisonMDRange, point.ComparisonMD);
                }
            }
        }

        private static MeasuredDepthRange ExpandRange(MeasuredDepthRange? range, double md)
        {
            if (range == null)
            {
                return new MeasuredDepthRange(md, md);
            }

            return new MeasuredDepthRange(
                Math.Min(range.StartMD, md),
                Math.Max(range.EndMD, md));
        }

        private static int GetMaxSeparationFactorParallelism(int stationCount)
        {
            return Math.Max(1, Math.Min(stationCount, Environment.ProcessorCount));
        }

        private static int GetSurveyStationStartIndex(List<SurveyStation> surveyStations, MeasuredDepthRange? range)
        {
            if (range == null)
            {
                return 0;
            }

            for (int i = 0; i < surveyStations.Count; i++)
            {
                if (surveyStations[i].MD is double md && md >= range.StartMD)
                {
                    return Math.Max(0, i - 1);
                }
            }

            return surveyStations.Count;
        }

        private static int GetSurveyStationEndIndex(List<SurveyStation> surveyStations, MeasuredDepthRange? range)
        {
            if (range == null)
            {
                return surveyStations.Count - 1;
            }

            for (int i = surveyStations.Count - 1; i >= 0; i--)
            {
                if (surveyStations[i].MD is double md && md <= range.EndMD)
                {
                    return Math.Min(surveyStations.Count - 1, i + 1);
                }
            }

            return -1;
        }

    }
}
