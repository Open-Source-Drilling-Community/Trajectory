using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    /// <summary>
    /// A manager for the sql database connection, registered as a singleton through dependency injection (see Program.cs)
    /// Prior to creating a database, existing database structure is checked for consistency with the structure defined in tableStructureDict_
    /// If inconsistent (table count, table names, fields count, fields names), a timestamped backup of the existing database is generated first
    /// </summary>
    /// <remarks>
    /// SQLite database connection strategy:
    /// - single connection for every access (chosen strategy in the general case)
    ///     each access to the database is performed through isolated connections stored in a List of connections
    ///     > isolation, reliability, fail-safe, thread-safe, but overhead due to opening connections
    /// - shared connection between access
    ///     one connection is opened for the lifetime of the application and used to access database through various web requests and commands 
    ///     > no overhead, but issues with concurrency, single-point of failure, state management
    /// - scoped connection (registering service with AddScoped rather than AddSingleton)
    ///     one connection is opened per web request
    ///     > same problems as with shared connection, but limited to the scope of one webrequest rather than to the whole lifetime of the application
    /// </remarks>
    public class SqlConnectionManagerTrajectory : SqlConnectionManager
    {
        private const string DatabaseName = "Trajectory.db";

        // dictionary describing tables format
        // Light weight data fields are enumerated explicitly in the data table implementing the light weight data concept
        // (thus duplicating info in the database) for 2 reasons
        // 1) to avoid loading the complete Trajectory (heavy weight data) each time we only need contextual info on the data (light weight data)
        // 2) to keep control of the logic of inserting and selecting a light data in the database
        //    localized at the controller/manager level (storing TrajectoryLight as a whole could induce database corruption issues)
        // If the light weight data concept is not implemented, the same contextual info can be retrieved directly from the Trajectory
        private static readonly IReadOnlyDictionary<string, string[]> TableStructureDictTrajectory = new Dictionary<string, string[]>()
            {
                { "TrajectoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldID text",
                    "ClusterID text",
                    "WellID text",
                    "WellBoreID text",
                    "TrajectoryType text",
                    "IsDefinitive integer",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "Trajectory text" }
                },
                { "SurveyRunTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldID text",
                    "ClusterID text",
                    "WellID text",
                    "WellBoreID text",
                    "SurveyInstrumentID text",
                    "SurveyRunType text",
                    "CalculationType text",
                    "ParentSurveyRunID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "SurveyRun text" }
                },
                { "SurveyRunMeasurementChunkTable", new string[] {
                    "ID text primary key",
                    "SurveyRunID text",
                    "ChunkIndex integer",
                    "MeasurementCount integer",
                    "StartMD real",
                    "EndMD real",
                    "SurveyMeasurementChunk text" }
                },
                { "SurveyStationChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "OwnerType text",
                    "ChunkIndex integer",
                    "StationCount integer",
                    "StartMD real",
                    "EndMD real",
                    "SurveyStationChunk text" }
                },
                { "InterpolatedTrajectoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "TrajectoryID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "InterpolatedTrajectory text" }
                },
                { "TrajectoryRealizationCaseTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "TrajectoryID text",
                    "RealizationCount integer",
                    "CoarseningMaximumDistance real",
                    "RandomSeed integer",
                    "ReferenceStationCount integer",
                    "CoarsenedStationCount integer",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "TrajectoryRealizationCase text" }
                },
                { "TrajectoryRealizationChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "RealizationCount integer",
                    "SurveyPointCount integer",
                    "StartMD real",
                    "EndMD real",
                    "TrajectoryRealizationChunk text" }
                },
                { "SurveyRunBatchImportTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "SurveyRunBatchImport text" }
                },
                { "SurveyStationEllipseCalculationTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "ConfidenceFactor real",
                    "SurveyStationEllipseCalculation text" }
                },
                { "TrajectoryMinimumDistanceCalculationTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "ReferenceTrajectoryID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "ResultCount integer",
                    "IntervalResultCount integer",
                    "TrajectoryMinimumDistanceCalculation text" }
                },
                { "TrajectoryMinimumDistanceResultChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "ResultCount integer",
                    "StartReferenceMD real",
                    "EndReferenceMD real",
                    "TrajectoryMinimumDistanceResultChunk text" }
                },
                { "SurveyRunMinimumDistanceCalculationTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "ReferenceSurveyRunID text",
                    "CalculationState text",
                    "CalculationProgress real",
                    "CalculationMessage text",
                    "ResultCount integer",
                    "IntervalResultCount integer",
                    "SurveyRunMinimumDistanceCalculation text" }
                },
                { "SurveyRunMinimumDistanceResultChunkTable", new string[] {
                    "ID text primary key",
                    "OwnerID text",
                    "ChunkIndex integer",
                    "ResultCount integer",
                    "StartReferenceMD real",
                    "EndReferenceMD real",
                    "SurveyRunMinimumDistanceResultChunk text" }
                }
            };

        public SqlConnectionManagerTrajectory(ILogger<SqlConnectionManagerTrajectory> logger)
            : base(logger, DatabaseName, TableStructureDictTrajectory)
        {
        }
    }
}
