using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class TrajectoryCatalogMigrationTests
{
    [Test]
    public void Version_one_database_imports_legacy_catalog_transactionally_and_retains_source_file()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-catalog-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        string catalogPath = Path.Combine(directory, "TrajectoryCatalog.db");
        Guid identityId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                DROP TABLE TrajectoryIdentityTable;
                DROP TABLE TrajectoryFeatureCategoryTable;
                INSERT INTO TrajectoryTable(ID,Trajectory) VALUES('preserved-record','{}');
                PRAGMA user_version=1;
                """);
            Execute(catalogPath, $$"""
                CREATE TABLE TrajectoryIdentityTable (ID text primary key,MetaInfo text,Name text,CreationDate text,LastModificationDate text,TrajectoryIdentity text);
                CREATE UNIQUE INDEX TrajectoryIdentityTableIndex ON TrajectoryIdentityTable(ID);
                CREATE TABLE TrajectoryFeatureCategoryTable (ID text primary key,MetaInfo text,Name text,IsExclusive integer,HasValidityPeriod integer,CreationDate text,LastModificationDate text,TrajectoryFeatureCategory text);
                CREATE UNIQUE INDEX TrajectoryFeatureCategoryTableIndex ON TrajectoryFeatureCategoryTable(ID);
                INSERT INTO TrajectoryIdentityTable VALUES('{{identityId}}','{"ID":"{{identityId}}"}','Legacy identity',NULL,NULL,'{"MetaInfo":{"ID":"{{identityId}}"},"Name":"Legacy identity"}');
                INSERT INTO TrajectoryFeatureCategoryTable VALUES('{{categoryId}}','{"ID":"{{categoryId}}"}','Legacy feature',0,0,NULL,NULL,'{"MetaInfo":{"ID":"{{categoryId}}"},"Name":"Legacy feature","IsExclusive":false,"HasValidityPeriod":false,"Options":[]}');
                PRAGMA user_version=1;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            using SqliteConnection legacy = Open(catalogPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<string>(main, $"SELECT Name FROM TrajectoryIdentityTable WHERE ID='{identityId}'"), Is.EqualTo("Legacy identity"));
                Assert.That(Scalar<string>(main, $"SELECT Name FROM TrajectoryFeatureCategoryTable WHERE ID='{categoryId}'"), Is.EqualTo("Legacy feature"));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM TrajectoryTable WHERE ID='preserved-record'"), Is.EqualTo(1));
                Assert.That(Scalar<string>(legacy, $"SELECT Name FROM TrajectoryIdentityTable WHERE ID='{identityId}'"), Is.EqualTo("Legacy identity"));
                Assert.That(File.Exists(catalogPath), Is.True);
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Malformed_legacy_catalog_stops_migration_without_changing_main_database()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-catalog-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        string catalogPath = Path.Combine(directory, "TrajectoryCatalog.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                DROP TABLE TrajectoryIdentityTable;
                DROP TABLE TrajectoryFeatureCategoryTable;
                INSERT INTO TrajectoryTable(ID,Trajectory) VALUES('preserved-record','{}');
                PRAGMA user_version=1;
                """);
            Execute(catalogPath, "CREATE TABLE UnexpectedCatalogTable(ID text primary key); INSERT INTO UnexpectedCatalogTable VALUES('keep-me');");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance))!;

            using SqliteConnection main = Open(mainPath);
            using SqliteConnection legacy = Open(catalogPath);
            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Does.Contain("No data was changed"));
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM TrajectoryTable WHERE ID='preserved-record'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TrajectoryIdentityTable'"), Is.Zero);
                Assert.That(Scalar<string>(legacy, "SELECT ID FROM UnexpectedCatalogTable"), Is.EqualTo("keep-me"));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
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
        var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        return connection;
    }

    private static T Scalar<T>(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }
}
