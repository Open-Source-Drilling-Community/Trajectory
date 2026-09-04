using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using System.Text.Json;
using System.Text;

namespace OSDC.Drilling.Trajectory.Service;

public enum TrajectoryBatchFailureKind { None, InvalidRequest, NotFound, Conflict, StorageFailure }

public sealed class TrajectoryBatchExportOutcome
{
    public TrajectoryBatchExportDocument? Document { get; init; }
    public TrajectoryBatchErrorEnvelope? Error { get; init; }
    public TrajectoryBatchFailureKind FailureKind { get; init; }
    public bool IsSuccess => Document != null && FailureKind == TrajectoryBatchFailureKind.None;
}

public sealed class TrajectoryBatchRestoreOutcome
{
    public TrajectoryBatchRestoreResponse? Response { get; init; }
    public TrajectoryBatchErrorEnvelope? Error { get; init; }
    public TrajectoryBatchFailureKind FailureKind { get; init; }
    public bool IsSuccess => Response != null && FailureKind == TrajectoryBatchFailureKind.None;
}

/// <summary>Creates dependency-closed backups and restores them without recalculation.</summary>
public sealed class TrajectoryBatchService
{
    private readonly SqlConnectionManager mainDatabase;
    private readonly TrajectoryIdentityManager identityManager;
    private readonly TrajectoryFeatureCategoryManager featureManager;
    private readonly OctreeManager octreeManager;
    private readonly ILogger<TrajectoryBatchService> logger;
    private const string SurveyRunStationOwner = "SurveyRun";
    private const string TrajectoryStationOwner = "Trajectory";
    private const int MeasurementChunkSize = 5000;

    public TrajectoryBatchService(SqlConnectionManager mainDatabase,
        TrajectoryIdentityManager identityManager,
        TrajectoryFeatureCategoryManager featureManager,
        OctreeManager octreeManager,
        ILogger<TrajectoryBatchService> logger)
    {
        this.mainDatabase = mainDatabase;
        this.identityManager = identityManager;
        this.featureManager = featureManager;
        this.octreeManager = octreeManager;
        this.logger = logger;
    }

    public TrajectoryBatchExportOutcome Export(TrajectoryBatchExportRequest? request)
    {
        List<TrajectoryBatchError> errors = ValidateExportRequest(request);
        if (errors.Count != 0) return ExportFailure(TrajectoryBatchFailureKind.InvalidRequest,
            "invalid_batch_export_request", "The Trajectory batch-export request is invalid.", errors);

        try
        {
            using SqliteConnection connection = mainDatabase.GetConnection()!;
            using SqliteTransaction transaction = connection.BeginTransaction();
            Dictionary<Guid, SurveyRun> surveyRuns = ReadSurveyRuns(connection, transaction);
            Dictionary<Guid, Model.Trajectory> trajectories = ReadTrajectories(connection, transaction);

            List<Model.Trajectory> selectedTrajectories;
            List<SurveyRun> selectedSurveyRuns;
            if (request!.Scope == TrajectoryBatchExportScope.All)
            {
                selectedSurveyRuns = surveyRuns.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
                selectedTrajectories = trajectories.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
            }
            else
            {
                selectedTrajectories = Select(request.TrajectoryIDs!, trajectories, "TrajectoryIDs", "trajectory", errors);
                List<SurveyRun> explicitRuns = Select(request.SurveyRunIDs!, surveyRuns, "SurveyRunIDs", "survey run", errors);
                if (errors.Count != 0) return ExportFailure(TrajectoryBatchFailureKind.NotFound,
                    "batch_record_not_found", "One or more selected records do not exist.", errors);

                var orderedIds = new List<Guid>();
                var includedIds = new HashSet<Guid>();
                foreach (SurveyRun run in explicitRuns) IncludeSurveyRun(run.MetaInfo!.ID, surveyRuns, includedIds, orderedIds, errors);
                foreach (Model.Trajectory trajectory in selectedTrajectories)
                    foreach (TrajectorySurveyRunSection section in trajectory.SurveyRunSectionList ?? [])
                        IncludeSurveyRun(section.SurveyRunID, surveyRuns, includedIds, orderedIds, errors);
                if (errors.Count != 0) return ExportFailure(TrajectoryBatchFailureKind.NotFound,
                    "survey_run_dependency_not_found", "A trajectory or survey run dependency is missing.", errors);
                selectedSurveyRuns = orderedIds.Select(id => surveyRuns[id]).ToList();
            }

            TrajectoryBatchCatalogDependencies? dependencies = BuildCatalogDependencies(
                selectedSurveyRuns, selectedTrajectories, identityManager.GetAll(), featureManager.GetAll(), errors);
            if (dependencies == null) return ExportFailure(TrajectoryBatchFailureKind.StorageFailure,
                "catalog_dependency_missing", "A referenced identity or feature definition is missing.", errors);

            transaction.Commit();
            return new()
            {
                Document = new()
                {
                    ExportedAtUtc = DateTimeOffset.UtcNow,
                    CatalogDependencies = dependencies,
                    SurveyRuns = selectedSurveyRuns,
                    Trajectories = selectedTrajectories
                }
            };
        }
        catch (Exception exception) when (exception is SqliteException or JsonException or InvalidOperationException)
        {
            logger.LogError(exception, "Unable to export Trajectory backup");
            return ExportFailure(TrajectoryBatchFailureKind.StorageFailure, "batch_export_failed",
                "The backup snapshot could not be produced.", [Error(null, "Document", "storage_failure", exception.Message)]);
        }
    }

