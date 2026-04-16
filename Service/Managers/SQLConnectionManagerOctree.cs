using System;
using System.Collections.Generic;
using System.Linq;
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
        internal const string WellboresIndexName = "GlobalOctreeWellboresIndex";
        internal const string WellboresIndexName2 = "GlobalOctreeWellboresIndex2";

        public SqlConnectionManagerOctree(ILogger<SqlConnectionManagerOctree> logger)
            : base(logger, DatabaseName, CreateTableStructureDict(), CreateTableIndexDefinitions())
        {
        }

        #region Helper methods to create database structure
        private static IReadOnlyDictionary<string, string[]> CreateTableStructureDict()
        {
            string[] cacheFields = CreateCacheLevelColumns(" TINYINT");

            return new Dictionary<string, string[]>
            {
                {
                    CacheTableName,
                    cacheFields
                },
                {
                    WellboresTableName,
                    [
                        "OctreeCodeCacheHigh BIGINT",
                        "OctreeCodeCacheLow BIGINT",
                        "OctreeDepth TINYINT",
                        "OctreeCodeHigh BIGINT",
                        "OctreeCodeLow BIGINT",
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
            string cacheIndexColumns = string.Join(", ", CreateCacheLevelColumns());

            return new Dictionary<string, string[]>
            {
                {
                    CacheTableName,
                    [$"CREATE UNIQUE INDEX {CacheIndexName} ON {CacheTableName} ({cacheIndexColumns})"]
                },
                {
                    WellboresTableName,
                    [
                        $"CREATE INDEX {WellboresIndexName} ON {WellboresTableName} (OctreeDepth, OctreeCodeHigh, OctreeCodeLow)",
                        $"CREATE INDEX {WellboresIndexName2} ON {WellboresTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow)"
                    ]
                }
            };
        }

        private static string[] CreateCacheLevelColumns(string suffix = "")
        {
            return Enumerable.Range(0, OctreeDepthCache)
                .Select(i => $"Level{i:00}{suffix}")
                .ToArray();
        }
        #endregion
    }
}
