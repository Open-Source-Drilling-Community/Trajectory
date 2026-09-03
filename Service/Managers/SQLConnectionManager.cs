using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace OSDC.Drilling.Trajectory.Service.Managers;

/// <summary>
/// Owns the SQLite connection configuration and validates a database before any manager uses it.
/// Existing tables and rows are never dropped, renamed, or rebuilt automatically.
/// </summary>
public abstract class SqlConnectionManager
{
    private readonly ILogger _logger;
    private readonly string _connectionString;
    private readonly string _dbPath;

    protected string DatabaseFilename { get; }
    protected IReadOnlyDictionary<string, string[]> TableStructureDict { get; }
    protected IReadOnlyDictionary<string, string[]> TableIndexDefinitions { get; }

    public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;
    public const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
    public const int CURRENT_SCHEMA_VERSION = 1;

    protected SqlConnectionManager(
        ILogger logger,
        string databaseFilename,
        IReadOnlyDictionary<string, string[]> tableStructureDict)
        : this(logger, databaseFilename, tableStructureDict, null)
    {
    }

    protected SqlConnectionManager(
        ILogger logger,
        string databaseFilename,
        IReadOnlyDictionary<string, string[]> tableStructureDict,
        IReadOnlyDictionary<string, string[]>? tableIndexDefinitions)
        : this(
            BuildConnectionString(BuildDatabasePath(databaseFilename)),
            logger,
            BuildDatabasePath(databaseFilename),
            databaseFilename,
            tableStructureDict,
            tableIndexDefinitions)
    {
    }

    protected SqlConnectionManager(
        string connectionString,
        ILogger logger,
        string dbPath,
        string databaseFilename,
        IReadOnlyDictionary<string, string[]> tableStructureDict,
        IReadOnlyDictionary<string, string[]>? tableIndexDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFilename);
        ArgumentNullException.ThrowIfNull(tableStructureDict);

        _connectionString = connectionString;
        _logger = logger;
        _dbPath = dbPath;
        DatabaseFilename = databaseFilename;
        TableStructureDict = tableStructureDict;
        TableIndexDefinitions = tableIndexDefinitions ?? CreateDefaultIndexDefinitions(tableStructureDict);