    public TrajectoryBatchRestoreOutcome Restore(TrajectoryBatchRestoreRequest? request)
    {
        List<TrajectoryBatchError> errors = ValidateRestoreRequest(request);
        if (errors.Count != 0) return RestoreFailure(TrajectoryBatchFailureKind.InvalidRequest,
            "invalid_batch_restore_request", "The backup is invalid. No changes were made.", errors);

        TrajectoryBatchExportDocument document = Clone(request!.Document!);
        List<TrajectoryIdentity> localIdentities = identityManager.GetAll();
        List<TrajectoryFeatureCategory> localCategories = featureManager.GetAll();
        CatalogResolution catalogs = ResolveCatalogs(document.CatalogDependencies, localIdentities, localCategories,
            request.CatalogPolicy, request.AllowNormalizedNameMapping, errors);
        if (errors.Count != 0) return RestoreFailure(TrajectoryBatchFailureKind.Conflict,
            "catalog_restore_conflict", "Catalog dependencies cannot be resolved. No changes were made.", errors);

        RemapAssignments(document, catalogs);
        ValidateAssignmentsAndDependencies(document, catalogs.FinalIdentities, catalogs.FinalCategories, errors);
        if (errors.Count != 0) return RestoreFailure(TrajectoryBatchFailureKind.InvalidRequest,
            "invalid_batch_dependencies", "The backup dependency graph is invalid. No changes were made.", errors);

        try
        {
            using SqliteConnection connection = mainDatabase.GetConnection()!;
            using SqliteTransaction transaction = connection.BeginTransaction();
            HashSet<Guid> existingSurveyRuns = ReadIds(connection, transaction, "SurveyRunTable");
            HashSet<Guid> existingTrajectories = ReadIds(connection, transaction, "TrajectoryTable");
            CheckRecordConflicts(document, request.ConflictPolicy, existingSurveyRuns, existingTrajectories, errors);
            if (errors.Count != 0) return RestoreFailure(TrajectoryBatchFailureKind.Conflict,
                "batch_restore_conflict", "One or more record UUIDs already exist. No changes were made.", errors);

            foreach (TrajectoryIdentity identity in catalogs.IdentitiesToCreate)
            {
                WriteIdentity(connection, transaction, identity);
            }
            foreach (TrajectoryFeatureCategory category in catalogs.CategoriesToCreate)
            {
                WriteFeatureCategory(connection, transaction, category);
            }

            int createdRuns = 0, replacedRuns = 0, createdTrajectories = 0, replacedTrajectories = 0;
            foreach (SurveyRun surveyRun in document.SurveyRuns)
            {
                bool exists = existingSurveyRuns.Contains(surveyRun.MetaInfo!.ID);
                WriteSurveyRun(connection, transaction, surveyRun, exists);
                if (exists) replacedRuns++; else createdRuns++;
            }
            foreach (Model.Trajectory trajectory in document.Trajectories)
            {
                bool exists = existingTrajectories.Contains(trajectory.MetaInfo!.ID);
                WriteTrajectory(connection, transaction, trajectory, exists);
                if (exists) replacedTrajectories++; else createdTrajectories++;
            }
            transaction.Commit();

            foreach (Model.Trajectory trajectory in document.Trajectories)
            {
                if (!octreeManager.Rebuild(trajectory))
                {
                    octreeManager.Delete(trajectory.MetaInfo!.ID);
                    logger.LogWarning("Restored trajectory {TrajectoryId}, but it has no indexable uncertainty envelope", trajectory.MetaInfo.ID);
                }
            }

            return new()
            {
                Response = new()
                {
                    RestoredAtUtc = DateTimeOffset.UtcNow,
                    CreatedSurveyRunCount = createdRuns,
                    ReplacedSurveyRunCount = replacedRuns,
                    CreatedTrajectoryCount = createdTrajectories,
                    ReplacedTrajectoryCount = replacedTrajectories,
                    CreatedCatalogDefinitionCount = catalogs.IdentitiesToCreate.Count + catalogs.CategoriesToCreate.Count,
                    CatalogMappings = catalogs.Mappings,
                    SurveyRunIDs = document.SurveyRuns.Select(value => value.MetaInfo!.ID).ToList(),
                    TrajectoryIDs = document.Trajectories.Select(value => value.MetaInfo!.ID).ToList()
                }
            };
        }
        catch (Exception exception) when (exception is SqliteException or JsonException or InvalidOperationException)
        {
            logger.LogError(exception, "Unable to restore Trajectory backup");
            return RestoreFailure(TrajectoryBatchFailureKind.StorageFailure, "batch_restore_failed",
                "The backup could not be restored. No record changes were committed.",
                [Error(null, "Document", "storage_failure", exception.Message)]);
        }
    }

    private static List<T> Select<T>(IEnumerable<Guid> requested, IReadOnlyDictionary<Guid, T> available,
        string property, string kind, List<TrajectoryBatchError> errors)
    {
        var result = new List<T>();
        int index = 0;
        foreach (Guid id in requested)
        {
            if (available.TryGetValue(id, out T? value)) result.Add(value);
            else errors.Add(Error(index, property, "record_not_found", $"No stored {kind} has UUID '{id}'."));
            index++;
        }
        return result;
    }

    private static void IncludeSurveyRun(Guid id, IReadOnlyDictionary<Guid, SurveyRun> available,
        HashSet<Guid> included, List<Guid> ordered, List<TrajectoryBatchError> errors)
    {
        if (id == Guid.Empty || included.Contains(id)) return;
        if (!available.TryGetValue(id, out SurveyRun? run))
        {
            errors.Add(Error(null, "SurveyRuns", "survey_run_dependency_not_found", $"Referenced survey run '{id}' does not exist."));
            return;
        }
        included.Add(id);
        if (run.ParentSurveyRunID is Guid parentId && parentId != Guid.Empty)
            IncludeSurveyRun(parentId, available, included, ordered, errors);
        ordered.Add(id);
    }

