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
    public class SurveyRunMinimumDistanceCalculationManager
    {
        private static SurveyRunMinimumDistanceCalculationManager? _instance;
        private readonly ILogger<SurveyRunMinimumDistanceCalculationManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private readonly SurveyRunManager _surveyRunManager;

        private SurveyRunMinimumDistanceCalculationManager(ILogger<SurveyRunMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _surveyRunManager = SurveyRunManager.GetInstance(Microsoft.Extensions.Logging.Abstractions.NullLogger<SurveyRunManager>.Instance, connectionManager);
        }

        public static SurveyRunMinimumDistanceCalculationManager GetInstance(ILogger<SurveyRunMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new SurveyRunMinimumDistanceCalculationManager(logger, connectionManager);
            return _instance;
        }

        private static SurveyRunMinimumDistanceCalculationLight CreateLight(SurveyRunMinimumDistanceCalculation calculation) => new()
        {
            MetaInfo = calculation.MetaInfo,
            Name = calculation.Name,
            Description = calculation.Description,
            CreationDate = calculation.CreationDate,
            LastModificationDate = calculation.LastModificationDate,
            ReferenceSurveyRunID = calculation.ReferenceSurveyRunID,
            ComparisonSurveyRunIDList = calculation.ComparisonSurveyRunIDList,
            CalculationState = calculation.CalculationState,
            CalculationProgress = calculation.CalculationProgress,
            CalculationMessage = calculation.CalculationMessage,
            ResultCount = calculation.ResultCount,
            IntervalResultCount = calculation.IntervalResultCount
        };

        public List<Guid>? GetAllSurveyRunMinimumDistanceCalculationId()
        {
            List<Guid> ids = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM SurveyRunMinimumDistanceCalculationTable";
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
                _logger.LogError(ex, "Impossible to get survey run minimum distance calculation IDs");
                return null;
            }
        }

        public List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>? GetAllSurveyRunMinimumDistanceCalculationMetaInfo()
        {
            List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> metaInfos = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM SurveyRunMinimumDistanceCalculationTable";
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
                _logger.LogError(ex, "Impossible to get survey run minimum distance calculation MetaInfo");
                return null;
            }
        }

        public SurveyRunMinimumDistanceCalculation? GetSurveyRunMinimumDistanceCalculationById(Guid id, bool includeResults = true)
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
            command.CommandText = "SELECT SurveyRunMinimumDistanceCalculation FROM SurveyRunMinimumDistanceCalculationTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRunMinimumDistanceCalculation? calculation = JsonSerializer.Deserialize<SurveyRunMinimumDistanceCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (calculation?.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned SurveyRunMinimumDistanceCalculation has the wrong ID.", 1);
                    }

                    if (includeResults)
                    {
                        calculation.ResultList ??= GetResultListBySurveyRunMinimumDistanceCalculationId(id);
                    }

                    return calculation;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get survey run minimum distance calculation");
            }

            return null;
        }

        public List<SurveyRunMinimumDistanceCalculation?>? GetAllSurveyRunMinimumDistanceCalculation()
        {
            List<SurveyRunMinimumDistanceCalculation?> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRunMinimumDistanceCalculation FROM SurveyRunMinimumDistanceCalculationTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRunMinimumDistanceCalculation? value = JsonSerializer.Deserialize<SurveyRunMinimumDistanceCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (value?.MetaInfo?.ID is Guid id)
                    {
                        value.ResultList ??= GetResultListBySurveyRunMinimumDistanceCalculationId(id);
                    }

                    values.Add(value);
                }

                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get survey run minimum distance calculations");
                return null;
            }
        }

        public List<SurveyRunMinimumDistanceCalculationLight>? GetAllSurveyRunMinimumDistanceCalculationLight()
        {
            List<SurveyRunMinimumDistanceCalculationLight> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRunMinimumDistanceCalculation FROM SurveyRunMinimumDistanceCalculationTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRunMinimumDistanceCalculation? value = JsonSerializer.Deserialize<SurveyRunMinimumDistanceCalculation>(reader.GetString(0), JsonSettings.Options);
                    if (value != null)
                    {
                        values.Add(CreateLight(value));
                    }
                }

                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get survey run minimum distance calculation lights");
                return null;
            }
        }

        public Task<bool> AddSurveyRunMinimumDistanceCalculation(SurveyRunMinimumDistanceCalculation? calculation)
        {
            try
            {
                if (calculation?.MetaInfo?.ID is not Guid id || id == Guid.Empty || calculation.ReferenceSurveyRunID == Guid.Empty)
                {
                    return Task.FromResult(false);
                }

                if (GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false) != null)
                {
                    return Task.FromResult(false);
                }

                MarkCalculationState(calculation, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateSurveyRunMinimumDistanceCalculation(calculation, false, null, replaceResultChunks: false);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateSurveyRunMinimumDistanceCalculationAsync(id));
                }

                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding survey run minimum distance calculation");
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateSurveyRunMinimumDistanceCalculationById(Guid id, SurveyRunMinimumDistanceCalculation? calculation)
        {
            try
            {
                if (id == Guid.Empty || calculation?.MetaInfo?.ID != id || calculation.ReferenceSurveyRunID == Guid.Empty)
                {
                    return Task.FromResult(false);
                }

                calculation.LastModificationDate = DateTimeOffset.UtcNow;
                MarkCalculationState(calculation, CalculationState.Running, 0.0, "Calculation queued");
                bool saved = InsertOrUpdateSurveyRunMinimumDistanceCalculation(calculation, true, null, replaceResultChunks: true);
                if (saved)
                {
                    _ = Task.Run(() => RecalculateSurveyRunMinimumDistanceCalculationAsync(id));
                }

                return Task.FromResult(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating survey run minimum distance calculation");
                return Task.FromResult(false);
            }
        }

        public bool DeleteSurveyRunMinimumDistanceCalculationById(Guid id)
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
                SurveyRunMinimumDistanceResultChunkStore.DeleteChunks(connection, transaction, id);
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM SurveyRunMinimumDistanceCalculationTable WHERE ID = @id";
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
                _logger.LogError(ex, "Impossible to delete survey run minimum distance calculation");
                return false;
            }
        }

        private async Task RecalculateSurveyRunMinimumDistanceCalculationAsync(Guid id)
        {
            try
            {
                UpdateCalculationState(id, CalculationState.Running, 0.02, "Preparing survey runs");
                SurveyRunMinimumDistanceCalculation? calculation = GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false);
                if (calculation == null)
                {
                    return;
                }

                Model.SurveyRun? referenceSurveyRun = _surveyRunManager.GetSurveyRunById(calculation.ReferenceSurveyRunID, includeMeasurements: false, includeCalculatedStations: true);
                if (referenceSurveyRun?.SurveyStationList is not { Count: > 1 })
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Reference survey run has no calculated survey stations");
                    DeleteResultChunks(id);
                    return;
                }

                List<Model.SurveyRun> comparisonSurveyRuns = [];
                foreach (Guid comparisonId in calculation.ComparisonSurveyRunIDList?.Distinct().Where(x => x != Guid.Empty && x != calculation.ReferenceSurveyRunID) ?? [])
                {
                    Model.SurveyRun? comparison = _surveyRunManager.GetSurveyRunById(comparisonId, includeMeasurements: false, includeCalculatedStations: true);
                    if (comparison?.SurveyStationList is { Count: > 1 })
                    {
                        comparisonSurveyRuns.Add(comparison);
                    }
                }

                if (comparisonSurveyRuns.Count == 0)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "No valid comparison survey run was found");
                    DeleteResultChunks(id);
                    return;
                }

                UpdateCalculationState(id, CalculationState.Running, 0.08, "Preparing centerline segments");
                List<SurveyStation> referenceStations = CenterlineMinimumDistanceEngine.PrepareStationList(
                    referenceSurveyRun.SurveyStationList,
                    referenceSurveyRun.CalculationType,
                    calculation.MaximumChordArcDistance);
                if (referenceStations.Count < 2)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Reference survey run has no valid 3D segments");
                    DeleteResultChunks(id);
                    return;
                }

                List<CenterlineComparisonSource> comparisonSources = comparisonSurveyRuns
                    .Select(surveyRun => new CenterlineComparisonSource(
                        surveyRun.MetaInfo!.ID,
                        CenterlineMinimumDistanceEngine.PrepareStationList(
                            surveyRun.SurveyStationList,
                            surveyRun.CalculationType,
                            calculation.MaximumChordArcDistance)))
                    .Where(source => source.StationList.Count > 1)
                    .ToList();
                if (comparisonSources.Count == 0)
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Comparison survey runs have no valid 3D segments");
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

                List<SurveyRunMinimumDistanceResult> results = engineResults.Select(result => new SurveyRunMinimumDistanceResult
                {
                    ReferenceMD = result.ReferenceMD,
                    ReferenceTVD = result.ReferenceTVD,
                    ReferenceNorth = result.ReferenceNorth,
                    ReferenceEast = result.ReferenceEast,
                    ReferenceBoreholeDiameter = result.ReferenceBoreholeDiameter,
                    ComparisonSurveyRunID = result.SourceID,
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

                if (!InsertOrUpdateSurveyRunMinimumDistanceCalculation(calculation, true, results, replaceResultChunks: true))
                {
                    UpdateCalculationState(id, CalculationState.Failed, 0.0, "Calculation failed while saving results");
                    DeleteResultChunks(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during survey run minimum distance calculation");
                UpdateCalculationState(id, CalculationState.Failed, 0.0, "survey run minimum distance calculation failed");
                DeleteResultChunks(id);
            }
        }

        private bool InsertOrUpdateSurveyRunMinimumDistanceCalculation(
            SurveyRunMinimumDistanceCalculation calculation,
            bool update,
            List<SurveyRunMinimumDistanceResult>? results,
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
                    command.CommandText = "UPDATE SurveyRunMinimumDistanceCalculationTable SET " +
                        "MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, ReferenceSurveyRunID = @ReferenceSurveyRunID, " +
                        "CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, ResultCount = @resultCount, IntervalResultCount = @intervalResultCount, " +
                        "SurveyRunMinimumDistanceCalculation = @calculation WHERE ID = @id";
                }
                else
                {
                    command.CommandText = "INSERT INTO SurveyRunMinimumDistanceCalculationTable " +
                        "(ID, MetaInfo, CreationDate, LastModificationDate, ReferenceSurveyRunID, CalculationState, CalculationProgress, CalculationMessage, ResultCount, IntervalResultCount, SurveyRunMinimumDistanceCalculation) " +
                        "VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @ReferenceSurveyRunID, @calculationState, @calculationProgress, @calculationMessage, @resultCount, @intervalResultCount, @calculation)";
                }

                command.Parameters.AddWithValue("@id", calculation.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@ReferenceSurveyRunID", calculation.ReferenceSurveyRunID.ToString());
                command.Parameters.AddWithValue("@calculationState", calculation.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", calculation.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)calculation.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@resultCount", calculation.ResultCount);
                command.Parameters.AddWithValue("@intervalResultCount", calculation.IntervalResultCount);
                command.Parameters.AddWithValue("@calculation", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success && replaceResultChunks)
                {
                    success = SurveyRunMinimumDistanceResultChunkStore.ReplaceChunks(connection, transaction, calculation.MetaInfo.ID, results);
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
                _logger.LogError(ex, "Impossible to save survey run minimum distance calculation");
                return false;
            }
        }

        private static void MarkCalculationState(SurveyRunMinimumDistanceCalculation calculation, CalculationState state, double progress, string? message)
        {
            calculation.CalculationState = state;
            calculation.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            calculation.CalculationMessage = message;
        }

        private bool UpdateCalculationState(Guid id, CalculationState state, double progress, string? message)
        {
            SurveyRunMinimumDistanceCalculation? calculation = GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false);
            if (calculation == null)
            {
                return false;
            }

            MarkCalculationState(calculation, state, progress, message);
            return InsertOrUpdateSurveyRunMinimumDistanceCalculation(calculation, true, null, replaceResultChunks: false);
        }

        private void DeleteResultChunks(Guid id)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            SurveyRunMinimumDistanceResultChunkStore.DeleteChunks(connection, transaction, id);
            transaction.Commit();
        }

        public int GetResultChunkCount(Guid id) => SurveyRunMinimumDistanceResultChunkStore.GetChunkCount(_logger, _connectionManager, id);

        public SurveyRunMinimumDistanceResultChunk? GetResultChunk(Guid id, int chunkIndex) =>
            SurveyRunMinimumDistanceResultChunkStore.GetChunk(_logger, _connectionManager, id, chunkIndex);

        public List<SurveyRunMinimumDistanceResult>? GetResultListBySurveyRunMinimumDistanceCalculationId(Guid id) =>
            SurveyRunMinimumDistanceResultChunkStore.GetResults(_logger, _connectionManager, id);

        private static void SetGlobalMinimum(SurveyRunMinimumDistanceCalculation calculation)
        {
            SurveyRunMinimumDistanceResult? bestClearance = calculation.ResultList?
                .Where(result => result.ClearanceDistance.HasValue)
                .MinBy(result => result.ClearanceDistance!.Value);
            SurveyRunMinimumDistanceResult? bestCenter = calculation.ResultList?
                .Where(result => result.CenterToCenterDistance.HasValue)
                .MinBy(result => result.CenterToCenterDistance!.Value);

            SurveyRunMinimumDistanceResult? best = bestClearance ?? bestCenter;
            calculation.GlobalMinimumClearanceDistance = bestClearance?.ClearanceDistance;
            calculation.GlobalMinimumCenterToCenterDistance = bestCenter?.CenterToCenterDistance;
            calculation.GlobalMinimumReferenceMD = best?.ReferenceMD;
            calculation.GlobalMinimumComparisonSurveyRunID = best?.ComparisonSurveyRunID;
            calculation.GlobalMinimumComparisonMD = best?.ComparisonMD;
            calculation.GlobalMinimumToolface = best?.Toolface;
            calculation.GlobalMinimumIsGravity = best?.IsGravity ?? false;
        }

        private static List<SurveyRunMinimumDistanceIntervalResult> CalculateIntervalResults(
            List<MinimumDistanceReferenceInterval>? intervals,
            List<SurveyRunMinimumDistanceResult>? results)
        {
            if (intervals is not { Count: > 0 } || results is not { Count: > 0 })
            {
                return [];
            }

            List<SurveyRunMinimumDistanceIntervalResult> intervalResults = [];
            foreach (MinimumDistanceReferenceInterval interval in intervals.Where(IsValidInterval))
            {
                foreach (IGrouping<Guid, SurveyRunMinimumDistanceResult> group in results
                    .Where(result => result.ComparisonSurveyRunID.HasValue &&
                                     !result.IsAdaptiveRefinementSample &&
                                     result.ReferenceMD is double md &&
                                     md >= interval.StartMD!.Value &&
                                     md <= interval.EndMD!.Value)
                    .GroupBy(result => result.ComparisonSurveyRunID!.Value))
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

                    intervalResults.Add(new SurveyRunMinimumDistanceIntervalResult
                    {
                        IntervalID = interval.ID,
                        IntervalName = interval.Name,
                        StartMD = interval.StartMD,
                        EndMD = interval.EndMD,
                        ComparisonSurveyRunID = group.Key,
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