        _logger.LogInformation("SQLite connection manager created. DB: {DbPath}", _dbPath);
        Initialize();
        ManageDatabase();
    }

    public SqliteConnection? GetConnection()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }

    protected static string BuildDatabasePath(string databaseFilename) => Path.Combine(HOME_DIRECTORY, databaseFilename);

    protected static string BuildConnectionString(string dbPath) => new SqliteConnectionStringBuilder
    {
        DataSource = dbPath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared
    }.ToString();

    protected static IReadOnlyDictionary<string, string[]> CreateDefaultIndexDefinitions(
        IReadOnlyDictionary<string, string[]> tableStructureDict)
    {
        Dictionary<string, string[]> result = [];
        foreach (KeyValuePair<string, string[]> table in tableStructureDict)
        {
            if (table.Value.Any(column => string.Equals(
                    ColumnName(column), "ID", StringComparison.OrdinalIgnoreCase)))
            {
                string tableName = QuoteIdentifier(table.Key);
                string indexName = QuoteIdentifier(table.Key + "Index");
                result[table.Key] = [$"CREATE UNIQUE INDEX {indexName} ON {tableName} ({QuoteIdentifier("ID")})"];
            }
        }
        return result;
    }

    private void Initialize()
    {
        try
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            _logger.LogInformation(File.Exists(_dbPath) ? "Opening database {DbPath}" : "Creating database {DbPath}", _dbPath);
            using SqliteConnection connection = new(_connectionString);
            connection.Open();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to initialize SQLite database at {DbPath}", _dbPath);
            throw new InvalidOperationException($"Unable to initialize SQLite database '{DatabaseFilename}'.", exception);
        }
    }

    private void ManageDatabase()
    {
        using SqliteConnection connection = GetConnection()
            ?? throw new InvalidOperationException($"Unable to open SQLite database '{DatabaseFilename}'.");

        List<string> tableNames = ReadTableNames(connection);
        int schemaVersion = ReadSchemaVersion(connection);
        if (schemaVersion > CURRENT_SCHEMA_VERSION)
        {
            throw new InvalidOperationException(
                $"{DatabaseFilename} schema version {schemaVersion} is newer than supported version {CURRENT_SCHEMA_VERSION}. No data was changed.");
        }

        if (tableNames.Count == 0)
        {
            if (schemaVersion != 0)
            {
                throw new InvalidOperationException(
                    $"The versioned database {DatabaseFilename} has no tables. No data was changed.");
            }

            CreateFreshDatabase(connection);
            return;
        }

        List<string> unexpected = tableNames.Except(TableStructureDict.Keys, StringComparer.Ordinal).Order().ToList();
        List<string> missing = TableStructureDict.Keys.Except(tableNames, StringComparer.Ordinal).Order().ToList();
        List<string> malformed = TableStructureDict
            .Where(table => tableNames.Contains(table.Key, StringComparer.Ordinal) && !HasExpectedColumns(connection, table))
            .Select(table => table.Key)
            .Order()
            .ToList();

        if (unexpected.Count != 0 || missing.Count != 0 || malformed.Count != 0)
        {
            throw new InvalidOperationException(
                $"Unexpected {DatabaseFilename} structure. No data was changed. " +
                $"Missing=[{string.Join(',', missing)}], unexpected=[{string.Join(',', unexpected)}], malformed=[{string.Join(',', malformed)}].");
        }

        if (schemaVersion < CURRENT_SCHEMA_VERSION)
        {
            using SqliteTransaction transaction = connection.BeginTransaction();
            SetSchemaVersion(connection, transaction);
            transaction.Commit();
            _logger.LogInformation("Adopted the existing {DatabaseFilename} schema as version {SchemaVersion} without rewriting rows.",
                DatabaseFilename, CURRENT_SCHEMA_VERSION);
        }
    }

    private void CreateFreshDatabase(SqliteConnection connection)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            foreach (KeyValuePair<string, string[]> table in TableStructureDict)
            {
                using SqliteCommand createTable = connection.CreateCommand();
                createTable.Transaction = transaction;
                createTable.CommandText = $"CREATE TABLE {QuoteIdentifier(table.Key)} ({string.Join(',', table.Value)})";
                createTable.ExecuteNonQuery();

                if (TableIndexDefinitions.TryGetValue(table.Key, out string[]? indexCommands))
                {
                    foreach (string indexCommand in indexCommands)
                    {
                        using SqliteCommand createIndex = connection.CreateCommand();
                        createIndex.Transaction = transaction;
                        createIndex.CommandText = indexCommand;
                        createIndex.ExecuteNonQuery();
                    }
                }
            }

            SetSchemaVersion(connection, transaction);
            transaction.Commit();
            _logger.LogInformation("Created {DatabaseFilename} schema version {SchemaVersion} transactionally.",
                DatabaseFilename, CURRENT_SCHEMA_VERSION);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static List<string> ReadTableNames(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> names = [];
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    private static int ReadSchemaVersion(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static bool HasExpectedColumns(SqliteConnection connection, KeyValuePair<string, string[]> table)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(table.Key)})";
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> actual = [];
        while (reader.Read()) actual.Add(reader.GetString(1));
        string[] expected = table.Value.Select(ColumnName).ToArray();
        return actual.SequenceEqual(expected, StringComparer.Ordinal);
    }

    private static void SetSchemaVersion(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA user_version={CURRENT_SCHEMA_VERSION}";
        command.ExecuteNonQuery();
    }

    private static string ColumnName(string definition) =>
        definition.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"")}\"";
}
