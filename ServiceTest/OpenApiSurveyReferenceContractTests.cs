using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiSurveyReferenceContractTests
{
    [Test]
    public void Generated_rest_and_merged_contracts_preserve_station_and_lookup_bindings()
    {
        string root=Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,"..","..","..",".."));
        foreach(var (path,type) in new[]{
            (Path.Combine(root,"ModelSharedOut","json-schemas","TrajectoryFullName.json"),"OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation"),
            (Path.Combine(root,"Service","wwwroot","json-schema","TrajectoryMergedModel.json"),"SurveyStation")})
        {
            var document=JsonNode.Parse(File.ReadAllText(path))!;
            var properties=document["components"]!["schemas"]![type]!["properties"]!;
            Assert.That(properties["Abscissa"]![SemanticMetadata.ExtensionName]!["concept"]!.ToString(),Is.EqualTo(Concepts.AlongHoleDepth),path);
            Assert.That(properties["Z"]![SemanticMetadata.ExtensionName]!["concept"]!.ToString(),Is.EqualTo(Concepts.TrueVerticalDepth),path);
            Assert.That(properties["Z"]![SemanticMetadata.ExtensionName]!["reference"]!.ToString(),Is.EqualTo(Concepts.Wgs84),path);
            Assert.That(properties["Z"]![SemanticMetadata.ExtensionName]!["physicalQuantity"]!["name"]!.ToString(),Is.EqualTo("DepthDrilling"),path);
            var id=document["paths"]!["/Trajectory/{id}"]!["get"]!["parameters"]!.AsArray().Single(p=>p!["name"]!.ToString()=="id")!;
            Assert.That(id[SemanticMetadata.ExtensionName]!["concept"]!.ToString(),Is.EqualTo(Concepts.ResourceIdentifier),path);
            Assert.That(id[SemanticMetadata.ExtensionName]!["resourceType"]!.ToString(),Is.EqualTo(Concepts.Trajectory),path);
        }
    }

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
        JsonObject bitExtrapolation = schemas["OSDC.Drilling.Trajectory.Model.SurveyRunBitExtrapolation"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(schemas["OSDC.Drilling.Trajectory.Model.SurveyInclinationReference"]!["description"]!.GetValue<string>(),
                Does.Contain("positive-down axis"));
            Assert.That(schemas["OSDC.Drilling.Trajectory.Model.SurveyAzimuthReference"]!["description"]!.GetValue<string>(),
                Does.Contain("geomagnetic-field vector"));
            Assert.That(run["properties"]!["DefaultInclinationReference"]!["not"]!["enum"]![0]!.GetValue<string>(),
                Is.EqualTo("InheritRun"));
            Assert.That(measurement["properties"]!["Inclination"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("rad"));
            Assert.That(measurement["properties"]!["Azimuth"]!["x-osdc-semantic"]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.WellboreAzimuth));
            Assert.That(measurement["properties"]!["Azimuth"]!["x-osdc-semantic"]!["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.TrueNorthClockwise));
            Assert.That(correction["properties"]!["GravityNorth"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m/s2"));
            Assert.That(correction["properties"]!["EvaluatedDepthWgs84"]!["description"]!.GetValue<string>(),
                Does.Contain("positive downward"));
            Assert.That(measurement["properties"]!["Origin"]!["description"]!.GetValue<string>(),
                Does.Contain("final caller-supplied bit station"));
            Assert.That(bitExtrapolation["properties"]!["MeasurementToolToBitDistance"]!["x-si-unit"]!.GetValue<string>(),
                Is.EqualTo("m"));
            Assert.That(bitExtrapolation["properties"]!["MeasurementToolToBitDistance"]!["exclusiveMinimum"]!.GetValue<bool>(),
                Is.True);
        });
    }
}
