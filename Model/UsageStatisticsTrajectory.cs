using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NORCE.Drilling.Trajectory.Model
{
    public struct CountPerDay
    {
        public DateTime Date { get; set; }
        public ulong Count { get; set; }

        public CountPerDay() { }

        public CountPerDay(DateTime date, ulong count)
        {
            Date = date;
            Count = count;
        }
    }

    public class History
    {
        public List<CountPerDay> Data { get; set; } = new List<CountPerDay>();

        public History()
        {
            if (Data == null)
            {
                Data = new List<CountPerDay>();
            }
        }

        public void Increment()
        {
            if (Data.Count == 0)
            {
                Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
            }
            else if (Data[Data.Count - 1].Date < DateTime.UtcNow.Date)
            {
                Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
            }
            else
            {
                Data[Data.Count - 1] = new CountPerDay(Data[Data.Count - 1].Date, Data[Data.Count - 1].Count + 1);
            }
        }
    }

    public class UsageStatisticsTrajectory
    {
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;

        public DateTime LastSaved { get; set; } = DateTime.MinValue;
        public TimeSpan BackUpInterval { get; set; } = TimeSpan.FromMinutes(5);

        public History GetAllTrajectoryIdPerDay { get; set; } = new History();
        public History GetAllTrajectoryMetaInfoPerDay { get; set; } = new History();
        public History GetTrajectoryByIdPerDay { get; set; } = new History();
        public History GetAllTrajectoryLightPerDay { get; set; } = new History();
        public History GetAllTrajectoryPerDay { get; set; } = new History();
        public History PostTrajectoryPerDay { get; set; } = new History();
        public History PutTrajectoryByIdPerDay { get; set; } = new History();
        public History DeleteTrajectoryByIdPerDay { get; set; } = new History();

        private static readonly object lock_ = new object();
        private static UsageStatisticsTrajectory? instance_ = null;

        public static UsageStatisticsTrajectory Instance
        {
            get
            {
                if (instance_ == null)
                {
                    if (File.Exists(HOME_DIRECTORY + "history.json"))
                    {
                        try
                        {
                            string? jsonStr = null;
                            lock (lock_)
                            {
                                using (StreamReader reader = new StreamReader(HOME_DIRECTORY + "history.json"))
                                {
                                    jsonStr = reader.ReadToEnd();
                                }

                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    instance_ = JsonSerializer.Deserialize<UsageStatisticsTrajectory>(jsonStr);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (instance_ == null)
                    {
                        instance_ = new UsageStatisticsTrajectory();
                    }
                }

                return instance_;
            }
        }

        public void IncrementGetAllTrajectoryIdPerDay()
        {
            lock (lock_)
            {
                GetAllTrajectoryIdPerDay ??= new History();
                GetAllTrajectoryIdPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementGetAllTrajectoryMetaInfoPerDay()
        {
            lock (lock_)
            {
                GetAllTrajectoryMetaInfoPerDay ??= new History();
                GetAllTrajectoryMetaInfoPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementGetTrajectoryByIdPerDay()
        {
            lock (lock_)
            {
                GetTrajectoryByIdPerDay ??= new History();
                GetTrajectoryByIdPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementGetAllTrajectoryLightPerDay()
        {
            lock (lock_)
            {
                GetAllTrajectoryLightPerDay ??= new History();
                GetAllTrajectoryLightPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementGetAllTrajectoryPerDay()
        {
            lock (lock_)
            {
                GetAllTrajectoryPerDay ??= new History();
                GetAllTrajectoryPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementPostTrajectoryPerDay()
        {
            lock (lock_)
            {
                PostTrajectoryPerDay ??= new History();
                PostTrajectoryPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementPutTrajectoryByIdPerDay()
        {
            lock (lock_)
            {
                PutTrajectoryByIdPerDay ??= new History();
                PutTrajectoryByIdPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementDeleteTrajectoryByIdPerDay()
        {
            lock (lock_)
            {
                DeleteTrajectoryByIdPerDay ??= new History();
                DeleteTrajectoryByIdPerDay.Increment();
                ManageBackup();
            }
        }

        private void ManageBackup()
        {
            if (DateTime.UtcNow > LastSaved + BackUpInterval)
            {
                LastSaved = DateTime.UtcNow;
                try
                {
                    string jsonStr = JsonSerializer.Serialize(this);
                    if (!string.IsNullOrEmpty(jsonStr) && Directory.Exists(HOME_DIRECTORY))
                    {
                        using (StreamWriter writer = new StreamWriter(HOME_DIRECTORY + "history.json"))
                        {
                            writer.Write(jsonStr);
                            writer.Flush();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
