using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service;

public interface ITrajectoryExternalReferenceValidator
{
    Task<IReadOnlyList<TrajectoryExternalReferenceValidation>> ValidateTrajectoriesAsync(
        IReadOnlyCollection<TrajectoryLight> trajectories, CancellationToken cancellationToken);

    Task<IReadOnlyList<SurveyRunExternalReferenceValidation>> ValidateSurveyRunsAsync(
        IReadOnlyCollection<SurveyRunLight> surveyRuns, CancellationToken cancellationToken);
}

internal sealed class UnavailableTrajectoryExternalReferenceValidator : ITrajectoryExternalReferenceValidator
{
    public Task<IReadOnlyList<TrajectoryExternalReferenceValidation>> ValidateTrajectoriesAsync(
        IReadOnlyCollection<TrajectoryLight> trajectories, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<IReadOnlyList<TrajectoryExternalReferenceValidation>>(trajectories.Select(value => new TrajectoryExternalReferenceValidation
        {
            TrajectoryID = value.MetaInfo?.ID ?? Guid.Empty,
            FieldID = value.FieldID,
            ClusterID = value.ClusterID,
            WellID = value.WellID,
            WellBoreID = value.WellBoreID,
            CheckedAtUtc = checkedAt,
            Status = ExternalReferenceValidationStatus.Unavailable,
            Issues = [UnavailableIssue("FieldID/ClusterID/WellID/WellBoreID")]
        }).ToList());
    }

    public Task<IReadOnlyList<SurveyRunExternalReferenceValidation>> ValidateSurveyRunsAsync(
        IReadOnlyCollection<SurveyRunLight> surveyRuns, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<IReadOnlyList<SurveyRunExternalReferenceValidation>>(surveyRuns.Select(value => new SurveyRunExternalReferenceValidation
        {
            SurveyRunID = value.MetaInfo?.ID ?? Guid.Empty,
            FieldID = value.FieldID,
            ClusterID = value.ClusterID,
            WellID = value.WellID,
            WellBoreID = value.WellBoreID,
            SurveyInstrumentID = value.SurveyInstrumentID,
            CheckedAtUtc = checkedAt,
            Status = ExternalReferenceValidationStatus.Unavailable,
            Issues = [UnavailableIssue("FieldID/ClusterID/WellID/WellBoreID/SurveyInstrumentID")]
        }).ToList());
    }

    private static ExternalReferenceIssue UnavailableIssue(string property) => new()
    {
        Property = property,
        Code = "external_reference_validation_unavailable",
        Message = "External reference validation is unavailable in this host."
    };
}

