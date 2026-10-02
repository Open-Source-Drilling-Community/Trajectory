using System.Text.Json.Nodes;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiConfidenceFactorContractTests
{
    [Test]
    public void Resource_specific_ellipse_routes_are_published()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ModelSharedOut", "json-schemas", "TrajectoryFullName.json"));
        JsonObject paths = JsonNode.Parse(File.ReadAllText(path))!["paths"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(paths.ContainsKey("/SurveyStationEllipseCalculation/SurveyRun/{surveyRunId}"), Is.True);
            Assert.That(paths.ContainsKey("/SurveyStationEllipseCalculation/Trajectory/{trajectoryId}"), Is.True);
        });
    }

    [Test]
    public void Whole_policy_delete_publishes_no_content_as_its_success_response()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ModelSharedOut", "json-schemas", "TrajectoryFullName.json"));
        JsonObject responses = JsonNode.Parse(File.ReadAllText(path))!["paths"]!
            ["/AntiCollisionPolicyRevision/Policy/{policyId}"]!["delete"]!["responses"]!.AsObject();

        Assert.That(responses.ContainsKey("204"), Is.True);
    }

    [Test]
    public void Confidence_factors_publish_proportion_semantics_and_si_bounds()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ModelSharedOut", "json-schemas", "TrajectoryFullName.json"));
        JsonObject schemas = JsonNode.Parse(File.ReadAllText(path))!["components"]!["schemas"]!.AsObject();
        JsonObject ellipse = ConfidenceProperty(schemas,
            "OSDC.Drilling.Trajectory.Model.SurveyStationEllipseCalculation");
        JsonObject policy = ConfidenceProperty(schemas,
            "OSDC.Drilling.GlobalAntiCollision.AntiCollisionPolicyRevisionCreate");
        JsonObject antiCollision = ConfidenceProperty(schemas,
            "OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision");

        Assert.Multiple(() =>
        {
            AssertQuantityAndLowerBound(ellipse);
            AssertQuantityAndLowerBound(policy);
            AssertQuantityAndLowerBound(antiCollision);
            Assert.That(ellipse["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(ellipse["exclusiveMaximum"]!.GetValue<bool>(), Is.False);
            Assert.That(policy["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(policy["exclusiveMaximum"]!.GetValue<bool>(), Is.False);
            Assert.That(antiCollision["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
        });
    }

    private static JsonObject ConfidenceProperty(JsonObject schemas, string schemaName) =>
        schemas[schemaName]!["properties"]!["ConfidenceFactor"]!.AsObject();

    private static void AssertQuantityAndLowerBound(JsonObject property)
    {
        Assert.That(property["x-osdc-semantic"]!.GetValue<string>(), Is.EqualTo("ProportionStandard"));
        Assert.That(property["x-si-unit"]!.GetValue<string>(), Is.EqualTo("1"));
        Assert.That(property["minimum"]!.GetValue<double>(), Is.Zero);
        Assert.That(property["exclusiveMinimum"]!.GetValue<bool>(), Is.True);
    }
}
