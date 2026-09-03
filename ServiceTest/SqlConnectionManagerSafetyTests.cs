using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class SqlConnectionManagerSafetyTests
{
    private static readonly IReadOnlyDictionary<string, string[]> Schema =
        new Dictionary<string, string[]> { ["TrajectoryTable"] = ["ID text primary key", "Name text"] };

    [Test]
    public void Fresh_database_is_created_transactionally_and_versioned()
    {
        WithDatabase(path =>
        {
            _ = new TestConnectionManager(path, Schema);

            using SqliteConnection connection = Open(path);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TrajectoryTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='TrajectoryTableIndex'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(connection, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManager.CURRENT_SCHEMA_VERSION));
            });
        });
    }

    [Test]
    public void Matching_unversioned_legacy_database_is_adopted_without_rewriting_rows()
    {
        WithDatabase(path =>
        {
            Execute(path, "CREATE TABLE TrajectoryTable (ID text primary key, Name text); INSERT INTO TrajectoryTable VALUES ('legacy-id','legacy-name');");

            _ = new TestConnectionManager(path, Schema);

            using SqliteConnection connection = Open(path);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<string>(connection, "SELECT Name FROM TrajectoryTable WHERE ID='legacy-id'"), Is.EqualTo("legacy-name"));
                Assert.That(Scalar<long>(connection, "SELECT COUNT(*) FROM TrajectoryTable"), Is.EqualTo(1));
                Assert.That(Scalar<long>(connection, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManager.CURRENT_SCHEMA_VERSION));
            });
        });
    }

    [Test]
    public void Malformed_database_fails_closed_without_dropping_or_replacing_data()
    {
        WithDatabase(path =>
        {
            Execute(path, "CREATE TABLE TrajectoryTable (ID text primary key, Unexpected text); INSERT INTO TrajectoryTable VALUES ('legacy-id','keep-me');");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _ = new TestConnectionManager(path, Schema))!;

            using SqliteConnection connection = Open(path);
            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Does.Contain("No data was changed"));
                Assert.That(Scalar<string>(connection, "SELECT Unexpected FROM TrajectoryTable WHERE ID='legacy-id'"), Is.EqualTo("keep-me"));
                Assert.That(Scalar<long>(connection, "PRAGMA user_version"), Is.Zero);
            });
        });
    }

    [Test]
    public void Newer_database_version_fails_closed_without_rewriting_rows()
    {
        WithDatabase(path =>
        {
            Execute(path, "CREATE TABLE TrajectoryTable (ID text primary key, Name text); INSERT INTO TrajectoryTable VALUES ('future-id','future-name'); PRAGMA user_version=2;");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _ = new TestConnectionManager(path, Schema))!;

            using SqliteConnection connection = Open(path);
            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Does.Contain("newer than supported"));
                Assert.That(Scalar<string>(connection, "SELECT Name FROM TrajectoryTable WHERE ID='future-id'"), Is.EqualTo("future-name"));
                Assert.That(Scalar<long>(connection, "PRAGMA user_version"), Is.EqualTo(2));
            });
        });
    }

    private static void WithDatabase(Action<string> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-db-safety", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            test(Path.Combine(directory, "Trajectory.db"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void Execute(string path, string sql)
    {
        using SqliteConnection connection = Open(path);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static SqliteConnection Open(string path)
    {
        SqliteConnection connection = new($"Data Source={path}");
        connection.Open();
        return connection;
    }

    private static T Scalar<T>(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    private sealed class TestConnectionManager : SqlConnectionManager
    {
        public TestConnectionManager(string path, IReadOnlyDictionary<string, string[]> schema)
            : base(BuildConnectionString(path), NullLogger.Instance, path, Path.GetFileName(path), schema)
        {
        }
    }
}
