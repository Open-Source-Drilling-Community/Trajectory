using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service.Mcp.Tools;

/// <summary>Builds the human- and machine-readable MCP contract for reflected REST actions.</summary>
internal static class TrajectoryMcpToolMetadata
{
    private static readonly NullabilityInfoContext Nullability = new();

    private static readonly IReadOnlyDictionary<string, string> ResourceDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Trajectory"] = "a calculated or imported wellbore trajectory and its survey stations",
        ["SurveyRun"] = "a survey run containing measured MD, inclination and azimuth values and its calculated survey stations",
        ["TrajectoryIdentity"] = "an identity definition shared by survey runs and trajectories",
        ["TrajectoryFeatureCategory"] = "a feature category and its options shared by survey runs and trajectories",
        ["SurveyRunBatchImport"] = "a batch-import definition used to create or update survey runs",
        ["InterpolatedTrajectory"] = "an interpolation case and its calculated trajectory stations",
        ["TrajectoryMinimumDistanceCalculation"] = "a minimum-distance calculation between a reference trajectory and comparison trajectories",
        ["SurveyRunMinimumDistanceCalculation"] = "a minimum-distance calculation between a reference survey run and comparison survey runs",
        ["SurveyStationEllipseCalculation"] = "a survey-station uncertainty-ellipse calculation",
        ["TrajectoryRealizationCase"] = "a stochastic trajectory-realization case",
        ["TrajectoryAggregationCase"] = "a case that aggregates multiple trajectories against a common reference",
        ["GlobalAntiCollisions"] = "a global anti-collision configuration used by the trajectory calculations",
        ["Octrees"] = "a cached spatial octree generated for a trajectory"
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
        else if (action == "ValidateExternalReferences")
            detail = controller == "Trajectory"
                ? "Check one stored trajectory's externally owned Field, Cluster, Well and WellBore UUIDs without modifying data. Missing resources are Invalid; configuration, transport, dependency-service and malformed-response failures are Unavailable, never Invalid. Optional unlinked references are valid."
                : "Check one stored survey run's externally owned Field, Cluster, Well, WellBore and SurveyInstrument UUIDs without modifying data. Missing resources are Invalid; configuration, transport, dependency-service and malformed-response failures are Unavailable, never Invalid. Optional unlinked references are valid.";
        else if (action == "AuditExternalReferences")
            detail = $"Check a deterministic UUID-ordered page of all or explicitly selected stored {resource} records without modifying data. Offset must be non-negative and limit is 1 through 100. Results and page counts distinguish Valid, Invalid and Unavailable checks; unavailable dependencies are never reported as missing data.";
        else if (action.StartsWith("Search", StringComparison.Ordinal))
            detail = $"Return one deterministic bounded page of lightweight {resource} records with the total match count. Filter by free text and owned relationship/type fields, use offset for continuation, and keep limit between 1 and 500. Fetch a selected resource by UUID when complete data is needed.";
        else if (controller == "SurveyRun" && action == "PutSurveyMeasurementChunk")
            detail = "Upload or replace one staged measurement chunk. Use a zero-based chunkIndex; chunk.SurveyRunID must equal id and chunk.ChunkIndex must equal chunkIndex. Measurements use MD in metres and Inclination/Azimuth in radians. Upload every chunk, then call the commit tool once to assemble the run and start recalculation.";
        else if (controller == "SurveyRun" && action == "CommitSurveyMeasurementChunks")
            detail = "Commit all previously uploaded survey-measurement chunks for the survey-run id. Call this only after every zero-based chunk has been uploaded; committing assembles the measurements and triggers the survey-station calculation.";
        else if (controller == "SurveyRun" && action == "DeleteSurveyMeasurementChunks")
            detail = "Delete the staged survey-measurement chunks for the survey-run id, for example to abandon or restart an incomplete chunked upload. This does not delete the survey run itself and does not use the persisted survey run's concurrency token.";
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
            detail = "Replace an existing global anti-collision configuration. The route id and the configuration body's identity must designate the same stored configuration; supply the complete configuration, not a partial patch.";
        else if (controller == "GlobalAntiCollisions" && action == "Delete")
            detail = "Permanently delete the global anti-collision configuration identified by its unique string id.";
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
                ? "List the string identifiers of all global anti-collision configurations. Use an identifier with the by-id tool to inspect the complete configuration."
                : "Return the complete global anti-collision configuration identified by id, including its trajectory selection and calculation settings.";
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
        if (definitions.Count > 0) result["$defs"] = definitions;
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

