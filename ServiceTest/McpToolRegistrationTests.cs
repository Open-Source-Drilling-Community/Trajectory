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

        Assert.That(endpoints, Has.Count.EqualTo(165));
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
            Assert.That(tools, Has.Length.EqualTo(165));
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
            Assert.That(serialized, Does.Contain("Canonical inclination from the local WGS84 geodetic-down axis in SI radians"));
        });
    }

    [Test]
    public void Survey_run_contract_distinguishes_observed_references_from_canonical_angles()
    {
        TrajectoryMcpEndpoint endpoint = Endpoint("survey_run_post_survey_run");
        JsonObject definitions = endpoint.InputSchema["$defs"]!.AsObject();
        JsonObject runProperties = definitions["SurveyRun"]!["properties"]!.AsObject();
        JsonObject measurementProperties = definitions["SurveyMeasurement"]!["properties"]!.AsObject();
        JsonObject correctionProperties = definitions["SurveyMeasurementCorrection"]!["properties"]!.AsObject();
        JsonObject bitExtrapolationProperties = definitions["SurveyRunBitExtrapolation"]!["properties"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Description, Does.Contain("canonical WGS84-geodetic/true-north"));
            Assert.That(runProperties["DefaultInclinationReference"]!["enum"]!.AsArray()
                .Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "GeodeticVertical", "GravityVertical" }));
            Assert.That(runProperties["DefaultAzimuthReference"]!["enum"]!.AsArray()
                .Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "TrueNorth", "MagneticNorth" }));
            Assert.That(measurementProperties["InclinationReference"]!["description"]!.GetValue<string>(),
                Does.Contain("Vertical reference"));
            Assert.That(measurementProperties["AzimuthReference"]!["description"]!.GetValue<string>(),
                Does.Contain("North reference"));
            Assert.That(measurementProperties["Inclination"]!["description"]!.GetValue<string>(),
                Does.Contain("WGS84 geodetic-down axis"));
            Assert.That(measurementProperties["Azimuth"]!["description"]!.GetValue<string>(),
                Does.Contain("geodetic true north"));
            Assert.That(correctionProperties["GravityNorth"]!["description"]!.GetValue<string>(),
                Does.Contain("metres per second squared"));
            Assert.That(correctionProperties["EvaluatedDepthWgs84"]!["description"]!.GetValue<string>(),
                Does.Contain("WGS84 reference ellipsoid"));
            Assert.That(measurementProperties["Origin"]!["description"]!.GetValue<string>(),
                Does.Contain("final supplied bit station"));
            Assert.That(bitExtrapolationProperties["MeasurementToolToBitDistance"]!["exclusiveMinimum"]!.GetValue<double>(),
                Is.EqualTo(0.0));
            Assert.That(bitExtrapolationProperties["MeasurementToolToBitDistance"]!["description"]!.GetValue<string>(),
                Does.Contain("canonical SI metres"));
            Assert.That(runProperties.ContainsKey("SurveyStationList"), Is.False);
            Assert.That(runProperties.ContainsKey("TieInPoint"), Is.False);
            Assert.That(runProperties.ContainsKey("CalculationState"), Is.False);
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
    [TestCase("target_landing_case_put")]
    [TestCase("target_landing_case_delete")]
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
            Assert.That(endpoints, Has.Count.EqualTo(165));
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
            Assert.That(properties.ContainsKey("PolicyEvaluationUtc"), Is.False);
            Assert.That(properties.ContainsKey("PolicyAssignmentID"), Is.False);
            Assert.That(properties.ContainsKey("PolicySnapshot"), Is.False);
            Assert.That(properties.ContainsKey("RequestedPolicyAssignmentID"), Is.True);
            Assert.That(properties["RequestedPolicyAssignmentID"]!["description"]!.GetValue<string>(),
                Does.Contain("Omit it").And.Contain("reference trajectory's Field"));
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
            Assert.That(create.Description, Does.Contain("RequestedPolicyAssignmentID").And.Contain("no policy classification"));
            Assert.That(update.Description, Does.Contain("route id and body ID must match"));
            Assert.That(status.Description, Does.Contain("lightweight"));
            Assert.That(get.Description, Does.Contain("SI metres"));
            Assert.That(get.Description, Does.Contain("dimensionless SeparationFactor"));
            Assert.That(status.Behavior.ReadOnlyHint, Is.True);
        });
    }

    [Test]
    public void Policy_tools_publish_closed_discriminated_conditions_and_audited_assignments()
    {
        TrajectoryMcpEndpoint createRevision = Endpoint("anti_collision_policy_revision_post");
        TrajectoryMcpEndpoint createAssignment = Endpoint("field_anti_collision_policy_assignment_post");
        TrajectoryMcpEndpoint updateAssignment = Endpoint("field_anti_collision_policy_assignment_put");
        TrajectoryMcpEndpoint deleteAssignment = Endpoint("field_anti_collision_policy_assignment_delete");
        TrajectoryMcpEndpoint deletePolicy = Endpoint("anti_collision_policy_revision_delete_policy");
        JsonObject definitions = createRevision.InputSchema["$defs"]!.AsObject();
        JsonObject condition = definitions["AntiCollisionPolicyCondition"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(condition["oneOf"]!.AsArray(), Has.Count.EqualTo(3));
            Assert.That(definitions["AntiCollisionTrajectoryAgeCondition"]!["properties"]!["ConditionType"]!["const"]!.GetValue<string>(), Is.EqualTo("TrajectoryAge"));
            Assert.That(definitions["AntiCollisionIdentityCondition"]!["properties"]!["ConditionType"]!["const"]!.GetValue<string>(), Is.EqualTo("Identity"));
            Assert.That(definitions["AntiCollisionFeatureCondition"]!["properties"]!["ConditionType"]!["const"]!.GetValue<string>(), Is.EqualTo("Feature"));
            Assert.That(createRevision.Description, Does.Contain("AlertThreshold greater than AlarmThreshold"));
            Assert.That(definitions["AntiCollisionPolicyRule"]!["properties"]!["Priority"]!["minimum"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(deletePolicy.Description, Does.Contain("no current or historical Field assignment"));
            Assert.That(deletePolicy.InputSchema["required"]!.AsArray().Select(value => value!.GetValue<string>()),
                Is.EquivalentTo(new[] { "policyId", "expectedLatestRevisionId" }));
            Assert.That(deletePolicy.Behavior.DestructiveHint, Is.True);
            Assert.That(createAssignment.Description, Does.Contain("exact immutable policy revision"));
            Assert.That(updateAssignment.Description, Does.Contain("expectedModifiedUtc"));
            Assert.That(deleteAssignment.Description, Does.Contain("historical assignments are immutable"));
            Assert.That(deleteAssignment.Behavior.DestructiveHint, Is.True);
        });
    }

    [Test]
    public void Trajectory_extrapolation_tools_publish_closed_discriminated_submissions_and_chunk_workflow()
    {
        TrajectoryMcpEndpoint create = Endpoint("trajectory_extrapolation_case_post");
        TrajectoryMcpEndpoint status = Endpoint("trajectory_extrapolation_case_get_status");
        TrajectoryMcpEndpoint chunk = Endpoint("trajectory_extrapolation_case_get_survey_station_chunk");
        JsonObject definitions = create.InputSchema["$defs"]!.AsObject();
        JsonObject caseDefinition = definitions["TrajectoryExtrapolationCase"]!.AsObject();
        JsonObject properties = caseDefinition["properties"]!.AsObject();
        JsonObject specification = definitions["TrajectoryExtrapolationSpecification"]!.AsObject();
        JsonObject section = definitions["WellPathSectionSpecification"]!.AsObject();
        JsonObject geosteering = definitions["GeosteeringTrajectoryExtrapolationSpecification"]!.AsObject();
        JsonObject departure = definitions["DepartureGeosteeringExtentConstraint"]!.AsObject();
        JsonObject drilledLength = definitions["DrilledLengthGeosteeringExtentConstraint"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(specification["oneOf"]!.AsArray(), Has.Count.EqualTo(4));
            Assert.That(section["oneOf"]!.AsArray(), Has.Count.EqualTo(3));
            Assert.That(definitions["GeosteeringExtentConstraint"]!["oneOf"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(definitions["FixedLengthExtrapolationSpecification"]!["properties"]!["Mode"]!["const"]!.GetValue<string>(), Is.EqualTo("FixedLength"));
            Assert.That(definitions["GeosteeringTrajectoryExtrapolationSpecification"]!["properties"]!["Mode"]!["const"]!.GetValue<string>(), Is.EqualTo("Geosteering"));
            Assert.That(definitions["DepartureGeosteeringExtentConstraint"]!["properties"]!["ExtentType"]!["const"]!.GetValue<string>(), Is.EqualTo("Departure"));
            Assert.That(definitions["CircularArcWellPathSectionSpecification"]!["properties"]!["CurveType"]!["const"]!.GetValue<string>(), Is.EqualTo("CircularArc"));
            Assert.That(geosteering["required"]!.AsArray().Select(node => node!.GetValue<string>()), Does.Contain("Extent"));
            Assert.That(geosteering["properties"]!["LeadInLength"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(geosteering["properties"]!["EndInclination"]!["maximum"]!.GetValue<double>(), Is.EqualTo(Math.PI).Within(1e-12));
            Assert.That(departure["properties"]!["DepartureDistance"]!["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(drilledLength["properties"]!["SteeringLength"]!["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(drilledLength["properties"]!["SteeringLengthRatio"]!["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(drilledLength["properties"]!["SteeringLength"]!["description"]!.GetValue<string>(), Does.Contain("excluding LeadInLength"));
            Assert.That(drilledLength["properties"]!.AsObject().ContainsKey("OverallDrilledLength"), Is.False);
            Assert.That(properties.ContainsKey("CalculationState"), Is.False);
            Assert.That(properties.ContainsKey("SolvedSectionList"), Is.False);
            Assert.That(properties.ContainsKey("SurveyStationList"), Is.False);
            Assert.That(caseDefinition["required"]!.AsArray().Select(node => node!.GetValue<string>()), Does.Contain("Specification"));
            Assert.That(create.Description, Does.Contain("3 × section-count"));
            Assert.That(status.Description, Does.Contain("poll").IgnoreCase);
            Assert.That(chunk.Description, Does.Contain("zero-based"));
            Assert.That(chunk.InputSchema["properties"]!["chunkIndex"]!["minimum"]!.GetValue<int>(), Is.Zero);
        });
    }

    [Test]
    public void Target_landing_tools_publish_closed_si_submission_and_polling_contract()
    {
        TrajectoryMcpEndpoint create = Endpoint("target_landing_case_post");
        TrajectoryMcpEndpoint status = Endpoint("target_landing_case_get_status");
        JsonObject definitions = create.InputSchema["$defs"]!.AsObject();
        JsonObject caseDefinition = definitions["TargetLandingCase"]!.AsObject();
        JsonObject properties = caseDefinition["properties"]!.AsObject();
        JsonObject plane = definitions["TargetPlaneDefinition"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(properties.ContainsKey("CalculationState"), Is.False);
            Assert.That(properties.ContainsKey("SampleList"), Is.False);
            Assert.That(properties.ContainsKey("ReachableTargetBoundary"), Is.False);
            Assert.That(properties.ContainsKey("SourceEndStation"), Is.False);
            Assert.That(properties.ContainsKey("LeadSurveyStationList"), Is.False);
            Assert.That(caseDefinition["required"]!.AsArray().Select(node => node!.GetValue<string>()),
                Is.EquivalentTo(new[] { "MetaInfo", "SourceTrajectoryID", "Target" }));
            Assert.That(properties["LeadLength"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(properties["ConfidenceFactor"]!["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(properties["ConfidenceFactor"]!["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(properties["MaximumLandingCurvature"]!["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(plane["description"]!.GetValue<string>(), Does.Contain("target plane").IgnoreCase);
            Assert.That(create.Description, Does.Contain("shortest drilling-relevant forward solution"));
            Assert.That(create.Description, Does.Contain("radians per metre"));
            Assert.That(status.Description, Does.Contain("poll").IgnoreCase);
        });
    }

    [Test]
    public void Survey_station_ellipse_confidence_uses_the_supported_proportion_interval_in_mcp()
    {
        TrajectoryMcpEndpoint endpoint = TrajectoryRestMcpToolRegistrations.Endpoints.Single(value =>
            value.ControllerType.Name == "SurveyStationEllipseCalculationController" &&
            value.Method.Name == "PostSurveyStationEllipseCalculation");
        JsonObject definition = endpoint.InputSchema["$defs"]!["SurveyStationEllipseCalculation"]!.AsObject();
        JsonObject confidence = definition["properties"]!["ConfidenceFactor"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(confidence["exclusiveMinimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(confidence["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(confidence.ContainsKey("exclusiveMaximum"), Is.False);
            Assert.That(confidence["description"]!.GetValue<string>(), Does.Contain("ProportionStandard"));
        });
    }

    [Test]
    public void Resource_specific_ellipse_tools_document_authoritative_lineage_replay()
    {
        TrajectoryMcpEndpoint surveyRun = TrajectoryRestMcpToolRegistrations.Endpoints.Single(value =>
            value.ControllerType.Name == "SurveyStationEllipseCalculationController" &&
            value.Method.Name == "PostSurveyRunSurveyStationEllipseCalculation");
        TrajectoryMcpEndpoint trajectory = TrajectoryRestMcpToolRegistrations.Endpoints.Single(value =>
            value.ControllerType.Name == "SurveyStationEllipseCalculationController" &&
            value.Method.Name == "PostTrajectorySurveyStationEllipseCalculation");

        Assert.Multiple(() =>
        {
            Assert.That(surveyRun.Description, Does.Contain("complete parent SurveyRun chain"));
            Assert.That(surveyRun.Description, Does.Contain("stale or partial submitted covariance"));
            Assert.That(trajectory.Description, Does.Contain("rematerializes its SurveyRun sections"));
            Assert.That(trajectory.Description, Does.Contain("complete parent SurveyRun chains"));
        });
    }

    private static TrajectoryMcpEndpoint Endpoint(string name) =>
        TrajectoryRestMcpToolRegistrations.Endpoints.Single(endpoint => endpoint.Name == name);
}
