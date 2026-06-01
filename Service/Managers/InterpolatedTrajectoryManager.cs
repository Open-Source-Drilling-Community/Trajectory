using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    /// <summary>
    /// Manager for InterpolatedTrajectory persistence and calculation.
    /// </summary>
    public class InterpolatedTrajectoryManager
    {
        private static InterpolatedTrajectoryManager? _instance = null;
        private readonly ILogger<InterpolatedTrajectoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private readonly TrajectoryManager _trajectoryManager;
        private const string SurveyStationOwnerType = "InterpolatedTrajectory";

        private InterpolatedTrajectoryManager(ILogger<InterpolatedTrajectoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _trajectoryManager = TrajectoryManager.GetInstance(logger as ILogger<TrajectoryManager> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance, connectionManager);
        }

        public static InterpolatedTrajectoryManager GetInstance(ILogger<InterpolatedTrajectoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new InterpolatedTrajectoryManager(logger, connectionManager);
            return _instance;
        }

        private static InterpolatedTrajectoryLight CreateDataLightInstance(InterpolatedTrajectory interpolatedTrajectory)
        {
            return new InterpolatedTrajectoryLight()
            {
                MetaInfo = interpolatedTrajectory.MetaInfo,
                Name = interpolatedTrajectory.Name,
                Description = interpolatedTrajectory.Description,
                CreationDate = interpolatedTrajectory.CreationDate,
                LastModificationDate = interpolatedTrajectory.LastModificationDate,
                TrajectoryID = interpolatedTrajectory.TrajectoryID,
                CalculationState = interpolatedTrajectory.CalculationState,
                CalculationProgress = interpolatedTrajectory.CalculationProgress,
                CalculationMessage = interpolatedTrajectory.CalculationMessage
            };
        }

        public List<Guid>? GetAllInterpolatedTrajectoryId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM InterpolatedTrajectoryTable";
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
                _logger.LogError(ex, "Impossible to get IDs from InterpolatedTrajectoryTable");
                return null;
            }
        }

        public List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>? GetAllInterpolatedTrajectoryMetaInfo()
        {
            List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM InterpolatedTrajectoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    metaInfos.Add(JsonSerializer.Deserialize<OSDC.DotnetLibraries.General.DataManagement.MetaInfo>(reader.GetString(0), JsonSettings.Options));
                }
                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get MetaInfo from InterpolatedTrajectoryTable");
                return null;
            }
        }

        public InterpolatedTrajectory? GetInterpolatedTrajectoryById(Guid id, bool includeCalculatedStations = true)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("The given InterpolatedTrajectory ID is null or empty");
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT InterpolatedTrajectory FROM InterpolatedTrajectoryTable WHERE ID = '{id}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    InterpolatedTrajectory? interpolatedTrajectory = JsonSerializer.Deserialize<InterpolatedTrajectory>(reader.GetString(0), JsonSettings.Options);
                    if (interpolatedTrajectory != null && interpolatedTrajectory.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned InterpolatedTrajectory is null or has been jsonified with the wrong ID.", 1);
                    }
                    if (includeCalculatedStations && interpolatedTrajectory != null)
                    {
                        interpolatedTrajectory.SurveyStationList ??= GetSurveyStationListByInterpolatedTrajectoryId(id);
                    }
                    return interpolatedTrajectory;
                }
                return null;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get the InterpolatedTrajectory with the given ID from InterpolatedTrajectoryTable");
                return null;
            }
        }

        public InterpolatedTrajectory? GetInterpolatedTrajectoryByTrajectoryId(Guid trajectoryId)
        {
            if (trajectoryId == Guid.Empty)
            {
                _logger.LogWarning("The given TrajectoryID is null or empty");
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT InterpolatedTrajectory FROM InterpolatedTrajectoryTable WHERE TrajectoryID = '{trajectoryId}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    InterpolatedTrajectory? interpolatedTrajectory = JsonSerializer.Deserialize<InterpolatedTrajectory>(reader.GetString(0), JsonSettings.Options);
                    if (interpolatedTrajectory != null && interpolatedTrajectory.TrajectoryID != trajectoryId)
                    {
                        throw new SqliteException("SQLite database corrupted: returned InterpolatedTrajectory has been jsonified with the wrong trajectory ID.", 1);
                    }
                    if (interpolatedTrajectory?.MetaInfo?.ID is Guid interpolatedTrajectoryId)
                    {
                        interpolatedTrajectory.SurveyStationList ??= GetSurveyStationListByInterpolatedTrajectoryId(interpolatedTrajectoryId);
                    }
                    return interpolatedTrajectory;
                }
                return null;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get the InterpolatedTrajectory with given TrajectoryID from InterpolatedTrajectoryTable");
                return null;
            }
        }

        public List<InterpolatedTrajectory?>? GetAllInterpolatedTrajectory()
        {
            List<InterpolatedTrajectory?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT InterpolatedTrajectory FROM InterpolatedTrajectoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    InterpolatedTrajectory? interpolatedTrajectory = JsonSerializer.Deserialize<InterpolatedTrajectory>(reader.GetString(0), JsonSettings.Options);
                    if (interpolatedTrajectory?.MetaInfo?.ID is Guid interpolatedTrajectoryId)
                    {
                        interpolatedTrajectory.SurveyStationList ??= GetSurveyStationListByInterpolatedTrajectoryId(interpolatedTrajectoryId);
                    }
                    vals.Add(interpolatedTrajectory);
                }
                return vals;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get InterpolatedTrajectory from InterpolatedTrajectoryTable");
                return null;
            }
        }

        public List<InterpolatedTrajectoryLight>? GetAllInterpolatedTrajectoryLight()
        {
            List<InterpolatedTrajectoryLight> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT InterpolatedTrajectory FROM InterpolatedTrajectoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    InterpolatedTrajectory? val = JsonSerializer.Deserialize<InterpolatedTrajectory>(reader.GetString(0), JsonSettings.Options);
                    if (val != null)
                    {
                        vals.Add(CreateDataLightInstance(val));
                    }
                }
                return vals;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get InterpolatedTrajectoryLight from InterpolatedTrajectoryTable");
                return null;
            }
        }

        public async Task<InterpolatedTrajectory?> CalculateInterpolatedTrajectoryAsync(InterpolatedTrajectory? interpolatedTrajectory)
        {
            await Task.Delay(0);
            try
            {
                if (interpolatedTrajectory?.MetaInfo == null || interpolatedTrajectory.TrajectoryID == Guid.Empty)
                {
                    _logger.LogWarning("The InterpolatedTrajectory or its TrajectoryID is null or empty");
                    return null;
                }

                NORCE.Drilling.Trajectory.Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(interpolatedTrajectory.TrajectoryID);
                if (trajectory == null)
                {
                    _logger.LogWarning("The linked Trajectory could not be found");
                    return null;
                }

                if (!interpolatedTrajectory.Calculate(trajectory))
                {
                    _logger.LogWarning("Impossible to calculate outputs for the given InterpolatedTrajectory");
                    return null;
                }

                return interpolatedTrajectory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during interpolated trajectory calculation");
                return null;
            }
        }

        public Task<bool> AddInterpolatedTrajectory(InterpolatedTrajectory? interpolatedTrajectory)
        {
            try
            {
                if (interpolatedTrajectory?.MetaInfo == null || interpolatedTrajectory.MetaInfo.ID == Guid.Empty || interpolatedTrajectory.TrajectoryID == Guid.Empty)
                {
                    _logger.LogWarning("The InterpolatedTrajectory ID or TrajectoryID is null or empty");
                    return Task.FromResult(false);
                }

                if (GetInterpolatedTrajectoryById(interpolatedTrajectory.MetaInfo.ID) != null)
                {
                    _logger.LogWarning("Impossible to post InterpolatedTrajectory. ID already found in database.");
                    return Task.FromResult(false);
                }

                MarkCalculationState(interpolatedTrajectory, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateInterpolatedTrajectory(interpolatedTrajectory, false, null);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateInterpolatedTrajectoryAsync(interpolatedTrajectory.MetaInfo.ID));
                }

                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during the addition of the InterpolatedTrajectory");
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateInterpolatedTrajectoryById(Guid id, InterpolatedTrajectory? interpolatedTrajectory)
        {
            try
            {
                if (id == Guid.Empty || interpolatedTrajectory?.MetaInfo == null || interpolatedTrajectory.MetaInfo.ID != id)
                {
                    _logger.LogWarning("The InterpolatedTrajectory ID or some of its attributes are null or empty");
                    return Task.FromResult(false);
                }

                interpolatedTrajectory.LastModificationDate = DateTimeOffset.UtcNow;
                MarkCalculationState(interpolatedTrajectory, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateInterpolatedTrajectory(interpolatedTrajectory, true, null);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateInterpolatedTrajectoryAsync(id));
                }

                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during the update of the InterpolatedTrajectory");
                return Task.FromResult(false);
            }
        }

        public bool DeleteInterpolatedTrajectoryById(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("The InterpolatedTrajectory ID is null or empty");
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                SurveyStationChunkStore.DeleteChunks(connection, transaction, id, SurveyStationOwnerType);
                command.CommandText = $"DELETE FROM InterpolatedTrajectoryTable WHERE ID = '{id}'";
                int count = command.ExecuteNonQuery();
                if (count < 0)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete the InterpolatedTrajectory of given ID from InterpolatedTrajectoryTable");
                return false;
            }
        }

        private async Task RecalculateInterpolatedTrajectoryAsync(Guid interpolatedTrajectoryId)
        {
            try
            {
                UpdateInterpolatedTrajectoryCalculationState(interpolatedTrajectoryId, CalculationState.Running, 0.05, "Preparing interpolation");
                InterpolatedTrajectory? interpolatedTrajectory = GetInterpolatedTrajectoryById(interpolatedTrajectoryId, includeCalculatedStations: false);
                if (interpolatedTrajectory == null)
                {
                    return;
                }

                UpdateInterpolatedTrajectoryCalculationState(interpolatedTrajectoryId, CalculationState.Running, 0.25, "Interpolating trajectory");
                if (await CalculateInterpolatedTrajectoryAsync(interpolatedTrajectory) == null)
                {
                    UpdateInterpolatedTrajectoryCalculationState(interpolatedTrajectoryId, CalculationState.Failed, 0.0, "Interpolated trajectory calculation failed");
                    DeleteSurveyStationChunks(interpolatedTrajectoryId);
                    return;
                }

                MarkCalculationState(interpolatedTrajectory, CalculationState.Completed, 1.0, null);
                interpolatedTrajectory.LastModificationDate = DateTimeOffset.UtcNow;
                if (!InsertOrUpdateInterpolatedTrajectory(interpolatedTrajectory, true, interpolatedTrajectory.SurveyStationList))
                {
                    UpdateInterpolatedTrajectoryCalculationState(interpolatedTrajectoryId, CalculationState.Failed, 0.0, "Interpolated trajectory calculation failed while saving");
                    DeleteSurveyStationChunks(interpolatedTrajectoryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during background InterpolatedTrajectory calculation");
                UpdateInterpolatedTrajectoryCalculationState(interpolatedTrajectoryId, CalculationState.Failed, 0.0, "Interpolated trajectory calculation failed");
                DeleteSurveyStationChunks(interpolatedTrajectoryId);
            }
        }

        private bool InsertOrUpdateInterpolatedTrajectory(
            InterpolatedTrajectory interpolatedTrajectory,
            bool update,
            List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? calculatedStationList)
        {
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                string metaInfo = JsonSerializer.Serialize(interpolatedTrajectory.MetaInfo, JsonSettings.Options);
                string? cDate = interpolatedTrajectory.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lDate = interpolatedTrajectory.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                interpolatedTrajectory.SurveyStationList = null;
                string data = JsonSerializer.Serialize(interpolatedTrajectory, JsonSettings.Options);

                var command = connection.CreateCommand();
                command.Transaction = transaction;
                if (update)
                {
                    command.CommandText = "UPDATE InterpolatedTrajectoryTable SET " +
                        "MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, TrajectoryID = @trajectoryId, " +
                        "CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, InterpolatedTrajectory = @interpolatedTrajectory " +
                        "WHERE ID = @id";
                }
                else
                {
                    command.CommandText = "INSERT INTO InterpolatedTrajectoryTable " +
                        "(ID, MetaInfo, CreationDate, LastModificationDate, TrajectoryID, CalculationState, CalculationProgress, CalculationMessage, InterpolatedTrajectory) " +
                        "VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @trajectoryId, @calculationState, @calculationProgress, @calculationMessage, @interpolatedTrajectory)";
                }

                command.Parameters.AddWithValue("@id", interpolatedTrajectory.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)cDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@trajectoryId", interpolatedTrajectory.TrajectoryID.ToString());
                command.Parameters.AddWithValue("@calculationState", interpolatedTrajectory.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", interpolatedTrajectory.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)interpolatedTrajectory.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@interpolatedTrajectory", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success)
                {
                    success = SurveyStationChunkStore.ReplaceChunks(connection, transaction, interpolatedTrajectory.MetaInfo.ID, SurveyStationOwnerType, calculatedStationList);
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
                _logger.LogError(ex, "Impossible to save the InterpolatedTrajectory");
                return false;
            }
        }

        private static void MarkCalculationState(InterpolatedTrajectory interpolatedTrajectory, CalculationState state, double progress, string? message)
        {
            interpolatedTrajectory.CalculationState = state;
            interpolatedTrajectory.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            interpolatedTrajectory.CalculationMessage = message;
        }

        private bool UpdateInterpolatedTrajectoryCalculationState(Guid interpolatedTrajectoryId, CalculationState state, double progress, string? message)
        {
            InterpolatedTrajectory? interpolatedTrajectory = GetInterpolatedTrajectoryById(interpolatedTrajectoryId, includeCalculatedStations: false);
            if (interpolatedTrajectory == null)
            {
                return false;
            }

            MarkCalculationState(interpolatedTrajectory, state, progress, message);
            return InsertOrUpdateInterpolatedTrajectory(interpolatedTrajectory, true, null);
        }

        private void DeleteSurveyStationChunks(Guid interpolatedTrajectoryId)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            SurveyStationChunkStore.DeleteChunks(connection, transaction, interpolatedTrajectoryId, SurveyStationOwnerType);
            transaction.Commit();
        }

        public int GetSurveyStationChunkCount(Guid interpolatedTrajectoryId)
        {
            return SurveyStationChunkStore.GetChunkCount(_logger, _connectionManager, interpolatedTrajectoryId, SurveyStationOwnerType);
        }

        public SurveyStationChunk? GetSurveyStationChunk(Guid interpolatedTrajectoryId, int chunkIndex)
        {
            return SurveyStationChunkStore.GetChunk(_logger, _connectionManager, interpolatedTrajectoryId, SurveyStationOwnerType, chunkIndex);
        }

        public List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? GetSurveyStationListByInterpolatedTrajectoryId(Guid interpolatedTrajectoryId)
        {
            return SurveyStationChunkStore.GetStations(_logger, _connectionManager, interpolatedTrajectoryId, SurveyStationOwnerType);
        }
    }
}
