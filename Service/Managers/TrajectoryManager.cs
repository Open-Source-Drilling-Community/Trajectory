using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using NORCE.Drilling.Trajectory.Model;

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
        public List<MetaInfo?>? GetAllTrajectoryMetaInfo()
        {
            List<MetaInfo?> metaInfos = new();
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
                        MetaInfo? metaInfo = JsonSerializer.Deserialize<MetaInfo>(mInfo, JsonSettings.Options);
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
        /// <param name="guid"></param>
        /// <returns>the Trajectory identified by its Guid from the microservice database</returns>
        public Model.Trajectory? GetTrajectoryById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.Trajectory? trajectory;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT Trajectory FROM TrajectoryTable WHERE ID = '{guid}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            trajectory = JsonSerializer.Deserialize<Model.Trajectory>(data, JsonSettings.Options);
                            if (trajectory != null && trajectory.MetaInfo != null && !trajectory.MetaInfo.ID.Equals(guid))
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
        /// Returns the list of all Trajectory present in the microservice database 
        /// </summary>
        /// <returns>the list of all Trajectory present in the microservice database</returns>
        public List<Model.Trajectory?>? GetAllTrajectory()
        {
            List<Model.Trajectory?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Trajectory FROM TrajectoryTable";
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
        public List<Model.TrajectoryLight>? GetAllTrajectoryLight()
        {
            List<Model.TrajectoryLight>? trajectoryLightList = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo, TrajectoryLight FROM TrajectoryTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string metaInfoStr = reader.GetString(0);
                        MetaInfo? metaInfo = JsonSerializer.Deserialize<MetaInfo>(metaInfoStr, JsonSettings.Options);
                        Model.TrajectoryLight? trajectoryLight = JsonSerializer.Deserialize<Model.TrajectoryLight>(reader.GetString(1), JsonSettings.Options);
                        if (trajectoryLight != null)
                        {
                            trajectoryLightList.Add(trajectoryLight);                            
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
        public bool AddTrajectory(Model.Trajectory? trajectory)
        {
            if (trajectory != null && trajectory.MetaInfo != null && trajectory.MetaInfo.ID != Guid.Empty)
            {
                //calculate outputs
                if (!trajectory.Calculate())
                {
                    _logger.LogWarning("Impossible to calculate outputs for the given Trajectory");
                    return false;
                }

                //if successful, check if another parent data with the same ID was calculated/added during the calculation time
                Model.Trajectory? newTrajectory = GetTrajectoryById(trajectory.MetaInfo.ID);
                if (newTrajectory == null)
                {
                    //update TrajectoryTable
                    var connection = _connectionManager.GetConnection();
                    if (connection != null)
                    {
                        using SqliteTransaction transaction = connection.BeginTransaction();
                        bool success = true;
                        try
                        {
                            //add the Trajectory to the TrajectoryTable
                            string metaInfo = JsonSerializer.Serialize(trajectory.MetaInfo, JsonSettings.Options);
                      
                            Model.TrajectoryLight trajectoryLight = CreateDataLightInstance(trajectory);
                            string dataLight = JsonSerializer.Serialize(trajectoryLight, JsonSettings.Options);                           

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
                                "TrajectoryLight, " +                                
                                "CreationDate, " +
                                "LastModificationDate, " +
                                "Trajectory" +
                                ") VALUES (" +
                                $"'{trajectory.MetaInfo.ID}', " +
                                $"'{metaInfo}', " +
                                $"'{dataLight}', " +
                                $"'{cDate}', " +
                                $"'{lDate}', " +
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
                        //finalizing SQL transaction
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
            return false;
        }

        /// <summary>
        /// Performs calculation on the given Trajectory and updates it in the microservice database
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns>true if the given Trajectory has been updated successfully</returns>
        public bool UpdateTrajectoryById(Guid guid, Model.Trajectory? trajectory)
        {
            bool success = true;
            if (guid != Guid.Empty && trajectory != null && trajectory.MetaInfo != null && trajectory.MetaInfo.ID == guid)
            {
                //calculate outputs
                if (!trajectory.Calculate())
                {
                    _logger.LogWarning("Impossible to calculate outputs of the given Trajectory");
                    return false;
                }
                //update TrajectoryTable
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    //update fields in TrajectoryTable
                    try
                    {
                        string metaInfo = JsonSerializer.Serialize(trajectory.MetaInfo, JsonSettings.Options);
                        Model.TrajectoryLight trajectoryLight = CreateDataLightInstance(trajectory);
                        string dataLight = JsonSerializer.Serialize(trajectoryLight, JsonSettings.Options);                           
                        string? cDate = null;
                        if (trajectory.CreationDate != null)
                            cDate = ((DateTimeOffset)trajectory.CreationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                        trajectory.LastModificationDate = DateTimeOffset.UtcNow;
                        string? lDate = ((DateTimeOffset)trajectory.LastModificationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                        string data = JsonSerializer.Serialize(trajectory, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.CommandText = $"UPDATE TrajectoryTable SET " +
                            $"MetaInfo = '{metaInfo}', " +
                            $"TrajectoryLight = '{dataLight}', " +                              
                            $"CreationDate = '{cDate}', " +
                            $"LastModificationDate = '{lDate}', " +
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
            return false;
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
                    //delete Trajectory from TrajectoryTable
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
    }
}