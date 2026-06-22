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
    public class TrajectoryAggregationCaseManager
    {
        private const string AggregatedPointOwnerType = "TrajectoryAggregationAggregatedSurveyPoints";
        private const string CoarsenedReferenceOwnerType = "TrajectoryAggregationCoarsenedReferencePoints";
        private static TrajectoryAggregationCaseManager? _instance;
        private readonly ILogger<TrajectoryAggregationCaseManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private readonly TrajectoryManager _trajectoryManager;

        private TrajectoryAggregationCaseManager(ILogger<TrajectoryAggregationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _trajectoryManager = TrajectoryManager.GetInstance(Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance, connectionManager);
        }

        public static TrajectoryAggregationCaseManager GetInstance(ILogger<TrajectoryAggregationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new TrajectoryAggregationCaseManager(logger, connectionManager);
            return _instance;
        }

        private static TrajectoryAggregationCaseLight CreateLight(TrajectoryAggregationCase value) => new()
        {
            MetaInfo = value.MetaInfo,
            Name = value.Name,
            Description = value.Description,
            CreationDate = value.CreationDate,
            LastModificationDate = value.LastModificationDate,
            CalculationState = value.CalculationState,
            CalculationProgress = value.CalculationProgress,
            CalculationMessage = value.CalculationMessage,
            EpsilonL = value.EpsilonL,
            EpsilonKappa = value.EpsilonKappa,
            Alpha = value.Alpha,
            InterpolationInterval = value.InterpolationInterval,
            DistanceReferenceCoarseningThreshold = value.DistanceReferenceCoarseningThreshold,
            TrajectoryAggregationList = value.TrajectoryAggregationList
        };

        public List<Guid>? GetAllTrajectoryAggregationCaseId()
        {
            List<Guid> ids = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM TrajectoryAggregationCaseTable";
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
                _logger.LogError(ex, "Impossible to get trajectory aggregation case IDs");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllTrajectoryAggregationCaseMetaInfo()
        {
            List<MetaInfo?> metaInfos = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM TrajectoryAggregationCaseTable";
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
                _logger.LogError(ex, "Impossible to get trajectory aggregation case MetaInfo");
                return null;
            }
        }

        public TrajectoryAggregationCase? GetTrajectoryAggregationCaseById(Guid id, bool includeResults = true)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryAggregationCase FROM TrajectoryAggregationCaseTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryAggregationCase? value = JsonSerializer.Deserialize<TrajectoryAggregationCase>(reader.GetString(0), JsonSettings.Options);
                    if (value?.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned TrajectoryAggregationCase has the wrong ID.", 1);
                    }

                    if (includeResults && value.TrajectoryAggregationList is { Count: > 0 })
                    {
                        foreach (TrajectoryAggregation aggregation in value.TrajectoryAggregationList)
                        {
                            AttachChunks(aggregation);
                        }
                    }

                    return value;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory aggregation case");
            }

            return null;
        }

        public List<TrajectoryAggregationCase?>? GetAllTrajectoryAggregationCase()
        {
            List<TrajectoryAggregationCase?> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryAggregationCase FROM TrajectoryAggregationCaseTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryAggregationCase? value = JsonSerializer.Deserialize<TrajectoryAggregationCase>(reader.GetString(0), JsonSettings.Options);
                    if (value?.TrajectoryAggregationList is { Count: > 0 })
                    {
                        foreach (TrajectoryAggregation aggregation in value.TrajectoryAggregationList)
                        {
                            AttachChunks(aggregation);
                        }
                    }
                    values.Add(value);
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory aggregation cases");
                return null;
            }
        }

        public List<TrajectoryAggregationCaseLight>? GetAllTrajectoryAggregationCaseLight()
        {
            List<TrajectoryAggregationCaseLight> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryAggregationCase FROM TrajectoryAggregationCaseTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryAggregationCase? value = JsonSerializer.Deserialize<TrajectoryAggregationCase>(reader.GetString(0), JsonSettings.Options);
                    if (value != null)
                    {
                        values.Add(CreateLight(value));
                    }
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory aggregation case lights");
                return null;
            }
        }

        public TrajectoryAggregation? GetTrajectoryAggregation(Guid caseId, Guid trajectoryId, bool includeResults = true)
        {
            TrajectoryAggregationCase? value = GetTrajectoryAggregationCaseById(caseId, includeResults: false);
            TrajectoryAggregation? aggregation = value?.TrajectoryAggregationList?.FirstOrDefault(item => item.TrajectoryID == trajectoryId);
            if (aggregation != null && includeResults)
            {
                AttachChunks(aggregation);
            }
            return aggregation;
        }

        public Task<bool> AddTrajectoryAggregationCase(TrajectoryAggregationCase? value)
        {
            try
            {
                if (value?.MetaInfo?.ID is not Guid id || id == Guid.Empty || value.TrajectoryAggregationList is not { Count: > 0 })
                {
                    return Task.FromResult(false);
                }

                if (GetTrajectoryAggregationCaseById(id, includeResults: false) != null)
                {
                    return Task.FromResult(false);
                }

                PrepareForQueuedCalculation(value);
                bool saved = InsertOrUpdateTrajectoryAggregationCase(value, false, replaceChunks: false);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateTrajectoryAggregationCaseAsync(id));
                }
                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding trajectory aggregation case");
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateTrajectoryAggregationCaseById(Guid id, TrajectoryAggregationCase? value)
        {
            try
            {
                if (id == Guid.Empty || value?.MetaInfo?.ID != id || value.TrajectoryAggregationList is not { Count: > 0 })
                {
                    return Task.FromResult(false);
                }

                value.LastModificationDate = DateTimeOffset.UtcNow;
                PrepareForQueuedCalculation(value);
                bool saved = InsertOrUpdateTrajectoryAggregationCase(value, true, replaceChunks: true);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateTrajectoryAggregationCaseAsync(id));
                }
                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating trajectory aggregation case");
                return Task.FromResult(false);
            }
        }

        public bool DeleteTrajectoryAggregationCaseById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            TrajectoryAggregationCase? existing = GetTrajectoryAggregationCaseById(id, includeResults: false);
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                DeleteChildChunks(connection, transaction, existing);
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM TrajectoryAggregationCaseTable WHERE ID = @id";
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
                _logger.LogError(ex, "Impossible to delete trajectory aggregation case");
                return false;
            }
        }

        private async Task RecalculateTrajectoryAggregationCaseAsync(Guid id)
        {
            try
            {
                UpdateCalculationState(id, CalculationState.Running, 0.02, "Preparing aggregation");
                TrajectoryAggregationCase? value = GetTrajectoryAggregationCaseById(id, includeResults: false);
                if (value == null)
                {
                    return;
                }

                bool success = value.Calculate(
                    trajectoryId => _trajectoryManager.GetTrajectoryById(trajectoryId),
                    (progress, message) => UpdateCalculationState(id, CalculationState.Running, Math.Clamp(progress, 0.02, 0.98), message));
                await Task.Delay(1);

                value.CalculationState = success && value.TrajectoryAggregationList?.All(x => x.CalculationState == CalculationState.Completed) == true
                    ? CalculationState.Completed
                    : CalculationState.Failed;
                value.CalculationProgress = 1.0;
                value.CalculationMessage = value.CalculationState == CalculationState.Completed ? null : "One or more trajectory aggregations failed.";
                value.LastModificationDate = DateTimeOffset.UtcNow;

                if (!InsertOrUpdateTrajectoryAggregationCase(value, true, replaceChunks: true))
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Trajectory aggregation failed while saving results");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during trajectory aggregation case calculation");
                UpdateCalculationState(id, CalculationState.Failed, 0.0, "Trajectory aggregation failed");
            }
        }

        private bool InsertOrUpdateTrajectoryAggregationCase(TrajectoryAggregationCase value, bool update, bool replaceChunks)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                List<TrajectoryAggregation>? children = value.TrajectoryAggregationList;
                Dictionary<Guid, ChildChunkPayload> chunkPayloads = [];
                if (children != null)
                {
                    foreach (TrajectoryAggregation child in children)
                    {
                        chunkPayloads[child.ID] = new ChildChunkPayload(child.AggregatedSurveyPointList, child.CoarsenedReferenceTrajectory, child.DistanceResultList);
                        child.AggregatedSurveyPointList = null;
                        child.CoarsenedReferenceTrajectory = null;
                        child.DistanceResultList = null;
                    }
                }

                string metaInfo = JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options);
                string? creationDate = value.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = value.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string data = JsonSerializer.Serialize(value, JsonSettings.Options);

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                if (update)
                {
                    command.CommandText = "UPDATE TrajectoryAggregationCaseTable SET " +
                        "MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, CalculationState = @calculationState, " +
                        "CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, EpsilonL = @epsilonL, EpsilonKappa = @epsilonKappa, " +
                        "Alpha = @alpha, InterpolationInterval = @interpolationInterval, DistanceReferenceCoarseningThreshold = @distanceReferenceCoarseningThreshold, " +
                        "TrajectoryAggregationCase = @case WHERE ID = @id";
                }
                else
                {
                    command.CommandText = "INSERT INTO TrajectoryAggregationCaseTable " +
                        "(ID, MetaInfo, CreationDate, LastModificationDate, CalculationState, CalculationProgress, CalculationMessage, EpsilonL, EpsilonKappa, Alpha, InterpolationInterval, DistanceReferenceCoarseningThreshold, TrajectoryAggregationCase) " +
                        "VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @calculationState, @calculationProgress, @calculationMessage, @epsilonL, @epsilonKappa, @alpha, @interpolationInterval, @distanceReferenceCoarseningThreshold, @case)";
                }

                command.Parameters.AddWithValue("@id", value.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@calculationState", value.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", value.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)value.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@epsilonL", (object?)value.EpsilonL ?? DBNull.Value);
                command.Parameters.AddWithValue("@epsilonKappa", (object?)value.EpsilonKappa ?? DBNull.Value);
                command.Parameters.AddWithValue("@alpha", (object?)value.Alpha ?? DBNull.Value);
                command.Parameters.AddWithValue("@interpolationInterval", (object?)value.InterpolationInterval ?? DBNull.Value);
                command.Parameters.AddWithValue("@distanceReferenceCoarseningThreshold", (object?)value.DistanceReferenceCoarseningThreshold ?? DBNull.Value);
                command.Parameters.AddWithValue("@case", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success && replaceChunks && children != null)
                {
                    foreach (TrajectoryAggregation child in children)
                    {
                        ChildChunkPayload payload = chunkPayloads[child.ID];
                        success =
                            SurveyPointChunkStore.ReplaceChunks(connection, transaction, child.ID, AggregatedPointOwnerType, payload.AggregatedPoints) &&
                            SurveyPointChunkStore.ReplaceChunks(connection, transaction, child.ID, CoarsenedReferenceOwnerType, payload.CoarsenedReferencePoints) &&
                            TrajectoryAggregationDistanceResultChunkStore.ReplaceChunks(connection, transaction, child.ID, payload.DistanceResults);
                        if (!success)
                        {
                            break;
                        }
                    }
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
                _logger.LogError(ex, "Impossible to save trajectory aggregation case");
                return false;
            }
        }

        private void AttachChunks(TrajectoryAggregation aggregation)
        {
            aggregation.AggregatedSurveyPointList ??= SurveyPointChunkStore.GetPoints(_logger, _connectionManager, aggregation.ID, AggregatedPointOwnerType);
            aggregation.CoarsenedReferenceTrajectory ??= SurveyPointChunkStore.GetPoints(_logger, _connectionManager, aggregation.ID, CoarsenedReferenceOwnerType);
            aggregation.DistanceResultList ??= TrajectoryAggregationDistanceResultChunkStore.GetResults(_logger, _connectionManager, aggregation.ID);
        }

        private static void PrepareForQueuedCalculation(TrajectoryAggregationCase value)
        {
            value.CalculationState = CalculationState.Running;
            value.CalculationProgress = 0.0;
            value.CalculationMessage = "Calculation queued";
            value.TrajectoryAggregationList ??= [];
            foreach (TrajectoryAggregation child in value.TrajectoryAggregationList)
            {
                if (child.ID == Guid.Empty)
                {
                    child.ID = Guid.NewGuid();
                }
                child.CalculationState = CalculationState.Queued;
                child.CalculationProgress = 0.0;
                child.CalculationMessage = "Calculation queued";
                child.SectionList = null;
                child.AggregatedSurveyPointList = null;
                child.CoarsenedReferenceTrajectory = null;
                child.DistanceResultList = null;
            }
        }

        private bool UpdateCalculationState(Guid id, CalculationState state, double progress, string? message)
        {
            TrajectoryAggregationCase? value = GetTrajectoryAggregationCaseById(id, includeResults: false);
            if (value == null)
            {
                return false;
            }

            value.CalculationState = state;
            value.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            value.CalculationMessage = message;
            return InsertOrUpdateTrajectoryAggregationCase(value, true, replaceChunks: false);
        }

        private static void DeleteChildChunks(SqliteConnection connection, SqliteTransaction transaction, TrajectoryAggregationCase? value)
        {
            if (value?.TrajectoryAggregationList is not { Count: > 0 })
            {
                return;
            }

            foreach (TrajectoryAggregation child in value.TrajectoryAggregationList)
            {
                SurveyPointChunkStore.DeleteChunks(connection, transaction, child.ID, AggregatedPointOwnerType);
                SurveyPointChunkStore.DeleteChunks(connection, transaction, child.ID, CoarsenedReferenceOwnerType);
                TrajectoryAggregationDistanceResultChunkStore.DeleteChunks(connection, transaction, child.ID);
            }
        }

        private Guid? GetChildAggregationId(Guid caseId, Guid trajectoryId)
        {
            return GetTrajectoryAggregationCaseById(caseId, includeResults: false)?
                .TrajectoryAggregationList?
                .FirstOrDefault(x => x.TrajectoryID == trajectoryId)?
                .ID;
        }

        public int GetAggregatedSurveyPointChunkCount(Guid caseId, Guid trajectoryId) =>
            GetChildAggregationId(caseId, trajectoryId) is Guid childId
                ? SurveyPointChunkStore.GetChunkCount(_logger, _connectionManager, childId, AggregatedPointOwnerType)
                : 0;

        public SurveyPointChunk? GetAggregatedSurveyPointChunk(Guid caseId, Guid trajectoryId, int chunkIndex) =>
            GetChildAggregationId(caseId, trajectoryId) is Guid childId
                ? SurveyPointChunkStore.GetChunk(_logger, _connectionManager, childId, AggregatedPointOwnerType, chunkIndex)
                : null;

        public int GetCoarsenedReferencePointChunkCount(Guid caseId, Guid trajectoryId) =>
            GetChildAggregationId(caseId, trajectoryId) is Guid childId
                ? SurveyPointChunkStore.GetChunkCount(_logger, _connectionManager, childId, CoarsenedReferenceOwnerType)
                : 0;

        public SurveyPointChunk? GetCoarsenedReferencePointChunk(Guid caseId, Guid trajectoryId, int chunkIndex) =>
            GetChildAggregationId(caseId, trajectoryId) is Guid childId
                ? SurveyPointChunkStore.GetChunk(_logger, _connectionManager, childId, CoarsenedReferenceOwnerType, chunkIndex)
                : null;

        public int GetDistanceResultChunkCount(Guid caseId, Guid trajectoryId) =>
            GetChildAggregationId(caseId, trajectoryId) is Guid childId
                ? TrajectoryAggregationDistanceResultChunkStore.GetChunkCount(_logger, _connectionManager, childId)
                : 0;

        public TrajectoryAggregationDistanceResultChunk? GetDistanceResultChunk(Guid caseId, Guid trajectoryId, int chunkIndex) =>
            GetChildAggregationId(caseId, trajectoryId) is Guid childId
                ? TrajectoryAggregationDistanceResultChunkStore.GetChunk(_logger, _connectionManager, childId, chunkIndex)
                : null;

        private sealed record ChildChunkPayload(
            List<SurveyPoint>? AggregatedPoints,
            List<SurveyPoint>? CoarsenedReferencePoints,
            List<TrajectoryAggregationDistanceResult>? DistanceResults);
    }
}