/// <summary>
/// Reads externally owned resources for diagnostics only. Dependency failures are reported as
/// unavailable checks and never as evidence that a reference is invalid.
/// </summary>
public sealed class TrajectoryExternalReferenceValidator : ITrajectoryExternalReferenceValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;

    public TrajectoryExternalReferenceValidator(IHttpClientFactory clients, IConfiguration configuration)
    {
        _clients = clients;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<TrajectoryExternalReferenceValidation>> ValidateTrajectoriesAsync(
        IReadOnlyCollection<TrajectoryLight> trajectories, CancellationToken cancellationToken)
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        Task<Dictionary<Guid, ReferenceResolution>> fieldTask = ResolveDistinctAsync(
            OptionalIds(trajectories.Select(value => value.FieldID)), "FieldHostURL", "Field/api/Field", "field", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> clusterTask = ResolveDistinctAsync(
            OptionalIds(trajectories.Select(value => value.ClusterID)), "ClusterHostURL", "Cluster/api/Cluster", "cluster", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> wellTask = ResolveDistinctAsync(
            OptionalIds(trajectories.Select(value => value.WellID)), "WellHostURL", "Well/api/Well", "well", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> wellBoreTask = ResolveDistinctAsync(
            RequiredIds(trajectories.Select(value => value.WellBoreID)), "WellBoreHostURL", "WellBore/api/WellBore", "wellbore", cancellationToken);
        await Task.WhenAll(fieldTask, clusterTask, wellTask, wellBoreTask);

        return trajectories.Select(value => ValidateTrajectory(value, checkedAt,
            fieldTask.Result, clusterTask.Result, wellTask.Result, wellBoreTask.Result)).ToList();
    }

    public async Task<IReadOnlyList<SurveyRunExternalReferenceValidation>> ValidateSurveyRunsAsync(
        IReadOnlyCollection<SurveyRunLight> surveyRuns, CancellationToken cancellationToken)
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        Task<Dictionary<Guid, ReferenceResolution>> fieldTask = ResolveDistinctAsync(
            OptionalIds(surveyRuns.Select(value => value.FieldID)), "FieldHostURL", "Field/api/Field", "field", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> clusterTask = ResolveDistinctAsync(
            OptionalIds(surveyRuns.Select(value => value.ClusterID)), "ClusterHostURL", "Cluster/api/Cluster", "cluster", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> wellTask = ResolveDistinctAsync(
            OptionalIds(surveyRuns.Select(value => value.WellID)), "WellHostURL", "Well/api/Well", "well", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> wellBoreTask = ResolveDistinctAsync(
            RequiredIds(surveyRuns.Select(value => value.WellBoreID)), "WellBoreHostURL", "WellBore/api/WellBore", "wellbore", cancellationToken);
        Task<Dictionary<Guid, ReferenceResolution>> instrumentTask = ResolveDistinctAsync(
            RequiredIds(surveyRuns.Select(value => value.SurveyInstrumentID)), "SurveyInstrumentHostURL", "SurveyInstrument/api/SurveyInstrument", "survey_instrument", cancellationToken);
        await Task.WhenAll(fieldTask, clusterTask, wellTask, wellBoreTask, instrumentTask);

        return surveyRuns.Select(value => ValidateSurveyRun(value, checkedAt,
            fieldTask.Result, clusterTask.Result, wellTask.Result, wellBoreTask.Result, instrumentTask.Result)).ToList();
    }

    private async Task<Dictionary<Guid, ReferenceResolution>> ResolveDistinctAsync(IEnumerable<Guid> identifiers,
        string configurationKey, string endpoint, string resourceName, CancellationToken cancellationToken)
    {
        Dictionary<Guid, ReferenceResolution> results = [];
        Guid[] orderedIds = identifiers.Distinct().Order().ToArray();
        for (int index = 0; index < orderedIds.Length; index++)
        {
            Guid id = orderedIds[index];
            ReferenceResolution resolution = await ReadAsync(id, configurationKey, endpoint, resourceName, cancellationToken);
            results[id] = resolution;
            if (!resolution.IsUnavailable) continue;

            // A service/configuration failure applies to the remaining references in this bounded page.
            // Avoid repeatedly waiting on a dependency that has already proved unavailable.
            for (int remaining = index + 1; remaining < orderedIds.Length; remaining++)
                results[orderedIds[remaining]] = resolution;
            break;
        }
        return results;
    }

    private async Task<ReferenceResolution> ReadAsync(Guid id, string configurationKey, string endpoint,
        string resourceName, CancellationToken cancellationToken)
    {
        string? host = _configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(host))
            return ReferenceResolution.Unavailable($"{resourceName}_service_not_configured", $"{configurationKey} is not configured.");
        try
        {
            using HttpClient client = _clients.CreateClient(nameof(TrajectoryExternalReferenceValidator));
            client.BaseAddress = new Uri(host.EndsWith('/') ? host : host + "/");
            using HttpResponseMessage response = await client.GetAsync($"{endpoint}/{id:D}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return ReferenceResolution.NotFound();
            if (!response.IsSuccessStatusCode)
                return ReferenceResolution.Unavailable($"{resourceName}_service_error",
                    $"{Title(resourceName)} service returned HTTP {(int)response.StatusCode}.");
            ExternalResourceDto? resource = await response.Content.ReadFromJsonAsync<ExternalResourceDto>(JsonOptions, cancellationToken);
            return resource?.MetaInfo?.ID == id
                ? ReferenceResolution.Found()
                : ReferenceResolution.Unavailable($"{resourceName}_response_invalid",
                    $"{Title(resourceName)} service returned a malformed or mismatched resource.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ReferenceResolution.Unavailable($"{resourceName}_service_unavailable",
                $"{Title(resourceName)} reference validation is temporarily unavailable.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or UriFormatException)
        {
            return ReferenceResolution.Unavailable($"{resourceName}_service_unavailable",
                $"{Title(resourceName)} reference validation is temporarily unavailable.");
        }
    }

    private static TrajectoryExternalReferenceValidation ValidateTrajectory(TrajectoryLight value, DateTimeOffset checkedAt,
        IReadOnlyDictionary<Guid, ReferenceResolution> fields, IReadOnlyDictionary<Guid, ReferenceResolution> clusters,
        IReadOnlyDictionary<Guid, ReferenceResolution> wells, IReadOnlyDictionary<Guid, ReferenceResolution> wellBores)
    {
        var result = new TrajectoryExternalReferenceValidation
        {
            TrajectoryID = value.MetaInfo?.ID ?? Guid.Empty,
            FieldID = value.FieldID,
            ClusterID = value.ClusterID,
            WellID = value.WellID,
            WellBoreID = value.WellBoreID,
            CheckedAtUtc = checkedAt,
            Status = ExternalReferenceValidationStatus.Valid
        };
        ValidateOptional(result, "FieldID", "field", value.FieldID, fields, exists => result.FieldExists = exists);
        ValidateOptional(result, "ClusterID", "cluster", value.ClusterID, clusters, exists => result.ClusterExists = exists);
        ValidateOptional(result, "WellID", "well", value.WellID, wells, exists => result.WellExists = exists);
        ValidateRequired(result, "WellBoreID", "wellbore", value.WellBoreID, wellBores, exists => result.WellBoreExists = exists);
        return result;
    }

    private static SurveyRunExternalReferenceValidation ValidateSurveyRun(SurveyRunLight value, DateTimeOffset checkedAt,
        IReadOnlyDictionary<Guid, ReferenceResolution> fields, IReadOnlyDictionary<Guid, ReferenceResolution> clusters,
        IReadOnlyDictionary<Guid, ReferenceResolution> wells, IReadOnlyDictionary<Guid, ReferenceResolution> wellBores,
        IReadOnlyDictionary<Guid, ReferenceResolution> instruments)
    {
        var result = new SurveyRunExternalReferenceValidation
        {
            SurveyRunID = value.MetaInfo?.ID ?? Guid.Empty,
            FieldID = value.FieldID,
            ClusterID = value.ClusterID,
            WellID = value.WellID,
            WellBoreID = value.WellBoreID,
            SurveyInstrumentID = value.SurveyInstrumentID,
            CheckedAtUtc = checkedAt,
            Status = ExternalReferenceValidationStatus.Valid
        };
        ValidateOptional(result, "FieldID", "field", value.FieldID, fields, exists => result.FieldExists = exists);
        ValidateOptional(result, "ClusterID", "cluster", value.ClusterID, clusters, exists => result.ClusterExists = exists);
        ValidateOptional(result, "WellID", "well", value.WellID, wells, exists => result.WellExists = exists);
        ValidateRequired(result, "WellBoreID", "wellbore", value.WellBoreID, wellBores, exists => result.WellBoreExists = exists);
        ValidateRequired(result, "SurveyInstrumentID", "survey_instrument", value.SurveyInstrumentID, instruments, exists => result.SurveyInstrumentExists = exists);
        return result;
    }

    private static void ValidateOptional(object result, string property, string resourceName, Guid? id,
        IReadOnlyDictionary<Guid, ReferenceResolution> resolutions, Action<bool?> setExists)
    {
        if (id is null) return;
        ValidateRequired(result, property, resourceName, id.Value, resolutions, setExists);
    }

    private static void ValidateRequired(object result, string property, string resourceName, Guid id,
        IReadOnlyDictionary<Guid, ReferenceResolution> resolutions, Action<bool?> setExists)
    {
        if (id == Guid.Empty)
        {
            AddInvalid(result, property, "empty_uuid", $"{property} is empty.");
            return;
        }
        if (!resolutions.TryGetValue(id, out ReferenceResolution? resolution) || resolution.IsUnavailable)
        {
            AddUnavailable(result, new ExternalReferenceIssue
            {
                Property = property,
                Code = resolution?.Code ?? $"{resourceName}_service_unavailable",
                Message = resolution?.Message ?? $"{Title(resourceName)} reference validation is unavailable."
            });
            return;
        }
        setExists(resolution.Exists);
        if (!resolution.Exists)
            AddInvalid(result, property, $"{resourceName}_not_found", $"{Title(resourceName)} UUID '{id}' does not exist.");
    }

    private static void AddUnavailable(object result, ExternalReferenceIssue issue)
    {
        if (result is TrajectoryExternalReferenceValidation trajectory)
        {
            if (trajectory.Status != ExternalReferenceValidationStatus.Invalid) trajectory.Status = ExternalReferenceValidationStatus.Unavailable;
            trajectory.Issues.Add(issue);
        }
        else if (result is SurveyRunExternalReferenceValidation surveyRun)
        {
            if (surveyRun.Status != ExternalReferenceValidationStatus.Invalid) surveyRun.Status = ExternalReferenceValidationStatus.Unavailable;
            surveyRun.Issues.Add(issue);
        }
    }

    private static void AddInvalid(object result, string property, string code, string message)
    {
        var issue = new ExternalReferenceIssue { Property = property, Code = code, Message = message };
        if (result is TrajectoryExternalReferenceValidation trajectory)
        {
            trajectory.Status = ExternalReferenceValidationStatus.Invalid;
            trajectory.Issues.Add(issue);
        }
        else if (result is SurveyRunExternalReferenceValidation surveyRun)
        {
            surveyRun.Status = ExternalReferenceValidationStatus.Invalid;
            surveyRun.Issues.Add(issue);
        }
    }

    private static IEnumerable<Guid> OptionalIds(IEnumerable<Guid?> identifiers) =>
        identifiers.Where(value => value is Guid id && id != Guid.Empty).Select(value => value!.Value);

    private static IEnumerable<Guid> RequiredIds(IEnumerable<Guid> identifiers) => identifiers.Where(id => id != Guid.Empty);
    private static string Title(string value) => string.Join(' ', value.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    private sealed class ExternalResourceDto { public MetaInfoDto? MetaInfo { get; set; } }
    private sealed class MetaInfoDto { public Guid ID { get; set; } }
    private sealed record ReferenceResolution(bool Exists, bool IsUnavailable, string? Code, string? Message)
    {
        public static ReferenceResolution Found() => new(true, false, null, null);
        public static ReferenceResolution NotFound() => new(false, false, null, null);
        public static ReferenceResolution Unavailable(string code, string message) => new(false, true, code, message);
    }
}
