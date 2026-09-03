using System.Text.Json.Nodes;
using NORCE.Drilling.Trajectory.Service.Mcp.Tools;

namespace ServiceTest;

[TestFixture]
public sealed class McpToolRegistrationTests
{
    [Test]
    public void Registration_exposes_all_non_statistics_actions_with_underscore_names()
    {
        var endpoints = TrajectoryRestMcpToolRegistrations.Endpoints;

        Assert.That(endpoints, Has.Count.EqualTo(104));
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
            });
        }
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

    private static TrajectoryMcpEndpoint Endpoint(string name) =>
        TrajectoryRestMcpToolRegistrations.Endpoints.Single(endpoint => endpoint.Name == name);
}
