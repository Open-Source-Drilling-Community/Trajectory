using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Math;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class SurveyRunUncertaintyContinuationTests
{
    [Test]
    public void Wolff_de_wardt_child_run_continues_parent_transfer_matrix_across_local_md_restart()
    {
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            Misalignment = 0.01,
            RelDepthError = 0.001,
            TrueInclination = 0.005,
            ReferenceError = 0.01,
            DrillStringMag = 0.002
        };
        List<SurveyStation> parent =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, instrument),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, instrument),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument)
        ];
        List<SurveyStation> child =
        [
            Station(0.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument),
            Station(30.0, 0.35, 0.36, 85.0, 20.0, 6.0, instrument)
        ];

        Assert.That(SurveyRunManager.ContinueWolffDeWardtFromParentHistory(parent, child, instrument), Is.True);

        double parentTieInTrace = Trace(parent[2].Covariance!);
        Assert.Multiple(() =>
        {
            Assert.That(parentTieInTrace, Is.GreaterThan(0.0));
            Assert.That(Trace(child[0].Covariance!), Is.EqualTo(parentTieInTrace).Within(1e-12));
            Assert.That(Trace(child[1].Covariance!), Is.GreaterThan(parentTieInTrace));
            Assert.That(child[0].MD, Is.EqualTo(0.0), "The child run keeps its local sidetrack MD coordinate.");
        });
    }

    [Test]
    public void Ellipse_request_replaces_stale_covariance_from_authoritative_trajectory()
    {
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            Misalignment = 0.01,
            RelDepthError = 0.001,
            TrueInclination = 0.005,
            ReferenceError = 0.01,
            DrillStringMag = 0.002
        };
        List<SurveyStation> authoritative =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, instrument),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, instrument),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument)
        ];
        Assert.That(CovarianceCalculatorWolffDeWardt.Calculate(authoritative), Is.True);

        SurveyStation requested = Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument);
        requested.Covariance = DiagonalCovariance(999.0);
        SurveyStationEllipseCalculation calculation = new()
        {
            SurveyStationList = [requested]
        };

        Assert.That(
            SurveyStationEllipseCalculationManager.ApplyAuthoritativeUncertainty(
                calculation,
                authoritative,
                TrajectoryCalculationType.MinimumCurvatureMethod,
                "Trajectory"),
            Is.True);
        Assert.That(
            Trace(calculation.SurveyStationList![0].Covariance!),
            Is.EqualTo(Trace(authoritative[2].Covariance!)).Within(1e-12));
    }

    [Test]
    public void Ellipse_request_interpolates_authoritative_covariance_at_display_md()
    {
        List<SurveyStation> authoritative =
        [
            StationWithCovariance(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, 1.0),
            StationWithCovariance(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, 2.0),
            StationWithCovariance(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, 3.0)
        ];
        SurveyStationEllipseCalculation calculation = new()
        {
            SurveyStationList =
            [
                StationWithCovariance(45.0, 0.275, 0.33, 43.0, 8.5, 3.0, 999.0)
            ]
        };

        Assert.That(
            SurveyStationEllipseCalculationManager.ApplyAuthoritativeUncertainty(
                calculation,
                authoritative,
                TrajectoryCalculationType.MinimumCurvatureMethod,
                "Trajectory"),
            Is.True);
        Assert.That(Trace(calculation.SurveyStationList![0].Covariance!), Is.InRange(6.0, 9.0));
    }

    private static SurveyStation Station(
        double md,
        double inclination,
        double azimuth,
        double tvd,
        double north,
        double east,
        SurveyInstrument instrument) => new()
        {
            MD = md,
            Abscissa = md,
            Inclination = inclination,
            Azimuth = azimuth,
            TVD = tvd,
            RiemannianNorth = north,
            RiemannianEast = east,
            SurveyTool = instrument
        };

    private static SurveyStation StationWithCovariance(
        double md,
        double inclination,
        double azimuth,
        double tvd,
        double north,
        double east,
        double diagonalCovariance) => new()
        {
            MD = md,
            Abscissa = md,
            Inclination = inclination,
            Azimuth = azimuth,
            TVD = tvd,
            RiemannianNorth = north,
            RiemannianEast = east,
            Covariance = DiagonalCovariance(diagonalCovariance)
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