    private static TrajectoryBatchCatalogDependencies? BuildCatalogDependencies(
        IEnumerable<SurveyRun> surveyRuns, IEnumerable<Model.Trajectory> trajectories,
        IEnumerable<TrajectoryIdentity> identities, IEnumerable<TrajectoryFeatureCategory> categories,
        List<TrajectoryBatchError> errors)
    {
        List<TrajectoryIdentityAssignment> identityAssignments = surveyRuns.SelectMany(value => value.SurveyRunIdentityAssignments ?? [])
            .Concat(trajectories.SelectMany(value => value.TrajectoryIdentityAssignments ?? [])).ToList();
        List<TrajectoryFeatureAssignment> featureAssignments = surveyRuns.SelectMany(value => value.SurveyRunFeatureAssignments ?? [])
            .Concat(trajectories.SelectMany(value => value.TrajectoryFeatureAssignments ?? [])).ToList();
        HashSet<Guid> identityIds = identityAssignments.Where(value => value.IdentityID.HasValue).Select(value => value.IdentityID!.Value).ToHashSet();
        HashSet<Guid> categoryIds = featureAssignments.Where(value => value.FeatureCategoryID.HasValue).Select(value => value.FeatureCategoryID!.Value).ToHashSet();
        Dictionary<Guid, TrajectoryIdentity> identityIndex = identities.Where(value => value.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .ToDictionary(value => value.MetaInfo!.ID);
        Dictionary<Guid, TrajectoryFeatureCategory> categoryIndex = categories.Where(value => value.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .ToDictionary(value => value.MetaInfo!.ID);
        foreach (Guid id in identityIds.Where(id => !identityIndex.ContainsKey(id)))
            errors.Add(Error(null, "CatalogDependencies.Identities", "identity_missing", $"Referenced identity '{id}' is missing."));
        foreach (Guid id in categoryIds.Where(id => !categoryIndex.ContainsKey(id)))
            errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "feature_category_missing", $"Referenced feature category '{id}' is missing."));
        if (errors.Count != 0) return null;
        return new()
        {
            Identities = identityIds.Order().Select(id => identityIndex[id]).ToList(),
            FeatureCategories = categoryIds.Order().Select(id => categoryIndex[id]).ToList()
        };
    }

    private static List<TrajectoryBatchError> ValidateExportRequest(TrajectoryBatchExportRequest? request)
    {
        var errors = new List<TrajectoryBatchError>();
        if (request == null) return [Error(null, "Request", "required", "A request is required.")];
        if (request.Scope == TrajectoryBatchExportScope.All)
        {
            if ((request.SurveyRunIDs?.Count ?? 0) != 0 || (request.TrajectoryIDs?.Count ?? 0) != 0)
                errors.Add(Error(null, "Request", "ids_forbidden", "ID lists must be omitted for an All export."));
        }
        else if (request.Scope == TrajectoryBatchExportScope.Selected)
        {
            ValidateIds(request.SurveyRunIDs, "SurveyRunIDs", errors);
            ValidateIds(request.TrajectoryIDs, "TrajectoryIDs", errors);
            if ((request.SurveyRunIDs?.Count ?? 0) + (request.TrajectoryIDs?.Count ?? 0) == 0)
                errors.Add(Error(null, "Request", "selection_required", "Select at least one survey run or trajectory."));
        }
        else errors.Add(Error(null, "Scope", "invalid_value", "Scope must be All or Selected."));
        return errors;
    }

    private static void ValidateIds(List<Guid>? ids, string property, List<TrajectoryBatchError> errors)
    {
        var seen = new HashSet<Guid>();
        for (int index = 0; index < (ids?.Count ?? 0); index++)
        {
            Guid id = ids![index];
            if (id == Guid.Empty) errors.Add(Error(index, property, "empty_uuid", "UUIDs must be non-empty."));
            else if (!seen.Add(id)) errors.Add(Error(index, property, "duplicate_uuid", $"UUID '{id}' occurs more than once."));
        }
    }

    private static List<TrajectoryBatchError> ValidateRestoreRequest(TrajectoryBatchRestoreRequest? request)
    {
        var errors = new List<TrajectoryBatchError>();
        if (request == null) return [Error(null, "Request", "required", "A request is required.")];
        if (request.ConflictPolicy is not TrajectoryBatchRestoreConflictPolicy.FailIfExists and not TrajectoryBatchRestoreConflictPolicy.ReplaceExisting)
            errors.Add(Error(null, "ConflictPolicy", "invalid_value", "ConflictPolicy must be FailIfExists or ReplaceExisting."));
        if (request.CatalogPolicy is not TrajectoryBatchCatalogRestorePolicy.MapExisting and not TrajectoryBatchCatalogRestorePolicy.MapOrCreateMissing)
            errors.Add(Error(null, "CatalogPolicy", "invalid_value", "CatalogPolicy must be MapExisting or MapOrCreateMissing."));
        TrajectoryBatchExportDocument? document = request.Document;
        if (document == null) return [.. errors, Error(null, "Document", "required", "Document is required.")];
        if (document.FormatIdentifier != TrajectoryBatchExportDocument.CurrentFormatIdentifier)
            errors.Add(Error(null, "Document.FormatIdentifier", "unsupported_format", $"FormatIdentifier must be '{TrajectoryBatchExportDocument.CurrentFormatIdentifier}'."));
        if (document.SchemaVersion != TrajectoryBatchExportDocument.CurrentSchemaVersion)
            errors.Add(Error(null, "Document.SchemaVersion", "unsupported_schema_version", $"SchemaVersion must be {TrajectoryBatchExportDocument.CurrentSchemaVersion}."));
        if (document.ExportedAtUtc == default || document.ExportedAtUtc.Offset != TimeSpan.Zero)
            errors.Add(Error(null, "Document.ExportedAtUtc", "utc_required", "ExportedAtUtc must use UTC offset +00:00."));
        if (document.CatalogDependencies == null) errors.Add(Error(null, "Document.CatalogDependencies", "required", "CatalogDependencies is required."));
        if ((document.SurveyRuns?.Count ?? 0) + (document.Trajectories?.Count ?? 0) == 0)
            errors.Add(Error(null, "Document", "records_required", "At least one survey run or trajectory is required."));
        ValidateRecords(document.SurveyRuns, "Document.SurveyRuns", errors);
        ValidateRecords(document.Trajectories, "Document.Trajectories", errors);
        ValidateCatalogDocument(document.CatalogDependencies, errors);
        return errors;
    }

