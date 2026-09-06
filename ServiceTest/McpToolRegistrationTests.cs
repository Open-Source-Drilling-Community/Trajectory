using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Mcp;
using OSDC.Drilling.Trajectory.Service.Mcp.Tools;

namespace ServiceTest;

[TestFixture]
public sealed class McpToolRegistrationTests
{
    [Test]
    public void Registration_exposes_all_non_statistics_actions_with_underscore_names()
    {
        var endpoints = TrajectoryRestMcpToolRegistrations.Endpoints;

        Assert.That(endpoints, Has.Count.EqualTo(130));
        Assert.That(endpoints.Select(endpoint => endpoint.Name), Is.Unique);
        Assert.That(endpoints.Select(endpoint => endpoint.Name), Has.None.Contains("."));
        Assert.That(endpoints.Select(endpoint => endpoint.Name), Has.None.Contains("usage_statistics"));
    }

    [Test]
    public void Every_tool_has_an_explicit_schema_and_actionable_description()
    {
        foreach (TrajectoryMcpEndpoint endpoint in TrajectoryRestMcpToolRegistrations.Endpoints)
        {
            Assert.Multiple(() =>
            {
                Assert.That(endpoint.Description.Length, Is.GreaterThan(100), endpoint.Name);
                Assert.That(endpoint.Description, Does.Contain("REST operation:"), endpoint.Name);
                Assert.That(endpoint.InputSchema, Is.Not.Null, endpoint.Name);
                Assert.That(endpoint.InputSchema?["type"]?.GetValue<string>(), Is.EqualTo("object"), endpoint.Name);
                Assert.That(endpoint.InputSchema?["additionalProperties"]?.GetValue<bool>(), Is.False, endpoint.Name);
                Assert.That(endpoint.OutputSchema["type"]?.GetValue<string>(), Is.EqualTo("object"), endpoint.Name);
                Assert.That(endpoint.OutputSchema["properties"]?["status"]?["type"]?.GetValue<string>(),
                    Is.EqualTo("integer"), endpoint.Name);
                Assert.That(endpoint.Behavior.Title, Is.Not.Empty, endpoint.Name);
                Assert.That(endpoint.Behavior.OpenWorldHint, Is.False, endpoint.Name);
            });
        }
    }

