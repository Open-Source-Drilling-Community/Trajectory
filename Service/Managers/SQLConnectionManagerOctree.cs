using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class SqlConnectionManagerOctree : SqlConnectionManager
    {
        public static int OctreeDepthCache = 21;

        private const string DatabaseName = "GlobalAntiCollision.db";
        internal const string CacheTableName = "GlobalOctreeCache";
        internal const string WellboresTableName = "GlobalOctreeWellbores";
        internal const string CacheIndexName = "GlobalOctreeCacheIndex";
        internal const string CacheTrajectoryIndexName = "GlobalOctreeCacheTrajectoryIndex";
        internal const string WellboresTrajectoryIndexName = "GlobalOctreeWellboresTrajectoryIndex";
        internal const string WellboresFilterIndexName = "GlobalOctreeWellboresFilterIndex";

        public SqlConnectionManagerOctree(ILogger<SqlConnectionManagerOctree> logger)
            : base(logger, DatabaseName, CreateTableStructureDict(), CreateTableIndexDefinitions())
        {
        }

        #region Helper methods to create database structure
        private static IReadOnlyDictionary<string, string[]> CreateTableStructureDict()
        {
            return new Dictionary<string, string[]>
            {
                {
                    CacheTableName,
                    [
                        "OctreeCodeCacheHigh BIGINT",
                        "OctreeCodeCacheLow BIGINT",
                        "TrajectoryID TEXT",
                        "IsPlanned BOOL",
                        "IsMeasured BOOL",
                        "IsDefinitive BOOL",
                        "OctreeCodeCount INTEGER",
                        "OctreeCodes BLOB"
                    ]
                },
                {
                    WellboresTableName,
                    [
                        "TrajectoryID TEXT",
                        "IsPlanned BOOL",
                        "IsMeasured BOOL",
                        "IsDefinitive BOOL"
                    ]
                }
            };
        }

        private static IReadOnlyDictionary<string, string[]> CreateTableIndexDefinitions()
        {
            return new Dictionary<string, string[]>
            {
                {
                    CacheTableName,
                    [
                        $"CREATE UNIQUE INDEX {CacheIndexName} ON {CacheTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID)",
                        $"CREATE INDEX {CacheIndexName}Lookup ON {CacheTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, IsPlanned, IsMeasured, IsDefinitive, TrajectoryID)",
                        $"CREATE INDEX {CacheTrajectoryIndexName} ON {CacheTableName} (TrajectoryID)"
                    ]
                },
                {
                    WellboresTableName,
                    [
                        $"CREATE UNIQUE INDEX {WellboresTrajectoryIndexName} ON {WellboresTableName} (TrajectoryID)",
                        $"CREATE INDEX {WellboresFilterIndexName} ON {WellboresTableName} (IsPlanned, IsMeasured, IsDefinitive, TrajectoryID)"
                    ]
                }
            };
        }
        #endregion
    }
}
