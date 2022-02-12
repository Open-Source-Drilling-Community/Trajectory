using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;

namespace NORCE.Drilling.Trajectory.Service
{
    /// <summary>
    /// A manager for the sql database connection. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class SQLConnectionManager
    {
        private static SQLConnectionManager instance_ = null;

        private SQLiteConnection connection_ = null;

        private object lock_ = new object();

        /// <summary>
        /// default constructor is private when implementing a singleton pattern
        /// </summary>
        private SQLConnectionManager()
        {

        }

        public static SQLConnectionManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new SQLConnectionManager();
                }
                return instance_;

            }
        }

        public SQLiteConnection Connection
        {
            get
            {
                if (connection_ == null)
                {
                    Initialize();
                }
                return connection_;
            }
        }

        private void ManageTrajectory()
        {
            var command = connection_.CreateCommand();
            command.CommandText = @"SELECT count(*) FROM Trajectory";
            long count = -1;
            try
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        count = reader.GetInt64(0);
                    }
                }
            }
            catch (SQLiteException e)
            {
            }
            if (count < 0)
            {
                bool success = true;
                // table does no exist
                command.CommandText =
                    @"CREATE TABLE Trajectory (" +
                    "ID integer primary key, " +
                    "Name text, " +
                    "TimeStamp real, " +
                    "DataSet text " +
                   ")";
                try
                {
                    int res = command.ExecuteNonQuery();
                }
                catch (SQLiteException e)
                {
                    success = false;
                }
                if (success)
                {
                    command.CommandText =
                        @"CREATE UNIQUE INDEX TrajectoryIndex ON Trajectory (ID)";
                    try
                    {
                        int res = command.ExecuteNonQuery();
                    }
                    catch (SQLiteException e)
                    {
                        success = false;
                    }
                }
                if (!success)
                {
                    command.CommandText =
                        @"DROP TABLE Trajectory";
                    try
                    {
                        int res = command.ExecuteNonQuery();
                    }
                    catch (SQLiteException e)
                    {
                        success = false;
                    }
                }
            }
        }

        private void Initialize()
        {
            string homeDirectory = ".." + Path.DirectorySeparatorChar + "home";
            if (!Directory.Exists(homeDirectory))
            {
                try
                {
                    Directory.CreateDirectory(homeDirectory);
                }
                catch (Exception e)
                {

                }
            }
            if (Directory.Exists(homeDirectory))
            {
                string connectionString = @"URI=file:" + homeDirectory + Path.DirectorySeparatorChar + "Trajectory.db";
                connection_ = new SQLiteConnection(connectionString);
                connection_.Open();
                ManageTrajectory();
            }
        }

    }
}
