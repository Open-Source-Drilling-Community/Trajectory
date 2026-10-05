using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OSDC.Drilling.Trajectory.Model
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
        public History GetAllTrajectoryExtrapolationCaseIdPerDay { get; set; } = new History();
        public History GetAllTrajectoryExtrapolationCaseMetaInfoPerDay { get; set; } = new History();
        public History GetAllTrajectoryExtrapolationCaseLightPerDay { get; set; } = new History();
        public History GetAllTrajectoryExtrapolationCasePerDay { get; set; } = new History();
        public History GetTrajectoryExtrapolationCaseByIdPerDay { get; set; } = new History();
        public History GetTrajectoryExtrapolationCaseStatusPerDay { get; set; } = new History();
        public History GetTrajectoryExtrapolationSurveyStationChunkCountPerDay { get; set; } = new History();
        public History GetTrajectoryExtrapolationSurveyStationChunkPerDay { get; set; } = new History();
        public History PostTrajectoryExtrapolationCasePerDay { get; set; } = new History();
        public History PutTrajectoryExtrapolationCaseByIdPerDay { get; set; } = new History();
        public History DeleteTrajectoryExtrapolationCaseByIdPerDay { get; set; } = new History();
        public History GetAllTargetLandingCaseIdPerDay { get; set; } = new History();
        public History GetAllTargetLandingCaseMetaInfoPerDay { get; set; } = new History();
        public History GetAllTargetLandingCaseLightPerDay { get; set; } = new History();
        public History GetAllTargetLandingCasePerDay { get; set; } = new History();
        public History GetTargetLandingCaseByIdPerDay { get; set; } = new History();
        public History GetTargetLandingCaseEditDataPerDay { get; set; } = new History();
        public History GetTargetLandingCaseDisplayDataPerDay { get; set; } = new History();
        public History GetTargetLandingCaseUncertaintyDisplayDataPerDay { get; set; } = new History();
        public History GetTargetLandingCaseStatusPerDay { get; set; } = new History();
        public History PostTargetLandingCasePerDay { get; set; } = new History();
        public History PutTargetLandingCaseByIdPerDay { get; set; } = new History();
        public History DeleteTargetLandingCaseByIdPerDay { get; set; } = new History();
        public History GetAllAntiCollisionPolicyRevisionIdPerDay { get; set; } = new History();
        public History GetAllAntiCollisionPolicyRevisionPerDay { get; set; } = new History();
        public History GetAntiCollisionPolicyRevisionByIdPerDay { get; set; } = new History();
        public History PostAntiCollisionPolicyRevisionPerDay { get; set; } = new History();
        public History DeleteAntiCollisionPolicyByPolicyIdPerDay { get; set; } = new History();
        public History GetAllFieldAntiCollisionPolicyAssignmentPerDay { get; set; } = new History();
        public History GetFieldAntiCollisionPolicyAssignmentByIdPerDay { get; set; } = new History();
        public History GetEffectiveFieldAntiCollisionPolicyAssignmentPerDay { get; set; } = new History();
        public History PostFieldAntiCollisionPolicyAssignmentPerDay { get; set; } = new History();
        public History PutFieldAntiCollisionPolicyAssignmentByIdPerDay { get; set; } = new History();
        public History DeleteFutureFieldAntiCollisionPolicyAssignmentByIdPerDay { get; set; } = new History();

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

        public void IncrementTrajectoryExtrapolationOperation(string operation)
        {
            IncrementOperation(operation);
        }

        public void IncrementOperation(string operation)
        {
            lock (lock_)
            {
                System.Reflection.PropertyInfo? property = GetType().GetProperty(operation + "PerDay");
                if (property?.PropertyType != typeof(History))
                    throw new ArgumentException($"Unknown Trajectory usage operation '{operation}'.", nameof(operation));
                History history = (History?)property.GetValue(this) ?? new History();
                property.SetValue(this, history);
                history.Increment();
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