    private static void ValidateRecords<T>(List<T>? values, string property, List<TrajectoryBatchError> errors)
    {
        if (values == null) { errors.Add(Error(null, property, "required", $"{property} is required.")); return; }
        var ids = new HashSet<Guid>();
        for (int index = 0; index < values.Count; index++)
        {
            Guid? id = values[index] switch { SurveyRun run => run.MetaInfo?.ID, Model.Trajectory trajectory => trajectory.MetaInfo?.ID, _ => null };
            if (id is not Guid value || value == Guid.Empty) errors.Add(Error(index, property + ".MetaInfo.ID", "empty_uuid", "Every record requires a non-empty UUID."));
            else if (!ids.Add(value)) errors.Add(Error(index, property + ".MetaInfo.ID", "duplicate_uuid", $"UUID '{value}' occurs more than once."));
        }
    }

    private static void ValidateCatalogDocument(TrajectoryBatchCatalogDependencies? dependencies, List<TrajectoryBatchError> errors)
    {
        if (dependencies == null) return;
        ValidateCatalogEntries(dependencies.Identities, value => value.MetaInfo?.ID, value => value.Name, "Document.CatalogDependencies.Identities", errors);
        ValidateCatalogEntries(dependencies.FeatureCategories, value => value.MetaInfo?.ID, value => value.Name, "Document.CatalogDependencies.FeatureCategories", errors);
        foreach ((TrajectoryFeatureCategory category, int index) in (dependencies.FeatureCategories ?? []).Select((value, index) => (value, index)))
        {
            if (category.Options == null || category.Options.Any(value => value.ID == Guid.Empty || string.IsNullOrWhiteSpace(value.Name)) ||
                category.Options.Select(value => value.ID).Distinct().Count() != category.Options.Count)
                errors.Add(Error(index, "Document.CatalogDependencies.FeatureCategories.Options", "invalid_options", "Feature options require unique non-empty UUIDs and names."));
        }
    }

    private static void ValidateCatalogEntries<T>(List<T>? values, Func<T, Guid?> id, Func<T, string?> name,
        string property, List<TrajectoryBatchError> errors)
    {
        if (values == null) { errors.Add(Error(null, property, "required", $"{property} is required.")); return; }
        var ids = new HashSet<Guid>();
        for (int index = 0; index < values.Count; index++)
        {
            Guid? valueId = id(values[index]);
            if (valueId is not Guid defined || defined == Guid.Empty) errors.Add(Error(index, property, "empty_uuid", "Catalog UUIDs must be non-empty."));
            else if (!ids.Add(defined)) errors.Add(Error(index, property, "duplicate_uuid", $"Catalog UUID '{defined}' occurs more than once."));
            if (string.IsNullOrWhiteSpace(name(values[index]))) errors.Add(Error(index, property + ".Name", "required", "Catalog names are required."));
        }
    }

