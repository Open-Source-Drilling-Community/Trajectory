using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Text.Json;
using System.IO;
using System.Net.Http;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.SurveyInstrument.Model;

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
                            int ID = (int)reader.GetInt64(0);
                            ids.Add(ID);
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
                                int ID = (int)reader.GetInt64(0);
                                ids.Add(ID);
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
                Model.Trajectory trajectory = null;
                if (SQLConnectionManager.Instance.Connection != null)
                {
                    var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                    command.CommandText = @"SELECT DataSet FROM Trajectory WHERE ID = " + trajectoryID.ToString();
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
                                            if (trajectory.ID != trajectoryID)
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
                    using (var transaction = SQLConnectionManager.Instance.Connection.BeginTransaction())
                    {
                        try
                        {
                            string json = JsonSerializer.Serialize<Model.Trajectory>(trajectory);
                            bool ok = !json.Contains('\'');
                            var command = SQLConnectionManager.Instance.Connection.CreateCommand();
                            command.CommandText = @"INSERT INTO Trajectory (ID, Name, TimeStamp, DataSet) VALUES (" +
                                + trajectory.ID + ", " +
                                "'" + trajectory.Name + "'" + ", " +
                                "'" + (DateTime.UtcNow - DateTime.MinValue).TotalSeconds.ToString() + "'" + ", " + 
                                "'" + json + "'" + ")";
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
                            command.CommandText = @"DELETE FROM Trajectory WHERE ID = " + trajectoryID.ToString();
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
                updatedTrajectory.ID = trajectoryID;
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
                                "WHERE ID = " + trajectoryID;
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
            int id;
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
                string homeDirectory = ".." + Path.DirectorySeparatorChar + "home";
                string directory = @homeDirectory + Path.DirectorySeparatorChar + "Wellbores";
                // The trajectories MD/TVD should be relative to the slot/cluster since different rigs with different RTE's can operate the same wellbore
                double rotaryTableElevation_Ullrigg = 8.78;
                //string directory = @"\Wellbores";
                if (Directory.Exists(directory))
                {
                    Model.Trajectory trajectory = new Model.Trajectory();
                    trajectory.SurveyList = new SurveyList();
                    trajectory.SurveyList.Surveys = new List<SurveyStation>();
                    string[] files = Directory.GetFiles(directory);
                    foreach (string file in files)
                    {
                        using (StreamReader r = new StreamReader(file))
                        {
                            SurveyList sl = new SurveyList();
                            //CultureInfo culture = CultureInfo.InvariantCulture;
                            bool startingPointAdded = false;
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
                                    st.Z = tvd - rotaryTableElevation_Ullrigg;
                                    st.MD = md - rotaryTableElevation_Ullrigg;

                                    var surveyToolll = LoadSurveyTool(1);

									//surveyTool = new SurveyInstrument.Model.SurveyInstrument(SurveyInstrument.Model.SurveyInstrument.WdWGoodMag);
									//st.SurveyTool = surveyTool;
									//WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
									//st.Uncertainty = wdwun;
									surveyTool = new SurveyInstrument.Model.SurveyInstrument(SurveyInstrument.Model.SurveyInstrument.ISCWSA_MWD_Rev5_OWSG);
                                    st.SurveyTool = surveyTool;
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    st.Uncertainty = iscwsaun;
                                    if (st.MD < 0 && st.Z < 0)
                                    {
                                        if (startingPointAdded)
                                        {
                                            st = null;
                                        }
                                        else
                                        {
                                            st.MD = 0;
                                            st.Z = 0;
                                            startingPointAdded = true;
                                        }
                                    }
                                    if (st != null)
                                    {
                                        sl.Add(st);
                                    }
                                }
                            }
                            trajectory.SurveyList = sl;
                            trajectory.SurveyList.ListOfSurveys = sl.ListOfSurveys;
                            trajectory.SurveyList.GetUncertaintyEnvelope(0.95, 1);
                            trajectory.Name = file.Substring(18).Split('.')[0].Split('-')[0] + "-Trajectory";
                            trajectory.Description = trajectory.Name + " at Ullrigg";
                            trajectory.ID = random_.Next();
                            Add(trajectory);
                        }
                    }
                }
            }
        }

        public async Task<SurveyInstrument.Model.SurveyInstrument> LoadSurveyTool(int id)
        {
            SurveyInstrument.Model.SurveyInstrument surveyList = await LoadSurveyTool("https://app.DigiWells.no/", id);
            if (surveyList == null)
            {
                surveyList = await LoadSurveyTool("http://host.docker.internal:10002/", id);
            }
            if (surveyList == null)
            {
                // Running in Docker via VS
                surveyList = await LoadSurveyTool("https://localhost:44369/", id);
            }
            if (surveyList == null)
            {
                // Running both services in VS without Docker
                surveyList = await LoadSurveyTool("https://localhost:10001/", id);
            }
            if (surveyList == null)
            {
                // Running both services in VS without Docker
                surveyList = await LoadSurveyTool("http://localhost:50002/", id);
            }
            return surveyList;
        }

        public async Task<SurveyInstrument.Model.SurveyInstrument> LoadSurveyTool(string host, int id)
        {
            HttpClient httpTrajectory;
            SurveyInstrument.Model.SurveyInstrument trajectory = null;
            try
            {
                httpTrajectory = new HttpClient();
                httpTrajectory.BaseAddress = new Uri(host + "SurveyInstrument/api/");
                httpTrajectory.DefaultRequestHeaders.Accept.Clear();
                httpTrajectory.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpTrajectory.GetAsync("SurveyInstruments");
                if (a.IsSuccessStatusCode)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        SurveyInstrument.Model.SurveyInstrument trajectoryShared = Newtonsoft.Json.JsonConvert.DeserializeObject<SurveyInstrument.Model.SurveyInstrument>(str);
                        if (trajectoryShared != null && trajectoryShared.Name != null )
                        {
                            // Convert trajectoryShared to a SurveyList
                            //trajectory = new SurveyList();
                            //foreach (Trajectory.ModelClientShared.SurveyStation ss in trajectoryShared.SurveyList.ListOfSurveys)
                            //{
                            //    SurveyStation s = new SurveyStation();
                            //    s.MD = ss.MD;
                            //    s.Incl = ss.Incl;
                            //    s.Az = ss.Az;
                            //    s.X = ss.X;
                            //    s.Y = ss.Y;
                            //    s.Z = ss.Z;
                            //    s.Abscissa = ss.Abscissa;

                            //    // The WdW uncertainty is default in the methods we use here. If others should be used, we have to translate them from the shared object
                            //    //s.Uncertainty = new Trajectory.WdWSurveyStationUncertainty();

                            //    trajectory.ListOfSurveys.Add(s);
                            //}
                        }
                    }
                }
            }
            catch (Exception e)
            {
                httpTrajectory = null;
                trajectory = null;
            }
            return trajectory;
        }
        private void GetSurveyTool()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));


                var response = client.GetAsync("http://localhost:50002/SurveyInstrument/api/surveyInstruments").Result;

                string content = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(content);
                Console.ReadLine();
            }
        }

        private SurveyInstrument.Model.SurveyInstrument surveyTool = new SurveyInstrument.Model.SurveyInstrument();
        HttpClient httpSurveyInstrument;
        private async void GetSurveyTool2()
        {
            //SurveyInstrument.Model.SurveyInstrument surveyInstrument = new SurveyInstrument.Model.SurveyInstrument();
            string host = "http://localhost:50002/";
            int[] initialSurveyInstrumentIDs = null;
            List<string> initialSurveyInstruments = null;
            try
            {
                httpSurveyInstrument = new HttpClient();
                httpSurveyInstrument.BaseAddress = new Uri(host + "SurveyInstrument/api/");
                httpSurveyInstrument.DefaultRequestHeaders.Accept.Clear();
                httpSurveyInstrument.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpSurveyInstrument.GetAsync("SurveyInstruments");
                if (a.IsSuccessStatusCode)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        initialSurveyInstrumentIDs = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(str);
                        for (int i = 0; i < initialSurveyInstrumentIDs.Length; i++)
                        {
                            var b = await httpSurveyInstrument.GetAsync("SurveyInstruments/" + initialSurveyInstrumentIDs[i].ToString());
                            if (b.IsSuccessStatusCode && a.Content != null)
                            {
                                str = await b.Content.ReadAsStringAsync();
                                if (!string.IsNullOrEmpty(str))
                                {
                                    surveyTool = Newtonsoft.Json.JsonConvert.DeserializeObject<SurveyInstrument.Model.SurveyInstrument>(str);
                                    if (surveyTool != null)
                                    {
                                        initialSurveyInstruments.Add(surveyTool.Name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                httpSurveyInstrument = null;
                initialSurveyInstruments = null;
            }            
        }
    }
}
