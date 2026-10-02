using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Managers
{
    public class SurveyStationEllipseCalculationManager
    {
        private static SurveyStationEllipseCalculationManager? _instance;
        private readonly ILogger<SurveyStationEllipseCalculationManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private readonly SurveyRunManager _surveyRunManager;
        private readonly TrajectoryManager _trajectoryManager;

        private SurveyStationEllipseCalculationManager(ILogger<SurveyStationEllipseCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _surveyRunManager = SurveyRunManager.GetInstance(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SurveyRunManager>.Instance,
                connectionManager);
            _trajectoryManager = TrajectoryManager.GetInstance(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance,
                connectionManager);
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

        public Task<SurveyStationEllipseCalculation?> AddSurveyStationEllipseCalculationAsync(
            SurveyStationEllipseCalculation? calculation) =>
            AddSurveyStationEllipseCalculationAsync(calculation, null, null);

        public async Task<SurveyStationEllipseCalculation?> AddSurveyStationEllipseCalculationAsync(
            SurveyStationEllipseCalculation? calculation,
            Guid? sourceSurveyRunId,
            Guid? sourceTrajectoryId)
        {
            try
            {
                if (calculation == null)
                {
                    _logger.LogWarning("The SurveyStationEllipseCalculation is null");
                    return null;
                }

                if (sourceSurveyRunId is Guid surveyRunId && surveyRunId != Guid.Empty &&
                    sourceTrajectoryId is Guid trajectoryId && trajectoryId != Guid.Empty)
                {
                    calculation.SetCalculationMessage("Specify either a source SurveyRun or a source Trajectory, not both.");
                    return null;
                }

                if (!await ApplyAuthoritativeSourceUncertaintyAsync(
                        calculation,
                        sourceSurveyRunId,
                        sourceTrajectoryId))
                {
                    return null;
                }

                calculation.MetaInfo ??= new MetaInfo();
                if (calculation.MetaInfo.ID == Guid.Empty)
                {
                    calculation.MetaInfo.ID = Guid.NewGuid();
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                calculation.CreationDate = now;
                calculation.LastModificationDate = now;
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

        private async Task<bool> ApplyAuthoritativeSourceUncertaintyAsync(
            SurveyStationEllipseCalculation calculation,
            Guid? sourceSurveyRunId,
            Guid? sourceTrajectoryId)
        {
            if (sourceSurveyRunId is Guid surveyRunId && surveyRunId != Guid.Empty)
            {
                SurveyRun? source = await _surveyRunManager.GetSurveyRunWithRecalculatedUncertaintyAsync(surveyRunId);
                if (source?.SurveyStationList is not { Count: > 0 } stations)
                {
                    calculation.SetCalculationMessage("The source SurveyRun uncertainty lineage could not be reconstructed.");
                    return false;
                }

                return ApplyAuthoritativeUncertainty(
                    calculation,
                    stations,
                    source.CalculationType,
                    "SurveyRun");
            }

            if (sourceTrajectoryId is Guid trajectoryId && trajectoryId != Guid.Empty)
            {
                Model.Trajectory? source = _trajectoryManager.GetTrajectoryById(trajectoryId);
                source = await _trajectoryManager.CalculateTrajectoryAsync(
                    source,
                    recalculateSurveyRunUncertainty: true);
                if (source?.SurveyStationList is not { Count: > 0 } stations)
                {
                    calculation.SetCalculationMessage("The source Trajectory uncertainty lineage could not be reconstructed.");
                    return false;
                }

                return ApplyAuthoritativeUncertainty(
                    calculation,
                    stations,
                    source.CalculationType,
                    "Trajectory");
            }

            return true;
        }

        internal static bool ApplyAuthoritativeUncertainty(
            SurveyStationEllipseCalculation calculation,
            IReadOnlyCollection<SurveyStation> authoritativeStations,
            TrajectoryCalculationType calculationType,
            string sourceName)
        {
            List<SurveyStation> source = authoritativeStations
                .Where(station => (station.MD ?? station.Abscissa) is double md &&
                    OSDC.DotnetLibraries.General.Common.Numeric.IsDefined(md))
                .OrderBy(station => station.MD ?? station.Abscissa)
                .ToList();
            if (source.Count == 0)
            {
                calculation.SetCalculationMessage($"The source {sourceName} has no usable survey stations.");
                return false;
            }

            if (calculation.SurveyStationList is not { Count: > 0 } requestedStations)
            {
                calculation.SurveyStationList = source.Select(station => new SurveyStation(station)).ToList();
                return true;
            }

            foreach (SurveyStation requested in requestedStations)
            {
                if ((requested.MD ?? requested.Abscissa) is not double requestedMd ||
                    !OSDC.DotnetLibraries.General.Common.Numeric.IsDefined(requestedMd))
                {
                    calculation.SetCalculationMessage("Every requested survey station must define a finite measured depth.");
                    return false;
                }

                SurveyStation? authoritative = source.FirstOrDefault(station =>
                    OSDC.DotnetLibraries.General.Common.Numeric.EQ(
                        (station.MD ?? station.Abscissa)!.Value,
                        requestedMd));
                if (authoritative == null &&
                    (!SurveyStation.InterpolateAtAbscissa(source, requestedMd, out authoritative, calculationType) ||
                     authoritative == null))
                {
                    calculation.SetCalculationMessage(
                        $"Requested measured depth {requestedMd} is outside the source {sourceName}.");
                    return false;
                }

                requested.SurveyTool = authoritative.SurveyTool;
                requested.Covariance = authoritative.Covariance;
                requested.Bias = authoritative.Bias;
                requested.EigenValues = authoritative.EigenValues;
                requested.EigenVectors = authoritative.EigenVectors;
            }

            return true;
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

            OSDC.Drilling.Trajectory.ModelShared.SurveyInstrument? surveyInstrument;
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

        private static OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument ConvertSurveyInstrument(OSDC.Drilling.Trajectory.ModelShared.SurveyInstrument surveyInstrument)
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
