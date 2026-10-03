using System.Text.Json.Nodes;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiTargetLandingContractTests
{
    [Test]
    public void Target_landing_uses_a_managed_background_queue_and_lightweight_status_query()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string worker = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "TargetLandingCalculationWorker.cs"));
        string manager = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Managers", "TargetLandingCaseManager.cs"));
        string controller = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Controllers", "TargetLandingCaseController.cs"));
        string program = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(worker, Does.Contain(": BackgroundService")
                .And.Contain("Channel.CreateUnbounded<CalculationRequest>")
                .And.Contain("PrepareInterruptedCalculationsForResume")
                .And.Contain("RecalculateAsync(request.Id, request.Revision, stoppingToken)"));
            Assert.That(program, Does.Contain("AddSingleton<TargetLandingCalculationWorker>()")
                .And.Contain("AddHostedService(sp => sp.GetRequiredService<TargetLandingCalculationWorker>())"));
            Assert.That(controller, Does.Contain("worker_.Queue(id, value.LastModificationDate!.Value)"));
            Assert.That(manager, Does.Contain("SELECT MetaInfo,CreationDate,LastModificationDate,SourceTrajectoryID,TargetType,CurveType,AttitudeMode,CalculationState,CalculationProgress,CalculationMessage")
                .And.Not.Contain("_ = Task.Run(() => RecalculateAsync"),
                "Polling status must not deserialize the heavy result, and request handlers must not launch fire-and-forget calculations.");
        });
    }

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
        JsonObject control = schemas["OSDC.Drilling.Trajectory.Model.TargetLandingControlPoint"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(landing["properties"]!["LeadLength"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(landing["properties"]!["LeadLength"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m"));
            Assert.That(landing["properties"]!["ConfidenceFactor"]!["maximum"]!.GetValue<double>(), Is.EqualTo(0.999));
            Assert.That(landing["properties"]!["MaximumLandingCurvature"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("rad/m"));
            Assert.That(landing["properties"]!["MaximumLandingCurvature"]!["exclusiveMinimum"]!.GetValue<bool>(), Is.True);
            Assert.That(landing["properties"]!["SourceEndStation"], Is.Not.Null,
                "The calculated source endpoint is required for the cylindrical curvature marker.");
            Assert.That(landing["properties"]!["LeadSurveyStationList"], Is.Not.Null,
                "The calculated lead path is required for the Cartesian landing view.");
            Assert.That(target["properties"]!["Plane"]!["description"]!.GetValue<string>(), Does.Contain("RiemannianNorth/RiemannianEast"));
            Assert.That(target["properties"]!["Polygon"]!["minItems"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(point["properties"]!["X"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("m"));
            Assert.That(point["properties"]!["Y"]!["description"]!.GetValue<string>(), Does.Contain("vertical-up"));
            Assert.That(control["properties"]!["NormalizedLength"]!["minimum"]!.GetValue<double>(), Is.Zero);
            Assert.That(control["properties"]!["NormalizedLength"]!["maximum"]!.GetValue<double>(), Is.EqualTo(1.0));
            Assert.That(control["properties"]!["Curvature"]!["x-si-unit"]!.GetValue<string>(), Is.EqualTo("rad/m"));
            Assert.That(control["properties"]!["BuildRate"]!["x-osdc-semantic"]!.GetValue<string>(), Is.EqualTo("BuildUpRate"));
            Assert.That(control["properties"]!["TurnRate"]!["x-osdc-semantic"]!.GetValue<string>(), Is.EqualTo("TurnRate"));
            Assert.That(control["properties"]!["Toolface"]!["description"]!.GetValue<string>(), Does.Contain("vary from the arc's start/reference toolface"));
        });
    }
}
