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

    [Test]
    public void Partial_wolff_de_wardt_covariance_without_history_is_rejected()
    {
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = 4.0;
        covariance[1, 1] = 9.0;
        covariance[2, 2] = 16.0;
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
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
        });
    }
}
