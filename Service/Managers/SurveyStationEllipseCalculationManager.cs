using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class SurveyStationEllipseCalculationManager
    {
        private static SurveyStationEllipseCalculationManager? _instance;
        private readonly ILogger<SurveyStationEllipseCalculationManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private SurveyStationEllipseCalculationManager(ILogger<SurveyStationEllipseCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static SurveyStationEllipseCalculationManager GetInstance(ILogger<SurveyStationEllipseCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new SurveyStationEllipseCalculationManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllSurveyStationEllipseCalculationId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM SurveyStationEllipseCalculationTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    ids.Add(Guid.Parse(reader.GetString(0)));
                }
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from SurveyStationEllipseCalculationTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllSurveyStationEllipseCalculationMetaInfo()
        {
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM SurveyStationEllipseCalculationTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from SurveyStationEllipseCalculationTable");
                return null;
            }
        }

        public SurveyStationEllipseCalculation? GetSurveyStationEllipseCalculationById(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("The given SurveyStationEllipseCalculation ID is null or empty");
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyStationEllipseCalculation FROM SurveyStationEllipseCalculationTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyStationEllipseCalculation? calculation = JsonSerializer.Deserialize<SurveyStationEllipseCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (calculation?.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned SurveyStationEllipseCalculation has the wrong ID.", 1);
                    }
                    return calculation;
                }
                return null;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get the SurveyStationEllipseCalculation with the given ID");
                return null;
            }
        }

        public async Task<SurveyStationEllipseCalculation?> AddSurveyStationEllipseCalculationAsync(SurveyStationEllipseCalculation? calculation)
        {
            try
            {
                if (calculation == null)
                {
                    _logger.LogWarning("The SurveyStationEllipseCalculation is null");
                    return null;
                }

                calculation.MetaInfo ??= new MetaInfo();
                if (calculation.MetaInfo.ID == Guid.Empty)
                {
                    calculation.MetaInfo.ID = Guid.NewGuid();
                }

                calculation.CreationDate ??= DateTimeOffset.UtcNow;
                calculation.LastModificationDate = DateTimeOffset.UtcNow;
                calculation.Name = string.IsNullOrWhiteSpace(calculation.Name) ? "Survey station ellipse calculation" : calculation.Name;
                calculation.Description ??= string.Empty;

                if (!await AttachSurveyInstrumentIfNeededAsync(calculation))
                {
                    _logger.LogWarning("Impossible to attach SurveyInstrument to SurveyStationEllipseCalculation");
                    return null;
                }

                if (!calculation.Calculate())
                {
                    _logger.LogWarning("Impossible to calculate the SurveyStationEllipseCalculation: {Message}", calculation.CalculationMessage);
                    return null;
                }

                var connection = _connectionManager.GetConnection();
                if (connection == null)
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                    return null;
                }

                using SqliteTransaction transaction = connection.BeginTransaction();
                try
                {
                    string metaInfo = JsonSerializer.Serialize(calculation.MetaInfo, JsonSettings.Options);
                    string? cDate = calculation.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                    string? lDate = calculation.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                    string data = JsonSerializer.Serialize(calculation, JsonSettings.Options);

                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = "INSERT INTO SurveyStationEllipseCalculationTable (" +
                        "ID, MetaInfo, CreationDate, LastModificationDate, ConfidenceFactor, SurveyStationEllipseCalculation" +
                        ") VALUES (" +
                        "@id, @metaInfo, @creationDate, @lastModificationDate, @confidenceFactor, @calculation)";
                    command.Parameters.AddWithValue("@id", calculation.MetaInfo.ID.ToString());
                    command.Parameters.AddWithValue("@metaInfo", metaInfo);
                    command.Parameters.AddWithValue("@creationDate", cDate ?? string.Empty);
                    command.Parameters.AddWithValue("@lastModificationDate", lDate ?? string.Empty);
                    command.Parameters.AddWithValue("@confidenceFactor", calculation.ConfidenceFactor);
                    command.Parameters.AddWithValue("@calculation", data);

                    if (command.ExecuteNonQuery() != 1)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    transaction.Commit();
                    return calculation;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Impossible to add the SurveyStationEllipseCalculation");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SurveyStationEllipseCalculation");
                return null;
            }
        }

        private async Task<bool> AttachSurveyInstrumentIfNeededAsync(SurveyStationEllipseCalculation calculation)
        {
            if (calculation.SurveyStationList is not { Count: > 0 } stations)
            {
                return true;
            }

            if (stations.Any(station => station.SurveyTool != null))
            {
                return true;
            }

            if (calculation.SurveyInstrumentID is not Guid surveyInstrumentId || surveyInstrumentId == Guid.Empty)
            {
                return true;
            }

            NORCE.Drilling.Trajectory.ModelShared.SurveyInstrument? surveyInstrument;
            try
            {
                surveyInstrument = await APIUtils.ClientSurveyInstrument.GetSurveyInstrumentByIdAsync(surveyInstrumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible to retrieve SurveyInstrument {SurveyInstrumentId} for ellipse calculation", surveyInstrumentId);
                return false;
            }

            if (surveyInstrument == null)
            {
                return false;
            }

            OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument surveyTool = ConvertSurveyInstrument(surveyInstrument);
            foreach (SurveyStation station in stations)
            {
                station.SurveyTool = surveyTool;
            }
            return true;
        }

        private static OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument ConvertSurveyInstrument(NORCE.Drilling.Trajectory.ModelShared.SurveyInstrument surveyInstrument)
        {
            string data = JsonSerializer.Serialize(surveyInstrument, JsonSettings.Options);
            return JsonSerializer.Deserialize<OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument>(data, JsonSettings.Options)
                ?? new OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument();
        }

        public bool DeleteSurveyStationEllipseCalculationById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM SurveyStationEllipseCalculationTable WHERE ID = @id";
                command.Parameters.AddWithValue("@id", id.ToString());
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete the SurveyStationEllipseCalculation");
                return false;
            }
        }
    }
}
