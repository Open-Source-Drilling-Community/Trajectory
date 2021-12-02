using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Text.Json;
using System.IO;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.SurveyInstrument;

namespace NORCE.Drilling.Trajectory.Service
{
    /// <summary>
    /// A manager for trajectories. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class TrajectoryManager
    {
        private static TrajectoryManager instance_ = null;

        private Random random_ = new Random();

        /// <summary>
        /// default constructor is private when implementing a singleton pattern
        /// </summary>
        private TrajectoryManager()
        {

        }

        public static TrajectoryManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new TrajectoryManager();
                    instance_.FillDefault();
                }
                return instance_;

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
                    command.CommandText = @"SELECT COUNT(*) FROM Trajectory";
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
                using (var transation = SQLConnectionManager.Instance.Connection.BeginTransaction())
                {
                    try
                    {
                        var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                        command.CommandText = @"DELETE FROM Trajectory";
                        int count = command.ExecuteNonQuery();
                        transation.Commit();
                        success = true;
                    } catch (SQLiteException e)
                    {
                        transation.Rollback();
                    }
                }
                return success;
            }
            else
            {
                return false;
            }
        }

        public bool Contains(int id)
        {
            int count = 0;
            if (SQLConnectionManager.Instance.Connection != null)
            {
                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                command.CommandText = @"SELECT COUNT(*) FROM Trajectory WHERE ID = " + id.ToString();
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
        public List<int> GetIDs()
        {
            List<int> ids = new List<int>();
            if (SQLConnectionManager.Instance.Connection != null)
            {
                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                command.CommandText = @"SELECT ID FROM Trajectory";
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                //long inttt = reader.GetInt64(0);
								string IDString = reader.GetString(0);
                                int ID = (int)Int64.Parse(IDString);
								ids.Add(ID);
							}
                            catch (Exception e)
							{

							}
                        }
                    }
                }
                catch (SQLiteException e)
                {
                }
            }
            if(ids.Count==0)
			{
                FillDefault();
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT ID FROM Trajectory";
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    //long inttt = reader.GetInt64(0);
                                    string IDString = reader.GetString(0);
                                    int ID = (int)Int64.Parse(IDString);
                                    ids.Add(ID);
                                }
                                catch (Exception e)
                                {

                                }
                            }
                        }
                    }
                    catch (SQLiteException e)
                    {
                    }
                }
            }
            return ids;
        }

        public Model.Trajectory Get(int trajectoryID)
        {
            if (trajectoryID > 0)
            {
                Model.Trajectory trajectory= null;
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT DataSet FROM Trajectory WHERE ID = " + "'" + trajectoryID.ToString() + "'";
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
                                            trajectory = JsonSerializer.Deserialize<Model.Trajectory>(json);
                                            if (!trajectory.ID.Equals(trajectoryID))
                                            {
                                                trajectory.ID = trajectoryID;
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
                return trajectory;
            }
            else
            {
                return null;
            }
        }
 
        public bool Add(Model.Trajectory trajectory)
        {
            bool result = false;
            if (trajectory != null)
            {
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    if (trajectory.ID <= 0)
                    {
                        trajectory.ID = GetNextID();
                    }
                    //lock (lock_)
                    {
                        using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                        {
                            try
                            {
                                string json = JsonSerializer.Serialize<Model.Trajectory>(trajectory);
                                bool ok = !json.Contains('\'');
                                var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                                command.CommandText = @"INSERT INTO Trajectory (" +
                                    "ID, " +
                                    "Name, " +
                                    "TimeStamp, " +
                                    "DataSet " +
                                    ") VALUES (" +
                                    "'" + trajectory.ID.ToString() + "'" + ", " +
                                    "'" + trajectory.Name + "'" + ", " +
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

        public bool Remove(Model.Trajectory trajectory)
        {
            bool result = false;
            if (trajectory != null)
            {
                result = Remove(trajectory.ID);
            }
            return result;
        }

        public bool Remove(int trajectoryID)
        {
            bool result = false;
            if (trajectoryID > 0)
            {
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                    {
                        try
                        {
                            var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                            command.CommandText = @"DELETE FROM Trajectory WHERE ID = " + "'" + trajectoryID.ToString() + "'";
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

        public bool Update(int trajectoryID, Model.Trajectory updatedTrajectory)
        {
            bool result = false;
            if (trajectoryID > 0 && updatedTrajectory != null)
            {
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                    {
                        try
                        {
                            string json = JsonSerializer.Serialize<Model.Trajectory>(updatedTrajectory);
                            bool ok = !json.Contains('\'');

                            var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                            command.CommandText = @"UPDATE Trajectory SET " +
                                "Name =  " + "'" + updatedTrajectory.Name + "'" + ", " +
                                "TimeStamp = " + (DateTime.UtcNow - DateTime.MinValue).TotalSeconds.ToString() + ", " +
                                "DataSet = " + "'" + json + "'" + " " +
                                "WHERE ID = " + "'" + trajectoryID.ToString() + "'";
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
            return result;
        }

        public int GetNextID()
        {
            int id = -1;
            bool exists = false;
            do
            {
                id = random_.Next();
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT count(*) FROM Trajectory WHERE ID = " + id.ToString();
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int count = (int)reader.GetInt64(0);
                                exists = count > 0;
                            }
                        }
                    }
                    catch (SQLiteException e)
                    {
                    }
                }
            }
            while (exists);
            return id;
        }

        /// <summary>
        /// populate with a few default trajectories
        /// </summary>
        private void FillDefault()
        {
			if (Count <= 0)
			{
				Model.Trajectory trajectory = new Model.Trajectory();
                trajectory.Name = "Trajectory1";
                trajectory.Description = "UllriggWell";
                trajectory.SurveyList = new SurveyList();
                trajectory.SurveyList.Surveys = new List<SurveyStation>();

                //string[] files = Directory.GetFiles(@"C:\NORCE-DrillingAndWells\AutomatedDrillingEngineeringDemoSummer2021\NORCE.DirectionalSurvyeingAnalyzerDisplayApp\InputData\Wellbores");
                string[] files = Directory.GetFiles(@"..\Wellbores");
                int id = 0;
                foreach (string file in files)
                {
                    id++;
                    using (StreamReader r = new StreamReader(file))
                    {
                        SurveyList sl = new SurveyList();
                        //CultureInfo culture = CultureInfo.InvariantCulture;
                        while (!r.EndOfStream)
                        {
                            char[] sep = { '\t' };
                            string[] words = r.ReadLine().Split(sep);
                            if (words.Length > 1)
                            {
                                SurveyStation st = new SurveyStation();
                                double md = 0.0;
                                bool ok = NORCE.General.Std.Numeric.TryParse(words[0], out md);
                                double incl = 0.0;
                                ok = NORCE.General.Std.Numeric.TryParse(words[1], out incl);
                                double az = 0.0;
                                ok = NORCE.General.Std.Numeric.TryParse(words[2], out az);
                                double tvd = 0.0;
                                ok = NORCE.General.Std.Numeric.TryParse(words[3], out tvd);
                                double X = 0.0;
                                ok = NORCE.General.Std.Numeric.TryParse(words[4], out X);
                                double Y = 0.0;
                                ok = NORCE.General.Std.Numeric.TryParse(words[5], out Y);
                                st.Az = az * Math.PI / 180.0;
                                st.Incl = incl * Math.PI / 180.0; ;
                                st.X = X;
                                st.Y = Y;
                                st.Z = tvd;
                                st.MD = md;
                                WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                                WdWSurveyTool surveyTool = new WdWSurveyTool(WdWSurveyTool.GoodMag);
                                wdwun.SurveyTool = surveyTool;
                                st.Uncertainty = wdwun;
                                sl.Add(st);
                            }
                        }
                        trajectory.SurveyList = sl;
                        trajectory.SurveyList.ListOfSurveys = sl.ListOfSurveys;
                        trajectory.SurveyList.GetUncertaintyEnvelope(0.95, 1);
                        trajectory.Name = file.Substring(13);
                        trajectory.ID = id;
                        Add(trajectory);

                        //SurveyListCollection.Add(sl);
                        //string wellname = file.Substring(29);
                    }
                }
                Get(1);
            }
        }
    }
}
