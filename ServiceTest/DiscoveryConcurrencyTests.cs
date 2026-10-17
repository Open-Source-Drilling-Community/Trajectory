using Microsoft.Data.Sqlite;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Text.Json;

namespace ServiceTest;

[TestFixture]
public sealed class DiscoveryConcurrencyTests
{
    [Test]
    public void Target_landing_discovery_uses_indexed_metadata_and_retains_filters_pages_and_staleness()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"LandingDiscovery_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var database = new SqlConnectionManagerTrajectory(Path.Combine(directory, "test.db"),
                NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Guid sourceId = Guid.NewGuid();
            using var connection = database.GetConnection()!;
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = "INSERT INTO TrajectoryTable (ID,LastModificationDate) VALUES (@id,'2026-01-02 12:00:00')";
                insert.Parameters.AddWithValue("@id", sourceId.ToString());
                insert.ExecuteNonQuery();
            }
            foreach (string suffix in new[] { "1", "2" })
            {
                Guid id = Guid.Parse("10000000-0000-4000-8000-00000000000" + suffix);
                using var insert = connection.CreateCommand();
                insert.CommandText = """
                    INSERT INTO TargetLandingCaseTable
                    (ID,MetaInfo,CreationDate,LastModificationDate,SourceTrajectoryID,TargetType,CurveType,AttitudeMode,
                     CalculationState,CalculationProgress,Name,Description,SourceTrajectoryRevision,CalculationFingerprint,TargetLandingCase)
                    VALUES (@id,@meta,'2026-01-02 12:00:00','2026-01-02 12:00:00',@source,'Point','BuildAndTurn','Free',
                            'Completed',1,@name,'metadata only','2026-01-02 12:00:00','fingerprint',@heavy)
                    """;
                insert.Parameters.AddWithValue("@id", id.ToString());
                insert.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(new MetaInfo { ID = id }, JsonSettings.Options));
                insert.Parameters.AddWithValue("@source", sourceId.ToString());
                insert.Parameters.AddWithValue("@name", "Landing " + suffix);
                // Discovery must not parse or load this engineering document at all.
                insert.Parameters.AddWithValue("@heavy", "not a JSON document");
                insert.ExecuteNonQuery();
            }
            var filters = new Dictionary<string, object?> { ["SourceTrajectoryID"] = sourceId };
            var first = DiscoveryRecords.ReadPage<TargetLandingCaseLight>(database.GetConnection(),
                "TargetLandingCaseTable", "TargetLandingCase", 0, 1, "lAnDiNg", filters, out int total);
            var second = DiscoveryRecords.ReadPage<TargetLandingCaseLight>(database.GetConnection(),
                "TargetLandingCaseTable", "TargetLandingCase", 1, 1, null, filters, out _);
            Assert.Multiple(() =>
            {
                Assert.That(total, Is.EqualTo(2));
                Assert.That(first.Single().Name, Is.EqualTo("Landing 1"));
                Assert.That(second.Single().Name, Is.EqualTo("Landing 2"));
                Assert.That(first.Single().IsStale, Is.False);
            });
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE TrajectoryTable SET LastModificationDate='2026-01-02 12:00:01'";
            update.ExecuteNonQuery();
            var stale = DiscoveryRecords.ReadPage<TargetLandingCaseLight>(database.GetConnection(),
                "TargetLandingCaseTable", "TargetLandingCase", 0, 1, null, filters, out _);
            Assert.That(stale.Single().IsStale, Is.True);
            filters["SourceTrajectoryID"] = Guid.NewGuid();
            Assert.That(DiscoveryRecords.ReadPage<TargetLandingCaseLight>(database.GetConnection(),
                "TargetLandingCaseTable", "TargetLandingCase", 0, 1, null, filters, out int emptyTotal), Is.Empty);
            Assert.That(emptyTotal, Is.Zero);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Discovery_reads_the_committed_snapshot_while_another_connection_is_writing()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"DiscoveryConcurrency_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, "test.db"), Cache = SqliteCacheMode.Private,
            Pooling = false, DefaultTimeout = 1
        }.ToString();
        try
        {
            using var writer = new SqliteConnection(connectionString);
            writer.Open();
            using (var setup = writer.CreateCommand())
            {
                setup.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE TrajectoryTable (ID TEXT PRIMARY KEY, Name TEXT, Trajectory TEXT);";
                setup.ExecuteNonQuery();
            }
            Guid committedId = Guid.NewGuid();
            Insert(writer, null, committedId, "Committed");
            using var transaction = writer.BeginTransaction();
            Insert(writer, transaction, Guid.NewGuid(), "Not committed yet");
            var reader = new SqliteConnection(connectionString);
            reader.Open();
            var page = DiscoveryRecords.ReadPage<TrajectoryLight>(reader, "TrajectoryTable", "Trajectory",
                0, 2, null, new Dictionary<string, object?>(), out int total);
            Assert.Multiple(() =>
            {
                Assert.That(total, Is.EqualTo(1));
                Assert.That(page, Has.Count.EqualTo(1));
                Assert.That(page[0].MetaInfo!.ID, Is.EqualTo(committedId));
            });
            transaction.Commit();
            using var count = writer.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM TrajectoryTable";
            Assert.That(count.ExecuteScalar(), Is.EqualTo(2L));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void Insert(SqliteConnection connection, SqliteTransaction? transaction, Guid id, string name)
    {
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO TrajectoryTable VALUES (@id,@name,@json)";
        insert.Parameters.AddWithValue("@id", id.ToString());
        insert.Parameters.AddWithValue("@name", name);
        insert.Parameters.AddWithValue("@json", JsonSerializer.Serialize(new TrajectoryLight
        {
            MetaInfo = new MetaInfo { ID = id }, Name = name
        }, JsonSettings.Options));
        insert.ExecuteNonQuery();
    }
}
