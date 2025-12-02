using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class Trajectory : TrajectoryLight
    {
        public List<SurveyStation>? SurveyStationList { get; set; }
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
            bool success = false;
            if (SurveyStationList is { Count: <= 2 })
                System.Console.WriteLine("not enough survey stations");
            if (!SurveyPoint.CompleteSurvey(SurveyStationList))
                System.Console.WriteLine("incomplete survey");

            if (SurveyStationList is { Count: > 2 } &&
                Numeric.IsDefined(MDStep) &&
                Numeric.GT(MDStep, 0) &&
                SurveyPoint.CompleteSurvey(SurveyStationList))
            {
                InterpolatedTrajectory = SurveyPoint.Interpolate(SurveyStationList, MDStep);
                success = true;
            }
            return success;
        }
    }
}
