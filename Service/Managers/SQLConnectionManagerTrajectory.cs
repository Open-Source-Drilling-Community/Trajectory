using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace OSDC.Drilling.Trajectory.Service.Managers
{
    /// <summary>
    /// A manager for the sql database connection, registered as a singleton through dependency injection (see Program.cs)
    /// Existing version-1 through version-5 databases are migrated additively to version 6 by adding
    /// the shared catalogs, trajectory extrapolation and target-landing cases, and anti-collision policy tables as needed.
    /// If a validated TrajectoryCatalog.db exists beside the main database, its rows are copied and the source file is retained.
    /// </summary>
    /// <remarks>
    /// SQLite database connection strategy:
    /// - single connection for every access (chosen strategy in the general case)
    ///     each access to the database is performed through isolated connections stored in a List of connections
    ///     > isolation, reliability, fail-safe, thread-safe, but overhead due to opening connections
    /// - shared connection between access
    ///     one connection is opened for the lifetime of the application and used to access database through various web requests and commands 
    ///     > no overhead, but issues with concurrency, single-point of failure, state management
    /// - scoped connection (registering service with AddScoped rather than AddSingleton)
    ///     one connection is opened per web request
    ///     > same problems as with shared connection, but limited to the scope of one webrequest rather than to the whole lifetime of the application
    /// </remarks>
    public class SqlConnectionManagerTrajectory : SqlConnectionManager
    {
        private const string DatabaseName = "Trajectory.db";
        public const int TrajectorySchemaVersion = 8;
        internal const string TargetLandingLightCoveringIndexName = "TargetLandingCaseLightCoveringIndex";

        private static readonly string[] TrajectoryVersion6Columns =
        [
            "ID", "MetaInfo", "CreationDate", "LastModificationDate", "FieldID", "ClusterID", "WellID",
            "WellBoreID", "TrajectoryType", "IsDefinitive", "CalculationState", "CalculationProgress",
            "CalculationMessage", "Trajectory"
        ];

        private static readonly string[] TargetLandingCaseVersion5Columns =
        [
            "ID", "MetaInfo", "CreationDate", "LastModificationDate", "SourceTrajectoryID", "TargetType",
            "CurveType", "AttitudeMode", "CalculationState", "CalculationProgress", "CalculationMessage",
            "TargetLandingCase"
        ];

        // dictionary describing tables format
        // Light weight data fields are enumerated explicitly in the data table implementing the light weight data concept
        // (thus duplicating info in the database) for 2 reasons
        // 1) to avoid loading the complete Trajectory (heavy weight data) each time we only need contextual info on the data (light weight data)
        // 2) to keep control of the logic of inserting and selecting a light data in the database
        //    localized at the controller/manager level (storing TrajectoryLight as a whole could induce database corruption issues)
        // If the light weight data concept is not implemented, the same contextual info can be retrieved directly from the Trajectory
        private static readonly IReadOnlyDictionary<string, string[]> TableStructureDictTrajectory = new Dictionary<string, string[]>()
            {
                { "TrajectoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldID text",
                    "ClusterID text",
                    "WellID text",
                    "WellBoreID text",
                    "TrajectoryType text",
                    "IsDefinitive integer",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "Trajectory text",
                    "Name text",
                    "Description text" }
                },
                { "SurveyRunTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldID text",
                    "ClusterID text",
                    "WellID text",
                    "WellBoreID text",
                    "SurveyInstrumentID text",
                    "SurveyRunType text",
                    "CalculationType text",
                    "ParentSurveyRunID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "SurveyRun text" }
                },
                { "SurveyRunMeasurementChunkTable", new string[] {
                    "ID text primary key",
                    "SurveyRunID text",
                    "ChunkIndex integer",
                    "MeasurementCount integer",
                    "StartMD real",
                    "EndMD real",
                    "SurveyMeasurementChunk text" }
                },
                { "SurveyStationChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "OwnerType text",
                    "ChunkIndex integer",
                    "StationCount integer",
                    "StartMD real",
                    "EndMD real",
                    "SurveyStationChunk text" }
                },
                { "InterpolatedTrajectoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "TrajectoryID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "InterpolatedTrajectory text" }
                },
                { "TrajectoryExtrapolationCaseTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "SourceTrajectoryID text",
                    "Mode text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "TrajectoryExtrapolationCase text" }
                },
                { "TargetLandingCaseTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "SourceTrajectoryID text",
                    "TargetType text",
                    "CurveType text",
                    "AttitudeMode text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "TargetLandingCase text",
                    "Name text",
                    "Description text",
                    "SourceTrajectoryRevision text",
                    "CalculationFingerprint text",
                    "TargetLandingCaseEditData text" }
                },
                { "TrajectoryRealizationCaseTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "TrajectoryID text",
                    "RealizationCount integer",
                    "CoarseningMaximumDistance real",
                    "RandomSeed integer",
                    "ReferenceStationCount integer",
                    "CoarsenedStationCount integer",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "TrajectoryRealizationCase text" }
                },
                { "TrajectoryRealizationChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "RealizationCount integer",
                    "SurveyPointCount integer",
                    "StartMD real",
                    "EndMD real",
                    "TrajectoryRealizationChunk text" }
                },
                { "TrajectoryAggregationCaseTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "EpsilonL real",
                    "EpsilonKappa real",
                    "Alpha real",
                    "InterpolationInterval real",
                    "DistanceReferenceCoarseningThreshold real",
                    "TrajectoryAggregationCase text" }
                },
                { "SurveyPointChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "OwnerType text",
                    "ChunkIndex integer",
                    "PointCount integer",
                    "StartMD real",
                    "EndMD real",
                    "SurveyPointChunk text" }
                },
                { "TrajectoryAggregationDistanceResultChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "ResultCount integer",
                    "StartReferenceMD real",
                    "EndReferenceMD real",
                    "TrajectoryAggregationDistanceResultChunk text" }
                },
                { "SurveyRunBatchImportTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "SurveyRunBatchImport text" }
                },
                { "SurveyStationEllipseCalculationTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "ConfidenceFactor real",
                    "SurveyStationEllipseCalculation text" }
                },
                { "TrajectoryMinimumDistanceCalculationTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "ReferenceTrajectoryID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "ResultCount integer",
                    "IntervalResultCount integer",
                    "TrajectoryMinimumDistanceCalculation text" }
                },
                { "TrajectoryMinimumDistanceResultChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "ResultCount integer",
                    "StartReferenceMD real",
                    "EndReferenceMD real",
                    "TrajectoryMinimumDistanceResultChunk text" }
                },
                { "SurveyRunMinimumDistanceCalculationTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "ReferenceSurveyRunID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "ResultCount integer",
                    "IntervalResultCount integer",
                    "SurveyRunMinimumDistanceCalculation text" }
                },
                { "SurveyRunMinimumDistanceResultChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "ResultCount integer",
                    "StartReferenceMD real",
                    "EndReferenceMD real",
                    "SurveyRunMinimumDistanceResultChunk text" }
                },
                { "TrajectoryIdentityTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "TrajectoryIdentity text" }
                },
                { "TrajectoryFeatureCategoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "IsExclusive integer",
                    "HasValidityPeriod integer",
                    "CreationDate text",
                    "LastModificationDate text",
                    "TrajectoryFeatureCategory text" }
                },
                { "AntiCollisionPolicyRevisionTable", new string[] {
                    "ID text primary key",
                    "PolicyID text",
                    "RevisionNumber integer",
                    "Name text",
                    "CreationDate text",
                    "AntiCollisionPolicyRevision text" }
                },
                { "FieldAntiCollisionPolicyAssignmentTable", new string[] {
                    "ID text primary key",
                    "FieldID text",
                    "PolicyRevisionID text",
                    "ValidFromUtc text",
                    "ValidToUtc text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldAntiCollisionPolicyAssignment text" }
                }
            };

        private static readonly IReadOnlyDictionary<string, string[]> TableIndexDefinitionsTrajectory =
            CreateTrajectoryIndexDefinitions();

        public SqlConnectionManagerTrajectory(ILogger<SqlConnectionManagerTrajectory> logger)
            : base(BuildConnectionString(PrepareDatabase(BuildDatabasePath(DatabaseName), logger)), logger,
                BuildDatabasePath(DatabaseName), DatabaseName, TableStructureDictTrajectory,
                TableIndexDefinitionsTrajectory,
                currentSchemaVersion: TrajectorySchemaVersion)
        {
        }

        public SqlConnectionManagerTrajectory(string databasePath, ILogger<SqlConnectionManagerTrajectory> logger)
            : base(BuildConnectionString(PrepareDatabase(databasePath, logger)), logger, databasePath, DatabaseName,
                TableStructureDictTrajectory, TableIndexDefinitionsTrajectory,
                currentSchemaVersion: TrajectorySchemaVersion)
        {
        }

        private static string PrepareDatabase(string databasePath, ILogger logger)
        {
            if (!File.Exists(databasePath)) return databasePath;

            using var connection = new SqliteConnection(BuildConnectionString(databasePath));
            connection.Open();
            List<string> tables = ReadTableNames(connection);
            int version = ReadSchemaVersion(connection);
            if (version >= TrajectorySchemaVersion) return databasePath;

            string[] catalogTables = ["TrajectoryIdentityTable", "TrajectoryFeatureCategoryTable"];
            string[] extrapolationTables = ["TrajectoryExtrapolationCaseTable"];
            string[] policyTables = ["AntiCollisionPolicyRevisionTable", "FieldAntiCollisionPolicyAssignmentTable"];
            string[] targetLandingTables = ["TargetLandingCaseTable"];
            string[] expectedBeforeMigration = version switch
            {
                1 => TableStructureDictTrajectory.Keys.Except(catalogTables, StringComparer.Ordinal)
                    .Except(extrapolationTables, StringComparer.Ordinal).Except(policyTables, StringComparer.Ordinal)
                    .Except(targetLandingTables, StringComparer.Ordinal).ToArray(),
                2 => TableStructureDictTrajectory.Keys.Except(extrapolationTables, StringComparer.Ordinal)
                    .Except(policyTables, StringComparer.Ordinal).Except(targetLandingTables, StringComparer.Ordinal).ToArray(),
                3 => TableStructureDictTrajectory.Keys.Except(policyTables, StringComparer.Ordinal)
                    .Except(targetLandingTables, StringComparer.Ordinal).ToArray(),
                4 => TableStructureDictTrajectory.Keys.Except(targetLandingTables, StringComparer.Ordinal).ToArray(),
                5 => TableStructureDictTrajectory.Keys.ToArray(),
                _ => []
            };
            bool expectedShape = tables.Order().SequenceEqual(expectedBeforeMigration.Order(), StringComparer.Ordinal);
            bool currentShape = tables.Order().SequenceEqual(TableStructureDictTrajectory.Keys.Order(), StringComparer.Ordinal);
            bool expectedColumns = tables.All(table => table switch
            {
                "TrajectoryTable" when version <= 6 => HasColumns(connection, table, TrajectoryVersion6Columns) || HasExpectedColumns(connection, table),
                "TargetLandingCaseTable" when version == 5 => HasColumns(connection, table, TargetLandingCaseVersion5Columns),
                _ => HasExpectedColumns(connection, table)
            });
            if ((!expectedShape && !currentShape) || !expectedColumns)
                return databasePath; // The base validator rejects the database without changing it.

            string legacyCatalogPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(databasePath))!, "TrajectoryCatalog.db");
            bool importLegacyCatalog = version == 1 && ValidateLegacyCatalog(legacyCatalogPath);
            if (importLegacyCatalog)
            {
                using SqliteCommand attach = connection.CreateCommand();
                attach.CommandText = "ATTACH DATABASE $path AS legacy_catalog";
                attach.Parameters.AddWithValue("$path", legacyCatalogPath);
                attach.ExecuteNonQuery();
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                string[] additiveTables = catalogTables.Concat(extrapolationTables).Concat(policyTables)
                    .Concat(targetLandingTables).ToArray();
                foreach (string table in additiveTables.Where(table => !tables.Contains(table, StringComparer.Ordinal)))
                {
                    using SqliteCommand create = connection.CreateCommand();
                    create.Transaction = transaction;
                    create.CommandText = $"CREATE TABLE \"{table}\" ({string.Join(',', TableStructureDictTrajectory[table])})";
                    create.ExecuteNonQuery();
                    using SqliteCommand index = connection.CreateCommand();
                    index.Transaction = transaction;
                    index.CommandText = $"CREATE UNIQUE INDEX \"{table}Index\" ON \"{table}\" (\"ID\")";
                    index.ExecuteNonQuery();
                }

                if (version == 5)
                {
                    AddTargetLandingLightColumns(connection, transaction);
                }
                if (version <= 6 && HasColumns(connection, "TrajectoryTable", TrajectoryVersion6Columns))
                {
                    AddTrajectoryLightColumns(connection, transaction);
                }

                CreateTargetLandingLightCoveringIndex(connection, transaction);

                if (importLegacyCatalog)
                {
                    CopyLegacyCatalog(connection, transaction, "TrajectoryIdentityTable",
                        "ID,MetaInfo,Name,CreationDate,LastModificationDate,TrajectoryIdentity");
                    CopyLegacyCatalog(connection, transaction, "TrajectoryFeatureCategoryTable",
                        "ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,TrajectoryFeatureCategory");
                }

                using SqliteCommand setVersion = connection.CreateCommand();
                setVersion.Transaction = transaction;
                setVersion.CommandText = $"PRAGMA user_version={TrajectorySchemaVersion}";
                setVersion.ExecuteNonQuery();
                transaction.Commit();
                logger.LogInformation("Migrated Trajectory.db to schema version {SchemaVersion}. Legacy catalog import performed: {ImportedLegacyCatalog}; the source catalog file, when present, was retained.",
                    TrajectorySchemaVersion, importLegacyCatalog);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            return databasePath;
        }

        private static IReadOnlyDictionary<string, string[]> CreateTrajectoryIndexDefinitions()
        {
            Dictionary<string, string[]> definitions = CreateDefaultIndexDefinitions(TableStructureDictTrajectory)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            definitions["TargetLandingCaseTable"] =
            [
                .. definitions["TargetLandingCaseTable"],
                TargetLandingLightCoveringIndexSql
            ];
            return definitions;
        }

        private const string TargetLandingLightCoveringIndexSql =
            "CREATE INDEX \"" + TargetLandingLightCoveringIndexName + "\" ON \"TargetLandingCaseTable\" " +
            "(\"ID\",\"MetaInfo\",\"CreationDate\",\"LastModificationDate\",\"SourceTrajectoryID\",\"TargetType\",\"CurveType\",\"AttitudeMode\",\"CalculationState\",\"CalculationProgress\",\"CalculationMessage\",\"Name\",\"Description\",\"SourceTrajectoryRevision\",\"CalculationFingerprint\")";

        private static void CreateTargetLandingLightCoveringIndex(
            SqliteConnection connection,
            SqliteTransaction transaction)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = TargetLandingLightCoveringIndexSql.Replace(
                "CREATE INDEX", "CREATE INDEX IF NOT EXISTS", StringComparison.Ordinal);
            command.ExecuteNonQuery();
        }

        private static bool ValidateLegacyCatalog(string path)
        {
            if (!File.Exists(path)) return false;
            using var connection = new SqliteConnection(BuildConnectionString(path));
            connection.Open();
            List<string> tables = ReadTableNames(connection);
            string[] expected = ["TrajectoryIdentityTable", "TrajectoryFeatureCategoryTable"];
            if (!tables.Order().SequenceEqual(expected.Order(), StringComparer.Ordinal))
                throw new InvalidOperationException("TrajectoryCatalog.db has an unexpected structure. No data was changed.");
            foreach (string table in expected)
                if (!HasExpectedColumns(connection, table))
                    throw new InvalidOperationException($"TrajectoryCatalog.db table '{table}' is malformed. No data was changed.");
            return true;
        }

        private static void CopyLegacyCatalog(SqliteConnection connection, SqliteTransaction transaction, string table, string columns)
        {
            using SqliteCommand copy = connection.CreateCommand();
            copy.Transaction = transaction;
            copy.CommandText = $"INSERT OR IGNORE INTO main.\"{table}\"({columns}) SELECT {columns} FROM legacy_catalog.\"{table}\"";
            copy.ExecuteNonQuery();
        }

        private static List<string> ReadTableNames(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            using SqliteDataReader reader = command.ExecuteReader();
            List<string> result = [];
            while (reader.Read()) result.Add(reader.GetString(0));
            return result;
        }

        private static int ReadSchemaVersion(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static bool HasExpectedColumns(SqliteConnection connection, string table)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{table}\")";
            using SqliteDataReader reader = command.ExecuteReader();
            List<string> actual = [];
            while (reader.Read()) actual.Add(reader.GetString(1));
            string[] expected = TableStructureDictTrajectory[table]
                .Select(value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]).ToArray();
            return actual.SequenceEqual(expected, StringComparer.Ordinal);
        }

        private static bool HasColumns(SqliteConnection connection, string table, IReadOnlyList<string> expected)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{table}\")";
            using SqliteDataReader reader = command.ExecuteReader();
            List<string> actual = [];
            while (reader.Read()) actual.Add(reader.GetString(1));
            return actual.SequenceEqual(expected, StringComparer.Ordinal);
        }

        private static void AddTargetLandingLightColumns(SqliteConnection connection, SqliteTransaction transaction)
        {
            foreach (string column in new[] { "Name text", "Description text", "SourceTrajectoryRevision text", "CalculationFingerprint text", "TargetLandingCaseEditData text" })
            {
                using SqliteCommand add = connection.CreateCommand();
                add.Transaction = transaction;
                add.CommandText = $"ALTER TABLE TargetLandingCaseTable ADD COLUMN {column}";
                add.ExecuteNonQuery();
            }

            using SqliteCommand backfill = connection.CreateCommand();
            backfill.Transaction = transaction;
            backfill.CommandText = """
                UPDATE TargetLandingCaseTable SET
                    Name=json_extract(TargetLandingCase,'$.Name'),
                    Description=json_extract(TargetLandingCase,'$.Description'),
                    SourceTrajectoryRevision=json_extract(TargetLandingCase,'$.SourceTrajectoryRevision'),
                    CalculationFingerprint=json_extract(TargetLandingCase,'$.CalculationFingerprint'),
                    TargetLandingCaseEditData=json_remove(TargetLandingCase,'$.SampleList','$.MeshTriangleList')
                """;
            backfill.ExecuteNonQuery();
        }

        private static void AddTrajectoryLightColumns(SqliteConnection connection, SqliteTransaction transaction)
        {
            foreach (string column in new[] { "Name text", "Description text" })
            {
                using SqliteCommand add = connection.CreateCommand();
                add.Transaction = transaction;
                add.CommandText = $"ALTER TABLE TrajectoryTable ADD COLUMN {column}";
                add.ExecuteNonQuery();
            }

            using SqliteCommand backfill = connection.CreateCommand();
            backfill.Transaction = transaction;
            backfill.CommandText = """
                UPDATE TrajectoryTable SET
                    Name=json_extract(Trajectory,'$.Name'),
                    Description=json_extract(Trajectory,'$.Description')
                """;
            backfill.ExecuteNonQuery();
        }
    }
}