    private static CatalogResolution ResolveCatalogs(TrajectoryBatchCatalogDependencies dependencies,
        List<TrajectoryIdentity> localIdentities, List<TrajectoryFeatureCategory> localCategories,
        TrajectoryBatchCatalogRestorePolicy policy, bool allowNormalizedNameMapping,
        List<TrajectoryBatchError> errors)
    {
        var result = new CatalogResolution(localIdentities, localCategories);
        foreach (TrajectoryIdentity source in dependencies.Identities)
        {
            TrajectoryIdentity? local = ResolveByIdOrName(source, source.MetaInfo!.ID, source.Name, localIdentities,
                value => value.MetaInfo!.ID, value => value.Name,
                (left, right) => SameName(left.Name, right.Name), "identity",
                allowNormalizedNameMapping, errors);
            if (local == null && policy == TrajectoryBatchCatalogRestorePolicy.MapOrCreateMissing)
            {
                local = source;
                result.IdentitiesToCreate.Add(source);
                result.FinalIdentities.Add(source);
                result.Mappings.Add(Mapping("Identity", source.Name, source.MetaInfo.ID, source.MetaInfo.ID, "Created"));
            }
            else if (local == null) AddMissing("identity", source.MetaInfo.ID, source.Name, errors);
            else result.Mappings.Add(Mapping("Identity", source.Name, source.MetaInfo.ID, local.MetaInfo!.ID,
                source.MetaInfo.ID == local.MetaInfo.ID ? "UUID" : "NormalizedName"));
            if (local != null) result.IdentityMap[source.MetaInfo.ID] = local.MetaInfo!.ID;
        }

        foreach (TrajectoryFeatureCategory source in dependencies.FeatureCategories)
        {
            TrajectoryFeatureCategory? local = ResolveByIdOrName(source, source.MetaInfo!.ID, source.Name, localCategories,
                value => value.MetaInfo!.ID, value => value.Name, SameCategory, "feature category",
                allowNormalizedNameMapping, errors);
            if (local == null && policy == TrajectoryBatchCatalogRestorePolicy.MapOrCreateMissing)
            {
                local = source;
                result.CategoriesToCreate.Add(source);
                result.FinalCategories.Add(source);
                result.Mappings.Add(Mapping("FeatureCategory", source.Name, source.MetaInfo.ID, source.MetaInfo.ID, "Created"));
            }
            else if (local == null) AddMissing("feature category", source.MetaInfo.ID, source.Name, errors);
            else result.Mappings.Add(Mapping("FeatureCategory", source.Name, source.MetaInfo.ID, local.MetaInfo!.ID,
                source.MetaInfo.ID == local.MetaInfo.ID ? "UUID" : "NormalizedName"));
            if (local == null) continue;
            result.CategoryMap[source.MetaInfo.ID] = local.MetaInfo!.ID;
            foreach (TrajectoryFeatureOption option in source.Options ?? [])
            {
                TrajectoryFeatureOption? localOption = (local.Options ?? []).SingleOrDefault(value => value.ID == option.ID);
                if (localOption != null && !SameName(localOption.Name, option.Name))
                {
                    errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories.Options", "catalog_semantic_conflict",
                        $"Local feature option '{option.ID}' has semantics incompatible with '{option.Name}'."));
                    continue;
                }
                if (localOption == null && allowNormalizedNameMapping)
                {
                    List<TrajectoryFeatureOption> namedOptions = (local.Options ?? [])
                        .Where(value => SameName(value.Name, option.Name)).ToList();
                    if (namedOptions.Count > 1)
                        errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories.Options", "ambiguous_catalog_match",
                            $"More than one local feature option is named '{option.Name}'."));
                    else if (namedOptions.Count == 1) localOption = namedOptions[0];
                }
                if (localOption == null)
                    errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories.Options", "catalog_semantic_conflict",
                        $"Feature category '{source.Name}' has no compatible option '{option.Name}'."));
                else
                {
                    result.OptionMap[option.ID] = localOption.ID;
                    result.Mappings.Add(Mapping("FeatureOption", option.Name, option.ID, localOption.ID,
                        option.ID == localOption.ID ? "UUID" : "NormalizedName"));
                }
            }
        }
        return result;
    }

    private static T? ResolveByIdOrName<T>(T source, Guid sourceId, string? sourceName, IEnumerable<T> locals,
        Func<T, Guid> id, Func<T, string?> name, Func<T, T, bool> compatible, string kind,
        bool allowNormalizedNameMapping, List<TrajectoryBatchError> errors) where T : class
    {
        T? byId = locals.FirstOrDefault(value => id(value) == sourceId);
        if (byId != null)
        {
            if (compatible(source, byId)) return byId;
            errors.Add(Error(null, "Document.CatalogDependencies", "catalog_semantic_conflict",
                $"Local {kind} '{sourceId}' has semantics incompatible with '{sourceName}'."));
            return null;
        }
        if (!allowNormalizedNameMapping) return null;
        List<T> byName = locals.Where(value => SameName(name(value), sourceName) && compatible(source, value)).ToList();
        if (byName.Count > 1)
            errors.Add(Error(null, "Document.CatalogDependencies", "ambiguous_catalog_match", $"More than one local {kind} is named '{sourceName}'."));
        return byName.Count == 1 ? byName[0] : null;
    }

    private static bool SameCategory(TrajectoryFeatureCategory left, TrajectoryFeatureCategory right) =>
        SameName(left.Name, right.Name) && left.IsExclusive == right.IsExclusive &&
        left.HasValidityPeriod == right.HasValidityPeriod &&
        (left.Options ?? []).Select(value => Normalize(value.Name)).ToHashSet().SetEquals((right.Options ?? []).Select(value => Normalize(value.Name)));

    private static void RemapAssignments(TrajectoryBatchExportDocument document, CatalogResolution catalogs)
    {
        foreach (SurveyRun run in document.SurveyRuns)
            Remap(run.SurveyRunIdentityAssignments, run.SurveyRunFeatureAssignments, catalogs);
        foreach (Model.Trajectory trajectory in document.Trajectories)
            Remap(trajectory.TrajectoryIdentityAssignments, trajectory.TrajectoryFeatureAssignments, catalogs);
    }

    private static void Remap(List<TrajectoryIdentityAssignment>? identities, List<TrajectoryFeatureAssignment>? features, CatalogResolution catalogs)
    {
        foreach (TrajectoryIdentityAssignment assignment in identities ?? [])
            if (assignment.IdentityID is Guid id && catalogs.IdentityMap.TryGetValue(id, out Guid mapped)) assignment.IdentityID = mapped;
        foreach (TrajectoryFeatureAssignment assignment in features ?? [])
        {
            if (assignment.FeatureCategoryID is Guid categoryId && catalogs.CategoryMap.TryGetValue(categoryId, out Guid mappedCategory)) assignment.FeatureCategoryID = mappedCategory;
            if (assignment.FeatureOptionID is Guid optionId && catalogs.OptionMap.TryGetValue(optionId, out Guid mappedOption)) assignment.FeatureOptionID = mappedOption;
        }
    }

    private static void ValidateAssignmentsAndDependencies(TrajectoryBatchExportDocument document,
        List<TrajectoryIdentity> identities, List<TrajectoryFeatureCategory> categories, List<TrajectoryBatchError> errors)
    {
        HashSet<Guid> identityIds = identities.Select(value => value.MetaInfo!.ID).ToHashSet();
        Dictionary<Guid, TrajectoryFeatureCategory> categoryIndex = categories.ToDictionary(value => value.MetaInfo!.ID);
        for (int index = 0; index < document.SurveyRuns.Count; index++)
        {
            SurveyRun run = document.SurveyRuns[index];
            if (run.WellBoreID == Guid.Empty || run.SurveyInstrumentID == Guid.Empty)
                errors.Add(Error(index, "Document.SurveyRuns", "required_reference_missing", "WellBoreID and SurveyInstrumentID must be non-empty."));
            ValidateAssignments(run.SurveyRunIdentityAssignments, run.SurveyRunFeatureAssignments, identityIds, categoryIndex, index, "Document.SurveyRuns", errors);
        }
        for (int index = 0; index < document.Trajectories.Count; index++)
        {
            Model.Trajectory trajectory = document.Trajectories[index];
            if (trajectory.WellBoreID == Guid.Empty)
                errors.Add(Error(index, "Document.Trajectories.WellBoreID", "required_reference_missing", "WellBoreID must be non-empty."));
            ValidateAssignments(trajectory.TrajectoryIdentityAssignments, trajectory.TrajectoryFeatureAssignments, identityIds, categoryIndex, index, "Document.Trajectories", errors);
        }
        HashSet<Guid> runIds = document.SurveyRuns.Select(value => value.MetaInfo!.ID).ToHashSet();
        foreach ((SurveyRun run, int index) in document.SurveyRuns.Select((value, index) => (value, index)))
            if (run.ParentSurveyRunID is Guid parentId && parentId != Guid.Empty && !runIds.Contains(parentId))
                errors.Add(Error(index, "Document.SurveyRuns.ParentSurveyRunID", "dependency_missing", $"Parent survey run '{parentId}' is absent from the backup."));
        foreach ((Model.Trajectory trajectory, int index) in document.Trajectories.Select((value, index) => (value, index)))
            foreach (TrajectorySurveyRunSection section in trajectory.SurveyRunSectionList ?? [])
                if (section.SurveyRunID == Guid.Empty || !runIds.Contains(section.SurveyRunID))
                    errors.Add(Error(index, "Document.Trajectories.SurveyRunSectionList", "dependency_missing", $"Survey run '{section.SurveyRunID}' is absent from the backup."));
    }

    private static void ValidateAssignments(List<TrajectoryIdentityAssignment>? identities, List<TrajectoryFeatureAssignment>? features,
        HashSet<Guid> identityIds, Dictionary<Guid, TrajectoryFeatureCategory> categories, int index, string property,
        List<TrajectoryBatchError> errors)
    {
        if ((identities ?? []).Any(value => value.ID == Guid.Empty) || (identities ?? []).Select(value => value.ID).Distinct().Count() != (identities?.Count ?? 0))
            errors.Add(Error(index, property + ".IdentityAssignments", "invalid_assignment_ids", "Identity assignment UUIDs must be non-empty and unique."));
        if ((features ?? []).Any(value => value.ID == Guid.Empty) || (features ?? []).Select(value => value.ID).Distinct().Count() != (features?.Count ?? 0))
            errors.Add(Error(index, property + ".FeatureAssignments", "invalid_assignment_ids", "Feature assignment UUIDs must be non-empty and unique."));
        foreach (TrajectoryIdentityAssignment assignment in identities ?? [])
            if (assignment.IdentityID is not Guid identityId || !identityIds.Contains(identityId))
                errors.Add(Error(index, property + ".IdentityAssignments.IdentityID", "dependency_missing", "An identity dependency is missing."));
        foreach (TrajectoryFeatureAssignment assignment in features ?? [])
        {
            if (assignment.FeatureCategoryID is not Guid categoryId || !categories.TryGetValue(categoryId, out TrajectoryFeatureCategory? category) ||
                assignment.FeatureOptionID is not Guid optionId || category.Options?.Any(value => value.ID == optionId) != true)
                errors.Add(Error(index, property + ".FeatureAssignments", "dependency_missing", "A feature category or option dependency is missing."));
            else if ((!category.HasValidityPeriod && (assignment.FromDate != null || assignment.ToDate != null)) || assignment.FromDate > assignment.ToDate)
                errors.Add(Error(index, property + ".FeatureAssignments", "invalid_validity_period", "Feature assignment validity is invalid for its category."));
        }
    }

    private static void CheckRecordConflicts(TrajectoryBatchExportDocument document, TrajectoryBatchRestoreConflictPolicy policy,
        HashSet<Guid> existingRuns, HashSet<Guid> existingTrajectories, List<TrajectoryBatchError> errors)
    {
        if (policy != TrajectoryBatchRestoreConflictPolicy.FailIfExists) return;
        for (int index = 0; index < document.SurveyRuns.Count; index++)
            if (existingRuns.Contains(document.SurveyRuns[index].MetaInfo!.ID)) errors.Add(Error(index, "Document.SurveyRuns.MetaInfo.ID", "record_exists", $"Survey run '{document.SurveyRuns[index].MetaInfo!.ID}' already exists."));
        for (int index = 0; index < document.Trajectories.Count; index++)
            if (existingTrajectories.Contains(document.Trajectories[index].MetaInfo!.ID)) errors.Add(Error(index, "Document.Trajectories.MetaInfo.ID", "record_exists", $"Trajectory '{document.Trajectories[index].MetaInfo!.ID}' already exists."));
    }

    private static Dictionary<Guid, SurveyRun> ReadSurveyRuns(SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<Guid, SurveyRun> result = ReadDocuments<SurveyRun>(connection, transaction, "SurveyRunTable", "SurveyRun")
            .ToDictionary(value => value.MetaInfo!.ID);
        foreach ((Guid id, SurveyRun value) in result)
        {
            value.SurveyMeasurementList = ReadMeasurementChunks(connection, transaction, id);
            value.SurveyStationList = ReadStationChunks(connection, transaction, id, SurveyRunStationOwner);
        }
        return result;
    }

    private static Dictionary<Guid, Model.Trajectory> ReadTrajectories(SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<Guid, Model.Trajectory> result = ReadDocuments<Model.Trajectory>(connection, transaction, "TrajectoryTable", "Trajectory")
            .ToDictionary(value => value.MetaInfo!.ID);
        foreach ((Guid id, Model.Trajectory value) in result)
            value.SurveyStationList = ReadStationChunks(connection, transaction, id, TrajectoryStationOwner);
        return result;
    }

    private static List<T> ReadDocuments<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        var result = new List<T>();
        while (reader.Read()) result.Add(JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options) ?? throw new JsonException($"Invalid {table} document."));
        return result;
    }

    private static List<SurveyMeasurement>? ReadMeasurementChunks(SqliteConnection connection, SqliteTransaction transaction, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT SurveyMeasurementChunk FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID=$id ORDER BY ChunkIndex";
        command.Parameters.AddWithValue("$id", id.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        var result = new List<SurveyMeasurement>();
        while (reader.Read()) result.AddRange(JsonSerializer.Deserialize<SurveyMeasurementChunk>(reader.GetString(0), JsonSettings.Options)?.SurveyMeasurementList ?? []);
        return result.Count == 0 ? null : result;
    }

    private static List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? ReadStationChunks(SqliteConnection connection, SqliteTransaction transaction, Guid id, string ownerType)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT SurveyStationChunk FROM SurveyStationChunkTable WHERE OwnerID=$id AND OwnerType=$type ORDER BY ChunkIndex";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$type", ownerType);
        using SqliteDataReader reader = command.ExecuteReader();
        var result = new List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>();
        while (reader.Read()) result.AddRange(JsonSerializer.Deserialize<SurveyStationChunk>(reader.GetString(0), JsonSettings.Options)?.SurveyStationList ?? []);
        return result.Count == 0 ? null : result;
    }

    private static HashSet<Guid> ReadIds(SqliteConnection connection, SqliteTransaction transaction, string table)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT ID FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        var result = new HashSet<Guid>();
        while (reader.Read()) result.Add(Guid.Parse(reader.GetString(0)));
        return result;
    }

    private static void WriteSurveyRun(SqliteConnection connection, SqliteTransaction transaction, SurveyRun surveyRun, bool exists)
    {
        Guid surveyRunId = surveyRun.MetaInfo!.ID;
        List<SurveyMeasurement>? measurements = surveyRun.SurveyMeasurementList;
        List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? stations = surveyRun.SurveyStationList;
        surveyRun.SurveyMeasurementList = null;
        surveyRun.SurveyStationList = null;
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = exists
            ? "UPDATE SurveyRunTable SET MetaInfo=$meta,CreationDate=$created,LastModificationDate=$modified,FieldID=$field,ClusterID=$cluster,WellID=$well,WellBoreID=$wellBore,SurveyInstrumentID=$instrument,SurveyRunType=$type,CalculationType=$calculationType,ParentSurveyRunID=$parent,CalculationState=$state,CalculationProgress=$progress,CalculationMessage=$message,SurveyRun=$document WHERE ID=$id"
            : "INSERT INTO SurveyRunTable(ID,MetaInfo,CreationDate,LastModificationDate,FieldID,ClusterID,WellID,WellBoreID,SurveyInstrumentID,SurveyRunType,CalculationType,ParentSurveyRunID,CalculationState,CalculationProgress,CalculationMessage,SurveyRun) VALUES($id,$meta,$created,$modified,$field,$cluster,$well,$wellBore,$instrument,$type,$calculationType,$parent,$state,$progress,$message,$document)";
        AddCommon(command, surveyRun.MetaInfo!, surveyRun.CreationDate, surveyRun.LastModificationDate);
        command.Parameters.AddWithValue("$field", SqlValue(surveyRun.FieldID));
        command.Parameters.AddWithValue("$cluster", SqlValue(surveyRun.ClusterID));
        command.Parameters.AddWithValue("$well", SqlValue(surveyRun.WellID));
        command.Parameters.AddWithValue("$wellBore", surveyRun.WellBoreID.ToString());
        command.Parameters.AddWithValue("$instrument", surveyRun.SurveyInstrumentID.ToString());
        command.Parameters.AddWithValue("$type", surveyRun.SurveyRunType.ToString());
        command.Parameters.AddWithValue("$calculationType", surveyRun.CalculationType.ToString());
        command.Parameters.AddWithValue("$parent", SqlValue(surveyRun.ParentSurveyRunID));
        command.Parameters.AddWithValue("$state", surveyRun.CalculationState.ToString());
        command.Parameters.AddWithValue("$progress", surveyRun.CalculationProgress);
        command.Parameters.AddWithValue("$message", (object?)surveyRun.CalculationMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(surveyRun, JsonSettings.Options));
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Could not write survey run '{surveyRunId}'.");
        ReplaceMeasurementChunks(connection, transaction, surveyRunId, measurements);
        if (!SurveyStationChunkStore.ReplaceChunks(connection, transaction, surveyRunId, SurveyRunStationOwner, stations))
            throw new InvalidOperationException($"Could not write survey-run stations for '{surveyRunId}'.");
    }

    private static void WriteTrajectory(SqliteConnection connection, SqliteTransaction transaction, Model.Trajectory trajectory, bool exists)
    {
        Guid trajectoryId = trajectory.MetaInfo!.ID;
        List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? stations = trajectory.SurveyStationList;
        trajectory.SurveyStationList = null;
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = exists
            ? "UPDATE TrajectoryTable SET MetaInfo=$meta,CreationDate=$created,LastModificationDate=$modified,FieldID=$field,ClusterID=$cluster,WellID=$well,WellBoreID=$wellBore,TrajectoryType=$type,IsDefinitive=$definitive,CalculationState=$state,CalculationProgress=$progress,CalculationMessage=$message,Trajectory=$document WHERE ID=$id"
            : "INSERT INTO TrajectoryTable(ID,MetaInfo,CreationDate,LastModificationDate,FieldID,ClusterID,WellID,WellBoreID,TrajectoryType,IsDefinitive,CalculationState,CalculationProgress,CalculationMessage,Trajectory) VALUES($id,$meta,$created,$modified,$field,$cluster,$well,$wellBore,$type,$definitive,$state,$progress,$message,$document)";
        AddCommon(command, trajectory.MetaInfo!, trajectory.CreationDate, trajectory.LastModificationDate);
        command.Parameters.AddWithValue("$field", SqlValue(trajectory.FieldID));
        command.Parameters.AddWithValue("$cluster", SqlValue(trajectory.ClusterID));
        command.Parameters.AddWithValue("$well", SqlValue(trajectory.WellID));
        command.Parameters.AddWithValue("$wellBore", trajectory.WellBoreID.ToString());
        command.Parameters.AddWithValue("$type", trajectory.TrajectoryType.ToString());
        command.Parameters.AddWithValue("$definitive", trajectory.IsDefinitive ? 1 : 0);
        command.Parameters.AddWithValue("$state", trajectory.CalculationState.ToString());
        command.Parameters.AddWithValue("$progress", trajectory.CalculationProgress);
        command.Parameters.AddWithValue("$message", (object?)trajectory.CalculationMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(trajectory, JsonSettings.Options));
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Could not write trajectory '{trajectoryId}'.");
        if (!SurveyStationChunkStore.ReplaceChunks(connection, transaction, trajectoryId, TrajectoryStationOwner, stations))
            throw new InvalidOperationException($"Could not write trajectory stations for '{trajectoryId}'.");
        trajectory.SurveyStationList = stations;
    }

    private static void AddCommon(SqliteCommand command, OSDC.DotnetLibraries.General.DataManagement.MetaInfo meta,
        DateTimeOffset? created, DateTimeOffset? modified)
    {
        command.Parameters.AddWithValue("$id", meta.ID.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta, JsonSettings.Options));
        command.Parameters.AddWithValue("$created", (object?)created?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("$modified", (object?)modified?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
    }

    private static void ReplaceMeasurementChunks(SqliteConnection connection, SqliteTransaction transaction, Guid surveyRunId, List<SurveyMeasurement>? measurements)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID=$id";
            delete.Parameters.AddWithValue("$id", surveyRunId.ToString());
            delete.ExecuteNonQuery();
        }
        int index = 0;
        foreach (List<SurveyMeasurement> values in (measurements ?? []).Chunk(MeasurementChunkSize).Select(value => value.ToList()))
        {
            var chunk = new SurveyMeasurementChunk { SurveyRunID = surveyRunId, ChunkIndex = index++, SurveyMeasurementList = values };
            chunk.UpdateMetadata();
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO SurveyRunMeasurementChunkTable(ID,SurveyRunID,ChunkIndex,MeasurementCount,StartMD,EndMD,SurveyMeasurementChunk) VALUES($id,$owner,$index,$count,$start,$end,$document)";
            command.Parameters.AddWithValue("$id", $"{surveyRunId:N}:{chunk.ChunkIndex:D10}");
            command.Parameters.AddWithValue("$owner", surveyRunId.ToString());
            command.Parameters.AddWithValue("$index", chunk.ChunkIndex);
            command.Parameters.AddWithValue("$count", chunk.MeasurementCount);
            command.Parameters.AddWithValue("$start", (object?)chunk.StartMD ?? DBNull.Value);
            command.Parameters.AddWithValue("$end", (object?)chunk.EndMD ?? DBNull.Value);
            command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(chunk, JsonSettings.Options));
            if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Could not write survey measurements for '{surveyRunId}'.");
        }
    }

    private static void WriteIdentity(SqliteConnection connection, SqliteTransaction transaction, TrajectoryIdentity identity)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO TrajectoryIdentityTable(ID,MetaInfo,Name,CreationDate,LastModificationDate,TrajectoryIdentity) VALUES($id,$meta,$name,$created,$modified,$document)";
        AddCatalogParameters(command, identity.MetaInfo!, identity.Name, identity.CreationDate, identity.LastModificationDate, identity);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Could not create identity '{identity.Name}'.");
    }

    private static void WriteFeatureCategory(SqliteConnection connection, SqliteTransaction transaction, TrajectoryFeatureCategory category)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO TrajectoryFeatureCategoryTable(ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,TrajectoryFeatureCategory) VALUES($id,$meta,$name,$exclusive,$validity,$created,$modified,$document)";
        AddCatalogParameters(command, category.MetaInfo!, category.Name, category.CreationDate, category.LastModificationDate, category);
        command.Parameters.AddWithValue("$exclusive", category.IsExclusive ? 1 : 0);
        command.Parameters.AddWithValue("$validity", category.HasValidityPeriod ? 1 : 0);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Could not create feature category '{category.Name}'.");
    }

    private static void AddCatalogParameters<T>(SqliteCommand command,
        OSDC.DotnetLibraries.General.DataManagement.MetaInfo meta, string? name,
        DateTimeOffset? created, DateTimeOffset? modified, T document)
    {
        command.Parameters.AddWithValue("$id", meta.ID.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta, JsonSettings.Options));
        command.Parameters.AddWithValue("$name", (object?)name ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", (object?)created?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$modified", (object?)modified?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(document, JsonSettings.Options));
    }

    private static object SqlValue(Guid? value) => value is Guid id && id != Guid.Empty ? id.ToString() : DBNull.Value;
    private static TrajectoryBatchExportDocument Clone(TrajectoryBatchExportDocument document) =>
        JsonSerializer.Deserialize<TrajectoryBatchExportDocument>(JsonSerializer.Serialize(document, JsonSettings.Options), JsonSettings.Options)
        ?? throw new JsonException("The backup document could not be cloned.");
    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static bool SameName(string? left, string? right) => Normalize(left) == Normalize(right);
    private static void AddMissing(string kind, Guid id, string? name, List<TrajectoryBatchError> errors) =>
        errors.Add(Error(null, "Document.CatalogDependencies", "catalog_definition_missing", $"No compatible local {kind} exists for '{name}' ({id}), and creation is disabled."));
    private static TrajectoryBatchCatalogMapping Mapping(string catalog, string? name, Guid source, Guid local, string resolution) =>
        new() { Catalog = catalog, Name = name ?? string.Empty, SourceID = source, LocalID = local, Resolution = resolution };
    private static TrajectoryBatchExportOutcome ExportFailure(TrajectoryBatchFailureKind kind, string error, string message, List<TrajectoryBatchError> errors) =>
        new() { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static TrajectoryBatchRestoreOutcome RestoreFailure(TrajectoryBatchFailureKind kind, string error, string message, List<TrajectoryBatchError> errors) =>
        new() { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static TrajectoryBatchError Error(int? index, string property, string code, string message) =>
        new() { PositionIndex = index, Property = property, Code = code, Message = message };

    private sealed class CatalogResolution
    {
        public CatalogResolution(List<TrajectoryIdentity> identities, List<TrajectoryFeatureCategory> categories)
        { FinalIdentities = [.. identities]; FinalCategories = [.. categories]; }
        public Dictionary<Guid, Guid> IdentityMap { get; } = [];
        public Dictionary<Guid, Guid> CategoryMap { get; } = [];
        public Dictionary<Guid, Guid> OptionMap { get; } = [];
        public List<TrajectoryIdentity> IdentitiesToCreate { get; } = [];
        public List<TrajectoryFeatureCategory> CategoriesToCreate { get; } = [];
        public List<TrajectoryIdentity> FinalIdentities { get; }
        public List<TrajectoryFeatureCategory> FinalCategories { get; }
        public List<TrajectoryBatchCatalogMapping> Mappings { get; } = [];
    }
}
