using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class TrajectoryBatchImportManager
    {
        private static TrajectoryBatchImportManager? _instance = null;
        private readonly ILogger<TrajectoryBatchImportManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private TrajectoryBatchImportManager(ILogger<TrajectoryBatchImportManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static TrajectoryBatchImportManager GetInstance(ILogger<TrajectoryBatchImportManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new TrajectoryBatchImportManager(logger, connectionManager);
            return _instance;
        }

        private static TrajectoryBatchImportLight CreateDataLightInstance(TrajectoryBatchImport batchImport)
        {
            return new TrajectoryBatchImportLight()
            {
                MetaInfo = batchImport.MetaInfo,
                Name = batchImport.Name,
                Description = batchImport.Description,
                CreationDate = batchImport.CreationDate,
                LastModificationDate = batchImport.LastModificationDate
            };
        }

        public List<Guid>? GetAllTrajectoryBatchImportId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID FROM TrajectoryBatchImportTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        ids.Add(reader.GetGuid(0));
                    }
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from TrajectoryBatchImportTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        public List<MetaInfo?>? GetAllTrajectoryBatchImportMetaInfo()
        {
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo FROM TrajectoryBatchImportTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        metaInfos.Add(JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options));
                    }
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get MetaInfo from TrajectoryBatchImportTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        public TrajectoryBatchImport? GetTrajectoryBatchImportById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT TrajectoryBatchImport FROM TrajectoryBatchImportTable WHERE ID = '{id}'";
                try
                {
                    using var reader = command.ExecuteReader();
                    if (reader.Read() && !reader.IsDBNull(0))
                    {
                        var batchImport = JsonSerializer.Deserialize<TrajectoryBatchImport>(reader.GetString(0), JsonSettings.Options);
                        if (batchImport != null && batchImport.MetaInfo != null && batchImport.MetaInfo.ID != id)
                        {
                            throw new SqliteException("SQLite database corrupted: returned TrajectoryBatchImport has the wrong ID.", 1);
                        }
                        return batchImport;
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get the TrajectoryBatchImport with the given ID from TrajectoryBatchImportTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        public List<TrajectoryBatchImport>? GetAllTrajectoryBatchImport()
        {
            List<TrajectoryBatchImport> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT TrajectoryBatchImport FROM TrajectoryBatchImportTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        TrajectoryBatchImport? batchImport = JsonSerializer.Deserialize<TrajectoryBatchImport>(reader.GetString(0), JsonSettings.Options);
                        if (batchImport != null)
                        {
                            values.Add(batchImport);
                        }
                    }
                    return values;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get TrajectoryBatchImport from TrajectoryBatchImportTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        public List<TrajectoryBatchImportLight>? GetAllTrajectoryBatchImportLight()
        {
            List<TrajectoryBatchImportLight> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT TrajectoryBatchImport FROM TrajectoryBatchImportTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        TrajectoryBatchImport? batchImport = JsonSerializer.Deserialize<TrajectoryBatchImport>(reader.GetString(0), JsonSettings.Options);
                        if (batchImport != null)
                        {
                            values.Add(CreateDataLightInstance(batchImport));
                        }
                    }
                    return values;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get TrajectoryBatchImportLight from TrajectoryBatchImportTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        public bool AddTrajectoryBatchImport(TrajectoryBatchImport? batchImport)
        {
            if (batchImport == null || batchImport.MetaInfo == null || batchImport.MetaInfo.ID == Guid.Empty)
            {
                _logger.LogWarning("The TrajectoryBatchImport or its ID is null or empty");
                return false;
            }
            if (GetTrajectoryBatchImportById(batchImport.MetaInfo.ID) != null)
            {
                _logger.LogWarning("Impossible to post TrajectoryBatchImport. ID already found in database.");
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                using var transaction = connection.BeginTransaction();
                bool success = true;
                try
                {
                    string metaInfo = JsonSerializer.Serialize(batchImport.MetaInfo, JsonSettings.Options);
                    string? creationDate = batchImport.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                    string? lastModificationDate = batchImport.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                    string data = JsonSerializer.Serialize(batchImport, JsonSettings.Options);

                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO TrajectoryBatchImportTable (" +
                        "ID, " +
                        "MetaInfo, " +
                        "CreationDate, " +
                        "LastModificationDate, " +
                        "TrajectoryBatchImport" +
                        ") VALUES (" +
                        $"'{batchImport.MetaInfo.ID}', " +
                        $"'{metaInfo}', " +
                        $"'{creationDate}', " +
                        $"'{lastModificationDate}', " +
                        $"'{data}'" +
                        ")";
                    success = command.ExecuteNonQuery() == 1;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to add the given TrajectoryBatchImport into TrajectoryBatchImportTable");
                    success = false;
                }

                if (success)
                {
                    transaction.Commit();
                    return true;
                }

                transaction.Rollback();
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return false;
        }

        public bool UpdateTrajectoryBatchImportById(Guid id, TrajectoryBatchImport? batchImport)
        {
            if (id == Guid.Empty || batchImport == null || batchImport.MetaInfo == null || batchImport.MetaInfo.ID != id)
            {
                _logger.LogWarning("The TrajectoryBatchImport or its ID is null, empty, or inconsistent");
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                using var transaction = connection.BeginTransaction();
                bool success = true;
                try
                {
                    string metaInfo = JsonSerializer.Serialize(batchImport.MetaInfo, JsonSettings.Options);
                    string? creationDate = batchImport.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                    batchImport.LastModificationDate = DateTimeOffset.UtcNow;
                    string? lastModificationDate = batchImport.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                    string data = JsonSerializer.Serialize(batchImport, JsonSettings.Options);

                    var command = connection.CreateCommand();
                    command.CommandText = $"UPDATE TrajectoryBatchImportTable SET " +
                        $"MetaInfo = '{metaInfo}', " +
                        $"CreationDate = '{creationDate}', " +
                        $"LastModificationDate = '{lastModificationDate}', " +
                        $"TrajectoryBatchImport = '{data}' " +
                        $"WHERE ID = '{id}'";
                    success = command.ExecuteNonQuery() == 1;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to update the TrajectoryBatchImport");
                    success = false;
                }

                if (success)
                {
                    transaction.Commit();
                    return true;
                }

                transaction.Rollback();
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return false;
        }

        public bool DeleteTrajectoryBatchImportById(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("The TrajectoryBatchImport ID is null or empty");
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                using var transaction = connection.BeginTransaction();
                bool success = true;
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $"DELETE FROM TrajectoryBatchImportTable WHERE ID = '{id}'";
                    success = command.ExecuteNonQuery() == 1;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to delete the TrajectoryBatchImport of given ID from TrajectoryBatchImportTable");
                    success = false;
                }

                if (success)
                {
                    transaction.Commit();
                    return true;
                }

                transaction.Rollback();
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return false;
        }
    }
}
