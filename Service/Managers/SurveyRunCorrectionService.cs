using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.Service.Managers;

internal static class SurveyRunCorrectionService
{
    internal const double ConvergenceToleranceRadians = 1e-10;

    public static async Task<(bool Success, double MaximumAngularChange)> ApplyAsync(
        SurveyRun surveyRun,
        ILogger logger,
        CancellationToken cancellationToken = default,
        ISurveyReferenceModelClient? referenceModelClient = null)
    {
        if (surveyRun.SurveyMeasurementList is not { Count: > 0 } measurements ||
            surveyRun.SurveyStationList is not { Count: > 0 } stations)
        {
            return (false, double.PositiveInfinity);
        }

        List<Candidate> candidates = [];
        bool failed = false;
        foreach (SurveyMeasurement measurement in measurements.Where(value => value.Origin == SurveyMeasurementOrigin.Measured))
        {
            measurement.MeasurementID = measurement.MeasurementID == Guid.Empty ? Guid.NewGuid() : measurement.MeasurementID;
            double? observedInclination = measurement.ObservedInclination ?? measurement.Inclination;
            double? observedAzimuth = measurement.ObservedAzimuth ?? measurement.Azimuth;
            SurveyInclinationReference inclinationReference = measurement.InclinationReference == SurveyInclinationReference.InheritRun ? surveyRun.DefaultInclinationReference : measurement.InclinationReference;
            SurveyAzimuthReference azimuthReference = measurement.AzimuthReference == SurveyAzimuthReference.InheritRun ? surveyRun.DefaultAzimuthReference : measurement.AzimuthReference;

            if (observedInclination is not { } definedInclination || observedAzimuth is not { } definedAzimuth)
            {
                Fail(measurement, "Observed inclination and azimuth are required.");
                failed = true;
                continue;
            }

            measurement.ObservedInclination ??= definedInclination;
            measurement.ObservedAzimuth ??= definedAzimuth;
            if (measurement.Correction is { Source: SurveyCorrectionSource.ManualOverride, Status: SurveyCorrectionStatus.Completed } manual)
            {
                measurement.Inclination = definedInclination + (manual.AppliedInclinationCorrection ?? 0.0);
                measurement.Azimuth = SurveyReferenceCorrectionCalculator.NormalizeAzimuth(definedAzimuth + (manual.AppliedAzimuthCorrection ?? 0.0));
                continue;
            }
            if (inclinationReference == SurveyInclinationReference.GeodeticVertical &&
                azimuthReference == SurveyAzimuthReference.TrueNorth)
            {
                measurement.Inclination = definedInclination;
                measurement.Azimuth = SurveyReferenceCorrectionCalculator.NormalizeAzimuth(definedAzimuth);
                measurement.Correction = new SurveyMeasurementCorrection
                {
                    Source = SurveyCorrectionSource.None,
                    Status = SurveyCorrectionStatus.NotRequired,
                    TimeMethod = SurveyCorrectionTimeMethod.NotRequired,
                    AppliedInclinationCorrection = 0.0,
                    AppliedAzimuthCorrection = 0.0
                };
                continue;
            }

            SurveyStation? station = ResolveEvaluationStation(surveyRun, stations, measurement);
            if (station?.Latitude is not { } latitude || station.Longitude is not { } longitude ||
                (station.TVD ?? station.Z) is not { } depth || !double.IsFinite(latitude) ||
                !double.IsFinite(longitude) || !double.IsFinite(depth))
            {
                Fail(measurement, "A calculated WGS84 position and depth are required before correcting this measurement.");
                failed = true;
                continue;
            }

            DateTimeOffset? evaluationTime = null;
            SurveyCorrectionTimeMethod timeMethod = SurveyCorrectionTimeMethod.NotRequired;
            if (azimuthReference == SurveyAzimuthReference.MagneticNorth)
            {
                (evaluationTime, timeMethod) = ResolveEvaluationTime(surveyRun, measurement);
                if (evaluationTime == null)
                {
                    Fail(measurement, "Magnetic-north correction requires a station time or a complete survey-run acquisition interval.");
                    failed = true;
                    continue;
                }
            }

            candidates.Add(new Candidate(measurement, definedInclination, definedAzimuth, inclinationReference,
                azimuthReference, latitude, longitude, depth, evaluationTime, timeMethod));
        }

        if (candidates.Count == 0)
        {
            return (!failed, 0.0);
        }

        Dictionary<Guid, GravityResult> gravity = [];
        Dictionary<Guid, MagneticResult> magnetic = [];
        referenceModelClient ??= new SurveyReferenceModelClient();
        try
        {
            List<Candidate> gravityCandidates = candidates
                .Where(value => value.InclinationReference == SurveyInclinationReference.GravityVertical).ToList();
            foreach (Candidate[] chunk in gravityCandidates.Chunk(1000))
            {
                EarthGravityEvaluationResponse response = await referenceModelClient.EvaluateGravityAsync(
                    new EarthGravityEvaluationRequest
                    {
                        Positions = chunk.Select(value => new EarthGravityPosition
                        {
                            Latitude = value.Latitude,
                            Longitude = value.Longitude,
                            Depth = value.Depth
                        }).ToList()
                    }, cancellationToken);
                if (response.Samples == null || response.Samples.Count != chunk.Length)
                    throw new InvalidOperationException("Earth Gravity returned an unexpected sample count.");
                foreach ((Candidate candidate, EarthGravitySample sample) in chunk.Zip(response.Samples))
                    gravity[candidate.Measurement.MeasurementID] = new(sample.Gravity, response.Model);
            }

            List<Candidate> magneticCandidates = candidates
                .Where(value => value.AzimuthReference == SurveyAzimuthReference.MagneticNorth).ToList();
            foreach (IGrouping<EarthMagneticFieldModel, Candidate> group in magneticCandidates.GroupBy(value => SelectModel(surveyRun, value.EvaluationTime!.Value)))
            {
                foreach (Candidate[] chunk in group.Chunk(1000))
                {
                    EvaluateEarthMagneticFieldResponse response = await referenceModelClient.EvaluateMagneticFieldAsync(
                        new EvaluateEarthMagneticFieldRequest
                        {
                            Model = group.Key,
                            Samples = chunk.Select(value => new EarthMagneticFieldEvaluationPoint
                            {
                                Latitude = value.Latitude,
                                Longitude = value.Longitude,
                                Depth = value.Depth,
                                DateTimeUtc = value.EvaluationTime!.Value.ToUniversalTime()
                            }).ToList()
                        }, cancellationToken);
                    if (response.Samples == null || response.Samples.Count != chunk.Length)
                        throw new InvalidOperationException("Earth Magnetic Field returned an unexpected sample count.");
                    foreach ((Candidate candidate, EarthMagneticFieldSample sample) in chunk.Zip(response.Samples))
                        magnetic[candidate.Measurement.MeasurementID] = new(sample, response.Model);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Survey reference correction dependency evaluation failed for SurveyRun {SurveyRunId}", surveyRun.MetaInfo?.ID);
            foreach (Candidate candidate in candidates)
                Fail(candidate.Measurement, "Reference correction dependency is unavailable or returned invalid data.");
            return (false, double.PositiveInfinity);
        }

        double maximumChange = 0.0;
        foreach (Candidate candidate in candidates)
        {
            GravityResult? gravityResult = gravity.GetValueOrDefault(candidate.Measurement.MeasurementID);
            MagneticResult? magneticResult = magnetic.GetValueOrDefault(candidate.Measurement.MeasurementID);
            (double North, double East, double Down)? gravityVector = gravityResult == null
                ? null
                : (gravityResult.Vector.North, gravityResult.Vector.East, gravityResult.Vector.Down);
            (double North, double East, double Down)? magneticVector = magneticResult == null
                ? null
                : (magneticResult.Sample.North, magneticResult.Sample.East, magneticResult.Sample.Down);

            if (!SurveyReferenceCorrectionCalculator.TryCorrect(candidate.ObservedInclination, candidate.ObservedAzimuth,
                candidate.InclinationReference, candidate.AzimuthReference, gravityVector, magneticVector,
                out double correctedInclination, out double correctedAzimuth))
            {
                Fail(candidate.Measurement, "The observed direction could not be transformed into the geodetic frame.");
                failed = true;
                continue;
            }

            maximumChange = Math.Max(maximumChange, Math.Abs((candidate.Measurement.Inclination ?? correctedInclination) - correctedInclination));
            maximumChange = Math.Max(maximumChange, Math.Abs(SurveyReferenceCorrectionCalculator.ShortestSignedAngle(
                (candidate.Measurement.Azimuth ?? correctedAzimuth) - correctedAzimuth)));
            candidate.Measurement.Inclination = correctedInclination;
            candidate.Measurement.Azimuth = correctedAzimuth;
            candidate.Measurement.Correction = new SurveyMeasurementCorrection
            {
                Source = SurveyCorrectionSource.Computed,
                Status = SurveyCorrectionStatus.Completed,
                AppliedInclinationCorrection = correctedInclination - candidate.ObservedInclination,
                AppliedAzimuthCorrection = SurveyReferenceCorrectionCalculator.ShortestSignedAngle(correctedAzimuth - candidate.ObservedAzimuth),
                MagneticDeclination = magneticResult?.Sample.Declination,
                GravityNorth = gravityResult?.Vector.North,
                GravityEast = gravityResult?.Vector.East,
                GravityDown = gravityResult?.Vector.Down,
                EvaluatedLatitude = candidate.Latitude,
                EvaluatedLongitude = candidate.Longitude,
                EvaluatedDepthWgs84 = candidate.Depth,
                EvaluationTimeUtc = candidate.EvaluationTime?.ToUniversalTime(),
                TimeMethod = candidate.TimeMethod,
                GravityModelID = gravityResult?.Model.ID,
                GravityModelVersion = gravityResult?.Model.DataVersion,
                GravityCoefficientSHA256 = gravityResult?.Model.CoefficientSHA256,
                GeomagneticModelID = magneticResult?.Model.ID,
                GeomagneticMetadataSHA256 = magneticResult?.Model.MetadataSHA256,
                GeomagneticCoefficientSHA256 = magneticResult?.Model.CoefficientSHA256,
                AlgorithmVersion = "1"
            };
        }

        return (!failed, maximumChange);
    }

    private static (DateTimeOffset?, SurveyCorrectionTimeMethod) ResolveEvaluationTime(SurveyRun run, SurveyMeasurement measurement)
    {
        if (measurement.MeasurementTimeUtc is { } stationTime)
            return (stationTime.ToUniversalTime(), SurveyCorrectionTimeMethod.StationMeasurementTime);
        if (run.AcquisitionStartUtc is { } start && run.AcquisitionEndUtc is { } end && end >= start)
            return ((start + TimeSpan.FromTicks((end - start).Ticks / 2)).ToUniversalTime(), SurveyCorrectionTimeMethod.RunAcquisitionMidpoint);
        return (null, SurveyCorrectionTimeMethod.NotRequired);
    }

    private static SurveyStation? ResolveEvaluationStation(
        SurveyRun surveyRun,
        IEnumerable<SurveyStation> stations,
        SurveyMeasurement measurement)
    {
        SurveyStation? station = stations.FirstOrDefault(value =>
            (value.MD ?? value.Abscissa) is { } stationMd && measurement.MD is { } measurementMd &&
            Math.Abs(stationMd - measurementMd) <= 1e-8);
        if (station != null)
        {
            return station;
        }

        // Measurements immediately above a resolved tie-in can be retained to provide
        // directional context, although the calculated survey begins at the tie-in and
        // therefore has no station at those earlier MDs. Earth reference fields vary
        // negligibly over that short interval, so evaluate them at the authoritative
        // tie-in position rather than rejecting the complete survey run.
        if (measurement.MD is { } md &&
            surveyRun.TieInPoint is { } tieIn &&
            (tieIn.MD ?? tieIn.Abscissa) is { } tieInMd &&
            md < tieInMd)
        {
            return tieIn;
        }

        return null;
    }

    private static EarthMagneticFieldModel SelectModel(SurveyRun run, DateTimeOffset time) => run.GeomagneticModel switch
    {
        SurveyGeomagneticModel.WMM2025 => EarthMagneticFieldModel.WMM2025,
        SurveyGeomagneticModel.IGRF14 => EarthMagneticFieldModel.IGRF14,
        _ when time >= new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) => EarthMagneticFieldModel.WMM2025,
        _ => EarthMagneticFieldModel.IGRF14
    };

    private static void Fail(SurveyMeasurement measurement, string message) => measurement.Correction = new SurveyMeasurementCorrection
    {
        Source = SurveyCorrectionSource.Computed,
        Status = SurveyCorrectionStatus.Failed,
        Message = message
    };

    private sealed record Candidate(SurveyMeasurement Measurement, double ObservedInclination, double ObservedAzimuth,
        SurveyInclinationReference InclinationReference, SurveyAzimuthReference AzimuthReference,
        double Latitude, double Longitude, double Depth, DateTimeOffset? EvaluationTime, SurveyCorrectionTimeMethod TimeMethod);
    private sealed record GravityResult(EarthGravityVector Vector, EarthGravityModelInfo Model);
    private sealed record MagneticResult(EarthMagneticFieldSample Sample, EarthMagneticModelInfo Model);
}

internal interface ISurveyReferenceModelClient
{
    Task<EarthGravityEvaluationResponse> EvaluateGravityAsync(
        EarthGravityEvaluationRequest request, CancellationToken cancellationToken);

    Task<EvaluateEarthMagneticFieldResponse> EvaluateMagneticFieldAsync(
        EvaluateEarthMagneticFieldRequest request, CancellationToken cancellationToken);
}

internal sealed class SurveyReferenceModelClient : ISurveyReferenceModelClient
{
    public Task<EarthGravityEvaluationResponse> EvaluateGravityAsync(
        EarthGravityEvaluationRequest request, CancellationToken cancellationToken) =>
        APIUtils.ClientEarthGravity.EvaluateEarthGravityAsync(request, cancellationToken);

    public Task<EvaluateEarthMagneticFieldResponse> EvaluateMagneticFieldAsync(
        EvaluateEarthMagneticFieldRequest request, CancellationToken cancellationToken) =>
        APIUtils.ClientEarthMagneticField.EvaluateEarthMagneticFieldAsync(request, cancellationToken);
}
