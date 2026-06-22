using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class SurveyRunManager
    {
        private static SurveyRunManager? _instance;
        private readonly ILogger<SurveyRunManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private const int DefaultSurveyMeasurementChunkSize = 5000;
        private const string SurveyStationOwnerType = "SurveyRun";

        private SurveyRunManager(ILogger<SurveyRunManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static SurveyRunManager GetInstance(ILogger<SurveyRunManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new SurveyRunManager(logger, connectionManager);
            return _instance;
        }

        private static SurveyRunLight CreateDataLightInstance(SurveyRun surveyRun)
        {
            return new SurveyRunLight
            {
                MetaInfo = surveyRun.MetaInfo,
                Name = surveyRun.Name,
                Description = surveyRun.Description,
                CreationDate = surveyRun.CreationDate,
                LastModificationDate = surveyRun.LastModificationDate,
                FieldID = surveyRun.FieldID,
                ClusterID = surveyRun.ClusterID,
                WellID = surveyRun.WellID,
                WellBoreID = surveyRun.WellBoreID,
                SurveyInstrumentID = surveyRun.SurveyInstrumentID,
                SurveyRunType = surveyRun.SurveyRunType,
                CalculationType = surveyRun.CalculationType,
                ParentSurveyRunID = surveyRun.ParentSurveyRunID,
                CalculationState = surveyRun.CalculationState,
                CalculationProgress = surveyRun.CalculationProgress,
                CalculationMessage = surveyRun.CalculationMessage
            };
        }

        public List<Guid>? GetAllSurveyRunId()
        {
            List<Guid> ids = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM SurveyRunTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    ids.Add(Guid.Parse(reader.GetString(0)));
                }
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from SurveyRunTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllSurveyRunMetaInfo()
        {
            List<MetaInfo?> metaInfos = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM SurveyRunTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from SurveyRunTable");
                return null;
            }
        }

        public SurveyRun? GetSurveyRunById(Guid id, bool includeMeasurements = false, bool includeCalculatedStations = true)
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
            command.CommandText = "SELECT SurveyRun FROM SurveyRunTable WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRun? surveyRun = JsonSerializer.Deserialize<SurveyRun>(reader.GetString(0), JsonSettings.Options);
                    if (surveyRun?.MetaInfo?.ID != id)
                    {
                        throw new SqliteException("SQLite database corrupted: returned SurveyRun has the wrong ID.", 1);
                    }
                    if (includeMeasurements && surveyRun != null)
                    {
                        surveyRun.SurveyMeasurementList = GetSurveyMeasurementListBySurveyRunId(id);
                    }
                    if (includeCalculatedStations && surveyRun != null)
                    {
                        surveyRun.SurveyStationList ??= GetSurveyStationListBySurveyRunId(id);
                    }
                    return surveyRun;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get the SurveyRun with the given ID from SurveyRunTable");
            }
            return null;
        }

        public List<SurveyRun>? GetAllSurveyRun(Guid? fieldId = null, Guid? clusterId = null, Guid? wellId = null, Guid? wellBoreId = null, Guid? surveyInstrumentId = null, SurveyRunType? surveyRunType = null)
        {
            List<SurveyRun> values = [];
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRun FROM SurveyRunTable" + BuildFilterClause(fieldId, clusterId, wellId, wellBoreId, surveyInstrumentId, surveyRunType);
            AddFilterParameters(command, fieldId, clusterId, wellId, wellBoreId, surveyInstrumentId, surveyRunType);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRun? surveyRun = JsonSerializer.Deserialize<SurveyRun>(reader.GetString(0), JsonSettings.Options);
                    if (surveyRun != null)
                    {
                        surveyRun.SurveyStationList ??= GetSurveyStationListBySurveyRunId(surveyRun.MetaInfo!.ID);
                        values.Add(surveyRun);
                    }
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SurveyRun from SurveyRunTable");
                return null;
            }
        }

        public List<SurveyRunLight>? GetAllSurveyRunLight(Guid? fieldId = null, Guid? clusterId = null, Guid? wellId = null, Guid? wellBoreId = null, Guid? surveyInstrumentId = null, SurveyRunType? surveyRunType = null)
        {
            return GetAllSurveyRun(fieldId, clusterId, wellId, wellBoreId, surveyInstrumentId, surveyRunType)?
                .Select(CreateDataLightInstance)
                .ToList();
        }

        public Task<bool> AddSurveyRun(SurveyRun? surveyRun)
        {
            if (!ValidateSurveyRunMetadata(surveyRun))
            {
                return Task.FromResult(false);
            }
            if (GetSurveyRunById(surveyRun!.MetaInfo!.ID) != null)
            {
                _logger.LogWarning("Impossible to post SurveyRun. ID already found in database.");
                return Task.FromResult(false);
            }

            List<SurveyMeasurement>? measurements = null;
            if (HasInlineMeasurementInput(surveyRun) &&
                (!TryPrepareSurveyMeasurementInput(surveyRun, out measurements) || measurements is not { Count: > 0 }))
            {
                return Task.FromResult(false);
            }
            if (measurements is { Count: > 0 })
            {
                surveyRun.SurveyMeasurementList = measurements;
            }

            MarkCalculationState(surveyRun, CalculationState.Running, 0.0, "Calculation queued");
            bool saved = InsertOrUpdateSurveyRun(surveyRun, false);
            if (saved)
            {
                _ = Task.Run(() => RecalculateSurveyRunAsync(surveyRun.MetaInfo!.ID));
            }

            return Task.FromResult(saved);
        }

        public Task<bool> UpdateSurveyRunById(Guid id, SurveyRun? surveyRun)
        {
            if (id == Guid.Empty || surveyRun?.MetaInfo?.ID != id || !ValidateSurveyRunMetadata(surveyRun))
            {
                _logger.LogWarning("The SurveyRun or its ID is null, empty, or inconsistent");
                return Task.FromResult(false);
            }

            if (HasInlineMeasurementInput(surveyRun))
            {
                if (!TryPrepareSurveyMeasurementInput(surveyRun, out List<SurveyMeasurement>? measurements) || measurements is not { Count: > 0 })
                {
                    return Task.FromResult(false);
                }
                surveyRun.SurveyMeasurementList = measurements;
            }
            else if (GetSurveyMeasurementChunkCount(id) > 0)
            {
                List<SurveyMeasurement>? measurements = GetSurveyMeasurementListBySurveyRunId(id);
                if (measurements is { Count: > 0 })
                {
                    surveyRun.SurveyMeasurementList = measurements;
                }
            }
            else if (GetSurveyRunById(id) is { } existingSurveyRun)
            {
                surveyRun.TieInPoint ??= existingSurveyRun.TieInPoint;
                surveyRun.SurveyStationList ??= existingSurveyRun.SurveyStationList;
            }

            surveyRun.LastModificationDate = DateTimeOffset.UtcNow;
            MarkCalculationState(surveyRun, CalculationState.Running, 0.0, "Calculation queued");
            bool saved = InsertOrUpdateSurveyRun(surveyRun, true);
            if (saved)
            {
                _ = Task.Run(() => RecalculateSurveyRunAsync(id));
            }

            return Task.FromResult(saved);
        }

        public bool DeleteSurveyRunById(Guid id)
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
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                SurveyStationChunkStore.DeleteChunks(connection, transaction, id, SurveyStationOwnerType);
                command.CommandText = "DELETE FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID = @id";
                command.Parameters.AddWithValue("@id", id.ToString());
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM SurveyRunTable WHERE ID = @id";
                command.Parameters.AddWithValue("@id", id.ToString());
                bool success = command.ExecuteNonQuery() == 1;
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
                _logger.LogError(ex, "Impossible to delete the SurveyRun of given ID from SurveyRunTable");
                return false;
            }
        }

        private static bool HasInlineMeasurementInput(SurveyRun surveyRun)
        {
            return surveyRun.SurveyMeasurementList is { Count: > 0 } ||
                surveyRun.SurveyStationList is { Count: > 0 };
        }

        private bool ValidateSurveyRunMetadata(SurveyRun? surveyRun)
        {
            if (surveyRun?.MetaInfo == null ||
                surveyRun.MetaInfo.ID == Guid.Empty ||
                surveyRun.WellBoreID == Guid.Empty ||
                surveyRun.SurveyInstrumentID == Guid.Empty)
            {
                _logger.LogWarning("The SurveyRun or its required IDs are null or empty");
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateAndPrepareSurveyRunAsync(SurveyRun? surveyRun)
        {
            if (!ValidateSurveyRunMetadata(surveyRun) || surveyRun == null ||
                GetSurveyMeasurementList(surveyRun) is not { Count: > 0 } measurements)
            {
                _logger.LogWarning("The SurveyRun, its IDs, or its survey measurements are null or empty");
                return false;
            }

            if (!ValidateSurveyMeasurementSequence(measurements, true))
            {
                return false;
            }

            surveyRun.SurveyMeasurementList = measurements;
            surveyRun.SurveyStationList = null;

            if (!await ResolveTieInPointAsync(surveyRun))
            {
                return false;
            }

            if (!surveyRun.Calculate())
            {
                _logger.LogWarning("Impossible to calculate the SurveyRun");
                return false;
            }

            if (!await CalculateSurveyRunUncertaintyAsync(surveyRun))
            {
                return false;
            }

            return true;
        }

        private bool TryPrepareSurveyMeasurementInput(SurveyRun surveyRun, out List<SurveyMeasurement>? measurements)
        {
            measurements = GetSurveyMeasurementList(surveyRun);
            if (measurements is not { Count: > 0 })
            {
                _logger.LogWarning("The SurveyRun survey measurements are null or empty");
                return false;
            }

            if (!ValidateSurveyMeasurementSequence(measurements, true))
            {
                return false;
            }

            return true;
        }

        private async Task RecalculateSurveyRunAsync(Guid surveyRunId)
        {
            try
            {
                UpdateSurveyRunCalculationState(surveyRunId, CalculationState.Running, 0.05, "Preparing survey run calculation");
                SurveyRun? surveyRun = GetSurveyRunById(surveyRunId, includeMeasurements: true, includeCalculatedStations: false);
                if (surveyRun == null)
                {
                    return;
                }

                surveyRun.SurveyMeasurementList ??= GetSurveyMeasurementListBySurveyRunId(surveyRunId);
                if (!await ValidateAndPrepareSurveyRunAsync(surveyRun))
                {
                    UpdateSurveyRunCalculationState(surveyRunId, CalculationState.Failed, 0.0, "Survey run calculation failed");
                    DeleteSurveyStationChunks(surveyRunId);
                    return;
                }

                MarkCalculationState(surveyRun, CalculationState.Completed, 1.0, null);
                surveyRun.LastModificationDate = DateTimeOffset.UtcNow;
                if (!InsertOrUpdateSurveyRun(surveyRun, true))
                {
                    UpdateSurveyRunCalculationState(surveyRunId, CalculationState.Failed, 0.0, "Survey run calculation failed while saving");
                    DeleteSurveyStationChunks(surveyRunId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during background SurveyRun calculation");
                UpdateSurveyRunCalculationState(surveyRunId, CalculationState.Failed, 0.0, "Survey run calculation failed");
                DeleteSurveyStationChunks(surveyRunId);
            }
        }

        private async Task<bool> CalculateSurveyRunUncertaintyAsync(SurveyRun surveyRun)
        {
            if (surveyRun.SurveyStationList is not { Count: > 0 } stations)
            {
                _logger.LogWarning("The SurveyRun has no calculated survey stations for uncertainty calculation");
                return false;
            }

            NORCE.Drilling.Trajectory.ModelShared.SurveyInstrument? surveyInstrument;
            try
            {
                surveyInstrument = await APIUtils.ClientSurveyInstrument.GetSurveyInstrumentByIdAsync(surveyRun.SurveyInstrumentID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible to retrieve the SurveyInstrument for SurveyRun {SurveyRunId}", surveyRun.MetaInfo?.ID);
                return false;
            }

            if (surveyInstrument == null)
            {
                _logger.LogWarning("The SurveyInstrument for SurveyRun {SurveyRunId} can not be retrieved", surveyRun.MetaInfo?.ID);
                return false;
            }

            OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument surveyTool = ConvertSurveyInstrument(surveyInstrument);
            foreach (SurveyStation station in stations)
            {
                station.SurveyTool = surveyTool;
            }

            try
            {
                return surveyInstrument.ModelType switch
                {
                    NORCE.Drilling.Trajectory.ModelShared.SurveyInstrumentModelType.MWD_WolffDeWardt or
                    NORCE.Drilling.Trajectory.ModelShared.SurveyInstrumentModelType.Gyro_WolffDeWardt =>
                        CovarianceCalculatorWolffDeWardt.Calculate(stations),

                    NORCE.Drilling.Trajectory.ModelShared.SurveyInstrumentModelType.MWD_ISCWSA or
                    NORCE.Drilling.Trajectory.ModelShared.SurveyInstrumentModelType.Gyro_ISCWSA =>
                        CovarianceCalculatorISCWSA.Calculate(stations),

                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible to calculate covariance for SurveyRun {SurveyRunId}", surveyRun.MetaInfo?.ID);
                return false;
            }
        }

        private static OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument ConvertSurveyInstrument(NORCE.Drilling.Trajectory.ModelShared.SurveyInstrument surveyInstrument)
        {
            string data = JsonSerializer.Serialize(surveyInstrument, JsonSettings.Options);
            return JsonSerializer.Deserialize<OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument>(data, JsonSettings.Options)
                ?? new OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument();
        }

        private bool ValidateSurveyMeasurementSequence(List<SurveyMeasurement> measurements, bool requireStrictlyIncreasing)
        {
            double? previousMd = null;
            foreach (SurveyMeasurement? measurement in measurements)
            {
                if (measurement == null)
                {
                    _logger.LogWarning("The SurveyRun contains a null survey measurement");
                    return false;
                }

                double? md = measurement.MD;
                if (md is not { } definedMd || !Numeric.IsDefined(definedMd))
                {
                    _logger.LogWarning("The SurveyRun contains a survey measurement without a defined measured depth");
                    return false;
                }
                if (measurement.Inclination is not { } inclination || !Numeric.IsDefined(inclination) ||
                    measurement.Azimuth is not { } azimuth || !Numeric.IsDefined(azimuth))
                {
                    _logger.LogWarning("The SurveyRun contains a survey measurement without defined inclination or azimuth");
                    return false;
                }
                if (requireStrictlyIncreasing && previousMd is { } previous && !Numeric.GT(definedMd, previous))
                {
                    _logger.LogWarning("The SurveyRun survey measurements must be ordered by strictly increasing measured depth");
                    return false;
                }
                previousMd = definedMd;
            }

            return true;
        }

        private static List<SurveyMeasurement>? GetSurveyMeasurementList(SurveyRun surveyRun)
        {
            if (surveyRun.SurveyMeasurementList is { Count: > 0 } measurements)
            {
                return measurements
                    .Where(measurement => measurement != null)
                    .OrderBy(measurement => measurement.MD)
                    .ToList();
            }

            return surveyRun.SurveyStationList?
                .Where(station => station != null)
                .Select(SurveyMeasurement.FromSurveyStation)
                .OrderBy(measurement => measurement.MD)
                .ToList();
        }

        private async Task<bool> ResolveTieInPointAsync(SurveyRun surveyRun)
        {
            (SurveyStation? wellheadTieInPoint, NORCE.Drilling.Trajectory.ModelShared.WellBore? wellBore, string message) = await APIUtils.GetReferencePointAsync(surveyRun.WellBoreID);
            if (wellheadTieInPoint is not { } definedWellheadTieInPoint ||
                (definedWellheadTieInPoint.MD ?? definedWellheadTieInPoint.Abscissa) is not { } wellheadMd)
            {
                _logger.LogWarning("Impossible to retrieve wellhead tie-in point for SurveyRun: {Message}", message);
                return false;
            }

            double firstMd = surveyRun.SurveyMeasurementList!
                .Select(measurement => measurement.MD)
                .Where(md => md.HasValue)
                .Select(md => md!.Value)
                .Min();

            if (wellBore?.IsSidetrack == true)
            {
                if (wellBore.TieInPointAlongHoleDepth?.GaussianValue?.Mean is not { } parentTieInMd ||
                    !Numeric.IsDefined(parentTieInMd))
                {
                    _logger.LogWarning("The sidetrack SurveyRun must define a valid tie-in point along the parent wellbore");
                    return false;
                }

                return ResolveParentSurveyRunTieInPoint(surveyRun, parentTieInMd, firstMd);
            }

            if (Numeric.LE(firstMd, wellheadMd))
            {
                surveyRun.ParentSurveyRunID = null;
                surveyRun.TieInPoint = definedWellheadTieInPoint;
                return true;
            }

            return ResolveParentSurveyRunTieInPoint(surveyRun, firstMd, firstMd);
        }

        private bool ResolveParentSurveyRunTieInPoint(SurveyRun surveyRun, double parentTieInMd, double surveyRunStartMd)
        {
            if (surveyRun.ParentSurveyRunID is not Guid parentSurveyRunId || parentSurveyRunId == Guid.Empty)
            {
                _logger.LogWarning("The SurveyRun must define a parent SurveyRun");
                return false;
            }
            if (parentSurveyRunId == surveyRun.MetaInfo!.ID)
            {
                _logger.LogWarning("The SurveyRun can not reference itself as parent SurveyRun");
                return false;
            }

            SurveyRun? parentSurveyRun = GetSurveyRunById(parentSurveyRunId);
            if (parentSurveyRun?.SurveyStationList is not { Count: > 1 } parentStations)
            {
                _logger.LogWarning("The parent SurveyRun is missing or has insufficient survey stations");
                return false;
            }

            SurveyStation? tieInPoint = CreateParentSurveyRunTieIn(parentStations, parentTieInMd, surveyRunStartMd);
            if (tieInPoint == null)
            {
                _logger.LogWarning("The parent SurveyRun does not cover or end close enough to the SurveyRun tie-in MD");
                return false;
            }

            surveyRun.TieInPoint = tieInPoint;
            return true;
        }

        private static SurveyStation? CreateParentSurveyRunTieIn(List<SurveyStation> parentStations, double parentTieInMd, double surveyRunStartMd)
        {
            List<SurveyStation> sortedStations = parentStations
                .Where(station => (station.MD ?? station.Abscissa) is { } md && Numeric.IsDefined(md))
                .OrderBy(station => station.MD ?? station.Abscissa)
                .ToList();

            if (sortedStations.Count < 2)
            {
                return null;
            }

            double firstParentMd = sortedStations.First().MD ?? sortedStations.First().Abscissa!.Value;
            double lastParentMd = sortedStations.Last().MD ?? sortedStations.Last().Abscissa!.Value;
            if (Numeric.GE(parentTieInMd, firstParentMd) && Numeric.LE(parentTieInMd, lastParentMd))
            {
                return SurveyStation.InterpolateAtAbscissa(sortedStations, parentTieInMd, out SurveyStation? tieInPoint)
                    ? CreateSurveyRunLocalTieInPoint(tieInPoint, surveyRunStartMd)
                    : null;
            }

            const double maxTieInGap = 10.0;
            if (Numeric.GT(parentTieInMd, lastParentMd) && Numeric.LE(parentTieInMd - lastParentMd, maxTieInGap))
            {
                SurveyStation tieInPoint = new(sortedStations.Last())
                {
                    MD = parentTieInMd,
                    Abscissa = parentTieInMd
                };
                return CreateSurveyRunLocalTieInPoint(tieInPoint, surveyRunStartMd);
            }

            return null;
        }

        private static SurveyStation? CreateSurveyRunLocalTieInPoint(SurveyStation? parentTieInPoint, double surveyRunStartMd)
        {
            return parentTieInPoint == null
                ? null
                : new SurveyStation(parentTieInPoint)
                {
                    MD = surveyRunStartMd,
                    Abscissa = surveyRunStartMd
                };
        }

        private bool InsertOrUpdateSurveyRun(SurveyRun surveyRun, bool update)
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
                List<SurveyMeasurement>? measurementList = surveyRun.SurveyMeasurementList;
                List<SurveyStation>? calculatedStationList = surveyRun.SurveyStationList;
                surveyRun.SurveyMeasurementList = null;
                surveyRun.SurveyStationList = null;
                string metaInfo = JsonSerializer.Serialize(surveyRun.MetaInfo, JsonSettings.Options);
                string? creationDate = surveyRun.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = surveyRun.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string data = JsonSerializer.Serialize(surveyRun, JsonSettings.Options);
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                if (update)
                {
                    command.CommandText = "UPDATE SurveyRunTable SET " +
                        "MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, " +
                        "FieldID = @fieldId, ClusterID = @clusterId, WellID = @wellId, WellBoreID = @wellBoreId, " +
                        "SurveyInstrumentID = @surveyInstrumentId, SurveyRunType = @surveyRunType, CalculationType = @calculationType, ParentSurveyRunID = @parentSurveyRunId, " +
                        "CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, SurveyRun = @surveyRun WHERE ID = @id";
                }
                else
                {
                    command.CommandText = "INSERT INTO SurveyRunTable " +
                        "(ID, MetaInfo, CreationDate, LastModificationDate, FieldID, ClusterID, WellID, WellBoreID, SurveyInstrumentID, SurveyRunType, CalculationType, ParentSurveyRunID, CalculationState, CalculationProgress, CalculationMessage, SurveyRun) " +
                        "VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @fieldId, @clusterId, @wellId, @wellBoreId, @surveyInstrumentId, @surveyRunType, @calculationType, @parentSurveyRunId, @calculationState, @calculationProgress, @calculationMessage, @surveyRun)";
                }

                command.Parameters.AddWithValue("@id", surveyRun.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@fieldId", ToSqlValue(surveyRun.FieldID));
                command.Parameters.AddWithValue("@clusterId", ToSqlValue(surveyRun.ClusterID));
                command.Parameters.AddWithValue("@wellId", ToSqlValue(surveyRun.WellID));
                command.Parameters.AddWithValue("@wellBoreId", surveyRun.WellBoreID.ToString());
                command.Parameters.AddWithValue("@surveyInstrumentId", surveyRun.SurveyInstrumentID.ToString());
                command.Parameters.AddWithValue("@surveyRunType", surveyRun.SurveyRunType.ToString());
                command.Parameters.AddWithValue("@calculationType", surveyRun.CalculationType.ToString());
                command.Parameters.AddWithValue("@parentSurveyRunId", ToSqlValue(surveyRun.ParentSurveyRunID));
                command.Parameters.AddWithValue("@calculationState", surveyRun.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", surveyRun.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)surveyRun.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@surveyRun", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success && measurementList is { Count: > 0 })
                {
                    success = ReplaceSurveyMeasurementChunks(connection, transaction, surveyRun.MetaInfo!.ID, measurementList);
                }
                if (success)
                {
                    success = SurveyStationChunkStore.ReplaceChunks(connection, transaction, surveyRun.MetaInfo!.ID, SurveyStationOwnerType, calculatedStationList);
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
                _logger.LogError(ex, "Impossible to save the SurveyRun into SurveyRunTable");
                return false;
            }
        }

        public int GetSurveyMeasurementChunkCount(Guid surveyRunId)
        {
            if (surveyRunId == Guid.Empty)
            {
                return 0;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return 0;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID = @surveyRunId";
            command.Parameters.AddWithValue("@surveyRunId", surveyRunId.ToString());
            try
            {
                object? result = command.ExecuteScalar();
                return result is long count ? (int)count : 0;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SurveyRun measurement chunk count");
                return 0;
            }
        }

        public SurveyMeasurementChunk? GetSurveyMeasurementChunk(Guid surveyRunId, int chunkIndex)
        {
            if (surveyRunId == Guid.Empty || chunkIndex < 0)
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
            command.CommandText = "SELECT SurveyMeasurementChunk FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID = @surveyRunId AND ChunkIndex = @chunkIndex";
            command.Parameters.AddWithValue("@surveyRunId", surveyRunId.ToString());
            command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    return JsonSerializer.Deserialize<SurveyMeasurementChunk>(reader.GetString(0), JsonSettings.Options);
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SurveyRun measurement chunk");
            }
            return null;
        }

        public bool PutSurveyMeasurementChunk(Guid surveyRunId, int chunkIndex, SurveyMeasurementChunk? chunk)
        {
            if (surveyRunId == Guid.Empty ||
                chunkIndex < 0 ||
                chunk == null ||
                chunk.SurveyRunID != surveyRunId ||
                chunk.ChunkIndex != chunkIndex ||
                chunk.SurveyMeasurementList is not { Count: > 0 } measurements ||
                GetSurveyRunById(surveyRunId) == null ||
                !ValidateSurveyMeasurementSequence(measurements, true))
            {
                _logger.LogWarning("The SurveyRun measurement chunk is null, invalid, or inconsistent");
                return false;
            }

            chunk.UpdateMetadata();

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            try
            {
                return UpsertSurveyMeasurementChunk(connection, null, chunk);
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to save SurveyRun measurement chunk");
                return false;
            }
        }

        public bool DeleteSurveyMeasurementChunks(Guid surveyRunId)
        {
            if (surveyRunId == Guid.Empty || GetSurveyRunById(surveyRunId) == null)
            {
                return false;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            try
            {
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID = @surveyRunId";
                command.Parameters.AddWithValue("@surveyRunId", surveyRunId.ToString());
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to delete SurveyRun measurement chunks");
                return false;
            }
        }

        public Task<bool> CommitSurveyMeasurementChunks(Guid surveyRunId)
        {
            SurveyRun? surveyRun = GetSurveyRunById(surveyRunId);
            if (surveyRun == null)
            {
                return Task.FromResult(false);
            }

            List<SurveyMeasurement>? measurements = GetSurveyMeasurementListBySurveyRunId(surveyRunId);
            if (measurements is not { Count: > 0 })
            {
                _logger.LogWarning("Impossible to commit SurveyRun measurement chunks: no measurements found");
                return Task.FromResult(false);
            }

            surveyRun.SurveyMeasurementList = measurements;
            MarkCalculationState(surveyRun, CalculationState.Running, 0.0, "Calculation queued");
            surveyRun.LastModificationDate = DateTimeOffset.UtcNow;
            bool saved = InsertOrUpdateSurveyRun(surveyRun, true);
            if (saved)
            {
                _ = Task.Run(() => RecalculateSurveyRunAsync(surveyRunId));
            }

            return Task.FromResult(saved);
        }

        public List<SurveyMeasurement>? GetSurveyMeasurementListBySurveyRunId(Guid surveyRunId)
        {
            if (surveyRunId == Guid.Empty)
            {
                return null;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            List<SurveyMeasurement> measurements = [];
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyMeasurementChunk FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID = @surveyRunId ORDER BY ChunkIndex";
            command.Parameters.AddWithValue("@surveyRunId", surveyRunId.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyMeasurementChunk? chunk = JsonSerializer.Deserialize<SurveyMeasurementChunk>(reader.GetString(0), JsonSettings.Options);
                    if (chunk?.SurveyMeasurementList is { Count: > 0 } chunkMeasurements)
                    {
                        measurements.AddRange(chunkMeasurements);
                    }
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get SurveyRun measurement chunks");
                return null;
            }

            return measurements.Count > 0 ? measurements : null;
        }

        public int GetSurveyStationChunkCount(Guid surveyRunId)
        {
            return SurveyStationChunkStore.GetChunkCount(_logger, _connectionManager, surveyRunId, SurveyStationOwnerType);
        }

        public SurveyStationChunk? GetSurveyStationChunk(Guid surveyRunId, int chunkIndex)
        {
            return SurveyStationChunkStore.GetChunk(_logger, _connectionManager, surveyRunId, SurveyStationOwnerType, chunkIndex);
        }

        public List<SurveyStation>? GetSurveyStationListBySurveyRunId(Guid surveyRunId)
        {
            return SurveyStationChunkStore.GetStations(_logger, _connectionManager, surveyRunId, SurveyStationOwnerType);
        }

        private static void MarkCalculationState(SurveyRun surveyRun, CalculationState state, double progress, string? message)
        {
            surveyRun.CalculationState = state;
            surveyRun.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
            surveyRun.CalculationMessage = message;
        }

        private bool UpdateSurveyRunCalculationState(Guid surveyRunId, CalculationState state, double progress, string? message)
        {
            SurveyRun? surveyRun = GetSurveyRunById(surveyRunId, includeMeasurements: false, includeCalculatedStations: false);
            if (surveyRun == null)
            {
                return false;
            }

            MarkCalculationState(surveyRun, state, progress, message);
            return UpdateSurveyRunRecordOnly(surveyRun);
        }

        private bool UpdateSurveyRunRecordOnly(SurveyRun surveyRun)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            try
            {
                surveyRun.SurveyMeasurementList = null;
                surveyRun.SurveyStationList = null;
                string data = JsonSerializer.Serialize(surveyRun, JsonSettings.Options);
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE SurveyRunTable SET CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, SurveyRun = @surveyRun WHERE ID = @id";
                command.Parameters.AddWithValue("@id", surveyRun.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@calculationState", surveyRun.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", surveyRun.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)surveyRun.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@surveyRun", data);
                return command.ExecuteNonQuery() == 1;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to update SurveyRun calculation state");
                return false;
            }
        }

        private void DeleteSurveyStationChunks(Guid surveyRunId)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            SurveyStationChunkStore.DeleteChunks(connection, transaction, surveyRunId, SurveyStationOwnerType);
            transaction.Commit();
        }

        private bool ReplaceSurveyMeasurementChunks(SqliteConnection connection, SqliteTransaction transaction, Guid surveyRunId, List<SurveyMeasurement> measurements)
        {
            SqliteCommand deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM SurveyRunMeasurementChunkTable WHERE SurveyRunID = @surveyRunId";
            deleteCommand.Parameters.AddWithValue("@surveyRunId", surveyRunId.ToString());
            deleteCommand.ExecuteNonQuery();

            int chunkIndex = 0;
            foreach (List<SurveyMeasurement> measurementChunk in measurements.Chunk(DefaultSurveyMeasurementChunkSize).Select(chunk => chunk.ToList()))
            {
                SurveyMeasurementChunk chunk = new()
                {
                    SurveyRunID = surveyRunId,
                    ChunkIndex = chunkIndex++,
                    SurveyMeasurementList = measurementChunk
                };
                chunk.UpdateMetadata();
                if (!UpsertSurveyMeasurementChunk(connection, transaction, chunk))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool UpsertSurveyMeasurementChunk(SqliteConnection connection, SqliteTransaction? transaction, SurveyMeasurementChunk chunk)
        {
            string chunkId = CreateSurveyMeasurementChunkId(chunk.SurveyRunID, chunk.ChunkIndex);
            string data = JsonSerializer.Serialize(chunk, JsonSettings.Options);
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO SurveyRunMeasurementChunkTable " +
                "(ID, SurveyRunID, ChunkIndex, MeasurementCount, StartMD, EndMD, SurveyMeasurementChunk) " +
                "VALUES (@id, @surveyRunId, @chunkIndex, @measurementCount, @startMd, @endMd, @surveyMeasurementChunk) " +
                "ON CONFLICT(ID) DO UPDATE SET " +
                "SurveyRunID = excluded.SurveyRunID, ChunkIndex = excluded.ChunkIndex, MeasurementCount = excluded.MeasurementCount, " +
                "StartMD = excluded.StartMD, EndMD = excluded.EndMD, SurveyMeasurementChunk = excluded.SurveyMeasurementChunk";
            command.Parameters.AddWithValue("@id", chunkId);
            command.Parameters.AddWithValue("@surveyRunId", chunk.SurveyRunID.ToString());
            command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("@measurementCount", chunk.MeasurementCount);
            command.Parameters.AddWithValue("@startMd", (object?)chunk.StartMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@endMd", (object?)chunk.EndMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@surveyMeasurementChunk", data);
            return command.ExecuteNonQuery() == 1;
        }

        private static string CreateSurveyMeasurementChunkId(Guid surveyRunId, int chunkIndex)
        {
            return $"{surveyRunId:N}:{chunkIndex:D10}";
        }

        private static object ToSqlValue(Guid? value)
        {
            return value is Guid guid && guid != Guid.Empty ? guid.ToString() : DBNull.Value;
        }

        private static string BuildFilterClause(Guid? fieldId, Guid? clusterId, Guid? wellId, Guid? wellBoreId, Guid? surveyInstrumentId, SurveyRunType? surveyRunType)
        {
            List<string> filters = [];
            if (fieldId is Guid definedFieldId && definedFieldId != Guid.Empty)
                filters.Add("FieldID = @fieldId");
            if (clusterId is Guid definedClusterId && definedClusterId != Guid.Empty)
                filters.Add("ClusterID = @clusterId");
            if (wellId is Guid definedWellId && definedWellId != Guid.Empty)
                filters.Add("WellID = @wellId");
            if (wellBoreId is Guid definedWellBoreId && definedWellBoreId != Guid.Empty)
                filters.Add("WellBoreID = @wellBoreId");
            if (surveyInstrumentId is Guid definedSurveyInstrumentId && definedSurveyInstrumentId != Guid.Empty)
                filters.Add("SurveyInstrumentID = @surveyInstrumentId");
            if (surveyRunType is SurveyRunType definedSurveyRunType)
                filters.Add("SurveyRunType = @surveyRunType");
            return filters.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", filters);
        }

        private static void AddFilterParameters(SqliteCommand command, Guid? fieldId, Guid? clusterId, Guid? wellId, Guid? wellBoreId, Guid? surveyInstrumentId, SurveyRunType? surveyRunType)
        {
            if (fieldId is Guid definedFieldId && definedFieldId != Guid.Empty)
                command.Parameters.AddWithValue("@fieldId", definedFieldId.ToString());
            if (clusterId is Guid definedClusterId && definedClusterId != Guid.Empty)
                command.Parameters.AddWithValue("@clusterId", definedClusterId.ToString());
            if (wellId is Guid definedWellId && definedWellId != Guid.Empty)
                command.Parameters.AddWithValue("@wellId", definedWellId.ToString());
            if (wellBoreId is Guid definedWellBoreId && definedWellBoreId != Guid.Empty)
                command.Parameters.AddWithValue("@wellBoreId", definedWellBoreId.ToString());
            if (surveyInstrumentId is Guid definedSurveyInstrumentId && definedSurveyInstrumentId != Guid.Empty)
                command.Parameters.AddWithValue("@surveyInstrumentId", definedSurveyInstrumentId.ToString());
            if (surveyRunType is SurveyRunType definedSurveyRunType)
                command.Parameters.AddWithValue("@surveyRunType", definedSurveyRunType.ToString());
        }
    }
}
