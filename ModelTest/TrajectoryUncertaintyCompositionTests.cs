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

    private static SurveyStation Station(
        double md,
        double inclination,
        double azimuth,
        double tvd,
        double north,
        double east,
        SymmetricMatrix3x3 covariance) => new()
        {
            MD = md,
            Abscissa = md,
            Inclination = inclination,
            Azimuth = azimuth,
            TVD = tvd,
            RiemannianNorth = north,
            RiemannianEast = east,
            Covariance = covariance
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
