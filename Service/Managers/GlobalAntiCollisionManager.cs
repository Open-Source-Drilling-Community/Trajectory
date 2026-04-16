using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    /// <summary>
    /// A manager for GlobalAntiCollision. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class GlobalAntiCollisionManager
    {
        public object lock_ = new object();
        private static GlobalAntiCollisionManager? _instance = null;
        private readonly ILogger<GlobalAntiCollisionManager> _logger;
        private readonly SqlConnectionManagerSeparationFactorResults _connectionManager;

        private GlobalAntiCollisionManager(ILogger<GlobalAntiCollisionManager> logger, SqlConnectionManagerSeparationFactorResults connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;

            Thread thread = new Thread(new ThreadStart(GC));
            thread.Start();
        }

        public static GlobalAntiCollisionManager GetInstance(ILogger<GlobalAntiCollisionManager> logger, SqlConnectionManagerSeparationFactorResults connectionManager)
        {
            _instance ??= new GlobalAntiCollisionManager(logger, connectionManager);
            return _instance;
        }

        private void GC()
        {
            while (true)
            {
                Remove(DateTime.UtcNow - TimeSpan.FromSeconds(3600));
                Thread.Sleep(10000);
            }
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
                    command.CommandText = @"SELECT COUNT(*) FROM SeparationFactorResults";
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
                        _logger.LogError(ex, "Impossible to count records in the SeparationFactorResults");
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
                lock (lock_)
                {
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        //empty TrajectoryTable
                        var command = connection.CreateCommand();
                        command.CommandText = @"DELETE FROM SeparationFactorResults";
                        command.ExecuteNonQuery();

                        transaction.Commit();
                        success = true;
                    }
                    catch (SqliteException ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Impossible to clear the SeparationFactorResults");
                    }
                }
                return success;
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }
        }

        public bool Contains(string id)//(Guid guid)
        {
            int count = 0;
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT COUNT(*) FROM SeparationFactorResults WHERE ID = " + "'" + id + "'";
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
                    _logger.LogError(ex, "Impossible to count rows from SeparationFactorResults");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        public List<string> GetIDs()
        {
            List<string> ids = new List<string>();
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT ID FROM SeparationFactorResults";
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ID = reader.GetString(0);
                            ids.Add(ID);
                        }
                    }
                }
                catch (SqliteException)
                {
                }
            }
            return ids;
        }

        public GlobalAntiCollision.GlobalAntiCollision? Get(string ID)
        {
            if (!string.IsNullOrEmpty(ID))
            {
                GlobalAntiCollision.GlobalAntiCollision? globalAntiCollision = null;
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"SELECT DataSet FROM SeparationFactorResults WHERE ID = " + "'" + ID.ToString() + "'";
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    string json = reader.GetString(0);
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        try
                                        {
                                            globalAntiCollision = JsonSerializer.Deserialize<GlobalAntiCollision.GlobalAntiCollision>(json);
                                            if (globalAntiCollision != null && !globalAntiCollision.ID.Equals(ID))
                                            {
                                                globalAntiCollision.ID = ID;
                                            }
                                        }
                                        catch (JsonException ex)
                                        {
                                            _logger.LogError(ex, "Impossible to deserialize the SeparationFactorResults payload");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the GlobalAntiCollision payload from SeparationFactorResults");
                    }
                }
                return globalAntiCollision;
            }
            else
            {
                return null;
            }
        }

        public bool Add(GlobalAntiCollision.GlobalAntiCollision? globalAntiCollision)
        {
            bool result = false;
            if (globalAntiCollision != null)
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    if (string.IsNullOrEmpty(globalAntiCollision.ID))
                    {
                        globalAntiCollision.ID = Guid.NewGuid().ToString();
                    }
                    lock (lock_)
                    {
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                string json = JsonSerializer.Serialize(globalAntiCollision);
                                var command = connection.CreateCommand();
                                command.CommandText = @"INSERT INTO SeparationFactorResults (ID, TimeStamp, DataSet) VALUES (" +
                                    "'" + globalAntiCollision.ID + "'" + ", " +
                                    "'" + (DateTime.UtcNow - DateTime.MinValue).TotalSeconds.ToString() + "'" + ", " + "'" + json + "'" + ")";
                                int count = command.ExecuteNonQuery();
                                result = count == 1;
                                if (result)
                                {
                                    transaction.Commit();
                                }
                                else
                                {
                                    transaction.Rollback();
                                }
                            }
                            catch (SqliteException ex)
                            {
                                transaction.Rollback();
                                _logger.LogError(ex, "Impossible to add the GlobalAntiCollision payload to SeparationFactorResults");
                            }
                        }
                    }
                }
            }
            return result;
        }

        public bool Remove(GlobalAntiCollision.GlobalAntiCollision? globalAntiCollision)
        {
            bool result = false;
            if (globalAntiCollision != null)
            {
                result = Remove(globalAntiCollision.ID);
            }
            return result;
        }

        public bool Remove(string ID)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(ID))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    lock (lock_)
                    {
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                var command = connection.CreateCommand();
                                command.CommandText = @"DELETE FROM SeparationFactorResults WHERE ID = " + "'" + ID.ToString() + "'";
                                int count = command.ExecuteNonQuery();
                                result = count >= 0;
                                if (result)
                                {
                                    transaction.Commit();
                                }
                                else
                                {
                                    transaction.Rollback();
                                }
                            }
                            catch (SqliteException ex)
                            {
                                transaction.Rollback();
                                _logger.LogError(ex, "Impossible to remove the GlobalAntiCollision payload from SeparationFactorResults");
                            }
                        }
                    }
                }
            }
            return result;
        }

        public bool Remove(DateTime old)
        {
            bool result = false;
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                lock (lock_)
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM SeparationFactorResults WHERE TimeStamp < " + "'" + (old - DateTime.MinValue).TotalSeconds.ToString() + "'";
                            int count = command.ExecuteNonQuery();
                            result = count >= 0;
                            if (result)
                            {
                                transaction.Commit();
                            }
                            else
                            {
                                transaction.Rollback();
                            }
                        }
                        catch (SqliteException ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Impossible to remove old GlobalAntiCollision payloads from SeparationFactorResults");
                        }
                    }
                }
            }
            return result;
        }

        public bool Update(string ID, GlobalAntiCollision.GlobalAntiCollision? updatedGlobalAntiCollision)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(ID) && updatedGlobalAntiCollision != null)
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    lock (lock_)
                    {
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                string json = JsonSerializer.Serialize<GlobalAntiCollision.GlobalAntiCollision>(updatedGlobalAntiCollision);
                                var command = connection.CreateCommand();
                                command.CommandText = @"UPDATE SeparationFactorResults SET " +
                                    "TimeStamp = " + "'" + (DateTime.UtcNow - DateTime.MinValue).TotalSeconds.ToString() + "'" + ", " +
                                    "DataSet = " + "'" + json + "'" + " " +
                                    "WHERE ID = " + "'" + ID.ToString() + "'";
                                int count = command.ExecuteNonQuery();
                                result = count == 1;
                                if (result)
                                {
                                    transaction.Commit();
                                }
                                else
                                {
                                    transaction.Rollback();
                                }
                            }
                            catch (SqliteException ex)
                            {
                                transaction.Rollback();
                                _logger.LogError(ex, "Impossible to update the GlobalAntiCollision payload in SeparationFactorResults");
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
