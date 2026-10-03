using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Managers
{
    public class TrajectoryExtrapolationCaseManager
    {
        private const string SurveyStationOwnerType = "TrajectoryExtrapolation";
        private static TrajectoryExtrapolationCaseManager? instance_;
        private readonly ILogger<TrajectoryExtrapolationCaseManager> logger_;
        private readonly SqlConnectionManager connectionManager_;
        private readonly TrajectoryManager trajectoryManager_;

        private TrajectoryExtrapolationCaseManager(ILogger<TrajectoryExtrapolationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            logger_ = logger;
            connectionManager_ = connectionManager;
            trajectoryManager_ = TrajectoryManager.GetInstance(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance,
                connectionManager);
        }

        public static TrajectoryExtrapolationCaseManager GetInstance(
            ILogger<TrajectoryExtrapolationCaseManager> logger,
            SqlConnectionManager connectionManager) =>
            instance_ ??= new TrajectoryExtrapolationCaseManager(logger, connectionManager);

        public List<Guid>? GetAllIds()
        {
            using SqliteConnection? connection = connectionManager_.GetConnection();
            if (connection == null) return null;
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM TrajectoryExtrapolationCaseTable ORDER BY CreationDate";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                List<Guid> result = [];
                while (reader.Read()) result.Add(reader.GetGuid(0));
                return result;
            }
            catch (SqliteException ex)
            {
                logger_.LogError(ex, "Unable to list trajectory extrapolation case IDs");
                return null;
            }
        }

        public List<MetaInfo>? GetAllMetaInfo() => ReadAll(false)
            ?.Where(value => value.MetaInfo != null).Select(value => value.MetaInfo!).ToList();

        public List<TrajectoryExtrapolationCaseLight>? GetAllLight() => ReadAll(false)
            ?.Select(ToLight).ToList();

        public TrajectoryExtrapolationCaseLight? GetLightById(Guid id)
        {
            TrajectoryExtrapolationCase? value = GetById(id);
            return value == null ? null : ToLight(value);
        }

        public List<TrajectoryExtrapolationCase>? GetAll() => ReadAll(false);

        /// <summary>
        /// Requeues durable work that was interrupted while the service was stopped. The persisted
        /// revision remains the concurrency token used by the worker, so a later caller update wins.
        /// </summary>
        public int ResumeInterruptedCalculations()
        {
            List<TrajectoryExtrapolationCase> interrupted = ReadAll(false)?
                .Where(value => value.MetaInfo != null &&
                    value.CalculationState is CalculationState.Queued or CalculationState.Running &&
                    value.LastModificationDate.HasValue)
                .ToList() ?? [];

            foreach (TrajectoryExtrapolationCase value in interrupted)
            {
                Guid id = value.MetaInfo!.ID;
                DateTimeOffset revision = value.LastModificationDate!.Value;
                Mark(value, CalculationState.Queued, value.CalculationProgress, "Calculation resumed after service restart");
                if (Save(value, true, null, revision))
                    _ = Task.Run(() => RecalculateAsync(id, revision));
            }

            return interrupted.Count;
        }

        public TrajectoryExtrapolationCase? GetById(Guid id, bool includeResults = false)
        {
            if (id == Guid.Empty) return null;
            using SqliteConnection? connection = connectionManager_.GetConnection();
            if (connection == null) return null;
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryExtrapolationCase FROM TrajectoryExtrapolationCaseTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                string? json = command.ExecuteScalar() as string;
                TrajectoryExtrapolationCase? result = json == null ? null : JsonSerializer.Deserialize<TrajectoryExtrapolationCase>(json, JsonSettings.Options);
                if (result != null) TrajectoryExtrapolationValidation.UpgradeLegacyGeosteeringExtent(result);
                if (result != null && includeResults) result.SurveyStationList = GetSurveyStationList(id);
                return result;
            }
            catch (Exception ex) when (ex is SqliteException or JsonException)
            {
                logger_.LogError(ex, "Unable to read trajectory extrapolation case {CaseId}", id);
                return null;
            }
        }

        public Task<bool> AddAsync(TrajectoryExtrapolationCase value)
        {
            if (TrajectoryExtrapolationValidation.Validate(value).Count > 0 || GetById(value.MetaInfo!.ID) != null)
                return Task.FromResult(false);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            value.CreationDate = now;
            value.LastModificationDate = now;
            Mark(value, CalculationState.Queued, 0.0, "Calculation queued");
            bool saved = Save(value, false, null);
            if (saved) _ = Task.Run(() => RecalculateAsync(value.MetaInfo!.ID, value.LastModificationDate!.Value));
            return Task.FromResult(saved);
        }

        public Task<bool> UpdateAsync(Guid id, DateTimeOffset expectedRevision, TrajectoryExtrapolationCase value)
        {
            TrajectoryExtrapolationCase? existing = GetById(id);
            if (existing == null || value.MetaInfo?.ID != id || TrajectoryExtrapolationValidation.Validate(value).Count > 0)
                return Task.FromResult(false);
            value.CreationDate = existing.CreationDate;
            value.LastModificationDate = DateTimeOffset.UtcNow;
            Mark(value, CalculationState.Queued, 0.0, "Calculation queued");
            bool saved = Save(value, true, null, expectedRevision);
            if (saved) _ = Task.Run(() => RecalculateAsync(id, value.LastModificationDate!.Value));
            return Task.FromResult(saved);
        }

        public bool Delete(Guid id, DateTimeOffset expectedRevision)
        {
            using SqliteConnection? connection = connectionManager_.GetConnection();
            if (connection == null) return false;
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                SurveyStationChunkStore.DeleteChunks(connection, transaction, id, SurveyStationOwnerType);
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM TrajectoryExtrapolationCaseTable WHERE ID = @id AND LastModificationDate = @expectedModified";
                command.Parameters.AddWithValue("@id", id.ToString());
                command.Parameters.AddWithValue("@expectedModified", expectedRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                bool success = command.ExecuteNonQuery() == 1;
                if (success) transaction.Commit(); else transaction.Rollback();
                return success;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                logger_.LogError(ex, "Unable to delete trajectory extrapolation case {CaseId}", id);
                return false;
            }
        }

        public int GetSurveyStationChunkCount(Guid id) =>
            SurveyStationChunkStore.GetChunkCount(logger_, connectionManager_, id, SurveyStationOwnerType);

        public SurveyStationChunk? GetSurveyStationChunk(Guid id, int chunkIndex) =>
            SurveyStationChunkStore.GetChunk(logger_, connectionManager_, id, SurveyStationOwnerType, chunkIndex);

        public List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? GetSurveyStationList(Guid id) =>
            SurveyStationChunkStore.GetStations(logger_, connectionManager_, id, SurveyStationOwnerType);

        private async Task RecalculateAsync(Guid id, DateTimeOffset queuedRevision)
        {
            await Task.Yield();
            try
            {
                TrajectoryExtrapolationCase? value = GetById(id);
                if (value == null || value.LastModificationDate != queuedRevision) return;
                Mark(value, CalculationState.Running, 0.1, "Preparing extrapolation");
                Save(value, true, null, queuedRevision);
                Model.Trajectory? source = trajectoryManager_.GetTrajectoryById(value.SourceTrajectoryID, includeCalculatedStations: true);
                source = await trajectoryManager_.CalculateTrajectoryAsync(
                    source,
                    recalculateSurveyRunUncertainty: true);
                if (source == null)
                {
                    Mark(value, CalculationState.Failed, 1.0,
                        "Source trajectory uncertainty lineage could not be reconstructed");
                    if (GetById(id)?.LastModificationDate == queuedRevision) Save(value, true, [], queuedRevision);
                    return;
                }
                bool success = TrajectoryExtrapolationCalculator.Calculate(
                    value,
                    source,
                    trajectoryId => trajectoryManager_.GetTrajectoryById(trajectoryId, includeCalculatedStations: true));
                value.LastModificationDate = DateTimeOffset.UtcNow;
                List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? stations = success ? value.SurveyStationList : [];
                TrajectoryExtrapolationCase? current = GetById(id);
                if (current?.LastModificationDate != queuedRevision) return;
                Save(value, true, stations, queuedRevision);
            }
            catch (Exception ex)
            {
                logger_.LogError(ex, "Unexpected trajectory extrapolation calculation failure for {CaseId}", id);
                TrajectoryExtrapolationCase? value = GetById(id);
                if (value != null && value.LastModificationDate == queuedRevision)
                {
                    Mark(value, CalculationState.Failed, 1.0, "Trajectory extrapolation calculation failed");
                    Save(value, true, [], queuedRevision);
                }
            }
        }

        private List<TrajectoryExtrapolationCase>? ReadAll(bool includeResults)
        {
            using SqliteConnection? connection = connectionManager_.GetConnection();
            if (connection == null) return null;
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryExtrapolationCase FROM TrajectoryExtrapolationCaseTable ORDER BY CreationDate";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                List<TrajectoryExtrapolationCase> result = [];
                while (reader.Read())
                {
                    TrajectoryExtrapolationCase? value = JsonSerializer.Deserialize<TrajectoryExtrapolationCase>(reader.GetString(0), JsonSettings.Options);
                    if (value != null)
                    {
                        TrajectoryExtrapolationValidation.UpgradeLegacyGeosteeringExtent(value);
                        if (includeResults && value.MetaInfo != null) value.SurveyStationList = GetSurveyStationList(value.MetaInfo.ID);
                        result.Add(value);
                    }
                }
                return result;
            }
            catch (Exception ex) when (ex is SqliteException or JsonException)
            {
                logger_.LogError(ex, "Unable to list trajectory extrapolation cases");
                return null;
            }
        }

        private bool Save(TrajectoryExtrapolationCase value, bool update,
            List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? stations,
            DateTimeOffset? expectedRevision = null)
        {
            using SqliteConnection? connection = connectionManager_.GetConnection();
            if (connection == null || value.MetaInfo == null) return false;
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                List<OSDC.DotnetLibraries.Drilling.Surveying.SurveyStation>? inMemoryStations = value.SurveyStationList;
                string json;
                try
                {
                    value.SurveyStationList = null;
                    json = JsonSerializer.Serialize(value, JsonSettings.Options);
                }
                finally
                {
                    value.SurveyStationList = inMemoryStations;
                }
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = update
                    ? "UPDATE TrajectoryExtrapolationCaseTable SET MetaInfo=@meta,CreationDate=@created,LastModificationDate=@modified,SourceTrajectoryID=@source,Mode=@mode,CalculationState=@state,CalculationProgress=@progress,CalculationMessage=@message,TrajectoryExtrapolationCase=@data WHERE ID=@id AND LastModificationDate=@expectedModified"
                    : "INSERT INTO TrajectoryExtrapolationCaseTable (ID,MetaInfo,CreationDate,LastModificationDate,SourceTrajectoryID,Mode,CalculationState,CalculationProgress,CalculationMessage,TrajectoryExtrapolationCase) VALUES (@id,@meta,@created,@modified,@source,@mode,@state,@progress,@message,@data)";
                command.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
                command.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
                command.Parameters.AddWithValue("@created", (object?)value.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
                command.Parameters.AddWithValue("@modified", (object?)value.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
                command.Parameters.AddWithValue("@source", value.SourceTrajectoryID.ToString());
                command.Parameters.AddWithValue("@mode", value.Mode.ToString());
                command.Parameters.AddWithValue("@state", value.CalculationState.ToString());
                command.Parameters.AddWithValue("@progress", value.CalculationProgress);
                command.Parameters.AddWithValue("@message", (object?)value.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@data", json);
                if (update)
                {
                    if (!expectedRevision.HasValue) return false;
                    command.Parameters.AddWithValue("@expectedModified", expectedRevision.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                }
                bool success = command.ExecuteNonQuery() == 1;
                if (success) success = SurveyStationChunkStore.ReplaceChunks(connection, transaction, value.MetaInfo.ID, SurveyStationOwnerType, stations);
                if (success) transaction.Commit(); else transaction.Rollback();
                return success;
            }
            catch (Exception ex) when (ex is SqliteException or JsonException)
            {
                transaction.Rollback();
                logger_.LogError(ex, "Unable to save trajectory extrapolation case {CaseId}", value.MetaInfo.ID);
                return false;
            }
        }

        private static TrajectoryExtrapolationCaseLight ToLight(TrajectoryExtrapolationCase value) => new()
        {
            MetaInfo = value.MetaInfo,
            Name = value.Name,
            Description = value.Description,
            CreationDate = value.CreationDate,
            LastModificationDate = value.LastModificationDate,
            SourceTrajectoryID = value.SourceTrajectoryID,
            Mode = value.Mode,
            CalculationState = value.CalculationState,
            CalculationProgress = value.CalculationProgress,
            CalculationMessage = value.CalculationMessage
        };

        private static void Mark(TrajectoryExtrapolationCase value, CalculationState state, double progress, string? message)
        {
            value.CalculationState = state;
            value.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            value.CalculationMessage = message;
        }
    }
}
