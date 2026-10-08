using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Trajectory.Service.Mcp.Tools;

/// <summary>Builds the human- and machine-readable MCP contract for reflected REST actions.</summary>
internal static class TrajectoryMcpToolMetadata
{
    private static readonly NullabilityInfoContext Nullability = new();

    private static readonly IReadOnlyDictionary<string, string> ResourceDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Trajectory"] = "a calculated or imported wellbore trajectory and its survey stations",
        ["SurveyRun"] = "a survey run preserving observed MD/inclination/azimuth references and canonical WGS84-geodetic/true-north calculated survey stations",
        ["TrajectoryIdentity"] = "an identity definition shared by survey runs and trajectories",
        ["TrajectoryFeatureCategory"] = "a feature category and its options shared by survey runs and trajectories",
        ["SurveyRunBatchImport"] = "a batch-import definition used to create or update survey runs",
        ["InterpolatedTrajectory"] = "an interpolation case and its calculated trajectory stations",
        ["TrajectoryMinimumDistanceCalculation"] = "a minimum-distance calculation between a reference trajectory and comparison trajectories",
        ["SurveyRunMinimumDistanceCalculation"] = "a minimum-distance calculation between a reference survey run and comparison survey runs",
        ["SurveyStationEllipseCalculation"] = "a survey-station uncertainty-ellipse calculation",
        ["TrajectoryRealizationCase"] = "a stochastic trajectory-realization case",
        ["TrajectoryAggregationCase"] = "a case that aggregates multiple trajectories against a common reference",
        ["TrajectoryExtrapolationCase"] = "an asynchronous extrapolation from a calculated trajectory endpoint",
        ["TargetLandingCase"] = "an asynchronous target-landing design from a calculated trajectory to a convex oriented target plane",
        ["DirectionalControlEvaluationCase"] = "an asynchronous comparison of expected reconnect steering commands with fitted actual trajectory response",
        ["GlobalAntiCollisions"] = "an asynchronous global anti-collision calculation job",
        ["AntiCollisionPolicyRevision"] = "an immutable anti-collision policy revision containing ordered comparison-trajectory conditions and Alert/Alarm thresholds",
        ["FieldAntiCollisionPolicyAssignment"] = "an effective-dated assignment from a Field to one exact immutable anti-collision policy revision",
        ["Octrees"] = "a cached spatial octree generated for a trajectory",
        ["TrajectoryUsageStatistics"] = "the durable per-endpoint usage history and current service counters"
    };

    public static string Describe(string controller, MethodInfo method, string verbs, string? template)
    {
        string resource = ResourceDescriptions.GetValueOrDefault(controller, $"a {SplitWords(controller).ToLowerInvariant()} resource");
        string action = method.Name;
        string route = $"REST operation: {verbs} {controller}{(string.IsNullOrWhiteSpace(template) ? string.Empty : "/" + template)}.";
        string detail;

        if (controller == "Trajectory" && action == "BatchExport")
            detail = "Create a versioned JSON backup of all records or an explicit selection. A selected trajectory automatically includes all survey runs referenced by its sections, and every selected survey run includes its parent chain. The document also carries the identity and feature definitions needed by those records.";
        else if (controller == "Trajectory" && action == "BatchRestore")
            detail = "Restore a versioned dependency-closed backup. The service validates the complete document, matches catalog definitions and options by exact UUID by default, optionally creates missing definitions, writes survey runs before dependent trajectories, and atomically commits record changes without recalculation. Normalized-name mapping of compatible definitions with different UUIDs occurs only when AllowNormalizedNameMapping is explicitly true. Use FailIfExists for a non-destructive import or ReplaceExisting explicitly.";
        else if (controller == "TrajectoryUsageStatistics")
            detail = "Return the service's read-only durable usage-statistics snapshot, including per-day endpoint counters, lifetime request counts, last-use timestamps, and persistence metadata. Use this for operational monitoring; it does not read or modify trajectory engineering data.";
        else if (action == "ValidateExternalReferences")
            detail = controller == "Trajectory"
                ? "Check one stored trajectory's externally owned Field, Cluster, Well and WellBore UUIDs without modifying data. Missing resources are Invalid; configuration, transport, dependency-service and malformed-response failures are Unavailable, never Invalid. Optional unlinked references are valid."
                : "Check one stored survey run's externally owned Field, Cluster, Well, WellBore and SurveyInstrument UUIDs without modifying data. Missing resources are Invalid; configuration, transport, dependency-service and malformed-response failures are Unavailable, never Invalid. Optional unlinked references are valid.";
        else if (action == "AuditExternalReferences")
            detail = $"Check a deterministic UUID-ordered page of all or explicitly selected stored {resource} records without modifying data. Offset must be non-negative and limit is 1 through 100. Results and page counts distinguish Valid, Invalid and Unavailable checks; unavailable dependencies are never reported as missing data.";
        else if (controller == "Octrees" && action == "QueueSearch")
            detail = "Queue an octree uncertainty-volume overlap scan and return immediately with a server-generated job UUID. The broad phase uses a one-cell-padded conservative swept-AABB cover of the 99.9%-confidence envelope at detailed depth 22; because downstream ConfidenceFactor is limited to 0.999, this fixed cover is conservative for every supported separation-factor request. ReferenceTrajectoryID must have a Current index; IncludePlanned and IncludeActual cannot both be false, and DefinitiveOnly controls whether temporary indexes participate. The reference trajectory is excluded from candidates. Poll octrees_get_search_status until Completed or Failed, call octrees_get_search_result only after Completed, then delete the transient job.";
        else if (controller == "Octrees" && action == "GetSearchStatus")
            detail = "Return lightweight state, measured progress from 0 through 1, the current processing stage, and the terminal candidate count for a queued octree scan. Poll while CalculationState is Queued or Running. On Completed retrieve the result; on Failed inspect CalculationMessage and resubmit if appropriate. Jobs are transient in-memory state and terminal jobs expire after one hour, so NotFound after restart or expiry requires a new scan.";
        else if (controller == "Octrees" && action == "GetSearchResult")
            detail = "Return the unique candidate trajectory UUIDs from a Completed octree scan. Candidates satisfy the requested planned/actual and definitive filters and have at least one intersecting cell in the one-cell-padded conservative swept-AABB covers of their 99.9%-confidence uncertainty volumes. This is a broad-phase candidate result and may contain false positives; only the separation-factor calculation determines the relevant measured-depth intervals and safety factors. A Queued, Running or Failed job returns conflict; poll the status tool first. Select the candidates to compare, submit them to global_anti_collisions_post, and delete this transient scan job after consuming the result.";
        else if (controller == "Octrees" && action == "DeleteSearch")
            detail = "Delete a completed or failed transient octree-search job. This does not modify trajectory data or persistent octree indexes; running work is not cancelled.";
        else if (action.StartsWith("Search", StringComparison.Ordinal))
            detail = $"Return one deterministic bounded page of lightweight {resource} records with the total match count. Filter by free text and owned relationship/type fields, use offset for continuation, and keep limit between 1 and 500. Fetch a selected resource by UUID when complete data is needed.";
        else if (controller == "SurveyRun" && action == "PutSurveyMeasurementChunk")
            detail = "Upload or replace one staged measurement chunk. Use a zero-based chunkIndex; chunk.SurveyRunID must equal id and chunk.ChunkIndex must equal chunkIndex. Measurements use MD in metres and Inclination/Azimuth in radians. Upload every chunk, then call the commit tool once to assemble the run and start recalculation.";
        else if (controller == "SurveyRun" && action == "CommitSurveyMeasurementChunks")
            detail = "Commit all previously uploaded survey-measurement chunks for the survey-run id. Call this only after every zero-based chunk has been uploaded; committing assembles the measurements and triggers the survey-station calculation.";
        else if (controller == "SurveyRun" && action == "DeleteSurveyMeasurementChunks")
            detail = "Delete the staged survey-measurement chunks for the survey-run id, for example to abandon or restart an incomplete chunked upload. This does not delete the survey run itself and does not use the persisted survey run's concurrency token.";
        else if (controller == "SurveyRun" && action is "PostSurveyRun" or "PutSurveyRunById")
            detail = "Create or replace a survey run preserving observed references and canonical WGS84-geodetic/true-north angles, then queue calculation. BitExtrapolation is optional. CalculateFromLastMeasurement requires only Measured rows and derives the terminal bit station using the run CalculationType; LastStationAlreadyExtrapolated requires exactly the final row to have Origin Extrapolated and its MD increment to equal the positive SI-metre tool-to-bit distance. Extrapolated rows are not corrected or treated as new instrument observations." +
                (action == "PutSurveyRunById" ? " Supply expectedModifiedUtc exactly as returned by the latest read; a stale token returns conflict." : string.Empty);
        else if (controller == "SurveyStationEllipseCalculation" && action == "PostSurveyRunSurveyStationEllipseCalculation")
            detail = "Calculate and store uncertainty ellipses for display stations using the authoritative source SurveyRun. The service reloads the source, replays its complete parent SurveyRun chain for Wolff-de Wardt and ISCWSA propagation, and replaces stale or partial submitted covariance before calculating ellipses. Requested measured depths must lie within the source run.";
        else if (controller == "SurveyStationEllipseCalculation" && action == "PostTrajectorySurveyStationEllipseCalculation")
            detail = "Calculate and store uncertainty ellipses for display stations using the authoritative source Trajectory. The service rematerializes its SurveyRun sections, replays complete parent SurveyRun chains for Wolff-de Wardt and ISCWSA propagation, and replaces stale or partial submitted covariance before calculating ellipses. Requested measured depths must lie within the source trajectory.";
        else if (controller == "TrajectoryExtrapolationCase" && action == "GetStatus")
            detail = "Return lightweight state, progress and message for a queued trajectory extrapolation. Poll while Queued or Running; after Completed retrieve solved metadata by UUID and sampled survey stations through the chunk-count and zero-based chunk tools.";
        else if (controller == "TargetLandingCase" && action == "GetStatus")
            detail = "Return lightweight state, progress, staleness and message for a queued target-landing calculation. Poll while Queued or Running, then retrieve the completed sampled target zones and drilling solution data by UUID.";
        else if (controller == "DirectionalControlEvaluationCase" && action == "GetStatus")
            detail = "Return lightweight state, progress, staleness and message without transferring interval samples. Poll while Queued or Running, then retrieve the compact bundle statistics and zero-based sample chunks after Completed.";
        else if (controller == "TargetLandingCase" && action == "GetUncertaintyDisplayData")
            detail = "Return a compact, read-only uncertainty projection for the Cartesian target-landing display. The response contains only MD-keyed perpendicular ellipse parameters for the authoritative source trajectory and sampled lead, calculated at the case confidence. It omits trajectory stations, horizontal and vertical ellipses, extreme paths, and landing-path perpendicular ellipses; target-plane landing ellipses are already available in DisplayData.";
        else if (action.Contains("ChunkCount", StringComparison.Ordinal))
            detail = $"Return the number of available result chunks for {resource}. Call this before requesting chunks, then retrieve zero-based chunkIndex values from 0 through count - 1. A count of zero means no chunks are currently available.";
        else if (action.Contains("Chunk", StringComparison.Ordinal) && action.StartsWith("Get", StringComparison.Ordinal))
            detail = $"Return one chunk belonging to {resource}. chunkIndex is zero-based and must be non-negative; call the corresponding chunk-count tool first. Along-hole depths, coordinates and distances are SI metres; angular values are radians.";
        else if (action.Contains("GetAll", StringComparison.Ordinal) && action.EndsWith("Id", StringComparison.Ordinal))
            detail = $"List the identifiers of all stored instances of {resource}. Use an identifier with the corresponding by-id, update or delete tool.";
        else if (action.Contains("MetaInfo", StringComparison.Ordinal))
            detail = $"List only the lightweight MetaInfo records for all stored instances of {resource}; use this for discovery when full numerical payloads are unnecessary.";
        else if (action.Contains("Light", StringComparison.Ordinal))
            detail = $"List lightweight summaries of {resource}, including identity, relationships and calculation state/progress where applicable, without large station or result arrays.";
        else if (action.Contains("Heavy", StringComparison.Ordinal))
            detail = $"List the full stored representations of {resource}. This can return a large payload; prefer the light-list and by-id/chunk tools when selecting a single resource.";
        else if (action.StartsWith("GetAll", StringComparison.Ordinal))
            detail = $"List the full stored representations of {resource}. Optional relationship/type filters are combined to narrow the result. This can return large numerical arrays; prefer the light-list and by-id/chunk tools when full data is unnecessary.";
        else if (action.StartsWith("Get", StringComparison.Ordinal) && action.Contains("ById", StringComparison.Ordinal))
            detail = DescribeById(resource, method);
        else if (controller == "InterpolatedTrajectory" && action == "GetInterpolatedTrajectoryByTrajectoryId")
            detail = "Return the interpolation case associated with the source trajectory UUID. Use this relationship lookup when the interpolation-case UUID is not known; the source trajectory must already have an interpolation case.";
        else if (controller == "TrajectoryAggregationCase" && action == "GetTrajectoryAggregationByCaseAndTrajectoryId")
            detail = "Return the aggregation for one trajectory within an aggregation case. caseId identifies the case and trajectoryId selects its member trajectory. Keep includeResults=false for status/metadata; use true only for inline results, or use the chunk tools for large outputs.";
        else if (controller == "Octrees" && action is "Post" or "Put")
            detail = action == "Post"
                ? "Create a missing derived spatial index from the trajectory's current uncertainty-envelope stations and return its new status/provenance. Normal trajectory writes and startup reconciliation maintain this index automatically; use this operational repair only when status reports Missing. Existing indexes return conflict."
                : "Force an atomic rebuild of the trajectory's derived spatial index from its current uncertainty-envelope stations and return its new status/provenance. Normal trajectory writes and startup reconciliation maintain this index automatically; use this operational repair only when status reports Missing or Stale.";
        else if (controller == "GlobalAntiCollisions" && action == "Put")
            detail = "Replace and requeue an existing separation-factor calculation. The route id and body ID must match, and a Queued or Running job returns conflict rather than racing two calculations. Submit only configuration fields; the service derives state, progress, message, relevant measured-depth ranges and profiles. Poll global_anti_collisions_get_status until Completed or Failed, then retrieve the full result with global_anti_collisions_get_by_id.";
        else if (controller == "GlobalAntiCollisions" && action == "Delete")
            detail = "Delete the durable separation-factor job and its completed results. Do this after retrieving a terminal result; deleting a running job removes its polling record but does not cancel work already executing.";
        else if (controller == "GlobalAntiCollisions" && action == "GetStatus")
            detail = "Return only ID, CalculationState, CalculationProgress, and CalculationMessage as a lightweight status for a durable separation-factor job. Poll while state is Queued or Running instead of repeatedly retrieving the potentially large result. Interrupted work is persisted as Queued and resumes after a normal service restart. Retrieve profiles only after Completed; Failed is terminal until the request is replaced and requeued.";
        else if (controller == "AntiCollisionPolicyRevision" && action == "Post")
            detail = "Create an immutable revision in an anti-collision policy family. PolicyID identifies the family; the service assigns the next RevisionNumber and creation timestamp atomically. Rules use unique explicit priorities and first-match semantics, conditions within a rule are ANDed, and the final lowest-precedence rule must be unconditional. Every rule requires dimensionless AlertThreshold greater than AlarmThreshold greater than zero. ConditionType is a closed discriminator: TrajectoryAge uses SI seconds; Identity matches one catalog definition at a comparison hierarchy level; Feature matches one category/option with explicit temporal semantics.";
        else if (controller == "AntiCollisionPolicyRevision" && action == "GetAll")
            detail = "List immutable anti-collision policy revisions, optionally restricted to one PolicyID family. Use the revision UUID, not merely the family UUID or revision number, for a Field assignment.";
        else if (controller == "AntiCollisionPolicyRevision" && action == "DeletePolicy")
            detail = "Delete an entire anti-collision policy family and all of its immutable revisions only when no current or historical Field assignment references any revision. Supply the latest revision UUID from a fresh read as expectedLatestRevisionId; a concurrently added revision causes a stale-write conflict rather than being deleted.";
        else if (controller == "FieldAntiCollisionPolicyAssignment" && action == "GetEffective")
            detail = "Resolve the one exact policy-revision assignment effective for a Field at the supplied UTC instant. Assignment intervals are non-overlapping half-open UTC intervals; not-found means no policy governs that Field at that instant.";
        else if (controller == "FieldAntiCollisionPolicyAssignment" && action == "Post")
            detail = "Assign one exact immutable policy revision to a Field for a non-overlapping UTC validity interval. The referenced revision must exist. The service derives creation and optimistic-concurrency timestamps; submit only the mutation shape.";
        else if (controller == "FieldAntiCollisionPolicyAssignment" && action == "Put")
            detail = "Replace a Field policy assignment while preserving its UUID. Copy expectedModifiedUtc exactly from the latest read; stale writes conflict. The exact policy revision must exist and the replacement validity interval must not overlap another assignment for the Field.";
        else if (controller == "FieldAntiCollisionPolicyAssignment" && action == "Delete")
            detail = "Delete only a future Field policy assignment. Active and historical assignments are immutable audit records. Copy expectedModifiedUtc exactly from the latest read; stale deletes conflict.";
        else if (action.StartsWith("Post", StringComparison.Ordinal))
            detail = DescribeCreate(controller, resource);
        else if ((controller is "TrajectoryIdentity" or "TrajectoryFeatureCategory") && action.StartsWith("Put", StringComparison.Ordinal))
            detail = $"Replace an existing {resource}. Supply expectedModifiedUtc from the latest LastModificationDate; stale writes return a conflict. Definitions currently referenced by survey runs or trajectories remain protected.";
        else if (action.StartsWith("Put", StringComparison.Ordinal))
            detail = $"Replace an existing instance of {resource}. The route id must be a non-empty UUID and must exactly match data.MetaInfo.ID; the target must already exist. Supply expectedModifiedUtc copied exactly from the latest LastModificationDate; stale writes return conflict. Supply a complete representation because this is a full update, not a partial patch.";
        else if ((controller is "TrajectoryIdentity" or "TrajectoryFeatureCategory") && action.StartsWith("Delete", StringComparison.Ordinal))
            detail = $"Delete an unused {resource}. Supply expectedModifiedUtc from the latest LastModificationDate; referenced definitions and stale writes return a conflict.";
        else if (action.StartsWith("Delete", StringComparison.Ordinal))
            detail = controller == "Octrees"
                ? "Remove one rebuildable derived spatial index without deleting its authoritative trajectory. Routine callers should not do this: subsequent spatial searches omit the trajectory until a rebuild or service-start reconciliation recreates the index."
                : $"Permanently delete one stored instance of {resource}. The id must identify an existing resource. Supply expectedModifiedUtc copied exactly from the latest LastModificationDate; stale deletes return conflict.";
        else if (controller == "Octrees" && action == "GetStatus")
            detail = "Return lightweight provenance and health for one trajectory's derived spatial index. State is one of Missing, NotIndexable, Stale or Current. The response reports source modification time, schema/calculation provenance, bucket count and detailed-code count without returning the large code array.";
        else if (controller == "Octrees" && action == "Get")
            detail = method.GetParameters().All(parameter => parameter.Name is "trajectoryType" or "isDefinitive")
                ? "List trajectory UUIDs having derived spatial indexes. Optional authoritative trajectory-type and definitive-state filters are combined. Use the status tool to inspect currentness and provenance without retrieving large code arrays."
                : "Return the serialized spatial-octree codes cached for the trajectory UUID. These are derived acceleration data for anti-collision and proximity calculations. Normal trajectory writes maintain them automatically; inspect status rather than rebuilding routinely.";
        else if (controller == "GlobalAntiCollisions" && action == "Get")
            detail = method.GetParameters().Length == 0
                ? "List the string identifiers of all global anti-collision calculation jobs. Use an identifier with the by-id tool to inspect progress or retrieve completed results."
                : "Return one separation-factor job and its profile results. Poll global_anti_collisions_get_status first and call this potentially large operation after Completed. Each result identifies one comparison trajectory, gives the relevant reference and comparison measured-depth intervals in SI metres, and contains profile points with ReferenceMD and ComparisonMD in SI metres and a dimensionless SeparationFactor. Disjoint relevant intervals may therefore produce separate plotted line segments in clients.";
        else
            detail = $"Operate on {resource}.";

        return $"{detail} {route}";
    }

    public static JsonObject CreateInputSchema(string controller, MethodInfo method)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        var definitions = new JsonObject();
        var building = new HashSet<Type>();

        foreach (ParameterInfo parameter in method.GetParameters())
        {
            string name = parameter.Name!;
            JsonObject schema = SchemaFor(parameter.ParameterType, definitions, building);
            schema["description"] = DescribeParameter(controller, method.Name, parameter);
            if (TrajectoryProviderSemantics.ForParameter(controller, parameter) is { } parameterMetadata)
                TrajectoryProviderSemantics.AttachMcpPropertyMetadata(schema, parameterMetadata, parameter.ParameterType);
            if (name == "chunkIndex") schema["minimum"] = 0;
            if (name == "offset") schema["minimum"] = 0;
            if (name == "limit")
            {
                schema["minimum"] = 1;
                schema["maximum"] = method.Name == "AuditExternalReferences" ? 100 : 500;
            }
            if (name == "id" && parameter.ParameterType == typeof(string)) schema["minLength"] = 1;
            properties[name] = schema;

            bool isBody = parameter.GetCustomAttribute<FromBodyAttribute>() is not null;
            if (isBody || (!parameter.HasDefaultValue && !IsNullable(parameter))) required.Add(name);
            if (parameter.HasDefaultValue && parameter.DefaultValue is not null)
                schema["default"] = JsonValue.Create(parameter.DefaultValue);
        }

        var result = new JsonObject
        {
            ["type"] = "object",
            ["description"] = $"Arguments for {SplitWords(controller).ToLowerInvariant()} {SplitWords(method.Name).ToLowerInvariant()}.",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
        ApplyInputOperationConstraints(controller, method.Name, definitions);
        if (definitions.Count > 0) result["$defs"] = definitions;
        if (TrajectoryProviderSemantics.ForOperation(controller, method) is { } operationMetadata)
            result[SemanticMetadata.ExtensionName] = operationMetadata;
        return result;
    }

    public static JsonObject CreateOutputSchema(MethodInfo method)
    {
        var definitions = new JsonObject();
        var building = new HashSet<Type>();
        var properties = new JsonObject
        {
            ["status"] = new JsonObject
            {
                ["type"] = "integer",
                ["minimum"] = 200,
                ["maximum"] = 299,
                ["description"] = "HTTP-compatible success status returned by the controller."
            }
        };
        Type? payloadType = ResponsePayloadType(method.ReturnType);
        JsonObject data = payloadType is null
            ? new JsonObject()
            : SchemaFor(payloadType, definitions, building);
        data["description"] = "Successful response payload when the controller returns a body.";
        properties["data"] = data;
        string? controller = method.DeclaringType?.Name.Replace("Controller", "", StringComparison.Ordinal);
        if (controller != null && TrajectoryProviderSemantics.ResourceForController(controller) is { } resource)
        {
            var payload = data["items"] as JsonObject ?? data;
            if (payload["format"]?.ToString() == "uuid")
                payload[SemanticMetadata.ExtensionName] = TrajectoryProviderSemantics.IdentifierForResource(resource);
            payload["x-osdc-resource-type"] = resource;
        }

        var result = new JsonObject
        {
            ["type"] = "object",
            ["description"] = "Successful MCP result. Failed requests are returned as MCP errors with a stable error envelope.",
            ["properties"] = properties,
            ["required"] = payloadType is null ? new JsonArray("status") : new JsonArray("status", "data"),
            ["additionalProperties"] = false
        };
        ApplyOutputOperationConstraints(payloadType, definitions);
        if (definitions.Count > 0) result["$defs"] = definitions;
        return result;
    }

    private static void ApplyInputOperationConstraints(string controller, string action, JsonObject definitions)
    {
        if (controller == "SurveyRun" && action is "PostSurveyRun" or "PutSurveyRunById" &&
            definitions[nameof(SurveyRun)] is JsonObject surveyRun &&
            surveyRun["properties"] is JsonObject surveyRunProperties)
        {
            string[] serverDerived =
            [
                nameof(SurveyRun.CreationDate),
                nameof(SurveyRun.LastModificationDate),
                nameof(SurveyRun.CalculationState),
                nameof(SurveyRun.CalculationProgress),
                nameof(SurveyRun.CalculationMessage),
                nameof(SurveyRun.TieInPoint),
                nameof(SurveyRun.SurveyStationList)
            ];
            foreach (string propertyName in serverDerived) surveyRunProperties.Remove(propertyName);
            if (surveyRun["required"] is JsonArray required)
            {
                foreach (string propertyName in serverDerived)
                {
                    JsonNode? node = required.FirstOrDefault(value => value?.GetValue<string>() == propertyName);
                    if (node != null) required.Remove(node);
                }
            }
            surveyRun["description"] = "Closed SurveyRun mutation. The server owns timestamps, tie-in resolution, calculation state and calculated stations; BitExtrapolation contains only the caller-selected mode and frozen positive SI tool-to-bit distance.";
        }

        if (controller == "Octrees" && action == "QueueSearch" &&
            definitions[nameof(OctreeSearchJobRequest)] is JsonObject searchRequest)
        {
            // Omitted flags retain their true model defaults. Explicitly setting both false is invalid.
            searchRequest["not"] = new JsonObject
            {
                ["properties"] = new JsonObject
                {
                    [nameof(OctreeSearchJobRequest.IncludePlanned)] = new JsonObject { ["const"] = false },
                    [nameof(OctreeSearchJobRequest.IncludeActual)] = new JsonObject { ["const"] = false }
                },
                ["required"] = new JsonArray(nameof(OctreeSearchJobRequest.IncludePlanned), nameof(OctreeSearchJobRequest.IncludeActual))
            };
        }

        if (controller == "TrajectoryExtrapolationCase" && action is "Post" or "Put" &&
            definitions[nameof(TrajectoryExtrapolationCase)] is JsonObject extrapolation &&
            extrapolation["properties"] is JsonObject extrapolationProperties)
        {
            string[] serverDerived =
            [
                nameof(TrajectoryExtrapolationCase.CreationDate),
                nameof(TrajectoryExtrapolationCase.LastModificationDate),
                nameof(TrajectoryExtrapolationCase.CalculationState),
                nameof(TrajectoryExtrapolationCase.CalculationProgress),
                nameof(TrajectoryExtrapolationCase.CalculationMessage),
                nameof(TrajectoryExtrapolationCase.StartStation),
                nameof(TrajectoryExtrapolationCase.TargetStation),
                nameof(TrajectoryExtrapolationCase.ClosestReferenceMD),
                nameof(TrajectoryExtrapolationCase.TargetReferenceMD),
                nameof(TrajectoryExtrapolationCase.SourceTrajectoryRevision),
                nameof(TrajectoryExtrapolationCase.ReferenceTrajectoryRevision),
                nameof(TrajectoryExtrapolationCase.SolvedSectionList),
                nameof(TrajectoryExtrapolationCase.SurveyStationList)
            ];
            foreach (string propertyName in serverDerived) extrapolationProperties.Remove(propertyName);
            extrapolation["required"] = new JsonArray(
                nameof(TrajectoryExtrapolationCase.MetaInfo),
                nameof(TrajectoryExtrapolationCase.SourceTrajectoryID),
                nameof(TrajectoryExtrapolationCase.Mode),
                nameof(TrajectoryExtrapolationCase.InterpolationInterval),
                nameof(TrajectoryExtrapolationCase.Specification));
            extrapolation["description"] = "Closed submission for an asynchronous trajectory extrapolation. The server derives timestamps, source/target snapshots, solved sections, calculation state and sampled stations.";
        }

        if (controller == "TargetLandingCase" && action is "Post" or "Put" &&
            definitions[nameof(TargetLandingCase)] is JsonObject landingCase &&
            landingCase["properties"] is JsonObject landingProperties)
        {
            string[] serverDerived =
            [
                nameof(TargetLandingCase.CreationDate),
                nameof(TargetLandingCase.LastModificationDate),
                nameof(TargetLandingCase.CalculationState),
                nameof(TargetLandingCase.CalculationProgress),
                nameof(TargetLandingCase.CalculationMessage),
                nameof(TargetLandingCase.IsStale),
                nameof(TargetLandingCase.SourceTrajectoryRevision),
                nameof(TargetLandingCase.CalculationFingerprint),
                nameof(TargetLandingCase.SourceEndStation),
                nameof(TargetLandingCase.LeadSurveyStationList),
                nameof(TargetLandingCase.SteeringStartStation),
                nameof(TargetLandingCase.GeologicalTargetBoundary),
                nameof(TargetLandingCase.DrillerTargetBoundary),
                nameof(TargetLandingCase.ReachableTargetBoundary),
                nameof(TargetLandingCase.DrillerTargetContourList),
                nameof(TargetLandingCase.ReachableTargetContourList),
                nameof(TargetLandingCase.SampleList),
                nameof(TargetLandingCase.MeshTriangleList)
            ];
            foreach (string propertyName in serverDerived) landingProperties.Remove(propertyName);
            landingCase["required"] = new JsonArray(
                nameof(TargetLandingCase.MetaInfo),
                nameof(TargetLandingCase.SourceTrajectoryID),
                nameof(TargetLandingCase.Target));
            landingCase["description"] = "Closed target-landing submission. Plane coordinates are canonical local North/East/WGS84 depth metres with WGS84 latitude/longitude and normal inclination/true-north azimuth in radians. The service derives timestamps, calculation state, source revision, adaptive samples, zones and landing solutions.";

            SetNumericBounds(landingProperties[nameof(TargetLandingCase.LeadLength)], 0.0, null);
            SetNumericBounds(landingProperties[nameof(TargetLandingCase.ConfidenceFactor)], 0.0,
                SurveyStationEllipseCalculation.MaximumConfidenceFactor, exclusiveMinimum: true);
            SetNumericBounds(landingProperties[nameof(TargetLandingCase.MaximumLandingCurvature)], 0.0, null,
                exclusiveMinimum: true);
        }

        if (controller == "DirectionalControlEvaluationCase" && action is "Post" or "Put" &&
            definitions[nameof(DirectionalControlEvaluationCase)] is JsonObject evaluation &&
            evaluation["properties"] is JsonObject evaluationProperties)
        {
            string[] serverDerived =
            [
                nameof(DirectionalControlEvaluationCase.CreationDate),
                nameof(DirectionalControlEvaluationCase.LastModificationDate),
                nameof(DirectionalControlEvaluationCase.CalculationState),
                nameof(DirectionalControlEvaluationCase.CalculationProgress),
                nameof(DirectionalControlEvaluationCase.CalculationMessage),
                nameof(DirectionalControlEvaluationCase.IsStale),
                nameof(DirectionalControlEvaluationCase.ReferenceTrajectoryRevision),
                nameof(DirectionalControlEvaluationCase.ActualTrajectoryRevision),
                nameof(DirectionalControlEvaluationCase.CalculationFingerprint),
                nameof(DirectionalControlEvaluationCase.SampleList),
                nameof(DirectionalControlEvaluationCase.BundleList)
            ];
            foreach (string propertyName in serverDerived) evaluationProperties.Remove(propertyName);
            evaluation["required"] = new JsonArray(
                nameof(DirectionalControlEvaluationCase.MetaInfo),
                nameof(DirectionalControlEvaluationCase.ReferenceTrajectoryID),
                nameof(DirectionalControlEvaluationCase.ActualTrajectoryID),
                nameof(DirectionalControlEvaluationCase.CurveType));
            evaluation["description"] = "Closed directional-control evaluation submission. Both trajectories must belong to one wellbore. MD values are SI metres, angles SI radians, and curvature/build/turn rates SI radians per metre. The service derives timestamps, progress, revisions, samples, empirical distributions and bundles.";
            SetNumericBounds(evaluationProperties[nameof(DirectionalControlEvaluationCase.EvaluationInterval)], 0.0, null, exclusiveMinimum: true);
            SetNumericBounds(evaluationProperties[nameof(DirectionalControlEvaluationCase.ReferenceMDAdvance)], 0.0, null, exclusiveMinimum: true);
            SetNumericBounds(evaluationProperties[nameof(DirectionalControlEvaluationCase.MaximumInvalidGap)], 0.0, null, exclusiveMinimum: true);
            SetNumericBounds(evaluationProperties[nameof(DirectionalControlEvaluationCase.MinimumBundleLength)], 0.0, null, exclusiveMinimum: true);
            SetNumericBounds(evaluationProperties[nameof(DirectionalControlEvaluationCase.MinimumBundleSampleCount)], 2.0, null);
            SetNumericBounds(evaluationProperties[nameof(DirectionalControlEvaluationCase.BundlingPenalty)], 0.0, null, exclusiveMinimum: true);
        }

        if (controller != "GlobalAntiCollisions" || action is not ("Post" or "Put") ||
            definitions[nameof(GlobalAntiCollision.GlobalAntiCollision)] is not JsonObject calculation ||
            calculation["properties"] is not JsonObject properties)
        {
            return;
        }

        // These values are exclusively owned by the worker. Omitting them from the closed MCP
        // input shape prevents callers from presenting derived state or results as facts.
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationState));
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationProgress));
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationMessage));
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.SeparationFactorResults));
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.PolicyEvaluationUtc));
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.PolicyAssignmentID));
        properties.Remove(nameof(GlobalAntiCollision.GlobalAntiCollision.PolicySnapshot));
        definitions.Remove(nameof(SeparationFactorResult));
        definitions.Remove(nameof(SeparationFactorPoint));
        definitions.Remove(nameof(MeasuredDepthRange));

        if (properties[nameof(GlobalAntiCollision.GlobalAntiCollision.ComparisonTrajectoryIDs)] is JsonObject comparisons)
        {
            comparisons["minItems"] = 1;
            comparisons["uniqueItems"] = true;
        }

        // The inactive alternative may be omitted or explicitly Guid.Empty on the REST DTO, but
        // exactly one reference must be non-empty in an MCP submission.
        foreach (string referenceName in new[]
                 {
                     nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceTrajectoryID),
                     nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceWellPathID)
                 })
        {
            if (properties[referenceName] is JsonObject referenceSchema)
            {
                referenceSchema.Remove("not");
            }
        }

        string emptyUuid = Guid.Empty.ToString();
        calculation["oneOf"] = new JsonArray(
            ReferenceChoice(nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceTrajectoryID),
                nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceWellPathID), emptyUuid),
            ReferenceChoice(nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceWellPathID),
                nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceTrajectoryID), emptyUuid));
        calculation["required"] = new JsonArray(
            nameof(GlobalAntiCollision.GlobalAntiCollision.ID),
            nameof(GlobalAntiCollision.GlobalAntiCollision.ConfidenceFactor),
            nameof(GlobalAntiCollision.GlobalAntiCollision.ComparisonTrajectoryIDs));
        calculation["description"] = "Closed MCP submission for an asynchronous separation-factor job. Exactly one reference identifier must be non-empty; calculation state and results are server-derived.";
    }

    private static JsonObject ReferenceChoice(string selectedName, string inactiveName, string emptyUuid) => new()
    {
        ["properties"] = new JsonObject
        {
            [selectedName] = new JsonObject { ["not"] = new JsonObject { ["const"] = emptyUuid } },
            [inactiveName] = new JsonObject { ["const"] = emptyUuid }
        },
        ["required"] = new JsonArray(selectedName)
    };

    private static void SetNumericBounds(JsonNode? node, double? minimum, double? maximum,
        bool exclusiveMinimum = false)
    {
        if (node is not JsonObject schema) return;
        if (minimum.HasValue) schema[exclusiveMinimum ? "exclusiveMinimum" : "minimum"] = minimum.Value;
        if (maximum.HasValue) schema["maximum"] = maximum.Value;
    }

    private static void ApplyOutputOperationConstraints(Type? payloadType, JsonObject definitions)
    {
        if (payloadType == typeof(OctreeSearchJobStatus) &&
            definitions[nameof(OctreeSearchJobStatus)] is JsonObject searchStatus)
        {
            searchStatus["allOf"] = new JsonArray(
                TerminalStatusRule(nameof(CalculationState.Completed), requireCandidateCount: true),
                TerminalStatusRule(nameof(CalculationState.Failed), requireCandidateCount: false));
        }
        else if (payloadType == typeof(OctreeSearchJobResult) &&
                 definitions[nameof(OctreeSearchJobResult)]?["properties"]?[nameof(OctreeSearchJobResult.CandidateTrajectoryIDs)] is JsonObject candidates)
        {
            candidates["uniqueItems"] = true;
        }
        else if (payloadType == typeof(GlobalAntiCollision.GlobalAntiCollision) &&
                 definitions[nameof(GlobalAntiCollision.GlobalAntiCollision)] is JsonObject calculation)
        {
            calculation["required"] = new JsonArray(
                nameof(GlobalAntiCollision.GlobalAntiCollision.ID),
                nameof(GlobalAntiCollision.GlobalAntiCollision.ConfidenceFactor),
                nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceWellPathID),
                nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceTrajectoryID),
                nameof(GlobalAntiCollision.GlobalAntiCollision.ComparisonTrajectoryIDs),
                nameof(GlobalAntiCollision.GlobalAntiCollision.SeparationFactorResults),
                nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationState),
                nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationProgress));
        }
        else if (payloadType == typeof(GlobalAntiCollisionCalculationStatus) &&
                 definitions[nameof(GlobalAntiCollisionCalculationStatus)] is JsonObject calculationStatus)
        {
            calculationStatus["required"] = new JsonArray(
                nameof(GlobalAntiCollisionCalculationStatus.ID),
                nameof(GlobalAntiCollisionCalculationStatus.CalculationState),
                nameof(GlobalAntiCollisionCalculationStatus.CalculationProgress));
        }
    }

    private static JsonObject TerminalStatusRule(string terminalState, bool requireCandidateCount)
    {
        var required = new JsonArray(nameof(OctreeSearchJobStatus.CompletedUtc));
        if (requireCandidateCount)
        {
            required.Add(nameof(OctreeSearchJobStatus.CandidateCount));
        }
        return new JsonObject
        {
            ["if"] = new JsonObject
            {
                ["properties"] = new JsonObject
                {
                    [nameof(OctreeSearchJobStatus.CalculationState)] = new JsonObject { ["const"] = terminalState }
                },
                ["required"] = new JsonArray(nameof(OctreeSearchJobStatus.CalculationState))
            },
            ["then"] = new JsonObject { ["required"] = required }
        };
    }

    public static McpToolBehavior CreateBehavior(string controller, MethodInfo method, string verbs)
    {
        string[] methods = verbs.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool readOnly = (methods.Length > 0 && methods.All(value => value == "GET")) ||
                        (controller == "Trajectory" && method.Name == "BatchExport") ||
                        method.Name == "AuditExternalReferences";
        bool destructive = methods.Contains("DELETE", StringComparer.Ordinal) ||
                           (controller == "Trajectory" && method.Name == "BatchRestore");
        bool idempotent = readOnly || methods.Contains("PUT", StringComparer.Ordinal) ||
                          methods.Contains("DELETE", StringComparer.Ordinal);
        string title = $"{SplitWords(controller)} — {SplitWords(method.Name)}";
        return new McpToolBehavior(title, readOnly, destructive, idempotent);
    }

    private static string DescribeById(string resource, MethodInfo method)
    {
        var options = method.GetParameters().Where(p => p.Name != "id").Select(p => p.Name).ToArray();
        string optionText = options.Length == 0 ? string.Empty :
            $" Optional flags ({string.Join(", ", options)}) control whether large calculated arrays are embedded; leave them false for metadata/status and use chunk endpoints for large data.";
        return $"Return one stored instance of {resource} by its non-empty UUID.{optionText} A missing identifier returns not found.";
    }

    private static string DescribeCreate(string controller, string resource)
    {
        if (controller == "InterpolatedTrajectory")
            return "Create an interpolation case for one stored trajectory and calculate a regularly sampled station sequence using the selected interpolation method and SI-metre interval. The service persists compact metadata separately from calculated stations; poll the light or by-id representation and retrieve large station results through chunk-count followed by zero-based chunks.";
        if (controller == "TrajectoryRealizationCase")
            return "Create a stochastic realization case from one uncertainty-bearing stored trajectory. The service rematerializes authoritative SurveyRun ancestry, optionally coarsens the source, samples the requested number of covariance-based alternatives, completes them with minimum curvature, and stores realizations in chunks. Poll CalculationState/CalculationProgress and retrieve completed realization chunks rather than requesting the heavy case.";
        if (controller == "TrajectoryAggregationCase")
            return "Create an aggregation case that approximates each selected source trajectory with a compact chain of fitted sections over common measured-depth periods. The result preserves trajectory direction and displacement, reports approximation error, and samples the fitted chain at the SI-metre interpolation interval with DLS, build rate and turn rate. Poll the light case and retrieve per-trajectory results or result chunks.";
        if (controller is "TrajectoryMinimumDistanceCalculation" or "SurveyRunMinimumDistanceCalculation")
            return $"Create {resource} and queue pairwise closest-approach calculation against the selected comparison resources. The service uses the configured octree refinement and persists compact case state separately from the potentially large result set. Poll CalculationState/CalculationProgress, then call result chunk-count and retrieve zero-based result chunks; distances and measured depths are SI metres.";
        if (controller == "SurveyStationEllipseCalculation")
            return "Create a standalone survey-station uncertainty-ellipse calculation for a complete station history. Prefer the SurveyRun- or Trajectory-specific creation tools for stored resources because they replay authoritative ancestry. The result contains horizontal, vertical and perpendicular confidence ellipses plus extreme paths; lengths are SI metres, angles radians and confidence is a dimensionless proportion.";
        if (controller == "SurveyRunBatchImport")
            return "Create and persist a reusable batch-import definition containing external survey-file rows, ownership selections, parsing columns/units/references, and replacement choices used to create or update SurveyRuns. This operation stores the definition; it does not itself execute the file import. Parsed SurveyRuns use canonical SI metres/radians and become authoritative inputs for trajectory composition.";
        if (controller == "TrajectoryExtrapolationCase")
            return "Create a trajectory extrapolation case and queue its calculation. Select exactly one discriminated specification matching Mode. FixedLength extends straight or continues the fitted last section; ReconnectToTrajectory optionally continues a lead-in, advances from the closest point on a reference trajectory, and solves two steering sections; Geosteering optionally continues a lead-in then reaches a target depth and attitude using either overall departure/bearing or steering length/steering-length ratio; WellPath requires exactly 3 × section-count constraints. The server derives timestamps, endpoint snapshots, solved sections and sampled stations. Poll status, then retrieve station chunks. SI units are metres, radians, and radians per metre.";
        if (controller == "TargetLandingCase")
            return "Create a target-landing case and queue adaptive calculation over its convex planar target. The source is a stored trajectory. Free landing attitude uses one CA, BT or CTC section; Land perpendicular to target plane uses the corresponding two-section solution. Geological targets require the projected confidence ellipse to remain inside the specified polygon. Maximum Landing Curvature applies only to newly designed landing sections. The server retains the shortest drilling-relevant forward solution and derives all samples, zones, result sections and staleness metadata. SI units are metres, radians and radians per metre.";
        if (controller == "DirectionalControlEvaluationCase")
            return "Create and queue a directional-control evaluation. Select reference and actual calculated trajectories from the same wellbore and one CA, BT or CTC curve family. At each actual-MD interval the service solves the first reconnect section toward closest-reference-MD plus the configured correction length, using the shortest azimuth branch, fits the actual interval with that same exact curve family, compares the linked command pair, and detects joint depth bundles. There is no assumed command delay or lead-in. Poll status, read compact bundle statistics, then page interval samples. SI units are metres, radians, and radians per metre.";
        if (controller is "Trajectory" or "SurveyRun")
            return $"Create {resource} and calculate its survey stations. data.MetaInfo.ID must be a caller-assigned, non-empty UUID that is not already stored. Identity and feature assignments must reference the shared catalogs; exclusive feature periods must not overlap. For very large survey runs, create the run first and use the survey-measurement chunk upload/commit workflow. Supply SI values: lengths/depths in metres, angles in radians and curvature in radians per metre.";
        if (controller is "TrajectoryIdentity" or "TrajectoryFeatureCategory")
            return $"Create {resource}. MetaInfo.ID must be a caller-assigned, non-empty UUID. Feature option IDs must also be non-empty UUIDs.";
        if (controller == "GlobalAntiCollisions")
            return "Create and queue a separation-factor calculation after octree candidate discovery. Supply a unique string ID, exactly one non-empty reference trajectory or well-path ID, at least one unique non-empty comparison trajectory UUID, and a confidence factor greater than 0 and at most 0.999. RequestedPolicyAssignmentID is optional: omit it for no policy classification, or supply an assignment belonging to the reference trajectory's Field; the service then derives the policy confidence factor and frozen policy evaluation. Do not submit calculation state, progress, message, applied assignment, policy snapshot, ranges or results: the service derives them and restricts work to geometrically relevant measured-depth intervals. The service returns immediately and continues independently of the caller. Poll global_anti_collisions_get_status while Queued or Running; after Completed retrieve profiles with global_anti_collisions_get_by_id, then delete the job when no longer needed.";
        return $"Create {resource}. data.MetaInfo.ID must be a caller-assigned, non-empty UUID that is not already stored; duplicate identifiers are rejected. Supply SI values: lengths/depths in metres, angles in radians and curvature in radians per metre.";
    }

    private static string DescribeParameter(string controller, string action, ParameterInfo parameter)
    {
        string name = parameter.Name!;
        return name switch
        {
            "id" when controller == "GlobalAntiCollisions" => "Unique string identifier of the global anti-collision configuration.",
            "id" when controller == "Octrees" => "Non-empty UUID of the trajectory whose spatial octree is addressed.",
            "id" => $"Non-empty UUID of the {SplitWords(controller).ToLowerInvariant()} resource.",
            "policyId" => "Stable non-empty UUID of the complete anti-collision policy family.",
            "expectedLatestRevisionId" => "Optimistic-concurrency token: the UUID of the latest immutable revision returned by a fresh policy read.",
            "jobId" when controller == "Octrees" => "Server-generated non-empty UUID of the transient octree-search job.",
            "caseId" => "Non-empty UUID of the trajectory aggregation case.",
            "trajectoryId" when controller == "TrajectoryAggregationCase" => "Non-empty UUID of the trajectory within the aggregation case.",
            "trajectoryId" => "Non-empty UUID of the source trajectory.",
            "fieldId" => "Optional field UUID filter; omit it to include resources from every field.",
            "clusterId" => "Optional cluster UUID filter; omit it to include resources from every cluster.",
            "wellId" => "Optional well UUID filter; omit it to include resources from every well.",
            "wellBoreId" => "Optional wellbore UUID filter; omit it to include resources from every wellbore.",
            "surveyInstrumentId" => "Optional survey-instrument UUID filter; omit it to include every instrument.",
            "trajectoryType" => "Optional trajectory-type enum filter (for example Actual or Planned); omit it to include every type.",
            "surveyRunType" => "Optional survey-run-type enum filter; omit it to include every type.",
            "isDefinitive" => "Optional filter for the definitive trajectory flag; omit it to include both definitive and non-definitive trajectories.",
            "includePlanned" => "Include spatially overlapping planned trajectories in the candidate result.",
            "includeActual" => "Include spatially overlapping actual trajectories in the candidate result.",
            "definitiveOnly" => "When true, include only definitive trajectories; false also includes temporary trajectories.",
            "chunkIndex" => "Zero-based index of the requested or uploaded chunk; must be non-negative.",
            "includeResults" => "When true, embed calculated result arrays; false (default) returns the case and status without large results. Prefer chunk tools for large results.",
            "includeRealizations" => "When true, embed all stochastic realization arrays; false (default) omits them. Prefer realization chunks for large results.",
            "includeMeasurements" => "When true, include the survey measurement list in the response; false (default) omits it.",
            "includeCalculatedStations" => "When true, include calculated survey stations; false (default) omits them. Prefer station chunks for large runs.",
            "expectedModifiedUtc" => "Optimistic-concurrency token copied exactly from the resource's latest LastModificationDate.",
            "query" => "Optional case-insensitive text matched against name, description, and UUID.",
            "offset" => "Zero-based number of matching records to skip; must be non-negative.",
            "limit" => "Maximum page size from 1 through 500; defaults to 100.",
            "chunk" => "Complete survey-measurement chunk. SurveyRunID and ChunkIndex must match the route arguments. MD is SI metres; observed and canonical angles are SI radians. Inclination/Azimuth are canonical WGS84-geodetic/true-north values, while ObservedInclination/ObservedAzimuth retain the submitted reference-frame values.",
            "request" when controller == "Trajectory" && action == "BatchExport" => "Backup scope and optional survey-run and trajectory UUID selections. For Selected, provide at least one UUID; dependent survey runs are added automatically.",
            "request" when controller == "Trajectory" && action == "BatchRestore" => "Complete backup document plus record-conflict and catalog-resolution policies. Restore validates the full graph before writing records.",
            "request" when controller == "Octrees" && action == "QueueSearch" => "Octree search filters and the non-empty UUID of the current reference trajectory index.",
            "request" when action == "AuditExternalReferences" => "Audit scope (All or Selected), optional selected resource UUIDs, and deterministic offset/limit page. Selected UUIDs must be non-empty and unique; limit is 1 through 100.",
            "data" => $"Complete {SplitWords(controller).ToLowerInvariant()} JSON representation. Follow the nested schema and SI-unit annotations.",
            "value" when controller == "GlobalAntiCollisions" => "Separation-factor job configuration. Supply ID, ConfidenceFactor, exactly one reference identifier, and unique selected comparison trajectory UUIDs. Server-derived calculation and result fields are not accepted by MCP.",
            "value" when controller == "TrajectoryExtrapolationCase" => "Trajectory extrapolation configuration using the Mode discriminator and its matching specification. Timestamps, calculation state, frozen endpoints, solved sections and station results are server-derived and forbidden in MCP submissions.",
            "value" when controller == "TargetLandingCase" => "Target-landing configuration with a source trajectory, convex oriented target plane, curve/attitude choices, confidence factor and optional Maximum Landing Curvature. Calculation state, source revision, adaptive samples, zones and solutions are server-derived and forbidden in MCP submissions.",
            "value" when controller == "DirectionalControlEvaluationCase" => "Directional-control configuration with same-wellbore reference and actual trajectory UUIDs, one curve family, actual-MD range/interval, correction length, and bundling thresholds. The shortest azimuth branch is fixed by the service. Timestamps, progress, trajectory revisions, samples, distributions and bundles are server-derived and forbidden in MCP submissions.",
            "value" => $"Complete {SplitWords(controller).ToLowerInvariant()} JSON representation.",
            _ => $"Value for {SplitWords(action).ToLowerInvariant()}."
        };
    }

    private static JsonObject SchemaFor(Type declaredType, JsonObject definitions, HashSet<Type> building)
    {
        Type type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        if (type == typeof(Guid))
            return new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["not"] = new JsonObject { ["const"] = Guid.Empty.ToString() }
            };
        if (type == typeof(string) || type == typeof(char)) return new JsonObject { ["type"] = "string" };
        if (type == typeof(bool)) return new JsonObject { ["type"] = "boolean" };
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return new JsonObject { ["type"] = "string", ["format"] = "date-time" };
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong))
            return new JsonObject { ["type"] = "integer" };
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return new JsonObject { ["type"] = "number" };
        if (type.IsEnum)
        {
            var enumSchema = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(Enum.GetNames(type).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray())
            };
            string? description = DescribeReferenceEnum(type);
            if (description != null) enumSchema["description"] = description;
            return enumSchema;
        }
        if (TryGetDictionaryValue(type, out Type? valueType))
            return new JsonObject { ["type"] = "object", ["additionalProperties"] = SchemaFor(valueType!, definitions, building) };
        if (TryGetEnumerableElement(type, out Type? elementType))
            return new JsonObject { ["type"] = "array", ["items"] = SchemaFor(elementType!, definitions, building) };

        string definitionName = DefinitionName(type);
        if (!definitions.ContainsKey(definitionName))
        {
            if (!building.Add(type)) return new JsonObject { ["$ref"] = $"#/$defs/{definitionName}" };
            definitions[definitionName] = BuildObjectDefinition(type, definitions, building);
            building.Remove(type);
        }
        return new JsonObject { ["$ref"] = $"#/$defs/{definitionName}" };
    }

    private static JsonObject BuildObjectDefinition(Type type, JsonObject definitions, HashSet<Type> building)
    {
        JsonDerivedTypeAttribute[] derivedTypes = type.GetCustomAttributes<JsonDerivedTypeAttribute>().ToArray();
        if (derivedTypes.Length > 0)
        {
            var alternatives = new JsonArray();
            foreach (JsonDerivedTypeAttribute derived in derivedTypes)
            {
                alternatives.Add(SchemaFor(derived.DerivedType, definitions, building));
            }
            return new JsonObject
            {
                ["description"] = $"Discriminated {SplitWords(type.Name)} variant.",
                ["oneOf"] = alternatives
            };
        }

        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.GetMethod is not null && property.GetIndexParameters().Length == 0)
                     .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                     .OrderBy(property => property.MetadataToken))
        {
            JsonObject schema = SchemaFor(property.PropertyType, definitions, building);
            if (IsNullable(property))
            {
                schema = AllowNull(schema);
            }
            ApplyDomainConstraints(type, property, schema, required);
            schema["description"] = TrajectoryProviderSemantics.PropertyDescription(type, property.Name) ?? DescribeProperty(type, property.Name);
            if (TrajectoryProviderSemantics.ForProperty(property) is { } propertyMetadata)
                TrajectoryProviderSemantics.AttachMcpPropertyMetadata(schema, propertyMetadata, property.PropertyType);
            properties[property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name] = schema;
        }

        Type? polymorphicBase = type.BaseType;
        JsonPolymorphicAttribute? polymorphic = polymorphicBase?.GetCustomAttribute<JsonPolymorphicAttribute>();
        JsonDerivedTypeAttribute? derivedMapping = polymorphicBase?.GetCustomAttributes<JsonDerivedTypeAttribute>()
            .FirstOrDefault(attribute => attribute.DerivedType == type);
        if (polymorphic != null && derivedMapping?.TypeDiscriminator is string discriminatorValue)
        {
            string discriminatorName = polymorphic.TypeDiscriminatorPropertyName ?? "$type";
            properties[discriminatorName] = new JsonObject
            {
                ["type"] = "string",
                ["const"] = discriminatorValue,
                ["description"] = $"Selects the {discriminatorValue} variant."
            };
            required.Add(discriminatorName);
        }
        var definition = new JsonObject
        {
            ["type"] = "object",
            ["description"] = $"JSON representation of {SplitWords(type.Name)}.",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };
        if (TrajectoryProviderSemantics.ForType(type) is { } typeMetadata)
            definition[SemanticMetadata.ExtensionName] = typeMetadata;
        if (required.Count > 0) definition["required"] = required;
        if (type == typeof(AntiCollisionFeatureCondition))
        {
            definition["allOf"] = new JsonArray(
                TemporalVariantRule(nameof(AntiCollisionFeatureTemporalOperator.ActiveAtSpecifiedTime),
                    [nameof(AntiCollisionFeatureCondition.SpecifiedTimeUtc)],
                    [nameof(AntiCollisionFeatureCondition.SpecifiedFromUtc), nameof(AntiCollisionFeatureCondition.SpecifiedToUtc)]),
                TemporalVariantRule(nameof(AntiCollisionFeatureTemporalOperator.OverlapsSpecifiedInterval),
                    [nameof(AntiCollisionFeatureCondition.SpecifiedFromUtc), nameof(AntiCollisionFeatureCondition.SpecifiedToUtc)],
                    [nameof(AntiCollisionFeatureCondition.SpecifiedTimeUtc)]));
        }
        return definition;
    }

    private static JsonObject TemporalVariantRule(string discriminator, string[] requiredWhenSelected, string[] forbiddenWhenSelected) => new()
    {
        ["if"] = new JsonObject
        {
            ["properties"] = new JsonObject { [nameof(AntiCollisionFeatureCondition.TemporalOperator)] = new JsonObject { ["const"] = discriminator } },
            ["required"] = new JsonArray(nameof(AntiCollisionFeatureCondition.TemporalOperator))
        },
        ["then"] = new JsonObject
        {
            ["required"] = new JsonArray(requiredWhenSelected.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()),
            ["not"] = new JsonObject { ["anyOf"] = new JsonArray(forbiddenWhenSelected.Select(value => (JsonNode?)new JsonObject { ["required"] = new JsonArray(value) }).ToArray()) }
        },
        ["else"] = new JsonObject
        {
            ["not"] = new JsonObject { ["anyOf"] = new JsonArray(requiredWhenSelected.Select(value => (JsonNode?)new JsonObject { ["required"] = new JsonArray(value) }).ToArray()) }
        }
    };

    private static void ApplyDomainConstraints(Type declaringType, PropertyInfo property, JsonObject schema, JsonArray required)
    {
        if ((typeof(SurveyRunLight).IsAssignableFrom(declaringType) || declaringType == typeof(SurveyImportSettings)) &&
            property.Name == nameof(SurveyRunLight.DefaultInclinationReference))
        {
            schema["enum"] = new JsonArray(nameof(SurveyInclinationReference.GeodeticVertical), nameof(SurveyInclinationReference.GravityVertical));
            required.Add(property.Name);
        }
        else if ((typeof(SurveyRunLight).IsAssignableFrom(declaringType) || declaringType == typeof(SurveyImportSettings)) &&
                 property.Name == nameof(SurveyRunLight.DefaultAzimuthReference))
        {
            schema["enum"] = new JsonArray(nameof(SurveyAzimuthReference.TrueNorth), nameof(SurveyAzimuthReference.MagneticNorth));
            required.Add(property.Name);
        }
        else
        if (declaringType == typeof(TrajectoryBatchExportRequest) && property.Name == nameof(TrajectoryBatchExportRequest.Scope))
        {
            schema["enum"] = new JsonArray("All", "Selected");
            required.Add(property.Name);
        }
        else if (declaringType == typeof(TrajectoryBatchRestoreRequest))
        {
            if (property.Name == nameof(TrajectoryBatchRestoreRequest.ConflictPolicy))
                schema["enum"] = new JsonArray("FailIfExists", "ReplaceExisting");
            else if (property.Name == nameof(TrajectoryBatchRestoreRequest.CatalogPolicy))
                schema["enum"] = new JsonArray("MapExisting", "MapOrCreateMissing");
            required.Add(property.Name);
        }
        else if (declaringType == typeof(TrajectoryBatchExportDocument))
        {
            required.Add(property.Name);
            if (property.Name == nameof(TrajectoryBatchExportDocument.FormatIdentifier))
                schema["const"] = TrajectoryBatchExportDocument.CurrentFormatIdentifier;
            else if (property.Name == nameof(TrajectoryBatchExportDocument.SchemaVersion))
                schema["const"] = TrajectoryBatchExportDocument.CurrentSchemaVersion;
        }
        else if (declaringType == typeof(TrajectoryExternalReferenceAuditRequest) ||
                 declaringType == typeof(SurveyRunExternalReferenceAuditRequest))
        {
            if (property.Name == "Scope") required.Add(property.Name);
            else if (property.Name == "Offset") schema["minimum"] = 0;
            else if (property.Name == "Limit")
            {
                schema["minimum"] = 1;
                schema["maximum"] = 100;
            }
            else if (property.Name is "TrajectoryIDs" or "SurveyRunIDs")
            {
                schema["minItems"] = 1;
                schema["uniqueItems"] = true;
            }
        }
        else if (declaringType == typeof(OctreeIndexStatus) && property.Name is
                 nameof(OctreeIndexStatus.TrajectoryID) or
                 nameof(OctreeIndexStatus.State) or
                 nameof(OctreeIndexStatus.HasIndex) or
                 nameof(OctreeIndexStatus.IsCurrent) or
                 nameof(OctreeIndexStatus.TrajectoryType) or
                 nameof(OctreeIndexStatus.IsDefinitive) or
                 nameof(OctreeIndexStatus.SurveyStationCount) or
                 nameof(OctreeIndexStatus.BucketCount) or
                 nameof(OctreeIndexStatus.OctreeCodeCount))
        {
            required.Add(property.Name);
        }
        else if (declaringType == typeof(OctreeSearchJobRequest))
        {
            if (property.Name == nameof(OctreeSearchJobRequest.ReferenceTrajectoryID))
                required.Add(property.Name);
            else if (property.Name is nameof(OctreeSearchJobRequest.IncludePlanned) or
                     nameof(OctreeSearchJobRequest.IncludeActual) or
                     nameof(OctreeSearchJobRequest.DefinitiveOnly))
                schema["default"] = true;
        }
        else if (declaringType == typeof(OctreeSearchJobStatus) && property.Name is
                 nameof(OctreeSearchJobStatus.JobID) or
                 nameof(OctreeSearchJobStatus.ReferenceTrajectoryID) or
                 nameof(OctreeSearchJobStatus.CalculationState) or
                 nameof(OctreeSearchJobStatus.CalculationProgress) or
                 nameof(OctreeSearchJobStatus.CreatedUtc))
        {
            required.Add(property.Name);
        }
        else if (declaringType == typeof(OctreeSearchJobResult))
        {
            required.Add(property.Name);
        }
        else if (declaringType == typeof(SeparationFactorPoint) || declaringType == typeof(MeasuredDepthRange))
        {
            required.Add(property.Name);
        }
        else if (declaringType == typeof(SeparationFactorResult) &&
                 property.Name is nameof(SeparationFactorResult.ComparisonTrajectoryID) or
                                  nameof(SeparationFactorResult.SeparationFactorProfile))
        {
            required.Add(property.Name);
        }
        else if (declaringType == typeof(AntiCollisionPolicyRevisionCreate))
        {
            if (property.Name is nameof(AntiCollisionPolicyRevisionCreate.MetaInfo) or
                nameof(AntiCollisionPolicyRevisionCreate.PolicyID) or
                nameof(AntiCollisionPolicyRevisionCreate.Name) or
                nameof(AntiCollisionPolicyRevisionCreate.ConfidenceFactor) or
                nameof(AntiCollisionPolicyRevisionCreate.Rules)) required.Add(property.Name);
            if (property.Name == nameof(AntiCollisionPolicyRevisionCreate.Rules)) schema["minItems"] = 1;
        }
        else if (declaringType == typeof(AntiCollisionPolicyRule))
        {
            required.Add(property.Name);
            if (property.Name == nameof(AntiCollisionPolicyRule.Priority)) schema["minimum"] = 1;
            if (property.Name is nameof(AntiCollisionPolicyRule.AlertThreshold) or nameof(AntiCollisionPolicyRule.AlarmThreshold))
                schema["exclusiveMinimum"] = 0.0;
        }
        else if (declaringType == typeof(AntiCollisionTrajectoryAgeCondition))
        {
            required.Add(property.Name);
            if (property.Name == nameof(AntiCollisionTrajectoryAgeCondition.AgeThreshold)) schema["minimum"] = 0.0;
        }
        else if (declaringType == typeof(AntiCollisionIdentityCondition))
        {
            required.Add(property.Name);
            if (property.Name == nameof(AntiCollisionIdentityCondition.Pattern)) schema["minLength"] = 1;
        }
        else if (declaringType == typeof(AntiCollisionFeatureCondition))
        {
            if (property.Name is nameof(AntiCollisionFeatureCondition.ConditionID) or
                nameof(AntiCollisionFeatureCondition.ResourceLevel) or
                nameof(AntiCollisionFeatureCondition.FeatureCategoryID) or
                nameof(AntiCollisionFeatureCondition.FeatureOptionID) or
                nameof(AntiCollisionFeatureCondition.TemporalOperator)) required.Add(property.Name);
        }
        else if (declaringType == typeof(FieldAntiCollisionPolicyAssignmentMutation))
        {
            if (property.Name is not nameof(FieldAntiCollisionPolicyAssignmentMutation.ValidToUtc)) required.Add(property.Name);
        }
        else if (declaringType == typeof(SurveyRunBitExtrapolation))
        {
            required.Add(property.Name);
            if (property.Name == nameof(SurveyRunBitExtrapolation.MeasurementToolToBitDistance))
                schema["exclusiveMinimum"] = 0.0;
        }
        else if (declaringType == typeof(GeosteeringTrajectoryExtrapolationSpecification))
        {
            required.Add(property.Name);
            if (property.Name == nameof(GeosteeringTrajectoryExtrapolationSpecification.LeadInLength))
                schema["minimum"] = 0.0;
            else if (property.Name == nameof(GeosteeringTrajectoryExtrapolationSpecification.EndInclination))
            {
                schema["minimum"] = 0.0;
                schema["maximum"] = Math.PI;
            }
        }
        else if (declaringType == typeof(ReconnectTrajectoryExtrapolationSpecification) &&
                 property.Name == nameof(ReconnectTrajectoryExtrapolationSpecification.LeadInLength))
        {
            required.Add(property.Name);
            schema["minimum"] = 0.0;
        }
        else if (declaringType == typeof(DepartureGeosteeringExtentConstraint))
        {
            required.Add(property.Name);
            if (property.Name == nameof(DepartureGeosteeringExtentConstraint.DepartureDistance))
                schema["exclusiveMinimum"] = 0.0;
        }
        else if (declaringType == typeof(DrilledLengthGeosteeringExtentConstraint))
        {
            required.Add(property.Name);
            if (property.Name is nameof(DrilledLengthGeosteeringExtentConstraint.SteeringLength) or
                nameof(DrilledLengthGeosteeringExtentConstraint.SteeringLengthRatio))
                schema["exclusiveMinimum"] = 0.0;
        }
        else if (declaringType.FullName == "OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision" &&
                  property.Name == "ID")
        {
            schema["minLength"] = 1;
            required.Add(property.Name);
        }

        if (declaringType == typeof(GlobalAntiCollision.GlobalAntiCollision) &&
            property.Name is nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceTrajectoryID) or
                             nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceWellPathID))
        {
            // One of these two alternative references is intentionally Guid.Empty on the wire.
            schema.Remove("not");
        }

        if (property.Name == "CalculationProgress")
        {
            schema["minimum"] = 0.0;
            schema["maximum"] = 1.0;
        }
        else if (property.Name == "ConfidenceFactor")
        {
            schema["exclusiveMinimum"] = 0.0;
            schema.Remove("exclusiveMaximum");
            schema["maximum"] = declaringType == typeof(SurveyStationEllipseCalculation)
                ? SurveyStationEllipseCalculation.MaximumConfidenceFactor
                : GlobalAntiCollision.GlobalAntiCollision.MaximumConfidenceFactor;
        }
        else if (property.Name.EndsWith("Count", StringComparison.Ordinal))
        {
            schema["minimum"] = 0;
        }

        if (((declaringType == typeof(TrajectoryMinimumDistanceCalculation) ||
              declaringType == typeof(SurveyRunMinimumDistanceCalculation)) && property.Name == "OctreeMaximumDepth") ||
            (declaringType == typeof(MinimumDistanceAdaptiveRefinementSettings) && property.Name == "MaximumDepth"))
        {
            schema["minimum"] = 1;
            schema["maximum"] = 12;
        }
    }

    private static Type? ResponsePayloadType(Type returnType)
    {
        Type type = returnType;
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) ||
                                   type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            type = type.GetGenericArguments()[0];
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>))
            return type.GetGenericArguments()[0];
        if (type == typeof(void) || type == typeof(Task) || type == typeof(ValueTask) ||
            typeof(IActionResult).IsAssignableFrom(type))
            return null;
        return type;
    }

    private static string DescribeProperty(Type declaringType, string name)
    {
        if (typeof(SurveyRunLight).IsAssignableFrom(declaringType) || declaringType == typeof(SurveyImportSettings))
        {
            if (name == nameof(SurveyRunLight.DefaultInclinationReference))
                return "Run-level observed-inclination reference. GeodeticVertical uses the local WGS84 geodetic-down axis (opposite the outward ellipsoid normal); GravityVertical follows the local total-gravity vector. InheritRun is forbidden here.";
            if (name == nameof(SurveyRunLight.DefaultAzimuthReference))
                return "Run-level observed-azimuth reference. TrueNorth uses WGS84 geodetic north; MagneticNorth uses the evaluated geomagnetic field. InheritRun is forbidden here.";
            if (name == nameof(SurveyRunLight.GeomagneticModel))
                return "Geomagnetic model for MagneticNorth observations. Automatic selects WMM2025 for 2025 or later and IGRF14 for earlier instants.";
            if (name == nameof(SurveyRunLight.AcquisitionStartUtc))
                return "Earliest known UTC acquisition instant; supply it together with AcquisitionEndUtc when magnetic observations lack station times.";
            if (name == nameof(SurveyRunLight.AcquisitionEndUtc))
                return "Latest known UTC acquisition instant; supply it together with AcquisitionStartUtc when magnetic observations lack station times.";
        }
        if (declaringType == typeof(SurveyMeasurement))
        {
            return name switch
            {
                nameof(SurveyMeasurement.MeasurementID) => "Stable measurement UUID independent of list position.",
                nameof(SurveyMeasurement.Origin) => "Measured for an instrument observation; Extrapolated only for the final supplied bit station in LastStationAlreadyExtrapolated mode.",
                nameof(SurveyMeasurement.MD) => "Measured or along-hole depth in canonical SI metres.",
                nameof(SurveyMeasurement.Inclination) => "Canonical inclination from the local WGS84 geodetic-down axis in SI radians, after reference correction.",
                nameof(SurveyMeasurement.Azimuth) => "Canonical clockwise azimuth from WGS84 geodetic true north in SI radians, after reference correction.",
                nameof(SurveyMeasurement.ObservedInclination) => "Original observed inclination in SI radians before transformation from InclinationReference.",
                nameof(SurveyMeasurement.ObservedAzimuth) => "Original observed clockwise azimuth in SI radians before transformation from AzimuthReference.",
                nameof(SurveyMeasurement.MeasurementTimeUtc) => "UTC measurement instant used for magnetic correction; the run acquisition midpoint is the fallback.",
                nameof(SurveyMeasurement.InclinationReference) => "Vertical reference of ObservedInclination. InheritRun selects the survey-run default.",
                nameof(SurveyMeasurement.AzimuthReference) => "North reference of ObservedAzimuth. InheritRun selects the survey-run default.",
                nameof(SurveyMeasurement.Correction) => "Frozen correction result and Earth-model provenance used to derive canonical angles.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(SurveyRunBitExtrapolation))
        {
            return name switch
            {
                nameof(SurveyRunBitExtrapolation.Mode) => "CalculateFromLastMeasurement derives the terminal station server-side; LastStationAlreadyExtrapolated identifies the final supplied row as the bit station.",
                nameof(SurveyRunBitExtrapolation.MeasurementToolToBitDistance) => "Distance-to-bit elevation of the measurement tool relative to the bit front face, positive upward in canonical SI metres; used as the positive along-hole MD increment to the bit.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(SurveyMeasurementCorrection))
        {
            return name switch
            {
                nameof(SurveyMeasurementCorrection.AppliedInclinationCorrection) => "Signed canonical-minus-observed inclination correction in SI radians.",
                nameof(SurveyMeasurementCorrection.AppliedAzimuthCorrection) => "Shortest signed canonical-minus-observed azimuth correction in SI radians.",
                nameof(SurveyMeasurementCorrection.MagneticDeclination) => "Evaluated magnetic declination clockwise from WGS84 geodetic true north in SI radians.",
                nameof(SurveyMeasurementCorrection.GravityNorth) => "North component of total gravity in the local WGS84 north-east-down frame, in SI metres per second squared.",
                nameof(SurveyMeasurementCorrection.GravityEast) => "East component of total gravity in the local WGS84 north-east-down frame, in SI metres per second squared.",
                nameof(SurveyMeasurementCorrection.GravityDown) => "Down component of total gravity in the local WGS84 north-east-down frame, in SI metres per second squared.",
                nameof(SurveyMeasurementCorrection.EvaluatedLatitude) => "Evaluated WGS84 geodetic latitude in SI radians.",
                nameof(SurveyMeasurementCorrection.EvaluatedLongitude) => "Evaluated WGS84 geodetic longitude in SI radians.",
                nameof(SurveyMeasurementCorrection.EvaluatedDepthWgs84) => "Evaluated depth in SI metres, positive downward from the WGS84 reference ellipsoid.",
                nameof(SurveyMeasurementCorrection.EvaluationTimeUtc) => "UTC instant used to evaluate the geomagnetic model.",
                nameof(SurveyMeasurementCorrection.TimeMethod) => "Method used to select EvaluationTimeUtc.",
                nameof(SurveyMeasurementCorrection.AlgorithmVersion) => "Opaque reference-correction algorithm version.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(OctreeSearchJobRequest))
        {
            return name switch
            {
                nameof(OctreeSearchJobRequest.ReferenceTrajectoryID) => "Non-empty UUID of the reference trajectory. Its 99.9%-confidence octree index must be Current.",
                nameof(OctreeSearchJobRequest.IncludePlanned) => "Include planned trajectory indexes. This and IncludeActual cannot both be false; defaults to true.",
                nameof(OctreeSearchJobRequest.IncludeActual) => "Include actual trajectory indexes. This and IncludePlanned cannot both be false; defaults to true.",
                nameof(OctreeSearchJobRequest.DefinitiveOnly) => "True includes only definitive trajectories; false includes both definitive and temporary trajectories. Defaults to true.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(OctreeSearchJobStatus))
        {
            return name switch
            {
                nameof(OctreeSearchJobStatus.JobID) => "Server-generated UUID used with the status, result and delete tools.",
                nameof(OctreeSearchJobStatus.ReferenceTrajectoryID) => "UUID of the scanned reference trajectory.",
                nameof(OctreeSearchJobStatus.CalculationState) => "Transient job state: Queued, Running, Completed or Failed.",
                nameof(OctreeSearchJobStatus.CalculationProgress) => "Measured completion fraction from 0 through 1.",
                nameof(OctreeSearchJobStatus.CalculationMessage) => "Sanitized current-stage or terminal-failure message.",
                nameof(OctreeSearchJobStatus.CandidateCount) => "Number of unique candidates; present when CalculationState is Completed.",
                nameof(OctreeSearchJobStatus.CreatedUtc) => "UTC creation timestamp of the transient job.",
                nameof(OctreeSearchJobStatus.CompletedUtc) => "UTC terminal timestamp; present when CalculationState is Completed or Failed.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(OctreeSearchJobResult))
        {
            return name == nameof(OctreeSearchJobResult.CandidateTrajectoryIDs)
                ? "Unique trajectory UUIDs whose indexed uncertainty-envelope cells overlap the reference after applying the requested classification filters. The reference UUID is excluded."
                : SplitWords(name) + ".";
        }
        if (declaringType == typeof(GlobalAntiCollision.GlobalAntiCollision))
        {
            return name switch
            {
                nameof(GlobalAntiCollision.GlobalAntiCollision.ID) => "Caller-assigned non-empty string identifier of this durable asynchronous job.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.ConfidenceFactor) => "Uncertainty-envelope confidence factor greater than 0 and at most 0.999.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.RequestedPolicyAssignmentID) => "Optional caller-selected Field anti-collision policy assignment UUID. Omit it for no policy classification. When supplied, the service validates that the assignment belongs to the reference trajectory's Field and derives the confidence factor and applied policy snapshot.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceTrajectoryID) => "Reference trajectory UUID. Exactly one of this and ReferenceWellPathID must be non-empty on submission.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.ReferenceWellPathID) => "Reference well-path UUID. Exactly one of this and ReferenceTrajectoryID must be non-empty on submission.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.ComparisonTrajectoryIDs) => "Unique non-empty trajectory UUIDs selected for comparison, normally from a completed octree scan. The service retains only valid spatial candidates with calculated stations.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.SeparationFactorResults) => "Server-derived results, one per comparison trajectory that produced a valid profile.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationState) => "Server-derived asynchronous state: Queued, Running, Completed or Failed.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationProgress) => "Server-derived completion fraction from 0 through 1.",
                nameof(GlobalAntiCollision.GlobalAntiCollision.CalculationMessage) => "Server-derived sanitized stage or failure message; may be null after successful completion.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(SurveyStationEllipseCalculation) &&
            name == nameof(SurveyStationEllipseCalculation.ConfidenceFactor))
        {
            return "Dimensionless ProportionStandard confidence factor greater than 0 and at most 0.999.";
        }
        if (declaringType == typeof(GlobalAntiCollisionCalculationStatus))
        {
            return name switch
            {
                nameof(GlobalAntiCollisionCalculationStatus.ID) => "Identifier of the durable separation-factor job.",
                nameof(GlobalAntiCollisionCalculationStatus.CalculationState) => "Current state: Queued, Running, Completed or Failed.",
                nameof(GlobalAntiCollisionCalculationStatus.CalculationProgress) => "Measured completion fraction from 0 through 1.",
                nameof(GlobalAntiCollisionCalculationStatus.CalculationMessage) => "Sanitized current-stage or terminal-failure message; may be null after successful completion.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(SeparationFactorResult))
        {
            return name switch
            {
                nameof(SeparationFactorResult.ComparisonTrajectoryID) => "Non-empty UUID of the comparison trajectory represented by this result.",
                nameof(SeparationFactorResult.ReferenceMDRange) => "Relevant measured-depth interval on the reference trajectory in SI metres; null when no bounded interval was derived.",
                nameof(SeparationFactorResult.ComparisonMDRange) => "Relevant measured-depth interval on the comparison trajectory in SI metres; null when no bounded interval was derived.",
                nameof(SeparationFactorResult.SeparationFactorProfile) => "Calculated profile points within the relevant depth interval. Clients may split non-contiguous reference-depth runs into separate plotted lines.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(SeparationFactorPoint))
        {
            return name switch
            {
                nameof(SeparationFactorPoint.ReferenceMD) => "Reference-trajectory measured depth in SI metres.",
                nameof(SeparationFactorPoint.ComparisonMD) => "Corresponding comparison-trajectory measured depth in SI metres; legacy no-correspondence points may use -1.",
                nameof(SeparationFactorPoint.SeparationFactor) => "Dimensionless separation factor at the reference/comparison depth pair.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(AntiCollisionPolicyRevisionCreate) || declaringType == typeof(AntiCollisionPolicyRevision))
        {
            return name switch
            {
                "MetaInfo" => "Non-empty immutable revision UUID and REST ownership metadata.",
                "PolicyID" => "Stable non-empty UUID shared by every immutable revision in this policy family.",
                "RevisionNumber" => "Server-derived positive monotonic revision number within PolicyID.",
                "Name" => "Human-readable non-empty policy name.",
                "CreationDate" => "Server-derived UTC creation instant for this immutable revision.",
                "ConfidenceFactor" => "Dimensionless uncertainty confidence proportion greater than zero and at most 0.999; this overrides the submitted calculation confidence.",
                "Rules" => "Non-empty rules with unique explicit priorities. Lower priority numbers are evaluated first; exactly one unconditional rule must be last.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(AntiCollisionPolicyRule))
        {
            return name switch
            {
                nameof(AntiCollisionPolicyRule.RuleID) => "Stable non-empty UUID of this rule within the immutable revision.",
                nameof(AntiCollisionPolicyRule.Priority) => "Unique explicit order greater than or equal to one within the revision; lower values are evaluated first.",
                nameof(AntiCollisionPolicyRule.AlertThreshold) => "Dimensionless separation-factor Alert threshold, strictly greater than AlarmThreshold.",
                nameof(AntiCollisionPolicyRule.AlarmThreshold) => "Positive dimensionless separation-factor Alarm threshold, strictly lower than AlertThreshold.",
                nameof(AntiCollisionPolicyRule.Conditions) => "Conditions combined with AND. Empty only for the one required final default rule.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(GeosteeringTrajectoryExtrapolationSpecification))
        {
            return name switch
            {
                nameof(GeosteeringTrajectoryExtrapolationSpecification.LeadInLength) => "Non-negative initial continuation of the source trajectory's final calculated curve before steering starts, in SI metres.",
                nameof(GeosteeringTrajectoryExtrapolationSpecification.TargetVerticalDepth) => "Absolute target WGS84 vertical depth, positive downward in SI metres.",
                nameof(GeosteeringTrajectoryExtrapolationSpecification.EndInclination) => "Target inclination from the local WGS84 geodetic-down axis, from 0 through pi radians.",
                nameof(GeosteeringTrajectoryExtrapolationSpecification.EndAzimuth) => "Target clockwise azimuth from WGS84 geodetic true north in SI radians.",
                nameof(GeosteeringTrajectoryExtrapolationSpecification.Extent) => "Exactly one extent: overall Departure from the final source station, or SteeringLength for the two steering sections.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(DepartureGeosteeringExtentConstraint))
            return name == nameof(DepartureGeosteeringExtentConstraint.DepartureDistance)
                ? "Positive overall horizontal departure from the final source station in SI metres."
                : "Overall departure bearing clockwise and positive east of WGS84 true north in SI radians.";
        if (declaringType == typeof(DrilledLengthGeosteeringExtentConstraint))
            return name == nameof(DrilledLengthGeosteeringExtentConstraint.SteeringLength)
                ? "Positive combined length of the upstream and downstream steering sections, excluding LeadInLength, in SI metres."
                : "Positive dimensionless upstream-to-downstream steering-section length ratio.";
        if (declaringType == typeof(AntiCollisionTrajectoryAgeCondition))
            return name == nameof(AntiCollisionTrajectoryAgeCondition.AgeThreshold)
                ? "Non-negative trajectory-age threshold in canonical SI seconds. Age uses the oldest defined contributing acquisition start or station measurement time."
                : SplitWords(name) + ".";
        if (declaringType == typeof(AntiCollisionIdentityCondition))
            return name == nameof(AntiCollisionIdentityCondition.IdentityDefinitionID)
                ? "Identity-definition UUID owned by the selected comparison-side hierarchy service."
                : name == nameof(AntiCollisionIdentityCondition.Pattern)
                    ? "Non-empty identity value pattern interpreted by MatchOperator; Glob supports '*' and '?' without regular expressions."
                    : SplitWords(name) + ".";
        if (declaringType == typeof(AntiCollisionFeatureCondition))
            return name switch
            {
                nameof(AntiCollisionFeatureCondition.FeatureCategoryID) => "Feature-category UUID owned by the selected comparison-side hierarchy service.",
                nameof(AntiCollisionFeatureCondition.FeatureOptionID) => "Feature-option UUID belonging to FeatureCategoryID.",
                nameof(AntiCollisionFeatureCondition.TemporalOperator) => "Temporal relation used against the frozen assignment validity and measurement/evaluation interval.",
                _ => SplitWords(name) + "."
            };
        if (declaringType == typeof(FieldAntiCollisionPolicyAssignmentMutation) || declaringType == typeof(FieldAntiCollisionPolicyAssignment))
        {
            return name switch
            {
                "FieldID" => "Non-empty UUID of the Field governed by this assignment.",
                "PolicyRevisionID" => "Non-empty UUID of one exact existing immutable policy revision.",
                "ValidFromUtc" => "Inclusive UTC start of the non-overlapping Field assignment interval.",
                "ValidToUtc" => "Exclusive UTC end of the assignment interval; null means open-ended.",
                "LastModificationDate" => "Opaque server concurrency token copied exactly into expectedModifiedUtc.",
                _ => SplitWords(name) + "."
            };
        }
        if (declaringType == typeof(MeasuredDepthRange))
        {
            return name == nameof(MeasuredDepthRange.StartMD)
                ? "Inclusive interval start measured depth in SI metres."
                : name == nameof(MeasuredDepthRange.EndMD)
                    ? "Inclusive interval end measured depth in SI metres; must be greater than or equal to StartMD."
                    : SplitWords(name) + ".";
        }
        if ((declaringType == typeof(TrajectoryMinimumDistanceCalculation) ||
             declaringType == typeof(SurveyRunMinimumDistanceCalculation)) && name == "OctreeMaximumDepth")
            return "Maximum octree subdivision level (dimensionless integer from 1 through 12).";
        if (declaringType == typeof(MinimumDistanceAdaptiveRefinementSettings) && name == "MaximumDepth")
            return "Maximum adaptive-refinement recursion level (dimensionless integer from 1 through 12).";
        if (declaringType == typeof(TrajectoryBatchRestoreRequest) && name == nameof(TrajectoryBatchRestoreRequest.AllowNormalizedNameMapping))
            return "Explicit opt-in to map compatible catalog definitions and options with different UUIDs by normalized name; false requires exact UUID matches.";
        if ((declaringType == typeof(TrajectoryExternalReferenceAuditRequest) ||
             declaringType == typeof(SurveyRunExternalReferenceAuditRequest)) && name == "Scope")
            return "Audit All stored resources or an explicit Selected UUID set.";
        if ((declaringType == typeof(TrajectoryExternalReferenceAuditRequest) ||
             declaringType == typeof(SurveyRunExternalReferenceAuditRequest)) && name == "Offset")
            return "Zero-based number of UUID-ordered matches to skip.";
        if ((declaringType == typeof(TrajectoryExternalReferenceAuditRequest) ||
             declaringType == typeof(SurveyRunExternalReferenceAuditRequest)) && name == "Limit")
            return "Maximum page size from 1 through 100.";
        if (name == "MD" || name.EndsWith("MD", StringComparison.Ordinal)) return "Measured/along-hole depth in SI metres.";
        if (name.Contains("Inclination", StringComparison.OrdinalIgnoreCase)) return "Inclination angle in SI radians.";
        if (name.Contains("Azimuth", StringComparison.OrdinalIgnoreCase)) return "Azimuth angle in SI radians.";
        if (name.Contains("Toolface", StringComparison.OrdinalIgnoreCase) || name.Contains("Angle", StringComparison.OrdinalIgnoreCase)) return "Angular value in SI radians.";
        if (name.Contains("Curvature", StringComparison.OrdinalIgnoreCase) || name.Contains("DogLeg", StringComparison.OrdinalIgnoreCase) || name.Contains("BuildUp", StringComparison.OrdinalIgnoreCase) || name.Contains("TurnRate", StringComparison.OrdinalIgnoreCase)) return "Curvature/rate in SI radians per metre.";
        if (name.Contains("Latitude", StringComparison.OrdinalIgnoreCase) || name.Contains("Longitude", StringComparison.OrdinalIgnoreCase)) return "Geodetic angular coordinate in SI radians.";
        if (name.Contains("Depth", StringComparison.OrdinalIgnoreCase) || name.Contains("Distance", StringComparison.OrdinalIgnoreCase) || name.Contains("Radius", StringComparison.OrdinalIgnoreCase) || name.Contains("North", StringComparison.OrdinalIgnoreCase) || name.Contains("East", StringComparison.OrdinalIgnoreCase) || name.Contains("TVD", StringComparison.OrdinalIgnoreCase) || name.Contains("Abscissa", StringComparison.OrdinalIgnoreCase) || name.Contains("Length", StringComparison.OrdinalIgnoreCase) || name.Contains("Step", StringComparison.OrdinalIgnoreCase)) return "Length, depth or distance in SI metres.";
        if (name.Contains("Progress", StringComparison.OrdinalIgnoreCase)) return "Calculation completion fraction, normally from 0.0 to 1.0.";
        if (name == "CalculationState") return "Current asynchronous calculation state; poll until Completed or Failed.";
        if (name.EndsWith("ID", StringComparison.Ordinal) || name == "ID") return "Resource identifier (UUID unless the owning API states otherwise).";
        if (name.EndsWith("Count", StringComparison.Ordinal)) return "Non-negative number of contained or available items.";
        if (name.EndsWith("List", StringComparison.Ordinal) || name.EndsWith("Results", StringComparison.Ordinal)) return $"Collection of {SplitWords(name)} values.";
        return SplitWords(name) + ".";
    }

    private static string? DescribeReferenceEnum(Type type) => type == typeof(SurveyInclinationReference)
        ? "Observed-inclination reference: GeodeticVertical is the local WGS84 geodetic-down axis (opposite the outward ellipsoid normal), GravityVertical follows the local total-gravity vector, and InheritRun uses the run default."
        : type == typeof(SurveyAzimuthReference)
            ? "Observed-azimuth reference: TrueNorth is WGS84 geodetic north, MagneticNorth is the evaluated geomagnetic-field direction projected onto the selected reference plane, and InheritRun uses the run default."
            : type == typeof(SurveyGeomagneticModel)
                ? "Geomagnetic model selection. Automatic uses WMM2025 for 2025 or later and IGRF14 for earlier instants."
                : null;

    private static bool TryGetEnumerableElement(Type type, out Type? elementType)
    {
        if (type.IsArray) { elementType = type.GetElementType(); return true; }
        Type? enumerable = type.GetInterfaces().Append(type).FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        elementType = enumerable?.GetGenericArguments()[0];
        return elementType is not null && type != typeof(string);
    }

    private static bool TryGetDictionaryValue(Type type, out Type? valueType)
    {
        Type? dictionary = type.GetInterfaces().Append(type).FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) && candidate.GetGenericArguments()[0] == typeof(string));
        valueType = dictionary?.GetGenericArguments()[1];
        return valueType is not null;
    }

    private static bool IsNullable(ParameterInfo parameter)
    {
        if (Nullable.GetUnderlyingType(parameter.ParameterType) is not null) return true;
        if (parameter.ParameterType.IsValueType) return false;
        return Nullability.Create(parameter).ReadState is not NullabilityState.NotNull;
    }

    private static bool IsNullable(PropertyInfo property) =>
        Nullable.GetUnderlyingType(property.PropertyType) is not null ||
        (!property.PropertyType.IsValueType && Nullability.Create(property).ReadState is not NullabilityState.NotNull);

    private static JsonObject AllowNull(JsonObject schema) => new()
    {
        ["anyOf"] = new JsonArray(schema, new JsonObject { ["type"] = "null" })
    };

    private static string DefinitionName(Type type)
    {
        string name = type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] + string.Join("", type.GetGenericArguments().Select(DefinitionName)) : type.Name;
        return name.Replace('+', '_');
    }

    private static string SplitWords(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var words = new System.Text.StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (i > 0 && char.IsUpper(current) && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1])))) words.Append(' ');
            words.Append(current);
        }
        return words.ToString();
    }
}
