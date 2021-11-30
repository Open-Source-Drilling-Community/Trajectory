using System;
using System.Collections.Generic;
using System.Threading;
using System.Data.SQLite;
using System.Text.Json;
using NORCE.Drilling.Trajectory.Model;

namespace NORCE.Drilling.Trajectory.Service
{
    /// <summary>
    /// A manager for trajectorySet. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class TrajectorySetManager
    {
        private static TrajectorySetManager instance_ = null;
        private Random random_ = new Random();

        public object lock_ = new object();

        /// <summary>
        /// default constructor is private when implementing a singleton pattern
        /// </summary>
        private TrajectorySetManager()
        {
            //Thread thread = new Thread(new ThreadStart(GC));
            //thread.Start();
        }

        public static TrajectorySetManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new TrajectorySetManager();
                }
                return instance_;

            }
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
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT COUNT(*) FROM TrajectorySet";
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                count = (int)reader.GetInt64(0);
                            }
                        }
                    }
                    catch (SQLiteException e)
                    {
                    }
                }
                return count;
            }
        }

        public bool Clear()
        {
            if (SQLConnectionManager.Instance.Connection != null)
            {
                bool success = false;
                lock (lock_)
                {
                    using (var transation = SQLConnectionManager.Instance.Connection.BeginTransaction())
                    {
                        try
                        {
                            var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                            command.CommandText = @"DELETE FROM TrajectorySet";
                            int count = command.ExecuteNonQuery();
                            transation.Commit();
                            success = true;
                        }
                        catch (SQLiteException e)
                        {
                            transation.Rollback();
                        }
                    }
                }
                return success;
            }
            else
            {
                return false;
            }
        }

        public bool Contains(string id)
        {
            int count = 0;
            if (SQLConnectionManager.Instance.Connection != null)
            {
                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                command.CommandText = @"SELECT COUNT(*) FROM TrajectorySet WHERE ID = " + "'" + id + "'";
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            count = (int)reader.GetInt64(0);
                        }
                    }
                }
                catch (SQLiteException e)
                {
                }
            }
            return count >= 1;
        }
        public List<string> GetIDs()
        {
            List<string> ids = new List<string>();
            if (SQLConnectionManager.Instance.Connection != null)
            {
                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                command.CommandText = @"SELECT ID FROM TrajectorySet";
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
                catch (SQLiteException e)
                {
                }
            }
            return ids;
        }

        public Model.Trajectory Get(int ID)
        {
            if (ID>=0)
            {
                Model.Trajectory trajectorySet = null;
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT DataSet FROM TrajectorySet WHERE ID = " + "'" + ID.ToString() + "'";
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
                                            trajectorySet = JsonSerializer.Deserialize<Model.Trajectory>(json);
                                            if (!trajectorySet.ID.Equals(ID))
                                            {
                                                trajectorySet.ID = ID;
                                            }
                                        }
                                        catch (Exception e)
                                        {

                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (SQLiteException e)
                    {
                    }
                }
                return trajectorySet;
            }
            else
            {
                return null;
            }
        }

        public Model.Trajectory GetByName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Model.Trajectory conversionSet = null;
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT DataSet FROM TrajectorySet WHERE Name = " + "'" + name + "'";
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
                                            conversionSet = JsonSerializer.Deserialize<Model.Trajectory>(json);
                                            if (!name.Equals(conversionSet.Name))
                                            {
                                                conversionSet.Name = name;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                        }
                                    }
                                }

                            }
                        }
                    }
                    catch (SQLiteException e)
                    {
                    }
                }
                return conversionSet;
            }
            else
            {
                return null;
            }
        }


        public bool Add(Model.Trajectory trajectorySet)
        {
            bool result = false;
            if (trajectorySet != null)
            {
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    if (trajectorySet.ID>0)
                    {
                        trajectorySet.ID = random_.Next();
                    }
                    lock (lock_)
                    {
                        using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                        {
                            try
                            {
                                string json = JsonSerializer.Serialize<Model.Trajectory>(trajectorySet);
                                bool ok = !json.Contains('\'');
                                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                                command.CommandText = @"INSERT INTO TrajectorySet (ID, Name, TimeStamp, DataSet) VALUES (" +
                                    "'" + trajectorySet.ID.ToString() + "'" + ", " +
                                    "'" + trajectorySet.Name + "'" + ", " +
                                    (DateTime.UtcNow - DateTime.MinValue).TotalSeconds.ToString() + ", " + "'" + json + "'" + ")";
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
                            catch (SQLiteException e)
                            {
                                transaction.Rollback();
                            }
                        }
                    }
                }
            }
            return result;
        }

        public bool Remove(Model.Trajectory trajectorySet)
        {
            bool result = false;
            if (trajectorySet != null)
            {
                result = Remove(trajectorySet.ID);
            }
            return result;
        }

        public bool Remove(int ID)
        {
            bool result = false;
            if (ID>0)
            {
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    lock (lock_)
                    {
                        using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                        {
                            try
                            {
                                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                                command.CommandText = @"DELETE FROM TrajectorySet WHERE ID = " + "'" + ID.ToString() + "'";
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
                            catch (SQLiteException e)
                            {
                                transaction.Rollback();
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
            if (SQLConnectionManager.Instance.Connection != null)
            {
                lock (lock_)
                {
                    using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                    {
                        try
                        {
                            var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                            command.CommandText = @"DELETE FROM TrajectorySet WHERE TimeStamp < " + (old - DateTime.MinValue).TotalSeconds.ToString();
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
                        catch (SQLiteException e)
                        {
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }

        public bool Update(int ID, Model.Trajectory upadedTrajectorySet)
        {
            bool result = false;
            if (ID>0&& upadedTrajectorySet != null)
            {
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    lock (lock_)
                    {
                        using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                        {
                            try
                            {
                                string json = JsonSerializer.Serialize<Model.Trajectory>(upadedTrajectorySet);
                                bool ok = !json.Contains('\'');

                                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                                command.CommandText = @"UPDATE TrajectorySet SET " +
                                    "Name =  " + "'" + upadedTrajectorySet.Name + "'" + ", " +
                                    "TimeStamp = " + (DateTime.UtcNow - DateTime.MinValue).TotalSeconds.ToString() + ", " +
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
                            catch (SQLiteException e)
                            {
                                transaction.Rollback();
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
