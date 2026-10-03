using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Math;

namespace OSDC.Drilling.Trajectory.ModelTest;

[TestFixture]
public sealed class SurveyStationEllipseCalculationTests
{
    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(0.9990001)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void Confidence_factor_outside_supported_interval_is_rejected(double confidenceFactor)
    {
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = covariance[1, 1] = covariance[2, 2] = 1.0;
        SurveyStationEllipseCalculation calculation = new()
        {
            ConfidenceFactor = confidenceFactor,
            SurveyStationList =
            [
                new SurveyStation
                {
                    MD = 0.0,
                    Inclination = 0.0,
                    Azimuth = 0.0,
                    RiemannianNorth = 0.0,
                    RiemannianEast = 0.0,
                    TVD = 0.0,
                    Covariance = covariance
                }
            ]
        };

        Assert.That(calculation.Calculate(), Is.False);
        Assert.That(calculation.CalculationMessage, Does.Contain("no greater than 0.999"));
    }

    [Test]
    public void Maximum_supported_confidence_factor_is_accepted()
    {
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = covariance[1, 1] = covariance[2, 2] = 1.0;
        SurveyStation station = new()
        {
            MD = 0.0,
            Inclination = 0.5,
            Azimuth = 0.3,
            RiemannianNorth = 0.0,
            RiemannianEast = 0.0,
            TVD = 0.0,
            Covariance = covariance
        };
        station.CalculateEigenProperties();
        SurveyStationEllipseCalculation calculation = new()
        {
            ConfidenceFactor = SurveyStationEllipseCalculation.MaximumConfidenceFactor,
            SurveyStationList = [station]
        };

        Assert.That(calculation.Calculate(), Is.False);
        Assert.That(calculation.CalculationMessage,
            Is.EqualTo("No uncertainty result could be calculated from the survey station covariance matrices."),
            "The boundary value must pass confidence validation even when the minimal fixture cannot form an ellipse.");
    }

    [TestCase(SurveyInstrumentModelType.MWD_WolffDeWardt, "Wolff-de Wardt")]
    [TestCase(SurveyInstrumentModelType.MWD_ISCWSA, "ISCWSA")]
    public void Partial_stateful_covariance_without_history_is_rejected(
        SurveyInstrumentModelType modelType,
        string modelName)
    {
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = 4.0;
        covariance[1, 1] = 9.0;
        covariance[2, 2] = 16.0;
        SurveyInstrument instrument = new()
        {
            ModelType = modelType,
            Misalignment = 0.01,
            RelDepthError = 0.001
        };
        SurveyStation start = new()
        {
            MD = 100.0,
            Inclination = 0.5,
            Azimuth = 0.3,
            RiemannianNorth = 40.0,
            RiemannianEast = 12.0,
            TVD = 90.0,
            Covariance = covariance,
            SurveyTool = instrument
        };
        start.CalculateEigenProperties();
        SurveyStation end = new()
        {
            MD = 130.0,
            Inclination = 0.5,
            Azimuth = 0.3,
            RiemannianNorth = 52.0,
            RiemannianEast = 16.0,
            TVD = 116.0
        };
        SurveyStationEllipseCalculation calculation = new()
        {
            ConfidenceFactor = 0.95,
            SurveyStationList = [start, end]
        };

        Assert.That(calculation.Calculate(), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(end.SurveyTool, Is.SameAs(instrument));
            Assert.That(end.Covariance, Is.Null);
            Assert.That(calculation.CalculationMessage, Does.Contain("cannot be continued from covariance alone"));
            Assert.That(calculation.CalculationMessage, Does.Contain(modelName));
        });
    }

    [Test]
    public void Vertical_ellipses_use_one_stable_vertical_section_curtain()
    {
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = 36.0;
        covariance[1, 1] = 4.0;
        covariance[2, 2] = 9.0;
        covariance[0, 1] = 0.0;
        covariance[0, 2] = 0.0;
        covariance[1, 2] = 0.0;
        List<SurveyStation> stations =
        [
            CreateStation(0.0, 0.0),
            CreateStation(10.0, Math.PI / 2.0),
            CreateStation(20.0, Math.PI)
        ];
        SurveyStationEllipseCalculation calculation = new()
        {
            ConfidenceFactor = 0.95,
            SurveyStationList = stations
        };

        Assert.That(calculation.Calculate(), Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SurveyStationEllipseResultList, Has.Count.EqualTo(3));
        double[] verticalSemiMajorAxes = calculation.SurveyStationEllipseResultList!
            .Select(result => result.VerticalEllipse!.SemiMajorAxis!.Value)
            .ToArray();
        Assert.That(verticalSemiMajorAxes, Is.All.EqualTo(verticalSemiMajorAxes[0]).Within(1e-10),
            "Changing station azimuth near vertical must not rotate the vertical projection plane.");

        SurveyStation CreateStation(double north, double azimuth) => new()
        {
            MD = north,
            Inclination = 0.01,
            Azimuth = azimuth,
            RiemannianNorth = north,
            RiemannianEast = 0.0,
            TVD = north,
            VerticalSection = north,
            Covariance = covariance
        };
    }
}
