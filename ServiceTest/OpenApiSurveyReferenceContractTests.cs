using System.Text.Json.Nodes;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiSurveyReferenceContractTests
{
    [Test]
    public void Generated_openapi_exposes_reference_semantics_units_and_run_default_constraints()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ModelSharedOut", "json-schemas", "TrajectoryFullName.json"));
        JsonObject document = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonObject schemas = document["components"]!["schemas"]!.AsObject();
        JsonObject run = schemas["OSDC.Drilling.Trajectory.Model.SurveyRun"]!.AsObject();
        JsonObject measurement = schemas["OSDC.Drilling.Trajectory.Model.SurveyMeasurement"]!.AsObject();
        JsonObject correction = schemas["OSDC.Drilling.Trajectory.Model.SurveyMeasurementCorrection"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(schemas["OSDC.Drilling.Trajectory.Model.SurveyInclinationReference"]!["description"]!.GetValue<string>(),
                Does.Contain("positive-down axis"));
            Assert.That(schemas["OSDC.Drilling.Trajectory.Model.SurveyAzimuthReference"]!["description"]!.GetValue<string>(),
                Does.Contain("geomagnetic-field vector"));
            Assert.That(run["properties"]!["DefaultInclinationReference"]!["not"]!["enum"]![0]!.GetValue<string>(),
                Is.EqualTo("InheritRun"));
            Assert.That(measurement["properties"]!["Inclination"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("rad"));
            Assert.That(measurement["properties"]!["Azimuth"]!["x-osdc-semantic"]!.GetValue<string>(), Is.EqualTo("TrueNorthAzimuth"));
            Assert.That(correction["properties"]!["GravityNorth"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m/s2"));
            Assert.That(correction["properties"]!["EvaluatedDepthWgs84"]!["description"]!.GetValue<string>(),
                Does.Contain("positive downward"));
        });
    }
}
