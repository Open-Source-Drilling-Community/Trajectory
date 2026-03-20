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
        /// <param name="slotCoordinate">the coordinates of the slot hosting the trajectory to compute in the cartographic projection of the field the trajectory belongs to</param>
        /// <returns></returns>
        public bool Calculate(CartographicCoordinate? slotCoordinate)
        {
            if (SurveyStationList is not { Count: > 2 })
            {
                System.Console.WriteLine("not enough survey stations");
                return false;
            }
            if (TieInPoint?.ReferencePoint is null)
            {
                System.Console.WriteLine("the tie-in point has not been properly set");
                return false;
            }
            //// Complete the survey station list
            //// In the Trajectory standard workflow, SIA coordinates (abscissa, inclination, azimuth) are provided.
            //// In the CompleteSurvey algorithm, SIA coordinates are tested first for non-nullity, and then and only then, local Cartesian coordinates XYZ are tested for non-nullity.
            //// Consequently, XYZ tend to be the ones recomputed every time there is an update.
            //// Note: in SurveyPoint class XYZ are made equal to RiemannianNorth and RiemannianEast (@Surveying.v1.2.3). However, calculations in trajectory are made within an Euclidean space
            //// (especially MCM, CTC curvature methods). Therefore, there is a flaw in the reasoning: XYZ should be defined as NED coordinates in the cartographic projection of the field the trajectory belongs to.
            //for (int i = 1; i < SurveyStationList.Count; i++)
            //{
            //    SurveyStationList[i].X = null;
            //    SurveyStationList[i].Y = null;
            //    SurveyStationList[i].Z = null;
            //    SurveyStationList[i].RiemannianNorth = null;
            //    SurveyStationList[i].RiemannianEast = null;
            //    SurveyStationList[i].TVD = null;
            //    SurveyStationList[i].X = null;
            //    SurveyStationList[i].Y = null;
            //    SurveyStationList[i].Z = null;
            //    SurveyStationList[i].Latitude = null;
            //    SurveyStationList[i].Longitude = null;
            //}
            if (!SurveyPoint.CompleteSurvey(SurveyStationList))
            {
                System.Console.WriteLine("incomplete survey");
                return false;
            }
            // Under the assumption that trajectory are run in an Euclidean space (at least so far),
            // the trajectory now needs to be translated to the absolute NED coordinates (=XYZ coordinates) of the slot they are connected to
            // Note that this slot represents the 0 in measured depth whether the wellbore hosting the trajectory is a sidetrack or not.
            if (slotCoordinate is { } p && p.Northing is { } x && p.Easting is { } y && p.VerticalDepth is { } z)
            {
                //foreach (var st in SurveyStationList)
                //{
                //    st.Translate(x, y, z);
                //    System.Console.WriteLine("sss");
                //}
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
