using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.General.Statistics;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    
    public class Trajectory : TrajectoryLight
    {

        /// <summary>
        /// The list of survey stations extracted from survey files, ordered by measured depths. 
        /// Each survey station contains the measured depth, the inclination and the azimuth of the wellbore at that depth, 
        /// as well as the uncertainty associated to these measurements.
        /// </summary>
        public List<SurveyStation>? SurveyStationList { get; set; }
        /// <summary>
        /// The uncertainty-aware geodetic coordinates of the tie-in point of the trajectory,
        /// i.e. the point at which the trajectory starts. Can either be:
        /// - the location of the slot of the cluster to which the wellbore is connected, if it is the main wellbore
        /// - or an interpolated point along a trajectory, if it is a sidetrack. In this case,
        ///   this location is determined by the TieInPointAlongHoleDepth univariate property of the wellbore, which is 
        ///   relative to the measured depth of the parent wellbore.
        /// </summary>
        public SurveyPoint? TieInPoint { get; set; }
        /// <summary>
        /// the method used to calculate the trajectory position from the curvilinear abscissa, inclination and azimuth.
        /// </summary>
        public TrajectoryCalculationType CalculationType { get; set; } = TrajectoryCalculationType.MinimumCurvatureMethod;
        /// <summary>
        /// the step in measured depth used to compute the interpolated trajectory from the list of survey stations
        /// </summary>
        public double MDStep { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public Trajectory() : base()
        {
        }

        /// <summary>
        /// main calculation method of the Trajectory
        /// </summary>
        /// <returns></returns>
        public bool Calculate()
        {
            if (SurveyStationList is not { Count: > 2 })
            {
                System.Console.WriteLine("not enough survey stations");
                return false;
            }
            if (TieInPoint is not null && TieInPoint.Abscissa is not null)
            {
                // remove the survey stations that are above the tie in point
                List<SurveyStation> updatedList = new List<SurveyStation>();
                foreach (var s in SurveyStationList)
                {
                    if (s is not null && s.Abscissa is not null && Numeric.GE(s.Abscissa, TieInPoint.Abscissa))
                    {
                        updatedList.Add(s);
                    }
                }
                if (updatedList.Count > 0)
                {
                    if (updatedList[0] is not null && updatedList[0].Abscissa is not null && Numeric.EQ(updatedList[0].Abscissa, TieInPoint.Abscissa))
                    {
                        updatedList[0].RiemannianNorth = TieInPoint.RiemannianNorth;
                        updatedList[0].RiemannianEast = TieInPoint.RiemannianEast;
                        updatedList[0].TVD = TieInPoint.TVD;
                        updatedList[0].VerticalSection = TieInPoint.VerticalSection ?? 0;
                    }
                    else
                    {
                        SurveyStation tieInStation = new SurveyStation();
                        tieInStation.Abscissa = TieInPoint.Abscissa;
                        tieInStation.Inclination = TieInPoint.Inclination;
                        tieInStation.Azimuth = TieInPoint.Azimuth;
                        tieInStation.TVD = TieInPoint.TVD;
                        tieInStation.RiemannianNorth = TieInPoint.RiemannianNorth;
                        tieInStation.RiemannianEast = TieInPoint.RiemannianEast;
                        tieInStation.VerticalSection = TieInPoint.VerticalSection ?? 0;
                        updatedList.Insert(0, tieInStation);
                    }
                }
                SurveyStationList = updatedList;
            }
            if (!SurveyPoint.CompleteSurvey(SurveyStationList, CalculationType))
            {
                System.Console.WriteLine("incomplete survey");
                return false;
            }
            if (!Numeric.IsDefined(MDStep) || !Numeric.GT(MDStep, 0))
            {
                System.Console.WriteLine("invalid measured depth step");
                return false;
            }
            return true;
        }
    }
}