        var result = new JsonObject
        {
            ["type"] = "object",
            ["description"] = "Successful MCP result. Failed requests are returned as MCP errors with a stable error envelope.",
            ["properties"] = properties,
            ["required"] = new JsonArray("status"),
            ["additionalProperties"] = false
        };
        if (definitions.Count > 0) result["$defs"] = definitions;
        return result;
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
        if (controller is "TrajectoryMinimumDistanceCalculation" or "SurveyRunMinimumDistanceCalculation" or "SurveyStationEllipseCalculation" or "TrajectoryRealizationCase" or "TrajectoryAggregationCase" or "InterpolatedTrajectory")
            return $"Create {resource} and start its calculation. data.MetaInfo.ID must be a caller-assigned, non-empty UUID that is not already stored. Poll the corresponding by-id or light-list tool for CalculationState/CalculationProgress; retrieve large outputs through the result chunk tools where available. All lengths and distances are metres and angles are radians.";
        if (controller is "Trajectory" or "SurveyRun")
            return $"Create {resource} and calculate its survey stations. data.MetaInfo.ID must be a caller-assigned, non-empty UUID that is not already stored. Identity and feature assignments must reference the shared catalogs; exclusive feature periods must not overlap. For very large survey runs, create the run first and use the survey-measurement chunk upload/commit workflow. Supply SI values: lengths/depths in metres, angles in radians and curvature in radians per metre.";
        if (controller is "TrajectoryIdentity" or "TrajectoryFeatureCategory")
            return $"Create {resource}. MetaInfo.ID must be a caller-assigned, non-empty UUID. Feature option IDs must also be non-empty UUIDs.";
        if (controller == "GlobalAntiCollisions")
            return "Create a global anti-collision configuration. Supply the complete configuration body with its unique string identity and trajectory-selection/calculation settings.";
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
            "chunkIndex" => "Zero-based index of the requested or uploaded chunk; must be non-negative.",
            "includeResults" => "When true, embed calculated result arrays; false (default) returns the case and status without large results. Prefer chunk tools for large results.",
            "includeRealizations" => "When true, embed all stochastic realization arrays; false (default) omits them. Prefer realization chunks for large results.",
            "includeMeasurements" => "When true, include the survey measurement list in the response; false (default) omits it.",
            "includeCalculatedStations" => "When true, include calculated survey stations; false (default) omits them. Prefer station chunks for large runs.",
            "expectedModifiedUtc" => "Optimistic-concurrency token copied exactly from the resource's latest LastModificationDate.",
            "query" => "Optional case-insensitive text matched against name, description, and UUID.",
            "offset" => "Zero-based number of matching records to skip; must be non-negative.",
            "limit" => "Maximum page size from 1 through 500; defaults to 100.",
            "chunk" => "Complete survey-measurement chunk. SurveyRunID and ChunkIndex must match the route arguments; MD is metres and Inclination/Azimuth are radians.",
            "request" when controller == "Trajectory" && action == "BatchExport" => "Backup scope and optional survey-run and trajectory UUID selections. For Selected, provide at least one UUID; dependent survey runs are added automatically.",
            "request" when controller == "Trajectory" && action == "BatchRestore" => "Complete backup document plus record-conflict and catalog-resolution policies. Restore validates the full graph before writing records.",
            "request" when action == "AuditExternalReferences" => "Audit scope (All or Selected), optional selected resource UUIDs, and deterministic offset/limit page. Selected UUIDs must be non-empty and unique; limit is 1 through 100.",
            "data" => $"Complete {SplitWords(controller).ToLowerInvariant()} JSON representation. Follow the nested schema and SI-unit annotations.",
            "value" when controller == "GlobalAntiCollisions" => "Complete global anti-collision configuration JSON representation.",
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
            return new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(Enum.GetNames(type).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray())
            };
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
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.GetMethod is not null && property.GetIndexParameters().Length == 0)
                     .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                     .OrderBy(property => property.MetadataToken))
        {
            JsonObject schema = SchemaFor(property.PropertyType, definitions, building);
            ApplyDomainConstraints(type, property, schema, required);
            schema["description"] = DescribeProperty(type, property.Name);
            properties[property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name] = schema;
        }
        var definition = new JsonObject
        {
            ["type"] = "object",
            ["description"] = $"JSON representation of {SplitWords(type.Name)}.",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };
        if (required.Count > 0) definition["required"] = required;
        return definition;
    }

    private static void ApplyDomainConstraints(Type declaringType, PropertyInfo property, JsonObject schema, JsonArray required)
    {
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
        else if (declaringType.FullName == "OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision" &&
                 property.Name == "ID")
        {
            schema["minLength"] = 1;
            required.Add(property.Name);
        }

        if (property.Name == "CalculationProgress")
        {
            schema["minimum"] = 0.0;
            schema["maximum"] = 1.0;
        }
        else if (property.Name == "ConfidenceFactor")
        {
            schema["exclusiveMinimum"] = 0.0;
            schema["maximum"] = 0.999;
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
