using System.Buffers.Binary;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.Octree;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class OctreePersistenceTests
{
    [Test]
    public void Version_one_database_is_backed_up_and_migrated_without_losing_memberships()
    {
        WithDatabase((directory, path) =>
        {
            Guid trajectoryId = Guid.NewGuid();
            byte[] codes = Serialize(new OctreeCodeLong(23, 123UL, 456UL));
            using (SqliteConnection connection = Open(path))
            {
                Execute(connection, """
                    CREATE TABLE GlobalOctreeCache (
                        OctreeCodeCacheHigh BIGINT, OctreeCodeCacheLow BIGINT, TrajectoryID TEXT,
                        IsPlanned BOOL, IsMeasured BOOL, IsDefinitive BOOL,
                        OctreeCodeCount INTEGER, OctreeCodes BLOB);
                    CREATE UNIQUE INDEX GlobalOctreeCacheIndex ON GlobalOctreeCache
                        (OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID);
                    CREATE TABLE GlobalOctreeWellbores (
                        TrajectoryID TEXT, IsPlanned BOOL, IsMeasured BOOL, IsDefinitive BOOL);
                    CREATE UNIQUE INDEX GlobalOctreeWellboresTrajectoryIndex ON GlobalOctreeWellbores (TrajectoryID);
                    PRAGMA user_version=1;
                    """);
                using SqliteCommand insertState = connection.CreateCommand();
                insertState.CommandText = "INSERT INTO GlobalOctreeWellbores VALUES($id,1,0,1)";
                insertState.Parameters.AddWithValue("$id", trajectoryId.ToString());
                insertState.ExecuteNonQuery();
                using SqliteCommand insertMembership = connection.CreateCommand();
                insertMembership.CommandText = "INSERT INTO GlobalOctreeCache VALUES(123,456,$id,1,0,1,1,$codes)";
                insertMembership.Parameters.AddWithValue("$id", trajectoryId.ToString());
                insertMembership.Parameters.Add("$codes", SqliteType.Blob).Value = codes;
                insertMembership.ExecuteNonQuery();
            }

            _ = new SqlConnectionManagerOctree(path, NullLogger<SqlConnectionManagerOctree>.Instance);

            string backupPath = Directory.GetFiles(directory, "GlobalAntiCollision.schema-v1-*.bak").Single();
            using SqliteConnection migrated = Open(path);
            using SqliteConnection backup = Open(backupPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(migrated, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerOctree.OctreeSchemaVersion));
                Assert.That(Scalar<long>(migrated, "SELECT COUNT(*) FROM GlobalOctreeBucketMembership"), Is.EqualTo(1));
                Assert.That(Scalar<string>(migrated, "SELECT TrajectoryType FROM GlobalOctreeTrajectoryState"), Is.EqualTo("Planned"));
                Assert.That(Scalar<long>(migrated, "SELECT IsDefinitive FROM GlobalOctreeTrajectoryState"), Is.EqualTo(1));
                Assert.That(ReadBlob(migrated, "SELECT OctreeCodes FROM GlobalOctreeBucketMembership"), Is.EqualTo(codes));
                Assert.That(Scalar<long>(migrated, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='GlobalOctreeCache'"), Is.Zero);
                Assert.That(Scalar<string>(backup, "PRAGMA integrity_check"), Is.EqualTo("ok"));
                Assert.That(Scalar<long>(backup, "SELECT COUNT(*) FROM GlobalOctreeCache"), Is.EqualTo(1));
            });
        });
    }

    [Test]
    public void Replacement_is_atomic_and_preserves_previous_index_when_an_insert_fails()
    {
        WithManager((path, manager) =>
        {
            Guid trajectoryId = Guid.NewGuid();
            var original = new OctreeCodeLong(23, 8UL, 16UL);
            var replacement = new OctreeCodeLong(23, 24UL, 32UL);
            Assert.That(manager.Add([original], trajectoryId, false, true, true), Is.True);

            using (SqliteConnection connection = Open(path))
            {
                Execute(connection, $"""
                    CREATE TRIGGER RejectMembership BEFORE INSERT ON GlobalOctreeBucketMembership
                    WHEN NEW.TrajectoryID = '{trajectoryId}'
                    BEGIN SELECT RAISE(ABORT, 'injected failure'); END;
                    """);
            }

            Assert.That(manager.Add([replacement], trajectoryId, true, false, false), Is.False);
            List<OctreeCodeLong> retained = manager.Get(trajectoryId);
            Assert.Multiple(() =>
            {
                Assert.That(retained, Has.Count.EqualTo(1));
                Assert.That(retained[0].Depth, Is.EqualTo(original.Depth));
                Assert.That(retained[0].CodeHigh, Is.EqualTo(original.CodeHigh));
                Assert.That(retained[0].CodeLow, Is.EqualTo(original.CodeLow));
                using SqliteConnection verification = Open(path);
                Assert.That(Scalar<string>(verification, "SELECT TrajectoryType FROM GlobalOctreeTrajectoryState"), Is.EqualTo("Actual"));
                Assert.That(Scalar<long>(verification, "SELECT IsDefinitive FROM GlobalOctreeTrajectoryState"), Is.EqualTo(1));
            });
        });
    }

    [Test]
    public void Search_filters_by_authoritative_trajectory_type_and_definitive_state()
    {
        WithManager((_, manager) =>
        {
            var sharedCode = new OctreeCodeLong(23, 80UL, 160UL);
            Guid actualDefinitive = Guid.NewGuid();
            Guid plannedDefinitive = Guid.NewGuid();
            Guid actualTemporary = Guid.NewGuid();
            Assert.That(manager.Add([sharedCode], actualDefinitive, false, true, true), Is.True);
            Assert.That(manager.Add([sharedCode], plannedDefinitive, true, false, true), Is.True);
            Assert.That(manager.Add([sharedCode], actualTemporary, false, true, false), Is.True);

            var indexedTrajectory = new OSDC.Drilling.Trajectory.Model.Trajectory
            {
                MetaInfo = new OSDC.DotnetLibraries.General.DataManagement.MetaInfo { ID = actualDefinitive },
                TrajectoryType = TrajectoryType.Actual,
                IsDefinitive = true
            };

            Assert.Multiple(() =>
            {
                Assert.That(manager.IsCurrent(indexedTrajectory), Is.True);
                Assert.That(manager.Search([sharedCode], TrajectoryType.Actual, true), Is.EqualTo(new[] { actualDefinitive }));
                Assert.That(manager.Search([sharedCode], TrajectoryType.Planned, true), Is.EqualTo(new[] { plannedDefinitive }));
                Assert.That(manager.Search([sharedCode], TrajectoryType.Actual, false), Is.EqualTo(new[] { actualTemporary }));
            });

            Assert.That(manager.UpdateClassification(actualTemporary, TrajectoryType.Planned, true), Is.True);
            Assert.That(manager.Search([sharedCode], TrajectoryType.Planned, true), Is.EquivalentTo(new[] { plannedDefinitive, actualTemporary }));
        });
    }

    [Test]
    public void Delete_removes_state_and_every_bucket_membership()
    {
        WithManager((path, manager) =>
        {
            Guid trajectoryId = Guid.NewGuid();
            Assert.That(manager.Add(
                [new OctreeCodeLong(23, 8UL, 16UL), new OctreeCodeLong(23, ulong.MaxValue / 2, 32UL)],
                trajectoryId, false, true, true), Is.True);
            Assert.That(manager.Delete(trajectoryId), Is.True);

            using SqliteConnection connection = Open(path);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(connection, $"SELECT COUNT(*) FROM GlobalOctreeTrajectoryState WHERE TrajectoryID='{trajectoryId}'"), Is.Zero);
                Assert.That(Scalar<long>(connection, $"SELECT COUNT(*) FROM GlobalOctreeBucketMembership WHERE TrajectoryID='{trajectoryId}'"), Is.Zero);
            });
        });
    }

    private static void WithManager(Action<string, OctreeManager> test)
    {
        WithDatabase((_, path) =>
        {
            var connection = new SqlConnectionManagerOctree(path, NullLogger<SqlConnectionManagerOctree>.Instance);
            var manager = new OctreeManager(NullLogger<OctreeManager>.Instance, connection);
            test(path, manager);
        });
    }

    private static void WithDatabase(Action<string, string> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-octree", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            test(directory, Path.Combine(directory, "GlobalAntiCollision.db"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static SqliteConnection Open(string path)
    {
        SqliteConnection connection = new($"Data Source={path}");
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static T Scalar<T>(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    private static byte[] ReadBlob(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return (byte[])command.ExecuteScalar()!;
    }

    private static byte[] Serialize(OctreeCodeLong code)
    {
        byte[] result = new byte[17];
        result[0] = code.Depth;
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(1, 8), code.CodeHigh);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(9, 8), code.CodeLow);
        return result;
    }
}
