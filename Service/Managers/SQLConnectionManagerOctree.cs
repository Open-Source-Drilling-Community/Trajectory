using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace OSDC.Drilling.Trajectory.Service.Managers;

/// <summary>
/// Owns the derived global anti-collision octree database. Version-1 databases are migrated
/// transactionally from the legacy per-bucket flags layout while preserving every cached code.
/// </summary>
public class SqlConnectionManagerOctree : SqlConnectionManager
{
    public const int OctreeDepthCache = 21;
    public const int OctreeSchemaVersion = 2;

    private const string DatabaseName = "GlobalAntiCollision.db";
    private const string LegacyCacheTableName = "GlobalOctreeCache";
    private const string LegacyWellboresTableName = "GlobalOctreeWellbores";

    internal const string CacheTableName = "GlobalOctreeBucketMembership";
    internal const string TrajectoryStateTableName = "GlobalOctreeTrajectoryState";
    internal const string CacheIndexName = "GlobalOctreeBucketMembershipIndex";
    internal const string CacheTrajectoryIndexName = "GlobalOctreeBucketMembershipTrajectoryIndex";
    internal const string StateFilterIndexName = "GlobalOctreeTrajectoryStateFilterIndex";

    private static readonly IReadOnlyDictionary<string, string[]> TableStructure =
        new Dictionary<string, string[]>
        {
            {
                CacheTableName,
                [
                    "OctreeCodeCacheDepth INTEGER",
                    "OctreeCodeCacheHigh BIGINT",
                    "OctreeCodeCacheLow BIGINT",
                    "TrajectoryID TEXT",
                    "OctreeCodeCount INTEGER",
                    "OctreeCodes BLOB"
                ]
            },
            {
                TrajectoryStateTableName,
                [
                    "TrajectoryID TEXT PRIMARY KEY",
                    "TrajectoryType TEXT",
                    "IsDefinitive INTEGER",
                    "SourceLastModificationDate TEXT",
                    "IndexSchemaVersion INTEGER",
                    "ConfidenceFactor REAL",
                    "CalculationParametersHash TEXT"
                ]
            }
        };

    private static readonly IReadOnlyDictionary<string, string[]> IndexDefinitions =
        new Dictionary<string, string[]>
        {
            {
                CacheTableName,
                [
                    $"CREATE UNIQUE INDEX {CacheIndexName} ON {CacheTableName} (OctreeCodeCacheDepth, OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID)",
                    $"CREATE INDEX {CacheTrajectoryIndexName} ON {CacheTableName} (TrajectoryID)"
                ]
            },
            {
                TrajectoryStateTableName,
                [$"CREATE INDEX {StateFilterIndexName} ON {TrajectoryStateTableName} (TrajectoryType, IsDefinitive, TrajectoryID)"]
            }
        };

    private static readonly string[] LegacyCacheColumns =
    [
        "OctreeCodeCacheHigh", "OctreeCodeCacheLow", "TrajectoryID", "IsPlanned", "IsMeasured",
        "IsDefinitive", "OctreeCodeCount", "OctreeCodes"
    ];

    private static readonly string[] LegacyStateColumns =
        ["TrajectoryID", "IsPlanned", "IsMeasured", "IsDefinitive"];

    public SqlConnectionManagerOctree(ILogger<SqlConnectionManagerOctree> logger)
        : this(BuildDatabasePath(DatabaseName), logger)
    {
    }

    public SqlConnectionManagerOctree(string databasePath, ILogger<SqlConnectionManagerOctree> logger)
        : base(BuildConnectionString(PrepareDatabase(databasePath, logger)), logger, databasePath, DatabaseName,
            TableStructure, IndexDefinitions, OctreeSchemaVersion)
    {
    }

