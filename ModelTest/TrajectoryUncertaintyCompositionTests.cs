using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Math;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

[TestFixture]
public sealed class TrajectoryUncertaintyCompositionTests
{
    [Test]
    public void Sidetrack_trajectory_preserves_parent_tie_in_and_survey_run_covariances()
    {
        SymmetricMatrix3x3 parentCovariance = DiagonalCovariance(4.0);
        SymmetricMatrix3x3 middleCovariance = DiagonalCovariance(5.0);
        SymmetricMatrix3x3 finalCovariance = DiagonalCovariance(6.0);
        TrajectoryModel trajectory = new()
        {
            MDStep = 30.0,
            TieInPoint = Station(0.0, 0.30, 0.40, 0.0, 0.0, 0.0, parentCovariance),
            SurveyStationList =
            [
                Station(0.0, 0.30, 0.40, 0.0, 0.0, 0.0, DiagonalCovariance(0.0)),
                Station(30.0, 0.32, 0.42, 28.0, 8.0, 3.0, middleCovariance),
                Station(60.0, 0.34, 0.44, 56.0, 17.0, 7.0, finalCovariance)
            ]
        };

        Assert.That(trajectory.Calculate(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(Trace(trajectory.SurveyStationList![0].Covariance!), Is.EqualTo(12.0));
            Assert.That(Trace(trajectory.SurveyStationList[1].Covariance!), Is.EqualTo(15.0));
            Assert.That(Trace(trajectory.SurveyStationList[2].Covariance!), Is.EqualTo(18.0));
        });
    }

    [Test]
    public void Matching_tie_in_does_not_erase_propagated_survey_run_covariance()
    {
        SymmetricMatrix3x3 propagatedCovariance = DiagonalCovariance(7.0);
        TrajectoryModel trajectory = new()
        {
            MDStep = 30.0,
            TieInPoint = Station(455.78, 0.30, 0.40, 400.0, 100.0, 20.0, DiagonalCovariance(0.0), 0.155575),
            SurveyStationList =
            [
                Station(455.78, 0.30, 0.40, 400.0, 100.0, 20.0, propagatedCovariance),
                Station(485.78, 0.32, 0.42, 428.0, 108.0, 23.0, DiagonalCovariance(8.0)),
                Station(515.78, 0.34, 0.44, 456.0, 117.0, 27.0, DiagonalCovariance(9.0))
            ]
        };

        Assert.That(trajectory.Calculate(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(Trace(trajectory.SurveyStationList![0].Covariance!), Is.EqualTo(21.0));
            Assert.That(trajectory.SurveyStationList[0].BoreholeRadius, Is.EqualTo(0.155575));
            Assert.That(Trace(trajectory.SurveyStationList[1].Covariance!), Is.EqualTo(24.0));
            Assert.That(Trace(trajectory.SurveyStationList[2].Covariance!), Is.EqualTo(27.0));
        });
    }

    private static SurveyStation Station(
        double md,
        double inclination,
        double azimuth,
        double tvd,
        double north,
        double east,
        SymmetricMatrix3x3 covariance,
        double boreholeRadius = 0.0) => new()
        {
            MD = md,
            Abscissa = md,
            Inclination = inclination,
            Azimuth = azimuth,
            TVD = tvd,
            RiemannianNorth = north,
            RiemannianEast = east,
            Covariance = covariance,
            BoreholeRadius = boreholeRadius
        };

    private static SymmetricMatrix3x3 DiagonalCovariance(double value)
    {
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = covariance[1, 1] = covariance[2, 2] = value;
        return covariance;
    }

    private static double Trace(SymmetricMatrix3x3 covariance) =>
        covariance[0, 0]!.Value + covariance[1, 1]!.Value + covariance[2, 2]!.Value;
}
