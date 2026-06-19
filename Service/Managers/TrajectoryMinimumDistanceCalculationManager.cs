using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class TrajectoryMinimumDistanceCalculationManager
    {
        private static TrajectoryMinimumDistanceCalculationManager? _instance;
        private readonly ILogger<TrajectoryMinimumDistanceCalculationManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private readonly TrajectoryManager _trajectoryManager;

        private TrajectoryMinimumDistanceCalculationManager(ILogger<TrajectoryMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _trajectoryManager = TrajectoryManager.GetInstance(Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance, connectionManager);
        }

        public static TrajectoryMinimumDistanceCalculationManager GetInstance(ILogger<TrajectoryMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new TrajectoryMinimumDistanceCalculationManager(logger, connectionManager);
            return _instance;
        }

        private static TrajectoryMinimumDistanceCalculationLight CreateLight(TrajectoryMinimumDistanceCalculation calculation) => new()
        {
            MetaInfo = calculation.MetaInfo,
            Name = calculation.Name,
            Description = calculation.Description,
            CreationDate = calculation.CreationDate,
            LastModificationDate = calculation.LastModificationDate,
            ReferenceTrajectoryID = calculation.ReferenceTrajectoryID,
            ComparisonTrajectoryIDList = calculation.ComparisonTrajectoryIDList,
            CalculationState = calculation.CalculationState,
            CalculationProgress = calculation.CalculationProgress,
            CalculationMessage = calculation.CalculationMessage,
            ResultCount = calculation.ResultCount,
            IntervalResultCount = calculation.IntervalResultCount
        };

        public List<Guid>? GetAllTrajectoryMinimumDistanceCalculationId()
        {
            List<Guid> ids = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM TrajectoryMinimumDistanceCalculationTable";
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
                _logger.LogError(ex, "Impossible to get trajectory minimum distance calculation IDs");
                return null;
            }
        }

        public List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>? GetAllTrajectoryMinimumDistanceCalculationMetaInfo()
        {
            List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> metaInfos = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM TrajectoryMinimumDistanceCalculationTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    metaInfos.Add(JsonSerializer.Deserialize<OSDC.DotnetLibraries.General.DataManagement.MetaInfo>(reader.GetString(0), JsonSettings.Options));
                }

                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory minimum distance calculation MetaInfo");
                return null;
            }
        }

        public TrajectoryMinimumDistanceCalculation? GetTrajectoryMinimumDistanceCalculationById(Guid id, bool includeResults = true)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryMinimumDistanceCalculation FROM TrajectoryMinimumDistanceCalculationTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryMinimumDistanceCalculation? calculation = JsonSerializer.Deserialize<TrajectoryMinimumDistanceCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (calculation?.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned TrajectoryMinimumDistanceCalculation has the wrong ID.", 1);
                    }

                    if (includeResults)
                    {
                        calculation.ResultList ??= GetResultListByTrajectoryMinimumDistanceCalculationId(id);
                    }

                    return calculation;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory minimum distance calculation");
            }

            return null;
        }

        public List<TrajectoryMinimumDistanceCalculation?>? GetAllTrajectoryMinimumDistanceCalculation()
        {
            List<TrajectoryMinimumDistanceCalculation?> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryMinimumDistanceCalculation FROM TrajectoryMinimumDistanceCalculationTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryMinimumDistanceCalculation? value = JsonSerializer.Deserialize<TrajectoryMinimumDistanceCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (value?.MetaInfo?.ID is Guid id)
                    {
                        value.ResultList ??= GetResultListByTrajectoryMinimumDistanceCalculationId(id);
                    }

                    values.Add(value);
                }

                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory minimum distance calculations");
                return null;
            }
        }

        public List<TrajectoryMinimumDistanceCalculationLight>? GetAllTrajectoryMinimumDistanceCalculationLight()
        {
            List<TrajectoryMinimumDistanceCalculationLight> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryMinimumDistanceCalculation FROM TrajectoryMinimumDistanceCalculationTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryMinimumDistanceCalculation? value = JsonSerializer.Deserialize<TrajectoryMinimumDistanceCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (value != null)
                    {
                        values.Add(CreateLight(value));
                    }
                }

                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get trajectory minimum distance calculation lights");
                return null;
            }
        }

        public Task<bool> AddTrajectoryMinimumDistanceCalculation(TrajectoryMinimumDistanceCalculation? calculation)
        {
            try
            {
                if (calculation?.MetaInfo?.ID is not Guid id || id == Guid.Empty || calculation.ReferenceTrajectoryID == Guid.Empty)
                {
                    return Task.FromResult(false);
                }

                if (GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false) != null)
                {
                    return Task.FromResult(false);
                }

                MarkCalculationState(calculation, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateTrajectoryMinimumDistanceCalculation(calculation, false, null, replaceResultChunks: false);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateTrajectoryMinimumDistanceCalculationAsync(id));
                }

                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding trajectory minimum distance calculation");
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateTrajectoryMinimumDistanceCalculationById(Guid id, TrajectoryMinimumDistanceCalculation? calculation)
        {
            try
            {
                if (id == Guid.Empty || calculation?.MetaInfo?.ID != id || calculation.ReferenceTrajectoryID == Guid.Empty)
                {
                    return Task.FromResult(false);
                }

                calculation.LastModificationDate = DateTimeOffset.UtcNow;
                MarkCalculationState(calculation, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateTrajectoryMinimumDistanceCalculation(calculation, true, null, replaceResultChunks: true);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateTrajectoryMinimumDistanceCalculationAsync(id));
                }

                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating trajectory minimum distance calculation");
                return Task.FromResult(false);
            }
        }

        public bool DeleteTrajectoryMinimumDistanceCalculationById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                TrajectoryMinimumDistanceResultChunkStore.DeleteChunks(connection, transaction, id);
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM TrajectoryMinimumDistanceCalculationTable WHERE ID = @id";
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
                _logger.LogError(ex, "Impossible to delete trajectory minimum distance calculation");
                return false;
            }
        }

        private async Task RecalculateTrajectoryMinimumDistanceCalculationAsync(Guid id)
        {
            try
            {
                UpdateCalculationState(id, CalculationState.Running, 0.02, "Preparing trajectories");
                TrajectoryMinimumDistanceCalculation? calculation = GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false);
                if (calculation == null)
                {
                    return;
                }

                Model.Trajectory? referenceTrajectory = _trajectoryManager.GetTrajectoryById(calculation.ReferenceTrajectoryID);
                if (referenceTrajectory?.SurveyStationList is not { Count: > 1 })
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Reference trajectory has no calculated survey stations");
                    DeleteResultChunks(id);
                    return;
                }

                List<Model.Trajectory> comparisonTrajectories = [];
                foreach (Guid comparisonId in calculation.ComparisonTrajectoryIDList?.Distinct().Where(x => x != Guid.Empty && x != calculation.ReferenceTrajectoryID) ?? [])
                {
                    Model.Trajectory? comparison = _trajectoryManager.GetTrajectoryById(comparisonId);
                    if (comparison?.SurveyStationList is { Count: > 1 })
                    {
                        comparisonTrajectories.Add(comparison);
                    }
                }

                if (comparisonTrajectories.Count == 0)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "No valid comparison trajectory was found");
                    DeleteResultChunks(id);
                    return;
                }

                UpdateCalculationState(id, CalculationState.Running, 0.08, "Preparing centerline segments");
                List<SurveyStation> referenceStations = CenterlineMinimumDistanceEngine.PrepareStationList(
                    referenceTrajectory.SurveyStationList,
                    referenceTrajectory.CalculationType,
                    calculation.MaximumChordArcDistance);
                if (referenceStations.Count < 2)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Reference trajectory has no valid 3D segments");
                    DeleteResultChunks(id);
                    return;
                }

                List<CenterlineComparisonSource> comparisonSources = comparisonTrajectories
                    .Select(trajectory => new CenterlineComparisonSource(
                        trajectory.MetaInfo!.ID,
                        CenterlineMinimumDistanceEngine.PrepareStationList(
                            trajectory.SurveyStationList,
                            trajectory.CalculationType,
                            calculation.MaximumChordArcDistance)))
                    .Where(source => source.StationList.Count > 1)
                    .ToList();
                if (comparisonSources.Count == 0)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Comparison trajectories have no valid 3D segments");
                    DeleteResultChunks(id);
                    return;
                }

                DateTimeOffset lastProgressUpdate = DateTimeOffset.UtcNow;
                List<CenterlineMinimumDistanceResult> engineResults = CenterlineMinimumDistanceEngine.Calculate(
                    referenceStations,
                    comparisonSources,
                    calculation.AccountForBoreholeRadius,
                    calculation.OctreeMaximumDepth,
                    calculation.OctreeMaximumSegmentCountPerLeaf,
                    calculation.AdaptiveRefinementSettings,
                    (completedComparisons, totalComparisons) =>
                    {
                        if ((DateTimeOffset.UtcNow - lastProgressUpdate).TotalMilliseconds > 500 || completedComparisons == totalComparisons)
                        {
                            double progress = 0.1 + 0.82 * completedComparisons / totalComparisons;
                            UpdateCalculationState(id, CalculationState.Running, progress, $"Compared {completedComparisons:N0}/{totalComparisons:N0} reference/comparison segment pairs");
                            lastProgressUpdate = DateTimeOffset.UtcNow;
                        }
                    });
                await Task.Delay(1);

                List<TrajectoryMinimumDistanceResult> results = engineResults.Select(result => new TrajectoryMinimumDistanceResult
                {
                    ReferenceMD = result.ReferenceMD,
                    ReferenceTVD = result.ReferenceTVD,
                    ReferenceNorth = result.ReferenceNorth,
                    ReferenceEast = result.ReferenceEast,
                    ReferenceBoreholeDiameter = result.ReferenceBoreholeDiameter,
                    ComparisonTrajectoryID = result.SourceID,
                    ComparisonMD = result.ComparisonMD,
                    ComparisonTVD = result.ComparisonTVD,
                    ComparisonNorth = result.ComparisonNorth,
                    ComparisonEast = result.ComparisonEast,
                    ComparisonBoreholeDiameter = result.ComparisonBoreholeDiameter,
                    CenterToCenterDistance = result.CenterToCenterDistance,
                    ClearanceDistance = result.ClearanceDistance,
                    Toolface = result.Toolface,
                    IsGravity = result.IsGravity,
                    IsAdaptiveRefinementSample = result.IsAdaptiveRefinementSample,
                    RefinementLevel = result.RefinementLevel
                }).ToList();

                calculation.ResultList = results;
                calculation.ResultCount = results.Count;
                calculation.IntervalResultList = CalculateIntervalResults(calculation.ReferenceIntervalList, results);
                calculation.IntervalResultCount = calculation.IntervalResultList?.Count ?? 0;
                SetGlobalMinimum(calculation);
                MarkCalculationState(calculation, CalculationState.Completed, 1.0, null);
                calculation.LastModificationDate = DateTimeOffset.UtcNow;

                if (!InsertOrUpdateTrajectoryMinimumDistanceCalculation(calculation, true, results, replaceResultChunks: true))
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Calculation failed while saving results");
                    DeleteResultChunks(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during trajectory minimum distance calculation");
                UpdateCalculationState(id, CalculationState.Failed, 0.0, "Trajectory minimum distance calculation failed");
                DeleteResultChunks(id);
            }
        }

        private bool InsertOrUpdateTrajectoryMinimumDistanceCalculation(
            TrajectoryMinimumDistanceCalculation calculation,
            bool update,
            List<TrajectoryMinimumDistanceResult>? results,
            bool replaceResultChunks)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                calculation.ResultList = null;
                string metaInfo = JsonSerializer.Serialize(calculation.MetaInfo, JsonSettings.Options);
                string? creationDate = calculation.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = calculation.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string data = JsonSerializer.Serialize(calculation, JsonSettings.Options);

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                if (update)
                {
                    command.CommandText = "UPDATE TrajectoryMinimumDistanceCalculationTable SET " +
                        "MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, ReferenceTrajectoryID = @referenceTrajectoryId, " +
                        "CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, ResultCount = @resultCount, IntervalResultCount = @intervalResultCount, " +
                        "TrajectoryMinimumDistanceCalculation = @calculation WHERE ID = @id";
                }
                else
                {
                    command.CommandText = "INSERT INTO TrajectoryMinimumDistanceCalculationTable " +
                        "(ID, MetaInfo, CreationDate, LastModificationDate, ReferenceTrajectoryID, CalculationState, CalculationProgress, CalculationMessage, ResultCount, IntervalResultCount, TrajectoryMinimumDistanceCalculation) " +
                        "VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @referenceTrajectoryId, @calculationState, @calculationProgress, @calculationMessage, @resultCount, @intervalResultCount, @calculation)";
                }

                command.Parameters.AddWithValue("@id", calculation.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@referenceTrajectoryId", calculation.ReferenceTrajectoryID.ToString());
                command.Parameters.AddWithValue("@calculationState", calculation.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", calculation.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)calculation.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@resultCount", calculation.ResultCount);
                command.Parameters.AddWithValue("@intervalResultCount", calculation.IntervalResultCount);
                command.Parameters.AddWithValue("@calculation", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success && replaceResultChunks)
                {
                    success = TrajectoryMinimumDistanceResultChunkStore.ReplaceChunks(connection, transaction, calculation.MetaInfo.ID, results);
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
                _logger.LogError(ex, "Impossible to save trajectory minimum distance calculation");
                return false;
            }
        }

        private static void MarkCalculationState(TrajectoryMinimumDistanceCalculation calculation, CalculationState state, double progress, string? message)
        {
            calculation.CalculationState = state;
            calculation.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            calculation.CalculationMessage = message;
        }

        private bool UpdateCalculationState(Guid id, CalculationState state, double progress, string? message)
        {
            TrajectoryMinimumDistanceCalculation? calculation = GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false);
            if (calculation == null)
            {
                return false;
            }

            MarkCalculationState(calculation, state, progress, message);
            return InsertOrUpdateTrajectoryMinimumDistanceCalculation(calculation, true, null, replaceResultChunks: false);
        }

        private void DeleteResultChunks(Guid id)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            TrajectoryMinimumDistanceResultChunkStore.DeleteChunks(connection, transaction, id);
            transaction.Commit();
        }

        public int GetResultChunkCount(Guid id) => TrajectoryMinimumDistanceResultChunkStore.GetChunkCount(_logger, _connectionManager, id);

        public TrajectoryMinimumDistanceResultChunk? GetResultChunk(Guid id, int chunkIndex) =>
            TrajectoryMinimumDistanceResultChunkStore.GetChunk(_logger, _connectionManager, id, chunkIndex);

        public List<TrajectoryMinimumDistanceResult>? GetResultListByTrajectoryMinimumDistanceCalculationId(Guid id) =>
            TrajectoryMinimumDistanceResultChunkStore.GetResults(_logger, _connectionManager, id);

        private static void SetGlobalMinimum(TrajectoryMinimumDistanceCalculation calculation)
        {
            TrajectoryMinimumDistanceResult? bestClearance = calculation.ResultList?
                .Where(result => result.ClearanceDistance.HasValue)
                .MinBy(result => result.ClearanceDistance!.Value);
            TrajectoryMinimumDistanceResult? bestCenter = calculation.ResultList?
                .Where(result => result.CenterToCenterDistance.HasValue)
                .MinBy(result => result.CenterToCenterDistance!.Value);

            TrajectoryMinimumDistanceResult? best = bestClearance ?? bestCenter;
            calculation.GlobalMinimumClearanceDistance = bestClearance?.ClearanceDistance;
            calculation.GlobalMinimumCenterToCenterDistance = bestCenter?.CenterToCenterDistance;
            calculation.GlobalMinimumReferenceMD = best?.ReferenceMD;
            calculation.GlobalMinimumComparisonTrajectoryID = best?.ComparisonTrajectoryID;
            calculation.GlobalMinimumComparisonMD = best?.ComparisonMD;
            calculation.GlobalMinimumToolface = best?.Toolface;
            calculation.GlobalMinimumIsGravity = best?.IsGravity ?? false;
        }

        private static List<TrajectoryMinimumDistanceIntervalResult> CalculateIntervalResults(
            List<MinimumDistanceReferenceInterval>? intervals,
            List<TrajectoryMinimumDistanceResult>? results)
        {
            if (intervals is not { Count: > 0 } || results is not { Count: > 0 })
            {
                return [];
            }

            List<TrajectoryMinimumDistanceIntervalResult> intervalResults = [];
            foreach (MinimumDistanceReferenceInterval interval in intervals.Where(IsValidInterval))
            {
                foreach (IGrouping<Guid, TrajectoryMinimumDistanceResult> group in results
                    .Where(result => result.ComparisonTrajectoryID.HasValue &&
                                     !result.IsAdaptiveRefinementSample &&
                                     result.ReferenceMD is double md &&
                                     md >= interval.StartMD!.Value &&
                                     md <= interval.EndMD!.Value)
                    .GroupBy(result => result.ComparisonTrajectoryID!.Value))
                {
                    List<double> centerDistances = group
                        .Select(result => result.CenterToCenterDistance)
                        .Where(IsDefined)
                        .Select(value => value!.Value)
                        .ToList();
                    List<double> clearanceDistances = group
                        .Select(result => result.ClearanceDistance)
                        .Where(IsDefined)
                        .Select(value => value!.Value)
                        .ToList();

                    intervalResults.Add(new TrajectoryMinimumDistanceIntervalResult
                    {
                        IntervalID = interval.ID,
                        IntervalName = interval.Name,
                        StartMD = interval.StartMD,
                        EndMD = interval.EndMD,
                        ComparisonTrajectoryID = group.Key,
                        SampleCount = group.Count(),
                        AverageCenterToCenterDistance = Mean(centerDistances),
                        StandardDeviationCenterToCenterDistance = StandardDeviation(centerDistances),
                        AverageClearanceDistance = Mean(clearanceDistances),
                        StandardDeviationClearanceDistance = StandardDeviation(clearanceDistances)
                    });
                }
            }

            return intervalResults;
        }

        private static bool IsValidInterval(MinimumDistanceReferenceInterval interval) =>
            interval.StartMD is double start &&
            interval.EndMD is double end &&
            IsDefined(start) &&
            IsDefined(end) &&
            end > start;

        private static bool IsDefined(double? value) => value is double defined && IsDefined(defined);
        private static bool IsDefined(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double? Mean(List<double> values) => values.Count == 0 ? null : values.Average();

        private static double? StandardDeviation(List<double> values)
        {
            if (values.Count < 2)
            {
                return null;
            }

            double mean = values.Average();
            double variance = values.Sum(value => Math.Pow(value - mean, 2.0)) / (values.Count - 1);
            return Math.Sqrt(variance);
        }

    }
}
