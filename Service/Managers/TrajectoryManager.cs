using DWIS.API.DTO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.General.Statistics;
using Parlot.Fluent;
using SharpYaml.Serialization.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Managers
{

    /// <summary>
    /// A manager for Trajectory. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class TrajectoryManager
    {
        private static TrajectoryManager? _instance = null;
        private readonly ILogger<TrajectoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private const string SurveyStationOwnerType = "Trajectory";
        private const string SurveyRunStationOwnerType = "SurveyRun";

        private TrajectoryManager(ILogger<TrajectoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static TrajectoryManager GetInstance(ILogger<TrajectoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new TrajectoryManager(logger, connectionManager);
            return _instance;
        }

        public int Count
        {
            get
            {
                int count = 0;
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM TrajectoryTable";
                    try
                    {
                        using SqliteDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            count = (int)reader.GetInt64(0);
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to count records in the TrajectoryTable");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
                return count;
            }
        }

        public bool Clear()
        {
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                bool success = false;
                using var transaction = connection.BeginTransaction();
                try
                {
                    //empty TrajectoryTable
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM TrajectoryTable";
                    command.ExecuteNonQuery();

                    transaction.Commit();
                    success = true;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Impossible to clear the TrajectoryTable");
                }
                return success;
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }
        }

        public bool Contains(Guid guid)
        {
            int count = 0;
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM TrajectoryTable WHERE ID = '{guid}'";
                try
                {
                    using SqliteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        count = (int)reader.GetInt64(0);
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to count rows from TrajectoryTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        private static Model.TrajectoryLight CreateDataLightInstance(Model.Trajectory trajectory)
        {
            return new Model.TrajectoryLight()
            {
                MetaInfo = trajectory.MetaInfo,
                Name = trajectory.Name,
                Description = trajectory.Description,
                CreationDate = trajectory.CreationDate,
                LastModificationDate = trajectory.LastModificationDate,
                FieldID = trajectory.FieldID,
                ClusterID = trajectory.ClusterID,
                WellID = trajectory.WellID,
                WellBoreID = trajectory.WellBoreID,
                TrajectoryType = trajectory.TrajectoryType,
                IsDefinitive = trajectory.IsDefinitive,
                CalculationState = trajectory.CalculationState,
                CalculationProgress = trajectory.CalculationProgress,
                CalculationMessage = trajectory.CalculationMessage
            };
        }
        /// <summary>
        /// Returns the list of Guid of all Trajectory present in the microservice database 
        /// </summary>
        /// <returns>the list of Guid of all Trajectory present in the microservice database</returns>
        public List<Guid>? GetAllTrajectoryId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID FROM TrajectoryTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        Guid id = reader.GetGuid(0);
                        ids.Add(id);
                    }
                    _logger.LogInformation("Returning the list of ID of existing records from TrajectoryTable");
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from TrajectoryTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Trajectory present in the microservice database 
        /// </summary>
        /// <returns>the list of MetaInfo of all Trajectory present in the microservice database</returns>
        public List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>? GetAllTrajectoryMetaInfo()
        {
            List<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> metaInfos = new();
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo FROM TrajectoryTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string mInfo = reader.GetString(0);
                        OSDC.DotnetLibraries.General.DataManagement.MetaInfo? metaInfo = JsonSerializer.Deserialize<OSDC.DotnetLibraries.General.DataManagement.MetaInfo>(mInfo, JsonSettings.Options);
                        metaInfos.Add(metaInfo);
                    }
                    _logger.LogInformation("Returning the list of MetaInfo of existing records from TrajectoryTable");
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from TrajectoryTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the Trajectory identified by its Guid from the microservice database 
        /// </summary>
        /// <param name="trajId"></param>
        /// <returns>the Trajectory identified by its Guid from the microservice database</returns>
        public Model.Trajectory? GetTrajectoryById(Guid trajId, bool includeCalculatedStations = true)
        {
            if (!trajId.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.Trajectory? trajectory;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT Trajectory FROM TrajectoryTable WHERE ID = '{trajId}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);
                            if (trajectory != null && trajectory.MetaInfo != null && !trajectory.MetaInfo.ID.Equals(trajId))
                                throw new SqliteException("SQLite database corrupted: returned Trajectory is null or has been jsonified with the wrong ID.", 1);
                            if (includeCalculatedStations && trajectory != null)
                            {
                                trajectory.SurveyStationList ??= GetSurveyStationListByTrajectoryId(trajId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No Trajectory of given ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the Trajectory with the given ID from TrajectoryTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the Trajectory of given ID from TrajectoryTable");
                    return trajectory;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given Trajectory ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the Trajectory identified by the ID of the wellbore it is connected to from the microservice database 
        /// </summary>
        /// <param name="wellBoreId"></param>
        /// <returns>the Trajectory identified by the ID of the wellbore it is connected to from the microservice database</returns>
        public Model.Trajectory? GetTrajectoryByWellBoreId(Guid wellBoreId)
        {
            if (!wellBoreId.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.Trajectory? trajectory;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT Trajectory FROM TrajectoryTable WHERE WellBoreID = '{wellBoreId}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);
                            if (trajectory != null && trajectory.MetaInfo != null && !trajectory.WellBoreID.Equals(wellBoreId))
                                throw new SqliteException("SQLite database corrupted: returned Trajectory is null or its wellbore ID has been jsonified with the wrong ID.", 1);
                            if (trajectory?.MetaInfo?.ID is Guid trajectoryId)
                            {
                                trajectory.SurveyStationList ??= GetSurveyStationListByTrajectoryId(trajectoryId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No Trajectory with given wellbore ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the Trajectory with given wellbore ID from TrajectoryTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the Trajectory with given wellbore ID from TrajectoryTable");
                    return trajectory;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The trajectory of given wellbore ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the List of Trajectories identified by their Guid's from the microservice database 
        /// </summary>
        /// <param name="trajId"></param>
        /// <returns>the List of Trajectories identified by their Guid's from the microservice database</returns>
        public List<Model.Trajectory>? GetListOfTrajectoryById(List<Guid> trajIdList)
        {
            if (trajIdList == null || trajIdList.Count == 0)
            {
                return [];
            }

            if (!trajIdList.Contains(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Dictionary<Guid, Model.Trajectory> trajectoriesById = [];
                    var command = connection.CreateCommand();
                    string[] parameterNames = trajIdList
                        .Select((_, index) => $"@trajId{index}")
                        .ToArray();
                    command.CommandText = $"SELECT ID, Trajectory FROM TrajectoryTable WHERE ID IN ({string.Join(", ", parameterNames)})";

                    for (int i = 0; i < trajIdList.Count; i++)
                    {
                        command.Parameters.AddWithValue(parameterNames[i], trajIdList[i].ToString());
                    }

                    try
                    {
                        using var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                            {
                                throw new SqliteException("SQLite database corrupted: returned trajectory row contains null values.", 1);
                            }

                            Guid returnedId = Guid.Parse(reader.GetString(0));
                            string data = reader.GetString(1);
                            Model.Trajectory? trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);

                            if (trajectory == null || trajectory.MetaInfo == null || trajectory.MetaInfo.ID != returnedId || !trajIdList.Contains(returnedId))
                            {
                                throw new SqliteException("SQLite database corrupted: returned Trajectory is null or has been jsonified with the wrong ID.", 1);
                            }

                            trajectory.SurveyStationList ??= GetSurveyStationListByTrajectoryId(returnedId);
                            trajectoriesById[returnedId] = trajectory;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the TrajectoryList with the given ID's from TrajectoryTable");
                        return null;
                    }

                    List<Model.Trajectory> trajectoryList = [];
                    foreach (Guid requestedId in trajIdList)
                    {
                        if (trajectoriesById.TryGetValue(requestedId, out Model.Trajectory? trajectory))
                        {
                            trajectoryList.Add(trajectory);
                        }
                    }

                    _logger.LogInformation("Returning the list of trajectories for the given IDs from TrajectoryTable");
                    return trajectoryList;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given Trajectory ID list contains empty ID's");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all Trajectory present in the microservice database 
        /// </summary>
        /// <returns>the list of all Trajectory present in the microservice database</returns>
        public List<Model.Trajectory?>? GetAllTrajectory(Guid? fieldId = null, Guid? clusterId = null, Guid? wellId = null, Guid? wellBoreId = null, TrajectoryType? trajectoryType = null, bool? isDefinitive = null)
        {
            List<Model.Trajectory?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Trajectory FROM TrajectoryTable" + BuildFilterClause(fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
                AddFilterParameters(command, fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.Trajectory? trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);
                        if (trajectory?.MetaInfo?.ID is Guid trajectoryId)
                        {
                            trajectory.SurveyStationList ??= GetSurveyStationListByTrajectoryId(trajectoryId);
                        }
                        vals.Add(trajectory);
                    }
                    _logger.LogInformation("Returning the list of existing Trajectory from TrajectoryTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get Trajectory from TrajectoryTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all TrajectoryLight present in the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the list of TrajectoryLight present in the microservice database</returns>
        public List<Model.TrajectoryLight>? GetAllTrajectoryLight(Guid? fieldId = null, Guid? clusterId = null, Guid? wellId = null, Guid? wellBoreId = null, TrajectoryType? trajectoryType = null, bool? isDefinitive = null)
        {
            List<Model.TrajectoryLight>? trajectoryLightList = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Trajectory FROM TrajectoryTable" + BuildFilterClause(fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
                AddFilterParameters(command, fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        Model.Trajectory? trajectory = JsonSerializer.Deserialize<Model.Trajectory>(reader.GetString(0), JsonSettings.Options);
                        if (trajectory != null)
                        {
                            trajectoryLightList.Add(CreateDataLightInstance(trajectory));
                        }
                    }
                    _logger.LogInformation("Returning the list of existing TrajectoryLight from TrajectoryTable");
                    return trajectoryLightList;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get light datas from TrajectoryTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Performs calculation on the given Trajectory and adds it to the microservice database
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns>true if the given Trajectory has been added successfully to the microservice database</returns>
        public Task<bool> AddTrajectory(Model.Trajectory? trajectory)
        {
            try
            {
                if (trajectory != null && trajectory.MetaInfo != null && trajectory.MetaInfo.ID != Guid.Empty && trajectory.WellBoreID != Guid.Empty)
                {
                    if (GetTrajectoryById(trajectory.MetaInfo.ID, includeCalculatedStations: false) != null)
                    {
                        _logger.LogWarning("Impossible to post Trajectory. ID already found in database.");
                        return Task.FromResult(false);
                    }

                    MarkCalculationState(trajectory, CalculationState.Running, 0.0, "Calculation queued");
                    bool saved = InsertOrUpdateTrajectoryRecord(trajectory, false, null);
                    if (saved)
                    {
                        _ = Task.Run(() => RecalculateTrajectoryAsync(trajectory.MetaInfo.ID));
                    }

                    return Task.FromResult(saved);
                }
                else
                {
                    _logger.LogWarning("The Trajectory ID or the ID of its input are null or empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during the addition of the Trajectory");
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Performs calculation on the given Trajectory and updates it in the microservice database
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns>true if the given Trajectory has been updated successfully</returns>
        public Task<bool> UpdateTrajectoryById(Guid guid, Model.Trajectory? trajectory)
        {
            try
            {
                if (guid != Guid.Empty && trajectory != null && trajectory.MetaInfo != null && trajectory.MetaInfo.ID == guid)
                {
                    MarkCalculationState(trajectory, CalculationState.Running, 0.0, "Calculation queued");
                    bool saved = InsertOrUpdateTrajectoryRecord(trajectory, true, null);
                    if (saved)
                    {
                        _ = Task.Run(() => RecalculateTrajectoryAsync(guid));
                    }

                    return Task.FromResult(saved);
                }
                else
                {
                    _logger.LogWarning("The Trajectory ID or the ID of some of its attributes are null or empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during the update of the Trajectory");
            }
            return Task.FromResult(false);
        }

        private static string ToSqlGuidLiteral(Guid? value)
        {
            return value is Guid guid && guid != Guid.Empty ? $"'{guid}'" : "NULL";
        }

        private static int ToSqlBoolLiteral(bool value) => value ? 1 : 0;

        private static string ToSqlStringLiteral(string? value) => value == null ? "NULL" : $"'{value.Replace("'", "''")}'";

        private static void MarkCalculationState(Model.Trajectory trajectory, CalculationState state, double progress, string? message)
        {
            trajectory.CalculationState = state;
            trajectory.CalculationProgress = System.Math.Clamp(progress, 0.0, 1.0);
            trajectory.CalculationMessage = message;
        }

        private async Task RecalculateTrajectoryAsync(Guid trajectoryId)
        {
            try
            {
                UpdateTrajectoryCalculationState(trajectoryId, CalculationState.Running, 0.05, "Preparing trajectory calculation");
                Model.Trajectory? trajectory = GetTrajectoryById(trajectoryId, includeCalculatedStations: false);
                if (trajectory == null)
                {
                    return;
                }

                UpdateTrajectoryCalculationState(trajectoryId, CalculationState.Running, 0.25, "Calculating trajectory");
                if (await CalculateTrajectoryAsync(trajectory) == null)
                {
                    UpdateTrajectoryCalculationState(trajectoryId, CalculationState.Failed, 0.0, "Trajectory calculation failed");
                    DeleteSurveyStationChunks(trajectoryId);
                    return;
                }

                MarkCalculationState(trajectory, CalculationState.Completed, 1.0, null);
                trajectory.LastModificationDate = DateTimeOffset.UtcNow;
                if (!InsertOrUpdateTrajectoryRecord(trajectory, true, trajectory.SurveyStationList))
                {
                    UpdateTrajectoryCalculationState(trajectoryId, CalculationState.Failed, 0.0, "Trajectory calculation failed while saving");
                    DeleteSurveyStationChunks(trajectoryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during background Trajectory calculation");
                UpdateTrajectoryCalculationState(trajectoryId, CalculationState.Failed, 0.0, "Trajectory calculation failed");
                DeleteSurveyStationChunks(trajectoryId);
            }
        }

        private bool InsertOrUpdateTrajectoryRecord(Model.Trajectory trajectory, bool update, List<SurveyStation>? calculatedStationList)
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
                string metaInfo = JsonSerializer.Serialize(trajectory.MetaInfo, JsonSettings.Options);
                string? creationDate = trajectory.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = trajectory.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                trajectory.SurveyStationList = null;
                string data = JsonSerializer.Serialize(trajectory, JsonSettings.Options);

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                if (update)
                {
                    command.CommandText = "UPDATE TrajectoryTable SET " +
                        "MetaInfo = @metaInfo, CreationDate = @creationDate, LastModificationDate = @lastModificationDate, " +
                        "FieldID = @fieldId, ClusterID = @clusterId, WellID = @wellId, WellBoreID = @wellBoreId, TrajectoryType = @trajectoryType, IsDefinitive = @isDefinitive, " +
                        "CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, Trajectory = @trajectory WHERE ID = @id";
                }
                else
                {
                    command.CommandText = "INSERT INTO TrajectoryTable " +
                        "(ID, MetaInfo, CreationDate, LastModificationDate, FieldID, ClusterID, WellID, WellBoreID, TrajectoryType, IsDefinitive, CalculationState, CalculationProgress, CalculationMessage, Trajectory) " +
                        "VALUES (@id, @metaInfo, @creationDate, @lastModificationDate, @fieldId, @clusterId, @wellId, @wellBoreId, @trajectoryType, @isDefinitive, @calculationState, @calculationProgress, @calculationMessage, @trajectory)";
                }

                command.Parameters.AddWithValue("@id", trajectory.MetaInfo!.ID.ToString());
                command.Parameters.AddWithValue("@metaInfo", metaInfo);
                command.Parameters.AddWithValue("@creationDate", (object?)creationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@fieldId", ToSqlValue(trajectory.FieldID));
                command.Parameters.AddWithValue("@clusterId", ToSqlValue(trajectory.ClusterID));
                command.Parameters.AddWithValue("@wellId", ToSqlValue(trajectory.WellID));
                command.Parameters.AddWithValue("@wellBoreId", trajectory.WellBoreID.ToString());
                command.Parameters.AddWithValue("@trajectoryType", trajectory.TrajectoryType.ToString());
                command.Parameters.AddWithValue("@isDefinitive", trajectory.IsDefinitive ? 1 : 0);
                command.Parameters.AddWithValue("@calculationState", trajectory.CalculationState.ToString());
                command.Parameters.AddWithValue("@calculationProgress", trajectory.CalculationProgress);
                command.Parameters.AddWithValue("@calculationMessage", (object?)trajectory.CalculationMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@trajectory", data);

                bool success = command.ExecuteNonQuery() == 1;
                if (success && trajectory.IsDefinitive)
                {
                    success = UnsetOtherDefinitiveTrajectories(connection, transaction, trajectory.MetaInfo.ID, trajectory.WellBoreID, trajectory.TrajectoryType);
                }
                if (success)
                {
                    success = SurveyStationChunkStore.ReplaceChunks(connection, transaction, trajectory.MetaInfo.ID, SurveyStationOwnerType, calculatedStationList);
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
                _logger.LogError(ex, "Impossible to save Trajectory");
                return false;
            }
        }

        private static object ToSqlValue(Guid? value)
        {
            return value is Guid guid && guid != Guid.Empty ? guid.ToString() : DBNull.Value;
        }

        private bool UpdateTrajectoryCalculationState(Guid trajectoryId, CalculationState state, double progress, string? message)
        {
            Model.Trajectory? trajectory = GetTrajectoryById(trajectoryId, includeCalculatedStations: false);
            if (trajectory == null)
            {
                return false;
            }

            trajectory.CalculationState = state;
            trajectory.CalculationProgress = System.Math.Clamp(progress, 0.0, 1.0);
            trajectory.CalculationMessage = message;
            string data = JsonSerializer.Serialize(trajectory, JsonSettings.Options);

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            try
            {
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE TrajectoryTable SET " +
                    "CalculationState = @calculationState, CalculationProgress = @calculationProgress, CalculationMessage = @calculationMessage, Trajectory = @trajectory " +
                    "WHERE ID = @id";
                command.Parameters.AddWithValue("@id", trajectoryId.ToString());
                command.Parameters.AddWithValue("@calculationState", state.ToString());
                command.Parameters.AddWithValue("@calculationProgress", System.Math.Clamp(progress, 0.0, 1.0));
                command.Parameters.AddWithValue("@calculationMessage", (object?)message ?? DBNull.Value);
                command.Parameters.AddWithValue("@trajectory", data);
                return command.ExecuteNonQuery() == 1;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to update Trajectory calculation state");
                return false;
            }
        }

        private void DeleteSurveyStationChunks(Guid trajectoryId)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            SurveyStationChunkStore.DeleteChunks(connection, transaction, trajectoryId, SurveyStationOwnerType);
            transaction.Commit();
        }

        private bool UnsetOtherDefinitiveTrajectories(SqliteConnection connection, SqliteTransaction transaction, Guid currentTrajectoryId, Guid wellBoreId, TrajectoryType trajectoryType)
        {
            try
            {
                List<(Guid Id, string Data)> rows = [];
                var selectCommand = connection.CreateCommand();
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = "SELECT ID, Trajectory FROM TrajectoryTable " +
                    "WHERE WellBoreID = @wellBoreId AND TrajectoryType = @trajectoryType AND IsDefinitive = 1 AND ID <> @id";
                selectCommand.Parameters.AddWithValue("@wellBoreId", wellBoreId.ToString());
                selectCommand.Parameters.AddWithValue("@trajectoryType", trajectoryType.ToString());
                selectCommand.Parameters.AddWithValue("@id", currentTrajectoryId.ToString());

                using (var reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                        {
                            rows.Add((Guid.Parse(reader.GetString(0)), reader.GetString(1)));
                        }
                    }
                }

                foreach ((Guid id, string data) in rows)
                {
                    Model.Trajectory? trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);
                    if (trajectory == null)
                    {
                        throw new SqliteException("SQLite database corrupted: definitive Trajectory can not be deserialized.", 1);
                    }

                    trajectory.IsDefinitive = false;
                    string updatedData = JsonSerializer.Serialize(trajectory, JsonSettings.Options);

                    var updateCommand = connection.CreateCommand();
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = "UPDATE TrajectoryTable SET IsDefinitive = 0, Trajectory = @trajectory WHERE ID = @id";
                    updateCommand.Parameters.AddWithValue("@trajectory", updatedData);
                    updateCommand.Parameters.AddWithValue("@id", id.ToString());

                    if (updateCommand.ExecuteNonQuery() != 1)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to enforce the unique definitive Trajectory constraint");
                return false;
            }
        }

        private static string BuildFilterClause(Guid? fieldId, Guid? clusterId, Guid? wellId, Guid? wellBoreId, TrajectoryType? trajectoryType = null, bool? isDefinitive = null)
        {
            List<string> filters = [];

            if (fieldId is Guid definedFieldId && definedFieldId != Guid.Empty)
            {
                filters.Add("FieldID = @fieldId");
            }
            if (clusterId is Guid definedClusterId && definedClusterId != Guid.Empty)
            {
                filters.Add("ClusterID = @clusterId");
            }
            if (wellId is Guid definedWellId && definedWellId != Guid.Empty)
            {
                filters.Add("WellID = @wellId");
            }
            if (wellBoreId is Guid definedWellBoreId && definedWellBoreId != Guid.Empty)
            {
                filters.Add("WellBoreID = @wellBoreId");
            }
            if (trajectoryType is TrajectoryType)
            {
                filters.Add("TrajectoryType = @trajectoryType");
            }
            if (isDefinitive is bool)
            {
                filters.Add("IsDefinitive = @isDefinitive");
            }

            return filters.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", filters);
        }

        private static void AddFilterParameters(SqliteCommand command, Guid? fieldId, Guid? clusterId, Guid? wellId, Guid? wellBoreId, TrajectoryType? trajectoryType = null, bool? isDefinitive = null)
        {
            if (fieldId is Guid definedFieldId && definedFieldId != Guid.Empty)
            {
                command.Parameters.AddWithValue("@fieldId", definedFieldId.ToString());
            }
            if (clusterId is Guid definedClusterId && definedClusterId != Guid.Empty)
            {
                command.Parameters.AddWithValue("@clusterId", definedClusterId.ToString());
            }
            if (wellId is Guid definedWellId && definedWellId != Guid.Empty)
            {
                command.Parameters.AddWithValue("@wellId", definedWellId.ToString());
            }
            if (wellBoreId is Guid definedWellBoreId && definedWellBoreId != Guid.Empty)
            {
                command.Parameters.AddWithValue("@wellBoreId", definedWellBoreId.ToString());
            }
            if (trajectoryType is TrajectoryType definedTrajectoryType)
            {
                command.Parameters.AddWithValue("@trajectoryType", definedTrajectoryType.ToString());
            }
            if (isDefinitive is bool definedIsDefinitive)
            {
                command.Parameters.AddWithValue("@isDefinitive", ToSqlBoolLiteral(definedIsDefinitive));
            }
        }

        /// <summary>
        /// Performs trajectory calculation without persisting the result.
        /// </summary>
        public async Task<Model.Trajectory?> CalculateTrajectoryAsync(Model.Trajectory? trajectory)
        {
            try
            {
                if (trajectory == null || trajectory.MetaInfo == null || trajectory.WellBoreID == Guid.Empty)
                {
                    _logger.LogWarning("The Trajectory or its WellBoreID is null or empty");
                    return null;
                }
                if (!await MaterializeSurveyRunSectionsAsync(trajectory))
                {
                    _logger.LogWarning("Impossible to materialize survey run sections for the given Trajectory");
                    return null;
                }
                // retrieve the slot position
                (SurveyStation? referencePoint, WellBore? wellBore, string msg) = await APIUtils.GetReferencePointAsync(trajectory);
                if (wellBore is null || referencePoint is null)
                {
                    _logger.LogError(msg);
                    return null;
                }

                _logger.LogInformation(msg);
                // manage the possible case of a sidetrack
                if (await GetTieInPointCoordinatesAsync(referencePoint, wellBore) is not { } tieInPoint)
                {
                    _logger.LogError("The tie-in point coordinates can not be evaluated");
                    return null;
                }

                tieInPoint = NormalizeSidetrackTieInPointAbscissa(trajectory, wellBore, tieInPoint);
                trajectory.TieInPoint = tieInPoint;
                if (!trajectory.Calculate())
                {
                    _logger.LogWarning("Impossible to calculate outputs for the given Trajectory");
                    return null;
                }

                WellBoreArchitecture? wellBoreArchitecture = await APIUtils.GetWellBoreArchitectureByWellBoreIdAsync(trajectory.WellBoreID);
                if (wellBoreArchitecture != null)
                {
                    FillBoreholeRadiusFromArchitecture(trajectory, wellBoreArchitecture);
                }
                else
                {
                    _logger.LogWarning("No WellBoreArchitecture found for WellBoreID {WellBoreID}", trajectory.WellBoreID);
                }

                return trajectory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during trajectory calculation");
                return null;
            }
        }

        private static SurveyStation NormalizeSidetrackTieInPointAbscissa(Model.Trajectory trajectory, WellBore wellBore, SurveyStation tieInPoint)
        {
            if (!wellBore.IsSidetrack ||
                trajectory.SurveyStationList is not { Count: > 0 } stations ||
                (tieInPoint.MD ?? tieInPoint.Abscissa) is not { } tieInAbscissa)
            {
                return tieInPoint;
            }

            double? firstStationAbscissa = stations
                .Select(station => station.MD ?? station.Abscissa)
                .Where(abscissa => abscissa.HasValue)
                .Select(abscissa => abscissa!.Value)
                .OrderBy(abscissa => abscissa)
                .Cast<double?>()
                .FirstOrDefault();

            if (firstStationAbscissa is not { } firstAbscissa || !Numeric.LT(firstAbscissa, tieInAbscissa))
            {
                return tieInPoint;
            }

            return new SurveyStation(tieInPoint)
            {
                MD = firstAbscissa,
                Abscissa = firstAbscissa
            };
        }

        /// <summary>
        /// Deletes the Trajectory of given ID from the microservice database
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Trajectory was deleted from the microservice database</returns>
        public bool DeleteTrajectoryById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using var transaction = connection.BeginTransaction();
                    bool success = true;
                    // delete Trajectory from TrajectoryTable
                    try
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        SurveyStationChunkStore.DeleteChunks(connection, transaction, guid, SurveyStationOwnerType);
                        command.CommandText = $"DELETE FROM TrajectoryTable WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count < 0)
                        {
                            _logger.LogWarning("Impossible to delete the Trajectory of given ID from the TrajectoryTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to delete the Trajectory of given ID from TrajectoryTable");
                        success = false;
                    }
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Removed the Trajectory of given ID from the TrajectoryTable successfully");
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return success;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Trajectory ID is null or empty");
            }
            return false;
        }

        /// <summary>
        /// ------------------------------------------------------------------------------
        /// Logic for defining the TieInPoint of the trajectory
        /// ------------------------------------------------------------------------------
        /// Important note 1:
        /// tie in point location information is collected throughout the microservice architecture by retrieving the hosting cluster and slot
        /// and wrapped into the mean part of a GaussianGeodeticPoint3D object (uncertainty-propagation capability is not implemented yet).
        ///
        /// Important note 2:
        /// when uncertainty-propagation will be enacted, extreme care should be taken that GaussianDrillingProperty coordinates
        /// are intrinsically independent from eachother. The GaussianDrillingProperty data structure does not offer the possibility to account
        /// for cross-correlation between coordinates: GaussianGeodeticPoint3D and GaussianPoint3D structures have been introduced to correct this.
        /// 
        /// </summary>
        /// <param name="referencePoint">the Gaussian geodetic coordinates of the slot hosting the trajectory</param>
        /// <param name="wellBore">the hosting wellBore needed to compute the geodetic coordinates of the tie-in point (no need to test for nullity)</param>
        /// <returns>the uncertainty-aware geodetic coordinates of the tie-in point of the given trajectory</returns>
        private async Task<SurveyStation?> GetTieInPointCoordinatesAsync(SurveyStation? referencePoint, WellBore wellBore)
        {
            await Task.Delay(1);
            try
            {
                if (wellBore.IsSidetrack)
                {
                    if (wellBore.TieInPointAlongHoleDepth?.GaussianValue?.Mean is { } tieInMD &&
                        wellBore.ParentWellBoreID is Guid parentWellBoreId && parentWellBoreId != Guid.Empty)
                    {
                        Model.Trajectory? parentTraj = GetTrajectoryByWellBoreId(parentWellBoreId);
                        if (parentTraj?.SurveyStationList is { } stList)
                        {
                            if (SurveyStation.InterpolateAtAbscissa(stList, tieInMD, out SurveyStation? surveyPoint, parentTraj.CalculationType) &&
                                surveyPoint is not null)
                            {
                                return surveyPoint;
                            }
                            else
                            {
                                _logger.LogError("calculation of Riemannian coordinates failed");
                                return null;
                            }
                        }
                        else
                        {
                            _logger.LogError("the trajectory of the parent wellbore can not be retrieved or has corrupted survey station list");
                            return null;
                        }

                    }
                    else
                    {
                        _logger.LogError("the parent wellbore of the wellbore has a corrupted ID");
                        return null;
                    }
                }
                else
                {
                    return referencePoint;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "an exception was raised while computing tie-in point coordinates");
                return null;
            }
        }

        private async Task<bool> MaterializeSurveyRunSectionsAsync(Model.Trajectory trajectory)
        {
            if (trajectory.SurveyRunSectionList is not { Count: > 0 } sections)
            {
                _logger.LogWarning("The Trajectory must define at least one survey run section");
                return false;
            }

            for (int i = 0; i < sections.Count; i++)
            {
                TrajectorySurveyRunSection section = sections[i];
                if (section.SurveyRunID == Guid.Empty ||
                    !Numeric.IsDefined(section.StartAbscissa) ||
                    (i > 0 && !Numeric.GT(section.StartAbscissa, sections[i - 1].StartAbscissa)))
                {
                    _logger.LogWarning("The Trajectory survey run sections must be ordered by strictly increasing start abscissa and reference valid survey runs");
                    return false;
                }
            }

            List<SurveyStation> materialized = [];
            for (int i = 0; i < sections.Count; i++)
            {
                TrajectorySurveyRunSection section = sections[i];
                double start = section.StartAbscissa;
                double? end = i + 1 < sections.Count ? sections[i + 1].StartAbscissa : null;
                Model.SurveyRun? surveyRun = GetSurveyRunById(section.SurveyRunID);

                if (surveyRun?.SurveyStationList is not { Count: > 0 } stations ||
                    surveyRun.WellBoreID != trajectory.WellBoreID ||
                    surveyRun.SurveyInstrumentID == Guid.Empty)
                {
                    _logger.LogWarning("The Trajectory references an invalid SurveyRun");
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

                if (surveyInstrument is null)
                {
                    _logger.LogWarning("The SurveyInstrument for SurveyRun {SurveyRunId} can not be retrieved", surveyRun.MetaInfo?.ID);
                    return false;
                }

                foreach (SurveyStation station in stations)
                {
                    double? md = station.MD ?? station.Abscissa;
                    if (md is not { } definedMd || !Numeric.IsDefined(definedMd))
                    {
                        continue;
                    }

                    if (!Numeric.GE(definedMd, start) ||
                        (end is { } definedEnd && !Numeric.LT(definedMd, definedEnd)))
                    {
                        continue;
                    }

                    SurveyStation copy = CloneSurveyStation(station);
                    copy.MD = definedMd;
                    copy.Abscissa = definedMd;
                    copy.SurveyTool = ConvertSurveyInstrument(surveyInstrument);

                    if (materialized.Count == 0 ||
                        materialized[^1].MD is not { } previousMd ||
                        !Numeric.EQ(previousMd, definedMd))
                    {
                        materialized.Add(copy);
                    }
                }
            }

            if (materialized.Count < 3)
            {
                _logger.LogWarning("The materialized Trajectory contains fewer than three survey stations");
                return false;
            }

            for (int i = 1; i < materialized.Count; i++)
            {
                if (materialized[i].MD is not { } md ||
                    materialized[i - 1].MD is not { } previousMd ||
                    !Numeric.GT(md, previousMd))
                {
                    _logger.LogWarning("The materialized Trajectory survey stations must be ordered by strictly increasing measured depth");
                    return false;
                }
            }

            trajectory.SurveyStationList = materialized;
            return true;
        }

        private Model.SurveyRun? GetSurveyRunById(Guid surveyRunId)
        {
            if (surveyRunId == Guid.Empty)
            {
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT SurveyRun FROM SurveyRunTable WHERE ID = @surveyRunId";
                command.Parameters.AddWithValue("@surveyRunId", surveyRunId.ToString());
                try
                {
                    using var reader = command.ExecuteReader();
                    if (reader.Read() && !reader.IsDBNull(0))
                    {
                        Model.SurveyRun? surveyRun = JsonSerializer.Deserialize<Model.SurveyRun>(reader.GetString(0), JsonSettings.Options);
                        if (surveyRun?.MetaInfo?.ID != surveyRunId)
                        {
                            throw new SqliteException("SQLite database corrupted: returned SurveyRun has the wrong ID.", 1);
                        }
                        surveyRun.SurveyStationList ??= SurveyStationChunkStore.GetStations(_logger, _connectionManager, surveyRunId, SurveyRunStationOwnerType);
                        return surveyRun;
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get the SurveyRun with the given ID from SurveyRunTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        public int GetSurveyStationChunkCount(Guid trajectoryId)
        {
            return SurveyStationChunkStore.GetChunkCount(_logger, _connectionManager, trajectoryId, SurveyStationOwnerType);
        }

        public SurveyStationChunk? GetSurveyStationChunk(Guid trajectoryId, int chunkIndex)
        {
            return SurveyStationChunkStore.GetChunk(_logger, _connectionManager, trajectoryId, SurveyStationOwnerType, chunkIndex);
        }

        public List<SurveyStation>? GetSurveyStationListByTrajectoryId(Guid trajectoryId)
        {
            return SurveyStationChunkStore.GetStations(_logger, _connectionManager, trajectoryId, SurveyStationOwnerType);
        }

        private static SurveyStation CloneSurveyStation(SurveyStation station)
        {
            string data = JsonSerializer.Serialize(station, JsonSettings.Options);
            return JsonSerializer.Deserialize<SurveyStation>(data, JsonSettings.Options) ?? new SurveyStation();
        }

        private static OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument ConvertSurveyInstrument(NORCE.Drilling.Trajectory.ModelShared.SurveyInstrument surveyInstrument)
        {
            string data = JsonSerializer.Serialize(surveyInstrument, JsonSettings.Options);
            return JsonSerializer.Deserialize<OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument>(data, JsonSettings.Options)
                ?? new OSDC.DotnetLibraries.Drilling.Surveying.SurveyInstrument();
        }

        public static int FillBoreholeRadiusFromArchitecture(Model.Trajectory trajectory, WellBoreArchitecture architecture)
        {
            if (trajectory.SurveyStationList is not { Count: > 0 })
            {
                return 0;
            }

            List<BoreholeRadiusInterval> intervals = BuildBoreholeRadiusIntervals(architecture);
            if (intervals.Count == 0)
            {
                return 0;
            }

            int filledCount = 0;
            foreach (SurveyStation station in trajectory.SurveyStationList)
            {
                if (station.MD is not double md)
                {
                    continue;
                }

                double? radius = ResolveBoreholeRadius(intervals, md);
                if (radius.HasValue)
                {
                    station.BoreholeRadius = radius.Value;
                    filledCount++;
                }
            }

            return filledCount;
        }

        private static List<BoreholeRadiusInterval> BuildBoreholeRadiusIntervals(WellBoreArchitecture architecture)
        {
            List<BoreholeRadiusInterval> intervals = [];

            AddSurfaceSectionIntervals(intervals, architecture);

            if (architecture.CasingSections is { Count: > 0 })
            {
                foreach (CasingSection casingSection in architecture.CasingSections)
                {
                    AddCasingSectionIntervals(intervals, casingSection);
                }
            }

            return intervals
                .OrderBy(interval => interval.StartMD)
                .ThenBy(interval => interval.EndMD)
                .ThenBy(interval => interval.Radius)
                .ToList();
        }

        private static void AddSurfaceSectionIntervals(List<BoreholeRadiusInterval> intervals, WellBoreArchitecture architecture)
        {
            if (architecture.SurfaceSections is not { Count: > 0 })
            {
                return;
            }

            if (GetGaussianMean(architecture.WellHead?.Depth) is not double wellHeadDepth ||
                !IsUsableDepthValue(wellHeadDepth))
            {
                return;
            }

            List<(SurfaceSection Section, double Length)> surfaceSections = [];
            foreach (SurfaceSection? surfaceSection in architecture.SurfaceSections)
            {
                if (surfaceSection is null)
                {
                    continue;
                }

                SurfaceSection nonNullSurfaceSection = surfaceSection;
                if (GetGaussianMean(nonNullSurfaceSection.SectionLength) is double length &&
                    IsUsablePositiveValue(length))
                {
                    surfaceSections.Add((nonNullSurfaceSection, length));
                }
            }

            if (surfaceSections.Count == 0)
            {
                return;
            }

            double currentMD = wellHeadDepth - surfaceSections.Sum(item => item.Length);
            foreach ((SurfaceSection surfaceSection, double surfaceLength) in surfaceSections)
            {
                double? radius = GetRadiusFromDiameter(surfaceSection?.BodyID);
                double endMD = currentMD + surfaceLength;
                if (radius.HasValue)
                {
                    TryAddBoreholeRadiusInterval(intervals, currentMD, endMD, radius.Value);
                }

                currentMD = endMD;
            }
        }

        private static void AddCasingSectionIntervals(List<BoreholeRadiusInterval> intervals, CasingSection? casingSection)
        {
            if (GetGaussianMean(casingSection?.TopDepth) is not double topDepth ||
                !IsUsableDepthValue(topDepth))
            {
                return;
            }

            double currentMD = topDepth;
            int sizeCount = casingSection?.CasingSectionSizeTable?.Count ?? 0;
            double casingSectionLength = GetGaussianMean(casingSection?.Length) ?? 0.0;

            if (casingSection?.CasingSectionSizeTable is { Count: > 0 })
            {
                foreach (BoreHoleSize boreHoleSize in casingSection.CasingSectionSizeTable)
                {
                    double? radius = GetRadiusFromDiameter(boreHoleSize?.HoleSize);
                    double? length = GetGaussianMean(boreHoleSize?.Length);
                    if ((!length.HasValue || !IsUsablePositiveValue(length.Value)) &&
                        sizeCount == 1 &&
                        IsUsablePositiveValue(casingSectionLength))
                    {
                        length = casingSectionLength;
                    }

                    if (length is not double boreHoleLength || !IsUsablePositiveValue(boreHoleLength))
                    {
                        continue;
                    }

                    double endMD = currentMD + boreHoleLength;
                    if (radius.HasValue)
                    {
                        TryAddBoreholeRadiusInterval(intervals, currentMD, endMD, radius.Value);
                    }

                    currentMD = endMD;
                }
            }

            AddOpenHoleIntervals(intervals, ResolveOpenHoleStartMD(casingSection, topDepth, currentMD), casingSection?.OpenHoleSection);
        }

        private static double ResolveOpenHoleStartMD(CasingSection? casingSection, double topDepth, double fallbackStartMD)
        {
            if (casingSection?.CasingSectionElements is { Count: > 0 })
            {
                double elementLengthSum = 0.0;
                foreach (CasingSectionElement casingElement in casingSection.CasingSectionElements)
                {
                    double? length = GetGaussianMean(casingElement?.SectionLength);
                    if (length is double elementLength && IsUsablePositiveValue(elementLength))
                    {
                        elementLengthSum += elementLength;
                    }
                }

                if (IsUsablePositiveValue(elementLengthSum))
                {
                    return topDepth + elementLengthSum;
                }
            }

            double? casingSectionLength = GetGaussianMean(casingSection?.Length);
            if (casingSectionLength is double sectionLength && IsUsablePositiveValue(sectionLength))
            {
                return topDepth + sectionLength;
            }

            return fallbackStartMD;
        }

        private static void AddOpenHoleIntervals(List<BoreholeRadiusInterval> intervals, double startMD, OpenHoleSection? openHoleSection)
        {
            if (openHoleSection?.HoleSizes is not { Count: > 0 })
            {
                return;
            }

            double currentMD = startMD;
            foreach (BoreHoleSize holeSize in openHoleSection.HoleSizes)
            {
                double? radius = GetRadiusFromDiameter(holeSize?.HoleSize);
                double? length = GetGaussianMean(holeSize?.Length);
                if (!radius.HasValue || length is not double holeLength || !IsUsablePositiveValue(holeLength))
                {
                    continue;
                }

                double endMD = currentMD + holeLength;
                TryAddBoreholeRadiusInterval(intervals, currentMD, endMD, radius.Value);
                currentMD = endMD;
            }
        }

        private static void TryAddBoreholeRadiusInterval(List<BoreholeRadiusInterval> intervals, double startMD, double endMD, double radius)
        {
            if (IsUsableDepthValue(startMD) &&
                IsUsableDepthValue(endMD) &&
                endMD > startMD &&
                IsUsablePositiveValue(radius))
            {
                intervals.Add(new BoreholeRadiusInterval(startMD, endMD, radius));
            }
        }

        private static double? ResolveBoreholeRadius(IReadOnlyList<BoreholeRadiusInterval> intervals, double md)
        {
            const double depthTolerance = 1e-6;
            double? radius = null;
            for (int i = 0; i < intervals.Count; i++)
            {
                BoreholeRadiusInterval interval = intervals[i];
                if (md + depthTolerance >= interval.StartMD && md <= interval.EndMD + depthTolerance)
                {
                    radius = radius.HasValue
                        ? Math.Max(radius.Value, interval.Radius)
                        : interval.Radius;
                }
            }

            return radius;
        }

        private static double? GetRadiusFromDiameter(GaussianDrillingProperty? property)
        {
            double? diameter = GetGaussianMean(property);
            return diameter is double value && IsUsablePositiveValue(value) ? 0.5 * value : null;
        }

        private static double? GetGaussianMean(GaussianDrillingProperty? property)
        {
            return property?.GaussianValue?.Mean;
        }

        private static bool IsUsablePositiveValue(double value)
        {
            return double.IsFinite(value) && value > 0;
        }

        private static bool IsUsableDepthValue(double value)
        {
            return double.IsFinite(value);
        }

        private sealed record BoreholeRadiusInterval(double StartMD, double EndMD, double Radius);
    }
}
