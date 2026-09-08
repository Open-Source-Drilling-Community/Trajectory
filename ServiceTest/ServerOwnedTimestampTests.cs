using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class ServerOwnedTimestampTests
{
    [Test]
    public void Batch_import_create_accepts_apostrophes_and_replaces_caller_timestamps()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"TrajectoryTimestamp_{Guid.NewGuid():N}.db");
        using ILoggerFactory loggers = LoggerFactory.Create(builder => builder.ClearProviders());
        try
        {
            var connections = new SqlConnectionManagerTrajectory(path,
                loggers.CreateLogger<SqlConnectionManagerTrajectory>());
            ConstructorInfo constructor = typeof(SurveyRunBatchImportManager).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                [typeof(ILogger<SurveyRunBatchImportManager>), typeof(SqlConnectionManager)], null)!;
            var manager = (SurveyRunBatchImportManager)constructor.Invoke(
                [loggers.CreateLogger<SurveyRunBatchImportManager>(), connections]);
            var batch = new SurveyRunBatchImport
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = "U1's import",
                Description = "It's valid JSON and SQL data",
                CreationDate = DateTimeOffset.UnixEpoch,
                LastModificationDate = DateTimeOffset.UnixEpoch
            };

            DateTimeOffset before = DateTimeOffset.UtcNow;
            Assert.That(manager.AddSurveyRunBatchImport(batch), Is.True);
            DateTimeOffset after = DateTimeOffset.UtcNow;
            SurveyRunBatchImport stored = manager.GetSurveyRunBatchImportById(batch.MetaInfo.ID)!;

            Assert.Multiple(() =>
            {
                Assert.That(stored.Name, Is.EqualTo(batch.Name));
                Assert.That(stored.Description, Is.EqualTo(batch.Description));
                Assert.That(stored.CreationDate, Is.InRange(before, after));
                Assert.That(stored.LastModificationDate, Is.EqualTo(stored.CreationDate));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
