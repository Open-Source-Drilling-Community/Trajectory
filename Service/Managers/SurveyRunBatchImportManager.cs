using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class SurveyRunBatchImportManager
    {
        private static SurveyRunBatchImportManager? _instance;
        private readonly ILogger<SurveyRunBatchImportManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private SurveyRunBatchImportManager(ILogger<SurveyRunBatchImportManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static SurveyRunBatchImportManager GetInstance(ILogger<SurveyRunBatchImportManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new SurveyRunBatchImportManager(logger, connectionManager);
            return _instance;
        }

        private static SurveyRunBatchImportLight CreateDataLightInstance(SurveyRunBatchImport batchImport)
        {
            return new SurveyRunBatchImportLight
            {
                MetaInfo = batchImport.MetaInfo,
                Name = batchImport.Name,
                Description = batchImport.Description,
                CreationDate = batchImport.CreationDate,
                LastModificationDate = batchImport.LastModificationDate
            };
        }

        public List<Guid>? GetAllSurveyRunBatchImportId()
        {
            List<Guid> ids = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
                return null;

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM SurveyRunBatchImportTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                    ids.Add(Guid.Parse(reader.GetString(0)));
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from SurveyRunBatchImportTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllSurveyRunBatchImportMetaInfo()
        {
            List<MetaInfo?> metaInfos = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
                return null;

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM SurveyRunBatchImportTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                    metaInfos.Add(JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options));
                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get MetaInfo from SurveyRunBatchImportTable");
                return null;
            }
        }

        public SurveyRunBatchImport? GetSurveyRunBatchImportById(Guid id)
        {
            if (id == Guid.Empty)
                return null;

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
                return null;

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRunBatchImport FROM SurveyRunBatchImportTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRunBatchImport? batchImport = JsonSerializer.Deserialize<SurveyRunBatchImport>(reader.GetString(0), JsonSettings.Options);
                    if (batchImport?.MetaInfo?.ID != id)
                        throw new SqliteException("SQLite database corrupted: returned SurveyRunBatchImport has the wrong ID.", 1);
                    return batchImport;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get the SurveyRunBatchImport with the given ID from SurveyRunBatchImportTable");
            }
            return null;
        }

        public List<SurveyRunBatchImport>? GetAllSurveyRunBatchImport()
        {
            List<SurveyRunBatchImport> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
                return null;

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRunBatchImport FROM SurveyRunBatchImportTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRunBatchImport? batchImport = JsonSerializer.Deserialize<SurveyRunBatchImport>(reader.GetString(0), JsonSettings.Options);
                    if (batchImport != null)
                        values.Add(batchImport);
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SurveyRunBatchImport from SurveyRunBatchImportTable");
                return null;
            }
        }

        public List<SurveyRunBatchImportLight>? GetAllSurveyRunBatchImportLight()
        {
            List<SurveyRunBatchImportLight> values = [];
            foreach (SurveyRunBatchImport batchImport in GetAllSurveyRunBatchImport() ?? [])
                values.Add(CreateDataLightInstance(batchImport));
            return values;
        }

        public bool AddSurveyRunBatchImport(SurveyRunBatchImport? batchImport)
        {
            if (batchImport?.MetaInfo?.ID is not Guid id || id == Guid.Empty)
                return false;
            if (GetSurveyRunBatchImportById(id) != null)
                return false;
            return InsertOrUpdate(batchImport, false);
        }

        public bool UpdateSurveyRunBatchImportById(Guid id, SurveyRunBatchImport? batchImport)
        {
            if (id == Guid.Empty || batchImport?.MetaInfo?.ID != id)
                return false;
            batchImport.LastModificationDate = DateTimeOffset.UtcNow;
            return InsertOrUpdate(batchImport, true);
        }

        public bool DeleteSurveyRunBatchImportById(Guid id)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (id == Guid.Empty || connection == null)
                return false;

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM SurveyRunBatchImportTable WHERE ID = @id";
                command.Parameters.AddWithValue("@id", id.ToString());
                bool success = command.ExecuteNonQuery() == 1;
                if (success)
                    transaction.Commit();
                else
                    transaction.Rollback();
                return success;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete the SurveyRunBatchImport of given ID");
                return false;
            }
        }

        private bool InsertOrUpdate(SurveyRunBatchImport batchImport, bool update)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
                return false;

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                string metaInfo = JsonSerializer.Serialize(batchImport.MetaInfo, JsonSettings.Options);
                string? creationDate = batchImport.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = batchImport.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string data = JsonSerializer.Serialize(batchImport, JsonSettings.Options);
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = update
                    ? "UPDATE SurveyRunBatchImportTable SET MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, SurveyRunBatchImport = @data WHERE ID = @id"
                    : "INSERT INTO SurveyRunBatchImportTable (ID, MetaInfo, CreationDate, LastModificationDate, SurveyRunBatchImport) VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @data)";
                command.Parameters.AddWithValue("@id", batchImport.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@data", data);
                bool success = command.ExecuteNonQuery() == 1;
                if (success)
                    transaction.Commit();
                else
                    transaction.Rollback();
                return success;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to save SurveyRunBatchImport");
                return false;
            }
        }
    }
}
