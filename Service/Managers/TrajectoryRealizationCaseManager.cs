using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class TrajectoryRealizationCaseManager
    {
        private static TrajectoryRealizationCaseManager? _instance;
        private readonly ILogger<TrajectoryRealizationCaseManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private readonly TrajectoryManager _trajectoryManager;

        private TrajectoryRealizationCaseManager(ILogger<TrajectoryRealizationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _trajectoryManager = TrajectoryManager.GetInstance(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance,
                connectionManager);
        }

        public static TrajectoryRealizationCaseManager GetInstance(ILogger<TrajectoryRealizationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new TrajectoryRealizationCaseManager(logger, connectionManager);
            return _instance;
        }

        private static TrajectoryRealizationCaseLight CreateLight(TrajectoryRealizationCase value) => new()
        {
            MetaInfo = value.MetaInfo,
            Name = value.Name,
            Description = value.Description,
            CreationDate = value.CreationDate,
            LastModificationDate = value.LastModificationDate,
            TrajectoryID = value.TrajectoryID,
            RealizationCount = value.RealizationCount,
            CoarseningMaximumDistance = value.CoarseningMaximumDistance,
            RandomSeed = value.RandomSeed,
            ReferenceStationCount = value.ReferenceStationCount,
            CoarsenedStationCount = value.CoarsenedStationCount,
            CalculationState = value.CalculationState,
            CalculationProgress = value.CalculationProgress,
            CalculationMessage = value.CalculationMessage
        };

        public List<Guid>? GetAllTrajectoryRealizationCaseId()
        {
            List<Guid> ids = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM TrajectoryRealizationCaseTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    ids.Add(reader.GetGuid(0));
                }
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from TrajectoryRealizationCaseTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllTrajectoryRealizationCaseMetaInfo()
        {
            List<MetaInfo?> metaInfos = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM TrajectoryRealizationCaseTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    metaInfos.Add(JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options));
                }
                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get MetaInfo from TrajectoryRealizationCaseTable");
                return null;
            }
        }

        public TrajectoryRealizationCase? GetTrajectoryRealizationCaseById(Guid id, bool includeRealizations = false)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("The given TrajectoryRealizationCase ID is null or empty");
                return null;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryRealizationCase FROM TrajectoryRealizationCaseTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryRealizationCase? value = JsonSerializer.Deserialize<TrajectoryRealizationCase>(reader.GetString(0), JsonSettings.Options);
                    if (value?.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned TrajectoryRealizationCase has the wrong ID.", 1);
                    }
                    if (includeRealizations)
                    {
                        value.RealizationList ??= TrajectoryRealizationChunkStore.GetRealizations(_logger, _connectionManager, id);
                    }
                    return value;
                }
                return null;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get TrajectoryRealizationCase by ID");
                return null;
            }
        }

        public List<TrajectoryRealizationCaseLight>? GetAllTrajectoryRealizationCaseLight()
        {
            List<TrajectoryRealizationCaseLight> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryRealizationCase FROM TrajectoryRealizationCaseTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryRealizationCase? value = JsonSerializer.Deserialize<TrajectoryRealizationCase>(reader.GetString(0), JsonSettings.Options);
                    if (value != null)
                    {
                        values.Add(CreateLight(value));
                    }
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get TrajectoryRealizationCase light data");
                return null;
            }
        }

        public List<TrajectoryRealizationCase?>? GetAllTrajectoryRealizationCase()
        {
            List<TrajectoryRealizationCase?> values = [];
            foreach (TrajectoryRealizationCaseLight light in GetAllTrajectoryRealizationCaseLight() ?? [])
            {
                if (light.MetaInfo?.ID is Guid id)
                {
                    values.Add(GetTrajectoryRealizationCaseById(id, includeRealizations: true));
                }
            }
            return values;
        }

        public Task<bool> AddTrajectoryRealizationCase(TrajectoryRealizationCase? value)
        {
            try
            {
                if (!IsValidForSave(value))
                {
                    return Task.FromResult(false);
                }
                if (GetTrajectoryRealizationCaseById(value!.MetaInfo!.ID) != null)
                {
                    _logger.LogWarning("Impossible to post TrajectoryRealizationCase. ID already found in database.");
                    return Task.FromResult(false);
                }

                MarkCalculationState(value, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateTrajectoryRealizationCase(value, false, null);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateTrajectoryRealizationCaseAsync(value.MetaInfo.ID));
                }
                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding TrajectoryRealizationCase");
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateTrajectoryRealizationCaseById(Guid id, TrajectoryRealizationCase? value)
        {
            try
            {
                if (!IsValidForSave(value) || value!.MetaInfo!.ID != id)
                {
                    return Task.FromResult(false);
                }

                value.LastModificationDate = DateTimeOffset.UtcNow;
                MarkCalculationState(value, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateTrajectoryRealizationCase(value, true, null);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateTrajectoryRealizationCaseAsync(id));
                }
                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating TrajectoryRealizationCase");
                return Task.FromResult(false);
            }
        }

        public bool DeleteTrajectoryRealizationCaseById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                TrajectoryRealizationChunkStore.DeleteChunks(connection, transaction, id);
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM TrajectoryRealizationCaseTable WHERE ID = @id";
                command.Parameters.AddWithValue("@id", id.ToString());
                bool success = command.ExecuteNonQuery() >= 0;
                if (success)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
                return success;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete TrajectoryRealizationCase");
                return false;
            }
        }

        private async Task RecalculateTrajectoryRealizationCaseAsync(Guid id)
        {
            try
            {
                UpdateCalculationState(id, CalculationState.Running, 0.02, "Loading reference trajectory");
                TrajectoryRealizationCase? value = GetTrajectoryRealizationCaseById(id);
                if (value == null)
                {
                    return;
                }

                Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(value.TrajectoryID);
                if (trajectory == null)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Reference trajectory could not be found");
                    DeleteChunks(id);
                    return;
                }

                if (HasMissingCovariance(trajectory) &&
                    await _trajectoryManager.CalculateTrajectoryAsync(trajectory) is { } recalculatedTrajectory)
                {
                    trajectory = recalculatedTrajectory;
                }

                bool slotCovarianceApplied = await TryApplySlotCovarianceToFirstStationAsync(trajectory);
                if (!EnsureTrajectoryCovariance(trajectory, preserveFirstStationCovariance: slotCovarianceApplied))
                {
                    _logger.LogWarning("Could not calculate covariance matrices for the reference trajectory before trajectory realization generation");
                }

                bool success = await Task.Run(() => value.Calculate(trajectory, (progress, message) =>
                {
                    UpdateCalculationState(id, CalculationState.Running, 0.05 + 0.90 * progress, message);
                }));

                if (!success)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, value.CalculationMessage ?? "Trajectory realization calculation failed");
                    DeleteChunks(id);
                    return;
                }

                MarkCalculationState(value, CalculationState.Completed, 1.0, null);
                value.LastModificationDate = DateTimeOffset.UtcNow;
                if (!InsertOrUpdateTrajectoryRealizationCase(value, true, value.RealizationList))
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Trajectory realization calculation failed while saving");
                    DeleteChunks(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during background TrajectoryRealizationCase calculation");
                UpdateCalculationState(id, CalculationState.Failed, 0.0, "Trajectory realization calculation failed");
                DeleteChunks(id);
            }
        }

        private bool InsertOrUpdateTrajectoryRealizationCase(
            TrajectoryRealizationCase value,
            bool update,
            List<List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyPoint>>? realizations)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                string metaInfo = JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options);
                string? creationDate = value.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = value.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                value.RealizationList = null;
                string data = JsonSerializer.Serialize(value, JsonSettings.Options);

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = update
                    ? "UPDATE TrajectoryRealizationCaseTable SET MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, TrajectoryID = @trajectoryId, RealizationCount = @realizationCount, CoarseningMaximumDistance = @coarseningMaximumDistance, RandomSeed = @randomSeed, ReferenceStationCount = @referenceStationCount, CoarsenedStationCount = @coarsenedStationCount, CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, TrajectoryRealizationCase = @case WHERE ID = @id"
                    : "INSERT INTO TrajectoryRealizationCaseTable (ID, MetaInfo, CreationDate, LastModificationDate, TrajectoryID, RealizationCount, CoarseningMaximumDistance, RandomSeed, ReferenceStationCount, CoarsenedStationCount, CalculationState, CalculationProgress, CalculationMessage, TrajectoryRealizationCase) VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @trajectoryId, @realizationCount, @coarseningMaximumDistance, @randomSeed, @referenceStationCount, @coarsenedStationCount, @calculationState, @calculationProgress, @calculationMessage, @case)";

                command.Parameters.AddWithValue("@id", value.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@trajectoryId", value.TrajectoryID.ToString());
                command.Parameters.AddWithValue("@realizationCount", value.RealizationCount);
                command.Parameters.AddWithValue("@coarseningMaximumDistance", value.CoarseningMaximumDistance);
                command.Parameters.AddWithValue("@randomSeed", (object?)value.RandomSeed ?? DBNull.Value);
                command.Parameters.AddWithValue("@referenceStationCount", (object?)value.ReferenceStationCount ?? DBNull.Value);
                command.Parameters.AddWithValue("@coarsenedStationCount", (object?)value.CoarsenedStationCount ?? DBNull.Value);
                command.Parameters.AddWithValue("@calculationState", value.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", value.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)value.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@case", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success)
                {
                    success = TrajectoryRealizationChunkStore.ReplaceChunks(connection, transaction, value.MetaInfo.ID, realizations);
                }

                if (success)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
                return success;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to save TrajectoryRealizationCase");
                return false;
            }
        }

        private async Task<bool> TryApplySlotCovarianceToFirstStationAsync(Model.Trajectory trajectory)
        {
            if (trajectory.SurveyStationList is not { Count: > 1 } stations ||
                HasUsableCovariance(stations[0]))
            {
                return false;
            }

            try
            {
                (OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation? referencePoint, _, string message) =
                    await APIUtils.GetReferencePointAsync(trajectory);
                if (referencePoint?.Covariance == null)
                {
                    _logger.LogInformation("Slot covariance could not be applied to the first trajectory realization station: {Message}", message);
                    return false;
                }

                stations[0].Covariance = referencePoint.Covariance;
                stations[0].EigenVectors = null;
                stations[0].EigenValues = null;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Slot covariance could not be applied to the first trajectory realization station");
                return false;
            }
        }

        private bool EnsureTrajectoryCovariance(Model.Trajectory trajectory, bool preserveFirstStationCovariance)
        {
            if (trajectory.SurveyStationList is not { Count: > 1 } stations ||
                stations.All(HasUsableCovariance))
            {
                return true;
            }

            OSDC.DotnetLibraries.General.Math.SymmetricMatrix3x3? firstCovariance = preserveFirstStationCovariance
                ? stations[0].Covariance
                : null;

            try
            {
                OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument? surveyTool = stations
                    .Select(station => station.SurveyTool)
                    .FirstOrDefault(tool => tool != null);
                if (surveyTool == null)
                {
                    return false;
                }

                foreach (OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation station in stations)
                {
                    station.SurveyTool ??= surveyTool;
                }

                bool success = surveyTool.ModelType switch
                {
                    OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrumentModelType.MWD_WolffDeWardt or
                    OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrumentModelType.Gyro_WolffDeWardt =>
                        OSDC.DotnetLibraries.Drilling.Surveying.CovarianceCalculatorWolffDeWardt.Calculate(stations),

                    OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrumentModelType.MWD_ISCWSA or
                    OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrumentModelType.Gyro_ISCWSA =>
                        OSDC.DotnetLibraries.Drilling.Surveying.CovarianceCalculatorISCWSA.Calculate(stations),

                    _ => false
                };

                if (success && firstCovariance != null)
                {
                    stations[0].Covariance = firstCovariance;
                    stations[0].EigenVectors = null;
                    stations[0].EigenValues = null;
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to calculate reference trajectory covariance matrices");
                return false;
            }
        }

        private static bool HasMissingCovariance(Model.Trajectory trajectory) =>
            trajectory.SurveyStationList is not { Count: > 1 } stations ||
            stations.Any(station => !HasUsableCovariance(station));

        private static bool HasUsableCovariance(OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation station)
        {
            if (station.Covariance == null)
            {
                return false;
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (station.Covariance[row, col] is double value &&
                        !double.IsNaN(value) &&
                        !double.IsInfinity(value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsValidForSave(TrajectoryRealizationCase? value) =>
            value?.MetaInfo != null &&
            value.MetaInfo.ID != Guid.Empty &&
            value.TrajectoryID != Guid.Empty &&
            value.RealizationCount > 0 &&
            value.RealizationCount <= TrajectoryRealizationCase.MaximumRealizationCount;

        private static void MarkCalculationState(TrajectoryRealizationCase value, CalculationState state, double progress, string? message)
        {
            value.CalculationState = state;
            value.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            value.CalculationMessage = message;
        }

        private bool UpdateCalculationState(Guid id, CalculationState state, double progress, string? message)
        {
            TrajectoryRealizationCase? value = GetTrajectoryRealizationCaseById(id);
            if (value == null)
            {
                return false;
            }

            MarkCalculationState(value, state, progress, message);
            return InsertOrUpdateTrajectoryRealizationCase(value, true, null);
        }

        private void DeleteChunks(Guid id)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            TrajectoryRealizationChunkStore.DeleteChunks(connection, transaction, id);
            transaction.Commit();
        }

        public int GetTrajectoryRealizationChunkCount(Guid id) =>
            TrajectoryRealizationChunkStore.GetChunkCount(_logger, _connectionManager, id);

        public TrajectoryRealizationChunk? GetTrajectoryRealizationChunk(Guid id, int chunkIndex) =>
            TrajectoryRealizationChunkStore.GetChunk(_logger, _connectionManager, id, chunkIndex);
    }
}
