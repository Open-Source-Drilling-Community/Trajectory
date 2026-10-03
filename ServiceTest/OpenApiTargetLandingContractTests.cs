using System.Text.Json.Nodes;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiTargetLandingContractTests
{
    [Test]
    public void Generated_openapi_describes_target_plane_si_references_and_landing_constraints()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ModelSharedOut", "json-schemas", "TrajectoryFullName.json"));
        JsonObject document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonObject schemas = document["components"]!["schemas"]!.AsObject();
        JsonObject landing = schemas["OSDC.Drilling.Trajectory.Model.TargetLandingCase"]!.AsObject();
        JsonObject target = schemas["OSDC.Drilling.Trajectory.Model.TargetPlaneDefinition"]!.AsObject();
        JsonObject point = schemas["OSDC.Drilling.Trajectory.Model.TargetPlanePoint"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(landing["properties"]!["LeadLength"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(landing["properties"]!["LeadLength"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m"));
            Assert.That(landing["properties"]!["ConfidenceFactor"]!["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(landing["properties"]!["MaximumLandingCurvature"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("rad/m"));
            Assert.That(landing["properties"]!["MaximumLandingCurvature"]!["exclusiveMinimum"]!.GetValue<bool>(), Is.True);
            Assert.That(target["properties"]!["Plane"]!["description"]!.GetValue<string>(), Does.Contain("RiemannianNorth/RiemannianEast"));
            Assert.That(target["properties"]!["Polygon"]!["minItems"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(point["properties"]!["X"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m"));
            Assert.That(point["properties"]!["Y"]!["description"]!.GetValue<string>(), Does.Contain("vertical-up"));
        });
    }
}
