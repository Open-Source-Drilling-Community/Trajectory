using System.Text.Json.Nodes;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class OpenApiTargetLandingContractTests
{
    [Test]
    public void Target_landing_display_selects_reachable_side_of_bisected_boundary()
    {
        TargetPlanePoint boundaryPoint = new() { X = 10.0, Y = 20.0 };
        TargetLandingSample excludedAtBoundary = new()
        {
            SampleID = Guid.NewGuid(), PlaneX = 10.0, PlaneY = 20.0,
            State = TargetLandingSampleState.ExceedsMaximumLandingCurvature,
            SolvedSectionList = [new TrajectoryExtrapolationSolvedSection()]
        };
        TargetLandingSample reachableBesideBoundary = new()
        {
            SampleID = Guid.NewGuid(), PlaneX = 10.05, PlaneY = 20.0,
            State = TargetLandingSampleState.Reachable,
            SolvedSectionList = [new TrajectoryExtrapolationSolvedSection()]
        };
        TargetLandingCase value = new()
        {
            ReachableTargetContourList = [[boundaryPoint]],
            SampleList = [excludedAtBoundary, reachableBesideBoundary]
        };

        List<TargetLandingSample> selected = TargetLandingCaseManager.SelectReachableBoundarySamples(value);

        Assert.That(selected, Has.Count.EqualTo(1));
        Assert.That(selected[0].SampleID, Is.EqualTo(reachableBesideBoundary.SampleID));
        Assert.That(selected[0].State, Is.EqualTo(TargetLandingSampleState.Reachable));
    }

    [Test]
    public void Target_landing_projection_operations_have_usage_statistics_counters()
    {
        var statistics = new OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory
        {
            LastSaved = DateTime.UtcNow,
            BackUpInterval = TimeSpan.FromDays(1)
        };

        Assert.DoesNotThrow(() =>
        {
            statistics.IncrementOperation("GetTargetLandingCaseEditData");
            statistics.IncrementOperation("GetTargetLandingCaseDisplayData");
        });
        Assert.Multiple(() =>
        {
            Assert.That(statistics.GetTargetLandingCaseEditDataPerDay.Data, Has.Count.EqualTo(1));
            Assert.That(statistics.GetTargetLandingCaseEditDataPerDay.Data[0].Count, Is.EqualTo(1));
            Assert.That(statistics.GetTargetLandingCaseDisplayDataPerDay.Data, Has.Count.EqualTo(1));
            Assert.That(statistics.GetTargetLandingCaseDisplayDataPerDay.Data[0].Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void Target_landing_uses_a_managed_background_queue_and_lightweight_status_query()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string worker = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "TargetLandingCalculationWorker.cs"));
        string manager = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Managers", "TargetLandingCaseManager.cs"));
        string controller = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Controllers", "TargetLandingCaseController.cs"));
        string clientSettings = File.ReadAllText(Path.Combine(repositoryRoot, "ModelSharedOut", "ClientJsonSerializerSettings.cs"));
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
            Assert.That(manager, Does.Contain("c.Name,c.Description,c.SourceTrajectoryRevision,c.CalculationFingerprint")
                .And.Not.Contain("json_extract(c.TargetLandingCase")
                .And.Contain("private bool UpdateProgress")
                .And.Not.Contain("_ = Task.Run(() => RecalculateAsync"),
                "Listing and progress polling must not parse or rewrite the heavy result, and request handlers must not launch fire-and-forget calculations.");
            Assert.That(controller, Does.Contain("GetTargetLandingCaseEditData")
                .And.Contain("GetTargetLandingCaseDisplayData"));
            Assert.That(controller, Does.Contain("CompactResponseJson")
                .And.Contain("JsonIgnoreCondition.WhenWritingNull")
                .And.Contain("new JsonResult(value, CompactResponseJson)"),
                "Target-landing edit/display projections must omit null members from repeated nested station payloads.");
            Assert.That(clientSettings, Does.Contain("DefaultIgnoreCondition")
                .And.Contain("JsonIgnoreCondition.WhenWritingNull"),
                "Generated client requests must omit null result members from compact save projections.");
        });
    }

    [Test]
    public void Target_landing_status_does_not_publish_completion_before_the_result_is_saved()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string manager = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Managers", "TargetLandingCaseManager.cs"));

        int calculatorCall = manager.IndexOf("bool calculated = TargetLandingCalculator.Calculate", StringComparison.Ordinal);
        int terminalSave = manager.IndexOf("if (!Save(value, true, queuedRevision))", calculatorCall, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(calculatorCall, Is.GreaterThanOrEqualTo(0));
            Assert.That(terminalSave, Is.GreaterThan(calculatorCall));
            Assert.That(manager, Does.Contain("value.CalculationState = CalculationState.Running;")
                .And.Contain("Math.Clamp(0.08 + 0.90 * progress, 0.08, 0.98)")
                .And.Contain("Target landing calculation completed, but its result could not be saved"),
                "Polling must remain Running until the terminal save succeeds, and a failed save must be visible as a failure.");
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
