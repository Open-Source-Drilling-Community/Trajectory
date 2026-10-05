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
