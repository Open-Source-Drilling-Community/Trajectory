using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class DirectionalControlEvaluationPersistenceTests
{
    [Test]
    public void Calculation_records_both_trajectory_revisions_before_creating_its_fingerprint()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string manager = File.ReadAllText(Path.Combine(repositoryRoot, "Service", "Managers",
            "DirectionalControlEvaluationCaseManager.cs"));

        int referenceRevision = manager.IndexOf(
            "value.ReferenceTrajectoryRevision = reference.LastModificationDate;", StringComparison.Ordinal);
        int actualRevision = manager.IndexOf(
            "value.ActualTrajectoryRevision = actual.LastModificationDate;", StringComparison.Ordinal);
        int fingerprint = manager.IndexOf(
            "value.CalculationFingerprint = CreateFingerprint(value, reference, actual);", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(referenceRevision, Is.GreaterThanOrEqualTo(0));
            Assert.That(actualRevision, Is.GreaterThan(referenceRevision));
            Assert.That(fingerprint, Is.GreaterThan(actualRevision));
        });
    }

    [Test]
    public void Staleness_comparison_uses_the_precision_persisted_by_sqlite()
    {
        DateTimeOffset calculated = DateTimeOffset.Parse("2026-10-05T10:15:30.9876543+02:00");
        DateTimeOffset stored = DateTimeOffset.Parse("2026-10-05T08:15:30Z");

        Assert.Multiple(() =>
        {
            Assert.That(DirectionalControlEvaluationCaseManager.SameStoredRevision(calculated, stored), Is.True);
            Assert.That(DirectionalControlEvaluationCaseManager.SameStoredRevision(calculated, stored.AddSeconds(1)), Is.False);
            Assert.That(DirectionalControlEvaluationCaseManager.SameStoredRevision(calculated, null), Is.False);
        });
    }

    [Test]
    public void Missing_trajectory_revisions_are_repaired_only_when_the_fingerprint_still_matches()
    {
        string path = Path.Combine(Path.GetTempPath(), $"DirectionalControl_{Guid.NewGuid():N}.db");
        try
        {
            var database = new SqlConnectionManagerTrajectory(path, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            var manager = new DirectionalControlEvaluationCaseManager(
                NullLogger<DirectionalControlEvaluationCaseManager>.Instance, database);
            DateTimeOffset revision = DateTimeOffset.Parse("2026-10-05T08:15:30Z");
            DateTimeOffset persistedRevision = DateTimeOffset.Parse(
                revision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            DirectionalControlEvaluationCase matching = CreateCase(revision);
            matching.CalculationState = CalculationState.Completed;
            matching.CalculationFingerprint = DirectionalControlEvaluationCaseManager.CreateFingerprint(
                matching, persistedRevision, persistedRevision);
            DirectionalControlEvaluationCase changed = CreateCase(revision);
            changed.CalculationState = CalculationState.Completed;
            changed.CalculationFingerprint = DirectionalControlEvaluationCaseManager.CreateFingerprint(
                changed, persistedRevision.AddMinutes(-1), persistedRevision);

            using (SqliteConnection connection = database.GetConnection()!)
            {
                foreach (Guid trajectoryId in new[]
                         {
                             matching.ReferenceTrajectoryID, matching.ActualTrajectoryID,
                             changed.ReferenceTrajectoryID, changed.ActualTrajectoryID
                         })
                {
                    using SqliteCommand insert = connection.CreateCommand();
                    insert.CommandText = "INSERT INTO TrajectoryTable (ID,LastModificationDate) VALUES (@id,@revision)";
                    insert.Parameters.AddWithValue("@id", trajectoryId.ToString());
                    insert.Parameters.AddWithValue("@revision", revision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                    insert.ExecuteNonQuery();
                }
            }
            Assert.That(manager.SaveCase(matching, update: false), Is.True);
            Assert.That(manager.SaveCase(changed, update: false), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(manager.RepairMissingTrajectoryRevisions(), Is.EqualTo(1));
                Assert.That(manager.GetLightById(matching.MetaInfo!.ID)!.IsStale, Is.False);
                Assert.That(manager.GetById(matching.MetaInfo.ID)!.IsStale, Is.False,
                    "The full editor payload must use the same staleness result as the light/status endpoint.");
                Assert.That(manager.GetLightById(changed.MetaInfo!.ID)!.IsStale, Is.True);
                Assert.That(manager.GetById(changed.MetaInfo.ID)!.IsStale, Is.True,
                    "The full editor payload must preserve a real stale result.");
                Assert.That(manager.RepairMissingTrajectoryRevisions(), Is.Zero, "The repair must be idempotent.");
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestCase("GetAllDirectionalControlEvaluationCaseId")]
    [TestCase("GetAllDirectionalControlEvaluationCaseLight")]
    [TestCase("GetDirectionalControlEvaluationCaseById")]
    [TestCase("GetDirectionalControlEvaluationCaseStatus")]
    [TestCase("GetDirectionalControlEvaluationSampleChunkCount")]
    [TestCase("GetDirectionalControlEvaluationSampleChunk")]
    [TestCase("PostDirectionalControlEvaluationCase")]
    [TestCase("PutDirectionalControlEvaluationCaseById")]
    [TestCase("DeleteDirectionalControlEvaluationCaseById")]
    public void Usage_statistics_define_every_directional_control_operation(string operation)
    {
        Assert.That(typeof(UsageStatisticsTrajectory).GetProperty(operation + "PerDay"), Is.Not.Null);
    }

    [Test]
    public void Terminal_results_are_chunked_and_the_case_payload_stays_compact()
    {
        string path = Path.Combine(Path.GetTempPath(), $"DirectionalControl_{Guid.NewGuid():N}.db");
        try
        {
            var database = new SqlConnectionManagerTrajectory(path, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            var manager = new DirectionalControlEvaluationCaseManager(
                NullLogger<DirectionalControlEvaluationCaseManager>.Instance, database);
            DateTimeOffset queuedRevision = DateTimeOffset.UtcNow.AddMinutes(-1);
            DirectionalControlEvaluationCase value = CreateCase(queuedRevision);
            Assert.That(manager.SaveCase(value, update: false), Is.True);

            value.SampleList = Enumerable.Range(0, 251).Select(index => new DirectionalControlEvaluationSample
            {
                ActualMD = index * 10.0,
                ActualEndMD = (index + 1) * 10.0,
                IsValid = true,
                CurvatureResidual = index * 1e-5,
                ToolfaceResidual = index * 1e-4
            }).ToList();
            value.BundleList = [new DirectionalControlEvaluationBundle { BundleIndex = 0, ValidSampleCount = 251 }];
            value.CalculationState = CalculationState.Completed;
            value.LastModificationDate = DateTimeOffset.UtcNow;

            Assert.That(manager.SaveTerminal(value, queuedRevision), Is.True);
            DirectionalControlEvaluationCase stored = manager.GetById(value.MetaInfo!.ID)!;
            DirectionalControlEvaluationSampleChunk first = manager.GetSampleChunk(value.MetaInfo.ID, 0)!;
            DirectionalControlEvaluationSampleChunk second = manager.GetSampleChunk(value.MetaInfo.ID, 1)!;

            Assert.Multiple(() =>
            {
                Assert.That(stored.SampleList, Is.Empty);
                Assert.That(stored.BundleList, Has.Count.EqualTo(1));
                Assert.That(manager.GetSampleChunkCount(value.MetaInfo.ID), Is.EqualTo(2));
                Assert.That(first.SampleList, Has.Count.EqualTo(250));
                Assert.That(second.SampleList, Has.Count.EqualTo(1));
                Assert.That(second.StartActualMD, Is.EqualTo(2500.0));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Stale_delete_rolls_back_and_preserves_sample_chunks()
    {
        string path = Path.Combine(Path.GetTempPath(), $"DirectionalControl_{Guid.NewGuid():N}.db");
        try
        {
            var database = new SqlConnectionManagerTrajectory(path, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            var manager = new DirectionalControlEvaluationCaseManager(
                NullLogger<DirectionalControlEvaluationCaseManager>.Instance, database);
            DateTimeOffset queuedRevision = DateTimeOffset.UtcNow.AddMinutes(-1);
            DirectionalControlEvaluationCase value = CreateCase(queuedRevision);
            Assert.That(manager.SaveCase(value, update: false), Is.True);
            value.SampleList = [new DirectionalControlEvaluationSample { ActualMD = 0, ActualEndMD = 10 }];
            value.LastModificationDate = DateTimeOffset.UtcNow;
            Assert.That(manager.SaveTerminal(value, queuedRevision), Is.True);

            Assert.That(manager.Delete(value.MetaInfo!.ID, queuedRevision), Is.False);
            Assert.Multiple(() =>
            {
                Assert.That(manager.GetById(value.MetaInfo.ID), Is.Not.Null);
                Assert.That(manager.GetSampleChunkCount(value.MetaInfo.ID), Is.EqualTo(1));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static DirectionalControlEvaluationCase CreateCase(DateTimeOffset revision) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        Name = "Evaluation",
        CreationDate = revision,
        LastModificationDate = revision,
        ReferenceTrajectoryID = Guid.NewGuid(),
        ActualTrajectoryID = Guid.NewGuid(),
        CurveType = ExtrapolationCurveType.CircularArc,
        CalculationState = CalculationState.Queued,
        EvaluationInterval = 10,
        ReferenceMDAdvance = 30,
        JunctionCurvatureRatio = 1,
        MaximumInvalidGap = 100,
        MinimumBundleLength = 90,
        MinimumBundleSampleCount = 5,
        BundlingPenalty = 8,
        SampleList = [],
        BundleList = []
    };
}