    [Test]
    public void Protocol_tools_publish_titles_output_schemas_and_safety_annotations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrajectoryRestMcpTools();
        using ServiceProvider provider = services.BuildServiceProvider();
        McpServerTool[] tools = provider.GetServices<McpServerTool>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(tools, Has.Length.EqualTo(130));
            Assert.That(tools.All(tool => !string.IsNullOrWhiteSpace(tool.ProtocolTool.Title)), Is.True);
            Assert.That(tools.All(tool => tool.ProtocolTool.OutputSchema.HasValue), Is.True);
            Assert.That(tools.All(tool => tool.ProtocolTool.Annotations is not null), Is.True);
            Assert.That(Endpoint("trajectory_get_trajectory_by_id").Behavior.ReadOnlyHint, Is.True);
            Assert.That(Endpoint("trajectory_batch_export").Behavior.ReadOnlyHint, Is.True);
            Assert.That(Endpoint("trajectory_batch_restore").Behavior.DestructiveHint, Is.True);
            Assert.That(Endpoint("trajectory_delete_trajectory_by_id").Behavior.DestructiveHint, Is.True);
        });
    }

    [Test]
    public void Identifier_schemas_forbid_empty_uuids()
    {
        JsonObject properties = Endpoint("trajectory_get_trajectory_by_id").InputSchema["properties"]!.AsObject();
        JsonObject id = properties["id"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(id["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
            Assert.That(id["not"]?["const"]?.GetValue<string>(), Is.EqualTo(Guid.Empty.ToString()));
        });
    }

    [Test]
    public async Task Delegate_contract_rejects_unknown_arguments_before_controller_invocation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrajectoryRestMcpTools();
        using ServiceProvider provider = services.BuildServiceProvider();
        IMcpTool tool = provider.GetServices<IMcpTool>().Single(value => value.Name == "trajectory_get_all_trajectory_id");

        JsonNode? result = await tool.InvokeAsync(new JsonObject { ["unexpected"] = true }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result?["status"]?.GetValue<int>(), Is.EqualTo(400));
            Assert.That(result?["error"]?.GetValue<string>(), Does.Contain("Unexpected argument"));
        });
    }

    [Test]
    public void Survey_measurement_upload_documents_workflow_and_si_units()
    {
        TrajectoryMcpEndpoint endpoint = Endpoint("survey_run_put_survey_measurement_chunk");
        JsonObject schema = endpoint.InputSchema!;
        JsonObject chunkIndex = (JsonObject)schema["properties"]!["chunkIndex"]!;
        string serialized = schema.ToJsonString();

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Description, Does.Contain("zero-based"));
            Assert.That(endpoint.Description, Does.Contain("commit"));
            Assert.That(endpoint.Description, Does.Contain("radians"));
            Assert.That(chunkIndex["minimum"]?.GetValue<int>(), Is.EqualTo(0));
            Assert.That(serialized, Does.Contain("SurveyMeasurementList"));
            Assert.That(serialized, Does.Contain("Measured/along-hole depth in SI metres"));
            Assert.That(serialized, Does.Contain("Inclination angle in SI radians"));
        });
    }

    [Test]
    public void Calculation_case_tools_explain_polling_and_chunked_results()
    {
        TrajectoryMcpEndpoint create = Endpoint("trajectory_minimum_distance_calculation_post_trajectory_minimum_distance_calculation");
        TrajectoryMcpEndpoint resultChunk = Endpoint("trajectory_minimum_distance_calculation_get_result_chunk");

        Assert.Multiple(() =>
        {
            Assert.That(create.Description, Does.Contain("Poll"));
            Assert.That(create.Description, Does.Contain("CalculationState"));
            Assert.That(create.Description, Does.Contain("metres"));
            Assert.That(resultChunk.Description, Does.Contain("chunk-count"));
            Assert.That(resultChunk.Description, Does.Contain("zero-based"));
        });
    }

    [Test]
    public void Optional_large_payload_flags_are_documented_and_default_to_false()
    {
        TrajectoryMcpEndpoint endpoint = Endpoint("trajectory_get_trajectory_by_id");
        JsonObject properties = (JsonObject)endpoint.InputSchema!["properties"]!;

        Assert.Multiple(() =>
        {
            Assert.That(properties["includeCalculatedStations"]?["default"]?.GetValue<bool>(), Is.False);
            Assert.That(endpoint.Description, Does.Contain("large calculated arrays"));
        });
    }

    [Test]
    public void Shared_identity_and_feature_catalog_tools_expose_concurrency_and_assignment_schemas()
    {
        TrajectoryMcpEndpoint identityUpdate = Endpoint("trajectory_identity_put");
        TrajectoryMcpEndpoint categoryCreate = Endpoint("trajectory_feature_category_post");
        TrajectoryMcpEndpoint trajectoryCreate = Endpoint("trajectory_post_trajectory");
        string trajectorySchema = trajectoryCreate.InputSchema!.ToJsonString();

        Assert.Multiple(() =>
        {
            Assert.That(identityUpdate.Description, Does.Contain("expectedModifiedUtc"));
            Assert.That(identityUpdate.InputSchema!["required"]!.AsArray().Select(value => value!.GetValue<string>()), Does.Contain("expectedModifiedUtc"));
            Assert.That(categoryCreate.InputSchema!.ToJsonString(), Does.Contain("HasValidityPeriod"));
            Assert.That(categoryCreate.InputSchema!.ToJsonString(), Does.Contain("Options"));
            Assert.That(trajectorySchema, Does.Contain("TrajectoryIdentityAssignments"));
            Assert.That(trajectorySchema, Does.Contain("TrajectoryFeatureAssignments"));
        });
    }

    [TestCase("trajectory_put_trajectory_by_id")]
    [TestCase("trajectory_delete_trajectory_by_id")]
    [TestCase("survey_run_put_survey_run_by_id")]
    [TestCase("survey_run_delete_survey_run_by_id")]
    [TestCase("interpolated_trajectory_put_interpolated_trajectory_by_id")]
    [TestCase("interpolated_trajectory_delete_interpolated_trajectory_by_id")]
    [TestCase("survey_run_batch_import_put_survey_run_batch_import_by_id")]
    [TestCase("survey_run_batch_import_delete_survey_run_batch_import_by_id")]
    [TestCase("trajectory_minimum_distance_calculation_put_trajectory_minimum_distance_calculation_by_id")]
    [TestCase("trajectory_minimum_distance_calculation_delete_trajectory_minimum_distance_calculation_by_id")]
    [TestCase("survey_run_minimum_distance_calculation_put_survey_run_minimum_distance_calculation_by_id")]
    [TestCase("survey_run_minimum_distance_calculation_delete_survey_run_minimum_distance_calculation_by_id")]
    [TestCase("trajectory_realization_case_put_trajectory_realization_case_by_id")]
    [TestCase("trajectory_realization_case_delete_trajectory_realization_case_by_id")]
    [TestCase("trajectory_aggregation_case_put_trajectory_aggregation_case_by_id")]
    [TestCase("trajectory_aggregation_case_delete_trajectory_aggregation_case_by_id")]
    [TestCase("survey_station_ellipse_calculation_delete_survey_station_ellipse_calculation_by_id")]
    public void Durable_core_mutations_require_optimistic_concurrency(string toolName)
    {
        TrajectoryMcpEndpoint endpoint = Endpoint(toolName);
        Assert.Multiple(() =>
        {
            Assert.That(endpoint.InputSchema!["properties"]!["expectedModifiedUtc"], Is.Not.Null);
            Assert.That(endpoint.InputSchema["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Does.Contain("expectedModifiedUtc"));
            Assert.That(endpoint.Description, Does.Contain("expectedModifiedUtc"));
        });
    }

    [Test]
    public void Core_search_tools_are_bounded_and_replace_unbounded_heavy_mcp_lists()
    {
        TrajectoryMcpEndpoint trajectorySearch = Endpoint("trajectory_search_trajectory");
        TrajectoryMcpEndpoint surveyRunSearch = Endpoint("survey_run_search_survey_run");
        IReadOnlyList<TrajectoryMcpEndpoint> endpoints = TrajectoryRestMcpToolRegistrations.Endpoints;

        Assert.Multiple(() =>
        {
            Assert.That(trajectorySearch.Description, Does.Contain("deterministic bounded page"));
            Assert.That(trajectorySearch.InputSchema!["properties"]!["limit"]!["default"]!.GetValue<int>(), Is.EqualTo(100));
            Assert.That(trajectorySearch.InputSchema["properties"]!["limit"]!["maximum"]!.GetValue<int>(), Is.EqualTo(500));
            Assert.That(surveyRunSearch.InputSchema!["properties"]!["offset"]!["default"]!.GetValue<int>(), Is.Zero);
            Assert.That(endpoints.Any(value => value.Name == "trajectory_get_all_trajectory"), Is.False);
            Assert.That(endpoints.Any(value => value.Name == "survey_run_get_all_survey_run"), Is.False);
            Assert.That(endpoints, Has.Count.EqualTo(130));
        });
    }

    [Test]
    public void External_reference_tools_are_bounded_read_only_and_distinguish_unavailable_dependencies()
    {
        TrajectoryMcpEndpoint trajectoryValidation = Endpoint("trajectory_validate_external_references");
        TrajectoryMcpEndpoint trajectoryAudit = Endpoint("trajectory_audit_external_references");
        TrajectoryMcpEndpoint surveyRunValidation = Endpoint("survey_run_validate_external_references");
        TrajectoryMcpEndpoint surveyRunAudit = Endpoint("survey_run_audit_external_references");

        Assert.Multiple(() =>
        {
            Assert.That(trajectoryValidation.Description, Does.Contain("Unavailable, never Invalid"));
            Assert.That(surveyRunValidation.Description, Does.Contain("SurveyInstrument"));
            Assert.That(trajectoryAudit.Description, Does.Contain("deterministic UUID-ordered page"));
            Assert.That(trajectoryAudit.Behavior.ReadOnlyHint, Is.True);
            Assert.That(trajectoryAudit.Behavior.DestructiveHint, Is.False);
            Assert.That(surveyRunAudit.Behavior.ReadOnlyHint, Is.True);
            Assert.That(trajectoryAudit.InputSchema.ToJsonString(), Does.Contain("TrajectoryIDs"));
            Assert.That(surveyRunAudit.InputSchema.ToJsonString(), Does.Contain("SurveyRunIDs"));
            Assert.That(trajectoryAudit.InputSchema.ToJsonString(), Does.Contain("\"maximum\":100"));
            Assert.That(trajectoryValidation.OutputSchema.ToJsonString(), Does.Contain("WellBoreExists"));
            Assert.That(surveyRunValidation.OutputSchema.ToJsonString(), Does.Contain("SurveyInstrumentExists"));
        });
    }

    [Test]
    public void Backup_tools_document_dependency_closure_and_restore_policies()
    {
        TrajectoryMcpEndpoint export = Endpoint("trajectory_batch_export");
        TrajectoryMcpEndpoint restore = Endpoint("trajectory_batch_restore");

        JsonObject restoreDefinitions = restore.InputSchema["$defs"]!.AsObject();
        JsonObject restoreRequest = restoreDefinitions["TrajectoryBatchRestoreRequest"]!.AsObject();
        JsonObject restoreProperties = restoreRequest["properties"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(export.Description, Does.Contain("automatically includes"));
            Assert.That(export.InputSchema!.ToJsonString(), Does.Contain("TrajectoryIDs"));
            Assert.That(restore.Description, Does.Contain("writes survey runs before"));
            Assert.That(restore.InputSchema!.ToJsonString(), Does.Contain("ConflictPolicy"));
            Assert.That(restore.InputSchema!.ToJsonString(), Does.Contain("CatalogPolicy"));
            Assert.That(restore.InputSchema!.ToJsonString(), Does.Contain("AllowNormalizedNameMapping"));
            Assert.That(restoreProperties["ConflictPolicy"]!["enum"]!.AsArray()
                .Select(value => value!.GetValue<string>()), Is.EqualTo(new[] { "FailIfExists", "ReplaceExisting" }));
            Assert.That(restoreProperties["CatalogPolicy"]!["enum"]!.AsArray()
                .Select(value => value!.GetValue<string>()), Is.EqualTo(new[] { "MapExisting", "MapOrCreateMissing" }));
            Assert.That(restoreRequest["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "ConflictPolicy", "CatalogPolicy", "AllowNormalizedNameMapping", "Document" }));
            Assert.That(restore.OutputSchema.ToJsonString(), Does.Contain("TrajectoryBatchRestoreResponse"));
        });
    }

    [Test]
    public void Octree_depth_controls_are_dimensionless_and_bounded()
    {
        string schema = Endpoint("trajectory_minimum_distance_calculation_post_trajectory_minimum_distance_calculation").InputSchema!.ToJsonString();

        Assert.Multiple(() =>
        {
            Assert.That(schema, Does.Contain("Maximum octree subdivision level (dimensionless integer from 1 through 12)."));
            Assert.That(schema, Does.Contain("Maximum adaptive-refinement recursion level (dimensionless integer from 1 through 12)."));
            Assert.That(schema, Does.Contain("\"minimum\":1,\"maximum\":12"));
            Assert.That(schema, Does.Not.Contain("OctreeMaximumDepth\":{\"type\":\"integer\",\"description\":\"Length, depth or distance in SI metres."));
        });
    }

    [Test]
    public void Octree_tools_expose_filters_currentness_provenance_and_safe_repair_guidance()
    {
        TrajectoryMcpEndpoint list = Endpoint("octrees_get");
        TrajectoryMcpEndpoint status = Endpoint("octrees_get_status");
        TrajectoryMcpEndpoint queueSearch = Endpoint("octrees_queue_search");
        TrajectoryMcpEndpoint searchStatus = Endpoint("octrees_get_search_status");
        TrajectoryMcpEndpoint searchResult = Endpoint("octrees_get_search_result");
        TrajectoryMcpEndpoint deleteSearch = Endpoint("octrees_delete_search");
        TrajectoryMcpEndpoint rebuild = Endpoint("octrees_put");
        TrajectoryMcpEndpoint delete = Endpoint("octrees_delete");
        JsonObject listProperties = list.InputSchema["properties"]!.AsObject();
        JsonObject statusDefinition = status.OutputSchema["$defs"]!["OctreeIndexStatus"]!.AsObject();
        JsonObject statusProperties = statusDefinition["properties"]!.AsObject();
        JsonObject searchRequest = queueSearch.InputSchema["$defs"]!["OctreeSearchJobRequest"]!.AsObject();
        JsonObject searchJobStatus = searchStatus.OutputSchema["$defs"]!["OctreeSearchJobStatus"]!.AsObject();
        JsonObject searchResultDefinition = searchResult.OutputSchema["$defs"]!["OctreeSearchJobResult"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(listProperties.ContainsKey("trajectoryType"), Is.True);
            Assert.That(listProperties.ContainsKey("isDefinitive"), Is.True);
            Assert.That(listProperties["trajectoryType"]!["enum"]!.AsArray()
                .Select(value => value!.GetValue<string>()), Is.EquivalentTo(Enum.GetNames<TrajectoryType>()));
            Assert.That(status.Description, Does.Contain("Missing, NotIndexable, Stale or Current"));
            Assert.That(statusProperties["State"]!["enum"]!.AsArray()
                .Select(value => value!.GetValue<string>()), Is.EquivalentTo(Enum.GetNames<OctreeIndexState>()));
            Assert.That(statusProperties["ConfidenceFactor"]!["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(statusProperties["ConfidenceFactor"]!["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(statusDefinition["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Does.Contain("TrajectoryID"));
            Assert.That(queueSearch.Description, Does.Contain("return immediately"));
            Assert.That(queueSearch.Description, Does.Contain("swept-AABB"));
            Assert.That(queueSearch.Description, Does.Contain("depth 22"));
            Assert.That(queueSearch.Description, Does.Contain("conservative for every supported separation-factor request"));
            Assert.That(searchRequest["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Does.Contain("ReferenceTrajectoryID"));
            Assert.That(searchRequest["properties"]!["ReferenceTrajectoryID"]!["not"]!["const"]!.GetValue<string>(),
                Is.EqualTo(Guid.Empty.ToString()));
            Assert.That(searchRequest["not"]!["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "IncludePlanned", "IncludeActual" }));
            Assert.That(queueSearch.Description, Does.Contain("cannot both be false"));
            Assert.That(searchStatus.Description, Does.Contain("measured progress"));
            Assert.That(searchStatus.Description, Does.Contain("expire after one hour"));
            Assert.That(searchStatus.Behavior.ReadOnlyHint, Is.True);
            Assert.That(searchJobStatus["properties"]!["CalculationProgress"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(searchJobStatus["properties"]!["CalculationProgress"]!["maximum"]!.GetValue<double>(), Is.EqualTo(1.0));
            Assert.That(searchJobStatus["allOf"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(searchResult.Description, Does.Contain("poll the status tool first"));
            Assert.That(searchResult.Description, Does.Contain("may contain false positives"));
            Assert.That(searchResult.Description, Does.Contain("only the separation-factor calculation determines"));
            Assert.That(searchResultDefinition["properties"]!["CandidateTrajectoryIDs"]!["uniqueItems"]!.GetValue<bool>(), Is.True);
            Assert.That(deleteSearch.Description, Does.Contain("does not modify trajectory data"));
            Assert.That(deleteSearch.Behavior.DestructiveHint, Is.True);
            Assert.That(rebuild.Description, Does.Contain("automatically"));
            Assert.That(rebuild.Description, Does.Contain("return its new status/provenance"));
            Assert.That(delete.Description, Does.Contain("without deleting its authoritative trajectory"));
            Assert.That(delete.Behavior.DestructiveHint, Is.True);
        });
    }

    [Test]
    public void Global_anti_collision_tools_enforce_ids_and_publish_asynchronous_progress()
    {
        TrajectoryMcpEndpoint create = Endpoint("global_anti_collisions_post");
        TrajectoryMcpEndpoint update = Endpoint("global_anti_collisions_put");
        TrajectoryMcpEndpoint get = Endpoint("global_anti_collisions_get_by_id");
        TrajectoryMcpEndpoint status = Endpoint("global_anti_collisions_get_status");
        JsonObject definition = create.InputSchema["$defs"]!["GlobalAntiCollision"]!.AsObject();
        JsonObject properties = definition["properties"]!.AsObject();
        JsonObject outputDefinition = get.OutputSchema["$defs"]!["GlobalAntiCollision"]!.AsObject();
        JsonObject resultDefinition = get.OutputSchema["$defs"]!["SeparationFactorResult"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(definition["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "ID", "ConfidenceFactor", "ComparisonTrajectoryIDs" }));
            Assert.That(properties["ID"]!["minLength"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(properties["ComparisonTrajectoryIDs"]!["minItems"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(properties["ComparisonTrajectoryIDs"]!["uniqueItems"]!.GetValue<bool>(), Is.True);
            Assert.That(definition["oneOf"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(properties.ContainsKey("CalculationState"), Is.False);
            Assert.That(properties.ContainsKey("CalculationProgress"), Is.False);
            Assert.That(properties.ContainsKey("CalculationMessage"), Is.False);
            Assert.That(properties.ContainsKey("SeparationFactorResults"), Is.False);
            Assert.That(create.InputSchema["$defs"]!.AsObject().ContainsKey("SeparationFactorResult"), Is.False);
            Assert.That(create.OutputSchema.ToJsonString(), Does.Contain("GlobalAntiCollision"));
            Assert.That(update.OutputSchema.ToJsonString(), Does.Contain("GlobalAntiCollision"));
            Assert.That(get.OutputSchema.ToJsonString(), Does.Contain("GlobalAntiCollision"));
            Assert.That(status.OutputSchema.ToJsonString(), Does.Contain("GlobalAntiCollisionCalculationStatus"));
            Assert.That(get.OutputSchema["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "status", "data" }));
            Assert.That(outputDefinition["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Does.Contain("SeparationFactorResults"));
            Assert.That(resultDefinition["properties"]!["ReferenceMDRange"]!["anyOf"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(resultDefinition["properties"]!["SeparationFactorProfile"]!["description"]!.GetValue<string>(),
                Does.Contain("non-contiguous"));
            Assert.That(create.Description, Does.Contain("returns immediately"));
            Assert.That(create.Description, Does.Contain("relevant measured-depth intervals"));
            Assert.That(update.Description, Does.Contain("route id and body ID must match"));
            Assert.That(status.Description, Does.Contain("lightweight"));
            Assert.That(get.Description, Does.Contain("SI metres"));
            Assert.That(get.Description, Does.Contain("dimensionless SeparationFactor"));
            Assert.That(status.Behavior.ReadOnlyHint, Is.True);
        });
    }

    private static TrajectoryMcpEndpoint Endpoint(string name) =>
        TrajectoryRestMcpToolRegistrations.Endpoints.Single(endpoint => endpoint.Name == name);
}
