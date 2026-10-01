using System.Text.Json.Nodes;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiTrajectoryExtrapolationContractTests
{
    [Test]
    public void Generated_openapi_exposes_geosteering_discriminators_units_and_numeric_constraints()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ModelSharedOut", "json-schemas", "TrajectoryFullName.json"));
        JsonObject document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonObject schemas = document["components"]!["schemas"]!.AsObject();
        JsonObject specification = schemas["OSDC.Drilling.Trajectory.Model.TrajectoryExtrapolationSpecification"]!.AsObject();
        JsonObject geosteering = schemas["OSDC.Drilling.Trajectory.Model.GeosteeringTrajectoryExtrapolationSpecification"]!.AsObject();
        JsonObject extent = schemas["OSDC.Drilling.Trajectory.Model.GeosteeringExtentConstraint"]!.AsObject();
        JsonObject departure = schemas["OSDC.Drilling.Trajectory.Model.DepartureGeosteeringExtentConstraint"]!.AsObject();
        JsonObject drilledLength = schemas["OSDC.Drilling.Trajectory.Model.DrilledLengthGeosteeringExtentConstraint"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(specification["discriminator"]!["mapping"]!["Geosteering"]!.GetValue<string>(),
                Does.EndWith("GeosteeringTrajectoryExtrapolationSpecification"));
            Assert.That(extent["discriminator"]!["mapping"]!.AsObject(), Has.Count.EqualTo(2));
            Assert.That(extent["discriminator"]!["propertyName"]!.GetValue<string>(), Is.EqualTo("ExtentType"));
            Assert.That(geosteering["required"]!.AsArray().Select(node => node!.GetValue<string>()), Does.Contain("Extent"));
            Assert.That(geosteering["properties"]!["TargetVerticalDepth"]!["x-osdc-semantic"]!.GetValue<string>(), Is.EqualTo("Wgs84Depth"));
            Assert.That(geosteering["properties"]!["TargetVerticalDepth"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m"));
            Assert.That(geosteering["properties"]!["LeadInLength"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(geosteering["properties"]!["EndInclination"]!["maximum"]!.GetValue<double>(), Is.EqualTo(Math.PI).Within(1e-12));
            Assert.That(departure["properties"]!["DepartureDistance"]!["exclusiveMinimum"]!.GetValue<bool>(), Is.True);
            Assert.That(drilledLength["properties"]!["SteeringLengthRatio"]!["exclusiveMinimum"]!.GetValue<bool>(), Is.True);
            Assert.That(drilledLength["properties"]!["SteeringLength"]!["description"]!.GetValue<string>(), Does.Contain("excluding LeadInLength"));
            Assert.That(drilledLength["properties"]!.AsObject().ContainsKey("OverallDrilledLength"), Is.False);
        });
    }
}
