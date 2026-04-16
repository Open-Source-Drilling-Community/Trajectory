using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public class SqlConnectionManagerOctree : SqlConnectionManager
    {
        public static int OctreeDepthCache = 21;

        private const string DatabaseName = "GlobalAntiCollision.db";
        private const string CacheTableName = "GlobalOctreeCache";
        private const string WellboresTableName = "GlobalOctreeWellbores";
        private const string CacheIndexName = "GlobalOctreeCacheIndex";
        private const string WellboresIndexName = "GlobalOctreeWellboresIndex";
        private const string WellboresIndexName2 = "GlobalOctreeWellboresIndex2";

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
        #endregion

        #region Helper methods to read data from database
        public bool ContainsInCache(byte[]? octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            byte[] validCode = octreeCode!;

            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {CacheTableName} WHERE {BuildCacheMatchClause()}";
            AddCacheParameters(command, validCode);

            try
            {
                return Convert.ToInt64(command.ExecuteScalar()) > 0;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool AddInCache(byte[]? octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            byte[] validCode = octreeCode!;

            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            string[] cacheColumns = CreateCacheLevelColumns();
            command.CommandText =
                $"INSERT INTO {CacheTableName} ({string.Join(", ", cacheColumns)}) VALUES ({string.Join(", ", cacheColumns.Select((_, i) => $"@level{i}"))})";
            AddCacheParameters(command, validCode);

            try
            {
                return command.ExecuteNonQuery() == 1;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool DeleteInCache(byte[]? octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            byte[] validCode = octreeCode!;

            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {CacheTableName} WHERE {BuildCacheMatchClause()}";
            AddCacheParameters(command, validCode);

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Contains(OctreeCodeLong code, Guid trajectoryID)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT COUNT(*) FROM {WellboresTableName} WHERE OctreeDepth = @depth AND OctreeCodeHigh = @high AND OctreeCodeLow = @low AND TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@depth", code.Depth);
            command.Parameters.AddWithValue("@high", (long)code.CodeHigh);
            command.Parameters.AddWithValue("@low", (long)code.CodeLow);
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            try
            {
                return Convert.ToInt64(command.ExecuteScalar()) > 0;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Contains(Guid trajectoryID)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {WellboresTableName} WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            try
            {
                return Convert.ToInt64(command.ExecuteScalar()) > 0;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public List<Guid> GetDetails(OctreeCodeLong truncatedCode)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TrajectoryID FROM {WellboresTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow";
            command.Parameters.AddWithValue("@cacheHigh", (long)truncatedCode.CodeHigh);
            command.Parameters.AddWithValue("@cacheLow", (long)truncatedCode.CodeLow);

            List<Guid> results = [];
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(ReadGuid(reader, 0));
                }
            }
            catch (SqliteException)
            {
                return [];
            }

            return results;
        }

        public List<Pair<OctreeCodeLong, Guid>> GetDetails(List<OctreeCodeLong>? truncatedCodes, bool isPlanned, bool isMeasured, bool isDefinitive, Guid? ignoredTrajectoryID = null)
        {
            if (truncatedCodes == null || truncatedCodes.Count == 0)
            {
                return [];
            }

            using var connection = GetConnection();
            if (connection == null)
            {
                return [];
            }

            List<Pair<OctreeCodeLong, Guid>> results = [];
            foreach (OctreeCodeLong truncatedCode in truncatedCodes)
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"SELECT OctreeDepth, OctreeCodeHigh, OctreeCodeLow, TrajectoryID FROM {WellboresTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow AND IsPlanned = @isPlanned AND IsMeasured = @isMeasured AND IsDefinitive = @isDefinitive";
                command.Parameters.AddWithValue("@cacheHigh", (long)truncatedCode.CodeHigh);
                command.Parameters.AddWithValue("@cacheLow", (long)truncatedCode.CodeLow);
                command.Parameters.AddWithValue("@isPlanned", isPlanned);
                command.Parameters.AddWithValue("@isMeasured", isMeasured);
                command.Parameters.AddWithValue("@isDefinitive", isDefinitive);
                if (ignoredTrajectoryID != null)
                {
                    command.CommandText += " AND TrajectoryID <> @ignoredTrajectoryId";
                    command.Parameters.AddWithValue("@ignoredTrajectoryId", ignoredTrajectoryID.Value.ToString());
                }

                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        results.Add(new Pair<OctreeCodeLong, Guid>(ReadCode(reader, 0), ReadGuid(reader, 3)));
                    }
                }
                catch (SqliteException)
                {
                    return [];
                }
            }

            return results;
        }

        public List<Guid> GetAllTrajectoryIDs(bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT DISTINCT TrajectoryID FROM {WellboresTableName} WHERE IsPlanned = @isPlanned AND IsMeasured = @isMeasured AND IsDefinitive = @isDefinitive";
            command.Parameters.AddWithValue("@isPlanned", isPlanned);
            command.Parameters.AddWithValue("@isMeasured", isMeasured);
            command.Parameters.AddWithValue("@isDefinitive", isDefinitive);

            List<Guid> results = [];
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(ReadGuid(reader, 0));
                }
            }
            catch (SqliteException)
            {
                return [];
            }

            return results;
        }

        public List<OctreeCodeLong> GetDetails(Guid trajectoryID)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT OctreeDepth, OctreeCodeHigh, OctreeCodeLow FROM {WellboresTableName} WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            List<OctreeCodeLong> results = [];
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(ReadCode(reader, 0));
                }
            }
            catch (SqliteException)
            {
                return [];
            }

            return results;
        }

        public List<Pair<OctreeCodeLong, Guid>> GetDetails(List<OctreeCodeLong>? codes, Guid trajectoryID)
        {
            if (codes == null || codes.Count == 0)
            {
                return [];
            }

            using var connection = GetConnection();
            if (connection == null)
            {
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT OctreeDepth, OctreeCodeHigh, OctreeCodeLow, TrajectoryID FROM {WellboresTableName} WHERE TrajectoryID = @trajectoryId AND ({BuildCodeMatchClause(codes, command)})";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            List<Pair<OctreeCodeLong, Guid>> results = [];
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new Pair<OctreeCodeLong, Guid>(ReadCode(reader, 0), ReadGuid(reader, 3)));
                }
            }
            catch (SqliteException)
            {
                return [];
            }

            return results;
        }

        public bool AddDetails(OctreeCodeLong code, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"INSERT INTO {WellboresTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, OctreeDepth, OctreeCodeHigh, OctreeCodeLow, TrajectoryID, IsPlanned, IsMeasured, IsDefinitive) VALUES (@cacheHigh, @cacheLow, @depth, @high, @low, @trajectoryId, @isPlanned, @isMeasured, @isDefinitive)";

            AddDetailParameters(command, code, trajectoryID, isPlanned, isMeasured, isDefinitive);

            try
            {
                return command.ExecuteNonQuery() == 1;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool AddDetails(List<OctreeCodeLong>? codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (codes == null || codes.Count == 0)
            {
                return false;
            }

            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"INSERT INTO {WellboresTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, OctreeDepth, OctreeCodeHigh, OctreeCodeLow, TrajectoryID, IsPlanned, IsMeasured, IsDefinitive) VALUES (@cacheHigh, @cacheLow, @depth, @high, @low, @trajectoryId, @isPlanned, @isMeasured, @isDefinitive)";

            int affected = 0;
            try
            {
                foreach (OctreeCodeLong code in codes)
                {
                    command.Parameters.Clear();
                    AddDetailParameters(command, code, trajectoryID, isPlanned, isMeasured, isDefinitive);
                    affected += command.ExecuteNonQuery();
                }

                transaction.Commit();
                return affected == codes.Count;
            }
            catch (SqliteException)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool DeleteDetails(OctreeCodeLong code)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {WellboresTableName} WHERE OctreeDepth = @depth AND OctreeCodeHigh = @high AND OctreeCodeLow = @low";
            command.Parameters.AddWithValue("@depth", code.Depth);
            command.Parameters.AddWithValue("@high", (long)code.CodeHigh);
            command.Parameters.AddWithValue("@low", (long)code.CodeLow);

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool DeleteDetails(Guid trajectoryID)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {WellboresTableName} WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool DeleteDetails(Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"DELETE FROM {WellboresTableName} WHERE TrajectoryID = @trajectoryId AND IsPlanned = @isPlanned AND IsMeasured = @isMeasured AND IsDefinitive = @isDefinitive";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
            command.Parameters.AddWithValue("@isPlanned", isPlanned);
            command.Parameters.AddWithValue("@isMeasured", isMeasured);
            command.Parameters.AddWithValue("@isDefinitive", isDefinitive);

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool DeleteDetails(OctreeCodeLong code, Guid trajectoryID)
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"DELETE FROM {WellboresTableName} WHERE OctreeDepth = @depth AND OctreeCodeHigh = @high AND OctreeCodeLow = @low AND TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@depth", code.Depth);
            command.Parameters.AddWithValue("@high", (long)code.CodeHigh);
            command.Parameters.AddWithValue("@low", (long)code.CodeLow);
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool CleanContent()
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                command.CommandText = $"DELETE FROM {CacheTableName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DELETE FROM {WellboresTableName}";
                command.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (SqliteException)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool Clean()
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                return false;
            }

            using var command = connection.CreateCommand();

            try
            {
                command.CommandText = $"DROP INDEX IF EXISTS {CacheIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {WellboresIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {WellboresIndexName2}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP TABLE IF EXISTS {CacheTableName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP TABLE IF EXISTS {WellboresTableName}";
                command.ExecuteNonQuery();

                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Delete(Guid trajectoryID)
        {
            bool success = true;
            List<OctreeCodeLong> currentCodes = GetDetails(trajectoryID);
            if (currentCodes.Count > 0)
            {
                List<OctreeCodeLong> truncatedCodes = GetTruncatedCodes(currentCodes);
                List<OctreeCodeLong> orphanCodes = [];
                foreach (OctreeCodeLong truncatedCode in truncatedCodes)
                {
                    List<Guid> trajectories = GetDetails(truncatedCode);
                    bool additionalTrajectory = trajectories.Any(trajId => trajId != trajectoryID);
                    if (!additionalTrajectory)
                    {
                        orphanCodes.Add(truncatedCode);
                    }
                }

                foreach (OctreeCodeLong orphanCode in orphanCodes)
                {
                    success &= DeleteInCache(orphanCode.Decode());
                    if (!success)
                    {
                        break;
                    }
                }

                foreach (OctreeCodeLong code in currentCodes)
                {
                    success &= DeleteDetails(code, trajectoryID);
                    if (!success)
                    {
                        break;
                    }
                }
            }

            return success;
        }

        public bool Add(List<OctreeCodeLong>? codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (codes == null)
            {
                return false;
            }

            bool success = true;
            List<OctreeCodeLong> truncatedCodes = GetTruncatedCodes(codes);
            if (Contains(trajectoryID))
            {
                success &= Delete(trajectoryID);
            }

            if (success)
            {
                success &= AddDetails(codes, trajectoryID, isPlanned, isMeasured, isDefinitive);
            }

            if (success)
            {
                foreach (OctreeCodeLong truncatedCode in truncatedCodes)
                {
                    byte[] bytes = truncatedCode.Decode();
                    if (!ContainsInCache(bytes))
                    {
                        success &= AddInCache(bytes);
                        if (!success)
                        {
                            break;
                        }
                    }
                }
            }

            return success;
        }

        public List<Guid> Search(List<OctreeCodeLong>? codes, bool isPlanned, bool isMeasured, bool isDefinitive, Guid? investigatedTrajectoryID = null)
        {
            List<Guid> trajectoryIDs = [];
            if (codes == null || codes.Count == 0)
            {
                return trajectoryIDs;
            }

            List<OctreeCodeLong> truncatedReferenceCodes = GetTruncatedCodes(codes);
            List<OctreeCodeLong> truncatedCodes = [];
            foreach (OctreeCodeLong truncatedCode in truncatedReferenceCodes)
            {
                if (ContainsInCache(truncatedCode.Decode()))
                {
                    truncatedCodes.Add(truncatedCode);
                }
            }

            List<Pair<OctreeCodeLong, Guid>> detailedList = GetDetails(truncatedCodes, isPlanned, isMeasured, isDefinitive, investigatedTrajectoryID);
            foreach (OctreeCodeLong code in codes)
            {
                foreach (Pair<OctreeCodeLong, Guid> detail in detailedList)
                {
                    if (code.Intersect(detail.Left) && !trajectoryIDs.Contains(detail.Right))
                    {
                        trajectoryIDs.Add(detail.Right);
                    }
                }
            }

            return trajectoryIDs;
        }

        private static string[] CreateCacheLevelColumns(string suffix = "")
        {
            return Enumerable.Range(0, OctreeDepthCache)
                .Select(i => $"Level{i:00}{suffix}")
                .ToArray();
        }

        private static bool HasValidCacheCode(byte[]? octreeCode)
        {
            return octreeCode != null && octreeCode.Length >= OctreeDepthCache;
        }

        private static string BuildCacheMatchClause()
        {
            return string.Join(" AND ", CreateCacheLevelColumns().Select((column, index) => $"{column} = @level{index}"));
        }

        private static void AddCacheParameters(SqliteCommand command, byte[] octreeCode)
        {
            for (int i = 0; i < OctreeDepthCache; i++)
            {
                command.Parameters.AddWithValue($"@level{i}", octreeCode[i]);
            }
        }

        private static void AddDetailParameters(SqliteCommand command, OctreeCodeLong code, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            OctreeCodeLong truncatedCode = CreateTruncatedCode(code);
            command.Parameters.AddWithValue("@cacheHigh", (long)truncatedCode.CodeHigh);
            command.Parameters.AddWithValue("@cacheLow", (long)truncatedCode.CodeLow);
            command.Parameters.AddWithValue("@depth", code.Depth);
            command.Parameters.AddWithValue("@high", (long)code.CodeHigh);
            command.Parameters.AddWithValue("@low", (long)code.CodeLow);
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
            command.Parameters.AddWithValue("@isPlanned", isPlanned);
            command.Parameters.AddWithValue("@isMeasured", isMeasured);
            command.Parameters.AddWithValue("@isDefinitive", isDefinitive);
        }

        private static string BuildCodeMatchClause(IReadOnlyList<OctreeCodeLong> codes, SqliteCommand command)
        {
            List<string> clauses = [];
            for (int i = 0; i < codes.Count; i++)
            {
                clauses.Add($"(OctreeDepth = @depth{i} AND OctreeCodeHigh = @high{i} AND OctreeCodeLow = @low{i})");
                command.Parameters.AddWithValue($"@depth{i}", codes[i].Depth);
                command.Parameters.AddWithValue($"@high{i}", (long)codes[i].CodeHigh);
                command.Parameters.AddWithValue($"@low{i}", (long)codes[i].CodeLow);
            }
            return string.Join(" OR ", clauses);
        }

        private static OctreeCodeLong ReadCode(SqliteDataReader reader, int startIndex)
        {
            return new OctreeCodeLong(reader.GetByte(startIndex), (ulong)reader.GetInt64(startIndex + 1), (ulong)reader.GetInt64(startIndex + 2));
        }

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return Guid.Parse(reader.GetString(index));
        }

        private static OctreeCodeLong CreateTruncatedCode(OctreeCodeLong code)
        {
            OctreeCodeLong truncatedCode = new OctreeCodeLong(code);
            truncatedCode.Truncate((byte)OctreeDepthCache);
            return truncatedCode;
        }

        private static List<OctreeCodeLong> GetTruncatedCodes(List<OctreeCodeLong> codes)
        {
            List<OctreeCodeLong> truncatedCodes = [];
            foreach (OctreeCodeLong code in codes)
            {
                OctreeCodeLong truncatedCode = CreateTruncatedCode(code);
                if (!truncatedCodes.Contains(truncatedCode))
                {
                    truncatedCodes.Add(truncatedCode);
                }
            }
            return truncatedCodes;
        }
        #endregion
    }
}
