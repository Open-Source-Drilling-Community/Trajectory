using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace ServiceTest;

[TestFixture]
public sealed class TrajectoryBatchServiceTests
{
    [Test]
    public void Selected_trajectory_export_is_dependency_closed_and_round_trips_chunked_data()
    {
        using var source = new TestEnvironment();
        TrajectoryIdentity identity = source.Identities.GetAll().Single(value => value.Name == "NameForPlanning");
        Guid parentId = Guid.NewGuid(), childId = Guid.NewGuid(), trajectoryId = Guid.NewGuid();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        var parent = SurveyRun(parentId, null, "Parent", timestamp);
        parent.SurveyMeasurementList = [new() { MD = 0, Inclination = 0.1, Azimuth = 0.2, Annotation = "measurement" }];
        parent.SurveyRunIdentityAssignments =
        [
            new() { ID = Guid.NewGuid(), IdentityID = identity.MetaInfo!.ID, Value = "Plan A" }
        ];
        var child = SurveyRun(childId, parentId, "Child", timestamp);
        child.SurveyStationList = [new SurveyStation { MD = 100, Abscissa = 100, Inclination = 0.2, Azimuth = 0.3 }];
        var trajectory = new OSDC.Drilling.Trajectory.Model.Trajectory
        {
            MetaInfo = new MetaInfo { ID = trajectoryId }, Name = "Trajectory", Description = "Round trip",
            CreationDate = timestamp, LastModificationDate = timestamp, WellBoreID = Guid.NewGuid(),
            SurveyRunSectionList = [new() { SurveyRunID = childId, StartAbscissa = 100 }],
            SurveyStationList = [new SurveyStation { MD = 100, Abscissa = 100, Inclination = 0.2, Azimuth = 0.3 }]
        };
        var seed = Document(timestamp, [parent, child], [trajectory], [identity]);

        TrajectoryBatchRestoreOutcome seeded = source.Service.Restore(new()
        {
            Document = seed,
            ConflictPolicy = TrajectoryBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = TrajectoryBatchCatalogRestorePolicy.MapExisting
        });
        Assert.That(seeded.IsSuccess, Is.True, ErrorText(seeded.Error));

        TrajectoryBatchExportOutcome exported = source.Service.Export(new()
        {
            Scope = TrajectoryBatchExportScope.Selected,
            SurveyRunIDs = [],
            TrajectoryIDs = [trajectoryId]
        });
        Assert.That(exported.IsSuccess, Is.True, ErrorText(exported.Error));
        Assert.Multiple(() =>
        {
            Assert.That(exported.Document!.SurveyRuns.Select(value => value.MetaInfo!.ID), Is.EqualTo(new[] { parentId, childId }));
            Assert.That(exported.Document.Trajectories.Select(value => value.MetaInfo!.ID), Is.EqualTo(new[] { trajectoryId }));
            Assert.That(exported.Document.SurveyRuns[0].SurveyMeasurementList, Has.Count.EqualTo(1));
            Assert.That(exported.Document.SurveyRuns[1].SurveyStationList, Has.Count.EqualTo(1));
            Assert.That(exported.Document.Trajectories[0].SurveyStationList, Has.Count.EqualTo(1));
            Assert.That(exported.Document.CatalogDependencies.Identities, Has.Count.EqualTo(1));
        });

        using var target = new TestEnvironment();
        TrajectoryBatchRestoreOutcome restored = target.Service.Restore(new()
        {
            Document = exported.Document,
            ConflictPolicy = TrajectoryBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = TrajectoryBatchCatalogRestorePolicy.MapExisting
        });
        Assert.That(restored.IsSuccess, Is.True, ErrorText(restored.Error));
        Assert.Multiple(() =>
        {
            Assert.That(restored.Response!.CreatedSurveyRunCount, Is.EqualTo(2));
            Assert.That(restored.Response.CreatedTrajectoryCount, Is.EqualTo(1));
            Assert.That(restored.Response.CatalogMappings.Any(value => value.Catalog == "Identity" && value.Resolution == "NormalizedName"), Is.True);
        });

        TrajectoryBatchExportOutcome roundTrip = target.Service.Export(new() { Scope = TrajectoryBatchExportScope.All });
        Assert.That(roundTrip.IsSuccess, Is.True, ErrorText(roundTrip.Error));
        Assert.Multiple(() =>
        {
            Assert.That(roundTrip.Document!.SurveyRuns.Single(value => value.MetaInfo!.ID == parentId).SurveyMeasurementList![0].Annotation, Is.EqualTo("measurement"));
            Assert.That(roundTrip.Document.SurveyRuns.Single(value => value.MetaInfo!.ID == childId).SurveyStationList, Has.Count.EqualTo(1));
            Assert.That(roundTrip.Document.Trajectories.Single().SurveyStationList, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Fail_if_exists_leaves_all_records_unchanged()
    {
        using var environment = new TestEnvironment();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        SurveyRun run = SurveyRun(Guid.NewGuid(), null, "Original", timestamp);
        TrajectoryBatchExportDocument document = Document(timestamp, [run], [], []);
        var request = new TrajectoryBatchRestoreRequest
        {
            Document = document,
            ConflictPolicy = TrajectoryBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = TrajectoryBatchCatalogRestorePolicy.MapExisting
        };
        Assert.That(environment.Service.Restore(request).IsSuccess, Is.True);

        run.Name = "Replacement";
        TrajectoryBatchRestoreOutcome conflict = environment.Service.Restore(request);
        Assert.Multiple(() =>
        {
            Assert.That(conflict.FailureKind, Is.EqualTo(TrajectoryBatchFailureKind.Conflict));
            Assert.That(conflict.Error!.Errors.Any(value => value.Code == "record_exists"), Is.True);
        });
        TrajectoryBatchExportOutcome after = environment.Service.Export(new() { Scope = TrajectoryBatchExportScope.All });
        Assert.That(after.Document!.SurveyRuns.Single().Name, Is.EqualTo("Original"));
    }

    private static SurveyRun SurveyRun(Guid id, Guid? parentId, string name, DateTimeOffset timestamp) => new()
    {
        MetaInfo = new MetaInfo { ID = id }, Name = name, Description = "Backup test",
        CreationDate = timestamp, LastModificationDate = timestamp,
        WellBoreID = Guid.NewGuid(), SurveyInstrumentID = Guid.NewGuid(), ParentSurveyRunID = parentId
    };

    private static TrajectoryBatchExportDocument Document(DateTimeOffset timestamp, List<SurveyRun> surveyRuns,
        List<OSDC.Drilling.Trajectory.Model.Trajectory> trajectories, List<TrajectoryIdentity> identities) => new()
    {
        ExportedAtUtc = timestamp,
        SurveyRuns = surveyRuns,
        Trajectories = trajectories,
        CatalogDependencies = new() { Identities = identities, FeatureCategories = [] }
    };

    private static string ErrorText(TrajectoryBatchErrorEnvelope? envelope) => envelope == null
        ? string.Empty
        : $"{envelope.Message} {string.Join("; ", envelope.Errors.Select(value => value.Message))}";

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string directory;
        public TrajectoryIdentityManager Identities { get; }
        public TrajectoryBatchService Service { get; }

        public TestEnvironment()
        {
            directory = Path.Combine(Path.GetTempPath(), "TrajectoryBatchServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var main = new SqlConnectionManagerTrajectory(Path.Combine(directory, "Trajectory.db"), NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Identities = new(main);
            var categories = new TrajectoryFeatureCategoryManager(main);
            Service = new(main, Identities, categories, NullLogger<TrajectoryBatchService>.Instance);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
