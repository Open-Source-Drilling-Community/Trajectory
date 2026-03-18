using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Statistics;
using System.Collections.Generic;

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
        /// - or the location of the geodetic point at which the sidetrack starts, if it is a sidetrack. In this case,
        ///   this location is determined by the TieInPointAlongHoleDepth univariate property of the wellbore, which is 
        ///   relative to the measured depth of the parent wellbore.
        /// </summary>
        public GaussianGeodeticPoint3D? TieInPoint { get; set; }
        /// <summary>
        /// the list of unique survey stations extracted from survey files, ordered by measured depths
        /// </summary>
        public List<SurveyPoint>? InterpolatedTrajectory { get; set; }
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

            if (!SurveyPoint.CompleteSurvey(SurveyStationList))
            {
                System.Console.WriteLine("incomplete survey");
                return false;
            }

            if (!Numeric.IsDefined(MDStep) || !Numeric.GT(MDStep, 0))
            {
                System.Console.WriteLine("invalid measured depth step");
                return false;
            }

            InterpolatedTrajectory = SurveyPoint.Interpolate(SurveyStationList, MDStep);
            return true;
        }
    }
}