    private static string PrepareDatabase(string databasePath, ILogger logger)
    {
        if (!File.Exists(databasePath)) return databasePath;

        using SqliteConnection connection = new(BuildConnectionString(databasePath));
        connection.Open();
        int version = ReadSchemaVersion(connection);
        if (version >= OctreeSchemaVersion) return databasePath;

        List<string> tables = ReadTableNames(connection);
        bool legacyShape = tables.Order().SequenceEqual(
            new[] { LegacyCacheTableName, LegacyWellboresTableName }.Order(), StringComparer.Ordinal) &&
            HasExactColumns(connection, LegacyCacheTableName, LegacyCacheColumns) &&
            HasExactColumns(connection, LegacyWellboresTableName, LegacyStateColumns);
        bool currentShape = tables.Order().SequenceEqual(TableStructure.Keys.Order(), StringComparer.Ordinal) &&
            TableStructure.All(table => HasExactColumns(connection, table.Key,
                table.Value.Select(ColumnName).ToArray()));

        if (!legacyShape && !currentShape)
        {
            return databasePath; // The base validator fails closed without changing this database.
        }

        if (currentShape)
        {
            using SqliteCommand versionCommand = connection.CreateCommand();
            versionCommand.CommandText = $"PRAGMA user_version={OctreeSchemaVersion}";
            versionCommand.ExecuteNonQuery();
            return databasePath;
        }

        string backupPath = CreateVerifiedBackup(connection, databasePath);
        List<LegacyMembership> legacyMemberships = ReadLegacyMemberships(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            CreateCurrentTables(connection, transaction);

            using (SqliteCommand copyState = connection.CreateCommand())
            {
                copyState.Transaction = transaction;
                copyState.CommandText = $"""
                    INSERT OR IGNORE INTO {TrajectoryStateTableName}
                        (TrajectoryID, TrajectoryType, IsDefinitive, SourceLastModificationDate,
                         IndexSchemaVersion, ConfidenceFactor, CalculationParametersHash)
                    SELECT TrajectoryID,
                           CASE WHEN IsPlanned <> 0 THEN 'Planned' ELSE 'Actual' END,
                           IsDefinitive,
                           NULL,
                           1,
                           0.999,
                           'legacy-v1-depth23-cache21-confidence0.999-scale1'
                    FROM {LegacyWellboresTableName}
                    """;
                copyState.ExecuteNonQuery();

                // Preserve cache-only trajectories as well if a legacy database was partially written.
                copyState.CommandText = $"""
                    INSERT OR IGNORE INTO {TrajectoryStateTableName}
                        (TrajectoryID, TrajectoryType, IsDefinitive, SourceLastModificationDate,
                         IndexSchemaVersion, ConfidenceFactor, CalculationParametersHash)
                    SELECT DISTINCT TrajectoryID,
                           CASE WHEN IsPlanned <> 0 THEN 'Planned' ELSE 'Actual' END,
                           IsDefinitive,
                           NULL,
                           1,
                           0.999,
                           'legacy-v1-depth23-cache21-confidence0.999-scale1'
                    FROM {LegacyCacheTableName}
                    """;
                copyState.ExecuteNonQuery();
            }

            using (SqliteCommand copyMemberships = connection.CreateCommand())
            {
                copyMemberships.Transaction = transaction;
                copyMemberships.CommandText = $"""
                    INSERT INTO {CacheTableName}
                        (OctreeCodeCacheDepth, OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID, OctreeCodeCount, OctreeCodes)
                    VALUES (@depth, @high, @low, @trajectoryId, @count, @codes)
                    """;
                foreach (LegacyMembership membership in legacyMemberships)
                {
                    copyMemberships.Parameters.Clear();
                    copyMemberships.Parameters.AddWithValue("@depth", membership.CacheDepth);
                    copyMemberships.Parameters.AddWithValue("@high", membership.CacheHigh);
                    copyMemberships.Parameters.AddWithValue("@low", membership.CacheLow);
                    copyMemberships.Parameters.AddWithValue("@trajectoryId", membership.TrajectoryId);
                    copyMemberships.Parameters.AddWithValue("@count", membership.CodeCount);
                    copyMemberships.Parameters.Add("@codes", SqliteType.Blob).Value = membership.Codes;
                    copyMemberships.ExecuteNonQuery();
                }
            }

            long legacyMembershipCount = Scalar(connection, transaction, $"SELECT COUNT(*) FROM {LegacyCacheTableName}");
            long migratedMembershipCount = Scalar(connection, transaction, $"SELECT COUNT(*) FROM {CacheTableName}");
            long orphanCount = Scalar(connection, transaction, $"""
                SELECT COUNT(*) FROM {CacheTableName} membership
                LEFT JOIN {TrajectoryStateTableName} state ON state.TrajectoryID = membership.TrajectoryID
                WHERE state.TrajectoryID IS NULL
                """);
            if (legacyMembershipCount != migratedMembershipCount || orphanCount != 0)
            {
                throw new InvalidOperationException("Octree migration verification failed. The transaction will be rolled back.");
            }

            Execute(connection, transaction, $"DROP TABLE {LegacyCacheTableName}");
            Execute(connection, transaction, $"DROP TABLE {LegacyWellboresTableName}");
            Execute(connection, transaction, $"PRAGMA user_version={OctreeSchemaVersion}");
            transaction.Commit();
            logger.LogInformation(
                "Migrated GlobalAntiCollision.db to schema version {SchemaVersion}; preserved {MembershipCount} bucket memberships. Verified backup: {BackupPath}",
                OctreeSchemaVersion, migratedMembershipCount, backupPath);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return databasePath;
    }

    private static void CreateCurrentTables(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach ((string tableName, string[] columns) in TableStructure)
        {
            Execute(connection, transaction, $"CREATE TABLE {tableName} ({string.Join(',', columns)})");
            foreach (string index in IndexDefinitions.GetValueOrDefault(tableName) ?? [])
            {
                Execute(connection, transaction, index);
            }
        }
    }

    private static string CreateVerifiedBackup(SqliteConnection source, string databasePath)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(databasePath))!;
        string backupPath = Path.Combine(directory,
            $"GlobalAntiCollision.schema-v1-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.bak");
        using (SqliteConnection backup = new(BuildConnectionString(backupPath)))
        {
            backup.Open();
            source.BackupDatabase(backup);
        }

        using SqliteConnection verification = new(BuildConnectionString(backupPath));
        verification.Open();
        using SqliteCommand integrity = verification.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check";
        if (!string.Equals(integrity.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The pre-migration octree backup failed its integrity check. No migration was attempted.");
        }
        return backupPath;
    }

    private static List<LegacyMembership> ReadLegacyMemberships(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID, OctreeCodeCount, OctreeCodes FROM {LegacyCacheTableName}";
        using SqliteDataReader reader = command.ExecuteReader();
        List<LegacyMembership> result = [];
        while (reader.Read())
        {
            byte[] codes = reader.IsDBNull(4) ? [] : (byte[])reader[4];
            int firstCodeDepth = codes.Length >= 17 ? codes[0] : OctreeDepthCache;
            result.Add(new LegacyMembership(
                Math.Min(firstCodeDepth, OctreeDepthCache),
                reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetInt32(3), codes));
        }
        return result;
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

    private static bool HasExactColumns(SqliteConnection connection, string table, IReadOnlyList<string> expected)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> actual = [];
        while (reader.Read()) actual.Add(reader.GetString(1));
        return actual.SequenceEqual(expected, StringComparer.Ordinal);
    }

    private static string ColumnName(string definition) =>
        definition.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    private static long Scalar(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private sealed record LegacyMembership(int CacheDepth, long CacheHigh, long CacheLow,
        string TrajectoryId, int CodeCount, byte[] Codes);
}
