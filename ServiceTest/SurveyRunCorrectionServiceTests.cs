using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.ModelShared;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class SurveyRunCorrectionServiceTests
{
    [Test]
    public async Task Canonical_references_require_no_dependency_evaluation()
    {
        SurveyRun run = CreateRun(SurveyInclinationReference.GeodeticVertical, SurveyAzimuthReference.TrueNorth);
        var client = new StubReferenceModelClient();

        (bool success, _) = await SurveyRunCorrectionService.ApplyAsync(run, NullLogger.Instance, referenceModelClient: client);

        SurveyMeasurement measurement = run.SurveyMeasurementList!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(client.GravityRequests, Is.Empty);
            Assert.That(client.MagneticRequests, Is.Empty);
            Assert.That(measurement.Inclination, Is.EqualTo(0.4));
            Assert.That(measurement.Azimuth, Is.EqualTo(0.3));
            Assert.That(measurement.Correction!.Status, Is.EqualTo(SurveyCorrectionStatus.NotRequired));
        });
    }

    [Test]
    public async Task Gravity_and_magnetic_references_are_evaluated_and_provenance_is_frozen()
    {
        SurveyRun run = CreateRun(SurveyInclinationReference.GravityVertical, SurveyAzimuthReference.MagneticNorth);
        run.AcquisitionStartUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        run.AcquisitionEndUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var client = new StubReferenceModelClient
        {
            GravityResponse = new EarthGravityEvaluationResponse
            {
                Model = new EarthGravityModelInfo { ID = "EGM96", DataVersion = "v1", CoefficientSHA256 = "gravity-sha" },
                Samples = new List<EarthGravitySample>
                {
                    new() { Gravity = new EarthGravityVector { North = 0.1, East = 0.0, Down = 9.8 } }
                }
            },
            MagneticResponse = new EvaluateEarthMagneticFieldResponse
            {
                Model = new EarthMagneticModelInfo { ID = "WMM2025", MetadataSHA256 = "metadata-sha", CoefficientSHA256 = "magnetic-sha" },
                Samples = new List<EarthMagneticFieldSample>
                {
                    new() { North = 20e-6, East = 5e-6, Down = 40e-6, Declination = Math.Atan2(5.0, 20.0) }
                }
            }
        };

        (bool success, double maximumChange) = await SurveyRunCorrectionService.ApplyAsync(
            run, NullLogger.Instance, referenceModelClient: client);

        SurveyMeasurementCorrection correction = run.SurveyMeasurementList!.Single().Correction!;
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(maximumChange, Is.GreaterThan(0.0));
            Assert.That(client.GravityRequests, Has.Count.EqualTo(1));
            Assert.That(client.MagneticRequests, Has.Count.EqualTo(1));
            Assert.That(client.MagneticRequests[0].Model, Is.EqualTo(EarthMagneticFieldModel.WMM2025));
            Assert.That(client.MagneticRequests[0].Samples.Single().DateTimeUtc,
                Is.EqualTo(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(correction.Status, Is.EqualTo(SurveyCorrectionStatus.Completed));
            Assert.That(correction.TimeMethod, Is.EqualTo(SurveyCorrectionTimeMethod.RunAcquisitionMidpoint));
            Assert.That(correction.GravityModelID, Is.EqualTo("EGM96"));
            Assert.That(correction.GeomagneticModelID, Is.EqualTo("WMM2025"));
            Assert.That(correction.EvaluatedDepthWgs84, Is.EqualTo(1200.0));
        });
    }

    [Test]
    public async Task Magnetic_reference_without_time_fails_before_dependency_call()
    {
        SurveyRun run = CreateRun(SurveyInclinationReference.GeodeticVertical, SurveyAzimuthReference.MagneticNorth);
        var client = new StubReferenceModelClient();

        (bool success, _) = await SurveyRunCorrectionService.ApplyAsync(run, NullLogger.Instance, referenceModelClient: client);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(client.MagneticRequests, Is.Empty);
            Assert.That(run.SurveyMeasurementList!.Single().Correction!.Status, Is.EqualTo(SurveyCorrectionStatus.Failed));
            Assert.That(run.SurveyMeasurementList!.Single().Correction!.Message, Does.Contain("requires a station time"));
        });
    }

    [Test]
    public async Task Dependency_exception_returns_sanitized_failed_correction()
    {
        SurveyRun run = CreateRun(SurveyInclinationReference.GravityVertical, SurveyAzimuthReference.TrueNorth);
        var client = new StubReferenceModelClient { Exception = new InvalidOperationException("secret upstream detail") };

        (bool success, _) = await SurveyRunCorrectionService.ApplyAsync(run, NullLogger.Instance, referenceModelClient: client);

        SurveyMeasurementCorrection correction = run.SurveyMeasurementList!.Single().Correction!;
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(correction.Status, Is.EqualTo(SurveyCorrectionStatus.Failed));
            Assert.That(correction.Message, Does.Not.Contain("secret"));
            Assert.That(correction.Message, Does.Contain("unavailable"));
        });
    }

    [Test]
    public async Task Measurement_above_tie_in_uses_tie_in_position_for_reference_evaluation()
    {
        SurveyRun run = CreateRun(SurveyInclinationReference.GravityVertical, SurveyAzimuthReference.MagneticNorth);
        SurveyMeasurement measurement = run.SurveyMeasurementList!.Single();
        measurement.MD = -99.22;
        measurement.MeasurementTimeUtc = new DateTimeOffset(1962, 4, 30, 0, 0, 0, TimeSpan.Zero);
        run.TieInPoint = new SurveyStation
        {
            MD = -91.2,
            Latitude = 1.0285845008237529,
            Longitude = 0.09961438594882861,
            TVD = -91.2
        };
        run.SurveyStationList =
        [
            new SurveyStation { MD = -91.2, Latitude = 1.0285845008237529, Longitude = 0.09961438594882861, TVD = -91.2 },
            new SurveyStation { MD = -49.22, Latitude = 1.0286, Longitude = 0.0997, TVD = -49.3 }
        ];
        var client = new StubReferenceModelClient
        {
            GravityResponse = new EarthGravityEvaluationResponse
            {
                Model = new EarthGravityModelInfo { ID = "EGM96", DataVersion = "v1", CoefficientSHA256 = "gravity-sha" },
                Samples =
                [
                    new EarthGravitySample { Gravity = new EarthGravityVector { North = 0.0, East = 0.0, Down = 9.81 } }
                ]
            },
            MagneticResponse = new EvaluateEarthMagneticFieldResponse
            {
                Model = new EarthMagneticModelInfo { ID = "IGRF14-A", MetadataSHA256 = "metadata-sha", CoefficientSHA256 = "magnetic-sha" },
                Samples =
                [
                    new EarthMagneticFieldSample { North = 20e-6, East = 5e-6, Down = 40e-6, Declination = Math.Atan2(5.0, 20.0) }
                ]
            }
        };

        (bool success, _) = await SurveyRunCorrectionService.ApplyAsync(
            run, NullLogger.Instance, referenceModelClient: client);

        EarthGravityPosition evaluatedPosition = client.GravityRequests.Single().Positions.Single();
        EarthMagneticFieldEvaluationPoint magneticPosition = client.MagneticRequests.Single().Samples.Single();
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(evaluatedPosition.Latitude, Is.EqualTo(run.TieInPoint.Latitude));
            Assert.That(evaluatedPosition.Longitude, Is.EqualTo(run.TieInPoint.Longitude));
            Assert.That(evaluatedPosition.Depth, Is.EqualTo(run.TieInPoint.TVD));
            Assert.That(client.MagneticRequests.Single().Model, Is.EqualTo(EarthMagneticFieldModel.IGRF14));
            Assert.That(magneticPosition.Latitude, Is.EqualTo(run.TieInPoint.Latitude));
            Assert.That(magneticPosition.Longitude, Is.EqualTo(run.TieInPoint.Longitude));
            Assert.That(magneticPosition.Depth, Is.EqualTo(run.TieInPoint.TVD));
            Assert.That(magneticPosition.DateTimeUtc, Is.EqualTo(measurement.MeasurementTimeUtc));
            Assert.That(measurement.Correction!.Status, Is.EqualTo(SurveyCorrectionStatus.Completed));
            Assert.That(measurement.Correction.EvaluatedDepthWgs84, Is.EqualTo(-91.2));
        });
    }

    private static SurveyRun CreateRun(SurveyInclinationReference inclinationReference, SurveyAzimuthReference azimuthReference)
    {
        return new SurveyRun
        {
            DefaultInclinationReference = inclinationReference,
            DefaultAzimuthReference = azimuthReference,
            SurveyMeasurementList =
            [
                new SurveyMeasurement
                {
                    MeasurementID = Guid.NewGuid(), MD = 100.0,
                    Inclination = 0.4, Azimuth = 0.3,
                    ObservedInclination = 0.4, ObservedAzimuth = 0.3
                }
            ],
            SurveyStationList =
            [
                new SurveyStation { MD = 100.0, Latitude = 0.9, Longitude = 0.1, TVD = 1200.0 }
            ]
        };
    }

    private sealed class StubReferenceModelClient : ISurveyReferenceModelClient
    {
        public List<EarthGravityEvaluationRequest> GravityRequests { get; } = [];
        public List<EvaluateEarthMagneticFieldRequest> MagneticRequests { get; } = [];
        public EarthGravityEvaluationResponse GravityResponse { get; init; } = new();
        public EvaluateEarthMagneticFieldResponse MagneticResponse { get; init; } = new();
        public Exception? Exception { get; init; }

        public Task<EarthGravityEvaluationResponse> EvaluateGravityAsync(
            EarthGravityEvaluationRequest request, CancellationToken cancellationToken)
        {
            GravityRequests.Add(request);
            return Exception == null
                ? Task.FromResult(GravityResponse)
                : Task.FromException<EarthGravityEvaluationResponse>(Exception);
        }

        public Task<EvaluateEarthMagneticFieldResponse> EvaluateMagneticFieldAsync(
            EvaluateEarthMagneticFieldRequest request, CancellationToken cancellationToken)
        {
            MagneticRequests.Add(request);
            return Exception == null
                ? Task.FromResult(MagneticResponse)
                : Task.FromException<EvaluateEarthMagneticFieldResponse>(Exception);
        }
    }
}
