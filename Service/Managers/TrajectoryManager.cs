using DWIS.API.DTO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.Surveying;
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
                WellBoreID = trajectory.WellBoreID
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
        public Model.Trajectory? GetTrajectoryById(Guid trajId)
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
            if (!trajIdList.Contains(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    List<Model.Trajectory>? trajectoryList;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT Trajectory FROM TrajectoryTable WHERE ID IN '{trajIdList}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            trajectoryList = JsonSerializer.Deserialize<List<Model.Trajectory>>(data, JsonSettings.Options);
                            if (trajectoryList != null)
                            {
                                foreach (Model.Trajectory trajectory in trajectoryList)
                                {
                                    if (trajectory != null && trajectory.MetaInfo != null && !trajIdList.Contains(trajectory.MetaInfo.ID))
                                    {
                                        throw new SqliteException("SQLite database corrupted: returned Trajectory is null or has been jsonified with the wrong ID.", 1);
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Problem with one of the trajectories of the given ID's in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the TrajectoryList with the given ID's from TrajectoryTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the Trajectory of given ID from TrajectoryTable");
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
        public List<Model.Trajectory?>? GetAllTrajectory(Guid? fieldId = null, Guid? clusterId = null, Guid? wellId = null, Guid? wellBoreId = null)
        {
            List<Model.Trajectory?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Trajectory FROM TrajectoryTable" + BuildFilterClause(fieldId, clusterId, wellId, wellBoreId);
                AddFilterParameters(command, fieldId, clusterId, wellId, wellBoreId);
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.Trajectory? trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);
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
        public List<Model.TrajectoryLight>? GetAllTrajectoryLight(Guid? fieldId = null, Guid? clusterId = null, Guid? wellId = null, Guid? wellBoreId = null)
        {
            List<Model.TrajectoryLight>? trajectoryLightList = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Trajectory FROM TrajectoryTable" + BuildFilterClause(fieldId, clusterId, wellId, wellBoreId);
                AddFilterParameters(command, fieldId, clusterId, wellId, wellBoreId);
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
        public async Task<bool> AddTrajectory(Model.Trajectory? trajectory)
        {
            try
            {
                if (trajectory != null && trajectory.MetaInfo != null && trajectory.MetaInfo.ID != Guid.Empty && trajectory.WellBoreID != Guid.Empty)
                {
                    if (await CalculateTrajectoryAsync(trajectory) == null)
                    {
                        _logger.LogWarning("Impossible to calculate outputs for the given Trajectory");
                        return false;
                    }

                    // if successful, check if another parent data with the same ID was calculated/added during the calculation time
                    Model.Trajectory? newTrajectory = GetTrajectoryById(trajectory.MetaInfo.ID);
                    if (newTrajectory == null)
                    {
                        // update TrajectoryTable
                        var connection = _connectionManager.GetConnection();
                        if (connection != null)
                        {
                            using SqliteTransaction transaction = connection.BeginTransaction();
                            bool success = true;
                            try
                            {
                                //add the Trajectory to the TrajectoryTable
                                string metaInfo = JsonSerializer.Serialize(trajectory.MetaInfo, JsonSettings.Options);

                                string? cDate = null;
                                if (trajectory.CreationDate != null)
                                    cDate = ((DateTimeOffset)trajectory.CreationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                                string? lDate = null;
                                if (trajectory.LastModificationDate != null)
                                    lDate = ((DateTimeOffset)trajectory.LastModificationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                                string data = JsonSerializer.Serialize(trajectory, JsonSettings.Options);

                                var command = connection.CreateCommand();
                                command.CommandText = "INSERT INTO TrajectoryTable (" +
                                    "ID, " +
                                    "MetaInfo, " +
                                    "CreationDate, " +
                                    "LastModificationDate, " +
                                    "FieldID, " +
                                    "ClusterID, " +
                                    "WellID, " +
                                    "WellBoreID, " +
                                    "Trajectory" +
                                    ") VALUES (" +
                                    $"'{trajectory.MetaInfo.ID}', " +
                                    $"'{metaInfo}', " +
                                    $"'{cDate}', " +
                                    $"'{lDate}', " +
                                    $"{ToSqlGuidLiteral(trajectory.FieldID)}, " +
                                    $"{ToSqlGuidLiteral(trajectory.ClusterID)}, " +
                                    $"{ToSqlGuidLiteral(trajectory.WellID)}, " +
                                    $"'{trajectory.WellBoreID}', " +
                                    $"'{data}'" +
                                    ")";
                                int count = command.ExecuteNonQuery();
                                if (count != 1)
                                {
                                    _logger.LogWarning("Impossible to insert the given Trajectory into the TrajectoryTable");
                                    success = false;
                                }
                            }
                            catch (SqliteException ex)
                            {
                                _logger.LogError(ex, "Impossible to add the given Trajectory into TrajectoryTable");
                                success = false;
                            }
                            // finalizing SQL transaction
                            if (success)
                            {
                                transaction.Commit();
                                _logger.LogInformation("Added the given Trajectory of given ID into the TrajectoryTable successfully");
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
                        _logger.LogWarning("Impossible to post Trajectory. ID already found in database.");
                        return false;
                    }

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
            return false;
        }

        /// <summary>
        /// Performs calculation on the given Trajectory and updates it in the microservice database
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns>true if the given Trajectory has been updated successfully</returns>
        public async Task<bool> UpdateTrajectoryById(Guid guid, Model.Trajectory? trajectory)
        {
            try
            {
                bool success = true;
                if (guid != Guid.Empty && trajectory != null && trajectory.MetaInfo != null && trajectory.MetaInfo.ID == guid)
                {
                    if (await CalculateTrajectoryAsync(trajectory) == null)
                    {
                        _logger.LogWarning("Impossible to calculate outputs of the given Trajectory");
                        return false;
                    }
                    // update TrajectoryTable
                    var connection = _connectionManager.GetConnection();
                    if (connection != null)
                    {
                        using SqliteTransaction transaction = connection.BeginTransaction();
                        // update fields in TrajectoryTable
                        try
                        {
                            string metaInfo = JsonSerializer.Serialize(trajectory.MetaInfo, JsonSettings.Options);
                            string? cDate = null;
                            if (trajectory.CreationDate != null)
                                cDate = ((DateTimeOffset)trajectory.CreationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                            trajectory.LastModificationDate = DateTimeOffset.UtcNow;
                            string? lDate = ((DateTimeOffset)trajectory.LastModificationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                            string data = JsonSerializer.Serialize(trajectory, JsonSettings.Options);
                            var command = connection.CreateCommand();
                            command.CommandText = $"UPDATE TrajectoryTable SET " +
                                $"MetaInfo = '{metaInfo}', " +
                                $"CreationDate = '{cDate}', " +
                                $"LastModificationDate = '{lDate}', " +
                                $"FieldID = {ToSqlGuidLiteral(trajectory.FieldID)}, " +
                                $"ClusterID = {ToSqlGuidLiteral(trajectory.ClusterID)}, " +
                                $"WellID = {ToSqlGuidLiteral(trajectory.WellID)}, " +
                                $"WellBoreID = '{trajectory.WellBoreID}', " +
                                $"Trajectory = '{data}' " +
                                $"WHERE ID = '{guid}'";
                            int count = command.ExecuteNonQuery();
                            if (count != 1)
                            {
                                _logger.LogWarning("Impossible to update the Trajectory");
                                success = false;
                            }
                        }
                        catch (SqliteException ex)
                        {
                            _logger.LogError(ex, "Impossible to update the Trajectory");
                            success = false;
                        }

                        // Finalizing
                        if (success)
                        {
                            transaction.Commit();
                            _logger.LogInformation("Updated the given Trajectory successfully");
                            return true;
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Impossible to access the SQLite database");
                    }
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
            return false;
        }

        private static string ToSqlGuidLiteral(Guid? value)
        {
            return value is Guid guid && guid != Guid.Empty ? $"'{guid}'" : "NULL";
        }

        private static string BuildFilterClause(Guid? fieldId, Guid? clusterId, Guid? wellId, Guid? wellBoreId)
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

            return filters.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", filters);
        }

        private static void AddFilterParameters(SqliteCommand command, Guid? fieldId, Guid? clusterId, Guid? wellId, Guid? wellBoreId)
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
                // retrieve the slot position
                (SurveyPoint? referencePoint, WellBore? wellBore, string msg) = await APIUtils.GetReferencePointAsync(trajectory);
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

                trajectory.TieInPoint = tieInPoint;
                if (!trajectory.Calculate())
                {
                    _logger.LogWarning("Impossible to calculate outputs for the given Trajectory");
                    return null;
                }

                return trajectory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during trajectory calculation");
                return null;
            }
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
        private async Task<SurveyPoint?> GetTieInPointCoordinatesAsync(SurveyPoint? referencePoint, WellBore wellBore)
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
                            // interpolating the parent trajectory at the TieInPoint depth value held by the child wellbore hosting the child trajectory
                            SurveyPoint? surveyPoint = new();
                            if (SurveyPoint.InterpolateAtAbscissa<SurveyStation>(stList, tieInMD, surveyPoint))
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
    }
}
