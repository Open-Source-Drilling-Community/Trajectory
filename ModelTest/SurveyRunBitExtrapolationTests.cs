using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.ModelTest;

[TestFixture]
public sealed class SurveyRunBitExtrapolationTests
{
    [Test]
    public void One_measurement_is_extrapolated_straight_to_the_bit()
    {
        SurveyInstrument surveyInstrument = new();
        SurveyRun run = Run(TrajectoryCalculationType.MinimumCurvatureMethod, 12.0,
            Measurement(100.0, Math.PI / 2.0, 0.0));
        run.TieInPoint = new SurveyStation
        {
            MD = 100.0, Inclination = Math.PI / 2.0, Azimuth = 0.0,
            RiemannianNorth = 10.0, RiemannianEast = 20.0, TVD = 30.0, VerticalSection = 0.0,
            SurveyTool = surveyInstrument
        };

        Assert.That(run.Calculate(), Is.True);
        Assert.That(run.SurveyStationList, Has.Count.EqualTo(2));
        SurveyStation bit = run.SurveyStationList![^1];
        Assert.Multiple(() =>
        {
            Assert.That(bit.MD, Is.EqualTo(112.0).Within(1e-10));
            Assert.That(bit.RiemannianNorth, Is.EqualTo(22.0).Within(1e-10));
            Assert.That(bit.RiemannianEast, Is.EqualTo(20.0).Within(1e-10));
            Assert.That(bit.TVD, Is.EqualTo(30.0).Within(1e-10));
            Assert.That(bit.Inclination, Is.EqualTo(Math.PI / 2.0).Within(1e-10));
            Assert.That(bit.Azimuth, Is.EqualTo(0.0).Within(1e-10));
            Assert.That(bit.SurveyTool, Is.SameAs(surveyInstrument));
        });
    }

    [TestCase(TrajectoryCalculationType.MinimumCurvatureMethod)]
    [TestCase(TrajectoryCalculationType.ConstantBuildAndTurnMethod)]
    [TestCase(TrajectoryCalculationType.ConstantCurvatureAndToolfaceMethod)]
    public void Two_measurements_continue_the_selected_last_curve(TrajectoryCalculationType method)
    {
        SurveyRun run = Run(method, 10.0,
            Measurement(0.0, 0.20, 0.30),
            Measurement(30.0, 0.26, 0.36));
        run.TieInPoint = new SurveyStation
        {
            MD = 0.0, Inclination = 0.20, Azimuth = 0.30,
            RiemannianNorth = 0.0, RiemannianEast = 0.0, TVD = 0.0, VerticalSection = 0.0
        };

        Assert.That(run.Calculate(), Is.True);
        Assert.That(run.SurveyStationList, Has.Count.EqualTo(3));
        SurveyStation bit = run.SurveyStationList![^1];
        Assert.Multiple(() =>
        {
            Assert.That(bit.MD, Is.EqualTo(40.0).Within(1e-8));
            Assert.That(bit.Inclination, Is.Not.Null);
            Assert.That(bit.Azimuth, Is.Not.Null);
            Assert.That(bit.RiemannianNorth, Is.Not.Null);
            Assert.That(bit.RiemannianEast, Is.Not.Null);
            Assert.That(bit.TVD, Is.Not.Null);
        });
    }

    [TestCase(TrajectoryCalculationType.MinimumCurvatureMethod)]
    [TestCase(TrajectoryCalculationType.ConstantBuildAndTurnMethod)]
    [TestCase(TrajectoryCalculationType.ConstantCurvatureAndToolfaceMethod)]
    public void Curved_terminal_extrapolation_inherits_the_last_station_survey_instrument(
        TrajectoryCalculationType method)
    {
        SurveyInstrument surveyInstrument = new();
        List<SurveyStation> stations =
        [
            new()
            {
                MD = 0.0, Inclination = 0.20, Azimuth = 0.30,
                RiemannianNorth = 0.0, RiemannianEast = 0.0, TVD = 0.0
            },
            new()
            {
                MD = 30.0, Inclination = 0.26, Azimuth = 0.36,
                RiemannianNorth = 6.0, RiemannianEast = 2.0, TVD = 29.0,
                SurveyTool = surveyInstrument
            }
        ];

        Assert.That(SurveyRunBitExtrapolationCalculator.TryAppendCalculatedStation(stations, method, 10.0), Is.True);
        Assert.That(stations[^1].SurveyTool, Is.SameAs(surveyInstrument));
    }

    [Test]
    public void Existing_terminal_extrapolation_must_be_last_and_match_the_distance()
    {
        SurveyRun run = new()
        {
            BitExtrapolation = new()
            {
                Mode = SurveyRunBitExtrapolationMode.LastStationAlreadyExtrapolated,
                MeasurementToolToBitDistance = 10.0
            },
            SurveyMeasurementList =
            [
                Measurement(100.0, 0.1, 0.2),
                new SurveyMeasurement { MD = 110.0, Inclination = 0.1, Azimuth = 0.2, Origin = SurveyMeasurementOrigin.Extrapolated }
            ]
        };

        Assert.That(SurveyRunBitExtrapolationValidation.Validate(run), Is.Empty);
        run.SurveyMeasurementList[^1].MD = 111.0;
        Assert.That(SurveyRunBitExtrapolationValidation.Validate(run),
            Does.Contain("The final MD minus the last measured MD must equal MeasurementToolToBitDistance."));
    }

    [Test]
    public void Calculated_mode_rejects_a_caller_supplied_extrapolated_row()
    {
        SurveyRun run = Run(TrajectoryCalculationType.MinimumCurvatureMethod, 10.0,
            Measurement(100.0, 0.1, 0.2));
        run.SurveyMeasurementList![0].Origin = SurveyMeasurementOrigin.Extrapolated;

        Assert.That(SurveyRunBitExtrapolationValidation.Validate(run),
            Does.Contain("CalculateFromLastMeasurement does not accept a caller-supplied extrapolated row."));
    }

    private static SurveyRun Run(TrajectoryCalculationType method, double distance, params SurveyMeasurement[] measurements) => new()
    {
        CalculationType = method,
        SurveyMeasurementList = measurements.ToList(),
        BitExtrapolation = new()
        {
            Mode = SurveyRunBitExtrapolationMode.CalculateFromLastMeasurement,
            MeasurementToolToBitDistance = distance
        }
    };

    private static SurveyMeasurement Measurement(double md, double inclination, double azimuth) => new()
    {
        MD = md,
        Inclination = inclination,
        Azimuth = azimuth
    };
}
