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
                DROP TABLE TrajectoryExtrapolationCaseTable;
                DROP TABLE AntiCollisionPolicyRevisionTable;
                DROP TABLE FieldAntiCollisionPolicyAssignmentTable;
                DROP TABLE TargetLandingCaseTable;
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
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
                DROP TABLE TrajectoryExtrapolationCaseTable;
                DROP TABLE AntiCollisionPolicyRevisionTable;
                DROP TABLE FieldAntiCollisionPolicyAssignmentTable;
                DROP TABLE TargetLandingCaseTable;
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
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

    [Test]
    public void Version_two_database_adds_extrapolation_table_without_changing_existing_data()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-extrapolation-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                DROP TABLE TrajectoryExtrapolationCaseTable;
                DROP TABLE AntiCollisionPolicyRevisionTable;
                DROP TABLE FieldAntiCollisionPolicyAssignmentTable;
                DROP TABLE TargetLandingCaseTable;
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                INSERT INTO TrajectoryTable(ID,Trajectory) VALUES('preserved-record','{}');
                PRAGMA user_version=2;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TrajectoryExtrapolationCaseTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TargetLandingCaseTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM TrajectoryTable WHERE ID='preserved-record'"), Is.EqualTo(1));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_three_database_adds_policy_tables_without_changing_existing_data()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-policy-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                DROP TABLE AntiCollisionPolicyRevisionTable;
                DROP TABLE FieldAntiCollisionPolicyAssignmentTable;
                DROP TABLE TargetLandingCaseTable;
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                INSERT INTO TrajectoryTable(ID,Trajectory) VALUES('preserved-record','{}');
                PRAGMA user_version=3;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AntiCollisionPolicyRevisionTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='FieldAntiCollisionPolicyAssignmentTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM TrajectoryTable WHERE ID='preserved-record'"), Is.EqualTo(1));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_four_database_adds_target_landing_table_without_changing_existing_data()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-target-landing-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                DROP TABLE TargetLandingCaseTable;
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                INSERT INTO TrajectoryTable(ID,Trajectory) VALUES('preserved-record','{}');
                PRAGMA user_version=4;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TargetLandingCaseTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM TrajectoryTable WHERE ID='preserved-record'"), Is.EqualTo(1));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_five_database_adds_and_backfills_target_landing_light_columns()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-target-landing-light-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                ALTER TABLE TargetLandingCaseTable RENAME TO TargetLandingCaseTableV6;
                CREATE TABLE TargetLandingCaseTable (
                    ID text primary key, MetaInfo text, CreationDate text, LastModificationDate text,
                    SourceTrajectoryID text, TargetType text, CurveType text, AttitudeMode text,
                    CalculationState text, CalculationProgress real, CalculationMessage text,
                    TargetLandingCase text);
                INSERT INTO TargetLandingCaseTable VALUES(
                    'case-id','{}','2026-10-03T10:00:00.0000000+00:00','2026-10-03T10:01:00.0000000+00:00',
                    'source-id','Polygon','ConstantBuildAndTurn','PositionOnly','Completed',1.0,'done',
                    '{"Name":"Landing A","Description":"Preserved","SourceTrajectoryRevision":"2026-10-03T09:00:00+00:00","CalculationFingerprint":"abc","SampleList":[{"PlaneX":1.0}]}');
                DROP TABLE TargetLandingCaseTableV6;
                CREATE UNIQUE INDEX TargetLandingCaseTableIndex ON TargetLandingCaseTable(ID);
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                PRAGMA user_version=5;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<string>(main, "SELECT Name FROM TargetLandingCaseTable WHERE ID='case-id'"), Is.EqualTo("Landing A"));
                Assert.That(Scalar<string>(main, "SELECT Description FROM TargetLandingCaseTable WHERE ID='case-id'"), Is.EqualTo("Preserved"));
                Assert.That(Scalar<string>(main, "SELECT CalculationFingerprint FROM TargetLandingCaseTable WHERE ID='case-id'"), Is.EqualTo("abc"));
                Assert.That(Scalar<string>(main, "SELECT TargetLandingCase FROM TargetLandingCaseTable WHERE ID='case-id'"), Does.Contain("SampleList"));
                Assert.That(Scalar<string>(main, "SELECT TargetLandingCaseEditData FROM TargetLandingCaseTable WHERE ID='case-id'"), Does.Not.Contain("SampleList"));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_six_database_adds_and_backfills_trajectory_light_columns()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-light-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                ALTER TABLE TrajectoryTable RENAME TO TrajectoryTableV7;
                CREATE TABLE TrajectoryTable (
                    ID text primary key, MetaInfo text, CreationDate text, LastModificationDate text,
                    FieldID text, ClusterID text, WellID text, WellBoreID text, TrajectoryType text,
                    IsDefinitive integer, CalculationState text, CalculationProgress real,
                    CalculationMessage text, Trajectory text);
                INSERT INTO TrajectoryTable VALUES(
                    'trajectory-id','{}','2026-10-03T10:00:00.0000000+00:00','2026-10-03T10:01:00.0000000+00:00',
                    NULL,NULL,NULL,'00000000-0000-0000-0000-000000000001','Planned',1,'Completed',1.0,NULL,
                    '{"Name":"Trajectory A","Description":"Preserved scalar projection","SurveyStationList":[{"MD":1.0}]}');
                DROP TABLE TrajectoryTableV7;
                CREATE UNIQUE INDEX TrajectoryTableIndex ON TrajectoryTable(ID);
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                PRAGMA user_version=6;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<string>(main, "SELECT Name FROM TrajectoryTable WHERE ID='trajectory-id'"), Is.EqualTo("Trajectory A"));
                Assert.That(Scalar<string>(main, "SELECT Description FROM TrajectoryTable WHERE ID='trajectory-id'"), Is.EqualTo("Preserved scalar projection"));
                Assert.That(Scalar<string>(main, "SELECT Trajectory FROM TrajectoryTable WHERE ID='trajectory-id'"), Does.Contain("SurveyStationList"));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_seven_database_adds_covering_index_for_target_landing_light_reads()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-target-landing-index-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, $$"""
                DROP INDEX {{SqlConnectionManagerTrajectory.TargetLandingLightCoveringIndexName}};
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                INSERT INTO TrajectoryTable(ID,LastModificationDate,Trajectory) VALUES(
                    'source-id','2026-10-05 10:00:00','{}');
                INSERT INTO TargetLandingCaseTable(
                    ID,MetaInfo,CreationDate,LastModificationDate,SourceTrajectoryID,TargetType,CurveType,
                    AttitudeMode,CalculationState,CalculationProgress,CalculationMessage,TargetLandingCase,
                    Name,Description,SourceTrajectoryRevision,CalculationFingerprint,TargetLandingCaseEditData)
                VALUES(
                    'case-id','{"ID":"case-id"}','2026-10-05 09:00:00','2026-10-05 10:00:00',
                    'source-id','DrillerTarget','CircularArc','FreeLandingAttitude','Completed',1.0,'done',
                    zeroblob(1048576),'Landing A','Preserved','2026-10-05 10:00:00','fingerprint','{}');
                PRAGMA user_version=7;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            List<string> plan = QueryStrings(main,
                "EXPLAIN QUERY PLAN " + TargetLandingCaseManager.LightSelect + " ORDER BY c.CreationDate");
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"),
                    Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<long>(main,
                    $"SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='{SqlConnectionManagerTrajectory.TargetLandingLightCoveringIndexName}'"),
                    Is.EqualTo(1));
                Assert.That(plan.Any(line => line.Contains(
                    $"COVERING INDEX {SqlConnectionManagerTrajectory.TargetLandingLightCoveringIndexName}",
                    StringComparison.OrdinalIgnoreCase)), Is.True,
                    $"The light query must not read the table rows containing the large result payload. Plan: {string.Join(" | ", plan)}");
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_eight_database_adds_directional_control_tables_without_changing_existing_data()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-directional-control-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                DROP TABLE DirectionalControlEvaluationSampleChunkTable;
                DROP TABLE DirectionalControlEvaluationCaseTable;
                INSERT INTO TrajectoryTable(ID,Trajectory) VALUES('preserved-record','{}');
                PRAGMA user_version=8;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DirectionalControlEvaluationCaseTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DirectionalControlEvaluationSampleChunkTable'"), Is.EqualTo(1));
                Assert.That(Scalar<long>(main, "SELECT COUNT(*) FROM TrajectoryTable WHERE ID='preserved-record'"), Is.EqualTo(1));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Version_nine_database_renames_unit_bearing_anti_collision_age_property()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-policy-unit-migration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "Trajectory.db");
        try
        {
            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Execute(mainPath, """
                INSERT INTO AntiCollisionPolicyRevisionTable(
                    ID,PolicyID,RevisionNumber,Name,CreationDate,AntiCollisionPolicyRevision)
                VALUES(
                    'revision-id','policy-id',1,'Policy','2026-10-05 10:00:00',
                    '{"Rules":[{"Conditions":[{"$type":"TrajectoryAge","AgeThresholdSeconds":86400}]}]}');
                PRAGMA user_version=9;
                """);

            _ = new SqlConnectionManagerTrajectory(mainPath, NullLogger<SqlConnectionManagerTrajectory>.Instance);

            using SqliteConnection main = Open(mainPath);
            string document = Scalar<string>(main,
                "SELECT AntiCollisionPolicyRevision FROM AntiCollisionPolicyRevisionTable WHERE ID='revision-id'");
            Assert.Multiple(() =>
            {
                Assert.That(Scalar<long>(main, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManagerTrajectory.TrajectorySchemaVersion));
                Assert.That(document, Does.Contain("\"AgeThreshold\":86400"));
                Assert.That(document, Does.Not.Contain("AgeThresholdSeconds"));
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

    private static List<string> QueryStrings(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> values = [];
        while (reader.Read()) values.Add(reader.GetString(3));
        return values;
    }
}
