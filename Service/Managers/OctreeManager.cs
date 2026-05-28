using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.Drilling.Surveying;
using Microsoft.Data.Sqlite;
using System.Linq;
using NORCE.Drilling.GlobalAntiCollision;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    /// <summary>
    /// A manager for GlobalAntiCollision. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class OctreeManager
    {
        public object lock_ = new object();
        private static OctreeManager? _instance = null;
        private readonly ILogger<OctreeManager> _logger;
        private readonly SqlConnectionManagerOctree _connectionManager;

        #region Octree settings
        private int octreeDepthCache_ = SqlConnectionManagerOctree.OctreeDepthCache;
        public int OctreeDepthDetails { get; } = 23; // Corresponds to 40 000 000 m / 2^23 ~ 4.8 m

        private double minX_ = -Numeric.PI / 2.0;
        private double minY_ = -Numeric.PI;
        private double minZ_ = -6000000.0; // The radius of the earth is around 6000 km.
        private double maxX_ = Numeric.PI / 2.0;
        private double maxY_ = Numeric.PI;
        private double maxZ_ = 34000000.0; // We want the resolution in z to be of the same order of magnitude as for the other directions in the relevant region (circumference of the earth is ca 40 000 km)
        private const double EarthRadiusMeters = 6000000.0;
        private const double EnvelopePointSpacingToCellSizeRatio = 0.5;
        private const int MinEnvelopeMeshSectorCount = 36;
        private const int MaxEnvelopeMeshSectorCount = 240;
        #endregion

        #region Octree settings for debugging against octree database from the summer demo containing 16 duplicates of Ullrigg wells
        /*
        private int octreeDepthCache_ = 7;
        private int octreeDepthDetails_ = 10;

        private double minX_ = -710.55;
        private double minY_ = -133.79;
        private double minZ_ = 0;
        private double maxX_ = 2544.7699999999995;
        private double maxY_ = 4292.45;
        private double maxZ_ = 6707.2;
        */
        #endregion

        private OctreeManager(ILogger<OctreeManager> logger, SqlConnectionManagerOctree connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static OctreeManager GetInstance(ILogger<OctreeManager> logger, SqlConnectionManagerOctree connectionManager)
        {
            _instance ??= new OctreeManager(logger, connectionManager);
            return _instance;
        }

        public bool Clear()
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.CacheTableName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.WellboresTableName}";
                command.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to clear the octree tables");
                return false;
            }
        }

        public bool Contains(Guid id)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", id.ToString());

            try
            {
                return Convert.ToInt64(command.ExecuteScalar()) > 0;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to check if trajectory {TrajectoryId} exists in the octree database", id);
                return false;
            }
        }

        internal List<OctreeCodeLong> GetLeavesFromSurveyList(List<SurveyStation>? surveyList, UncertaintyEnvelope.ErrorModelType errorModelType = UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt)
        {
            List<OctreeCodeLong> leaves = new List<OctreeCodeLong>();
            if (surveyList is { Count: >= 2 })
            {
                #region Calculate the uncertainty envelope at confidencefactor 0.999 and scalingFactor = 1.0 with point spacing linked to the octree cell size
                double confidencefactor = 0.999;
                double scalingFactor = 1.0;
                double targetPointSpacing = GetTargetEnvelopePointSpacing(OctreeDepthDetails);
                double latitudeCellSize = GetOctreeCellSize(minX_, maxX_, OctreeDepthDetails);
                double longitudeCellSize = GetOctreeCellSize(minY_, maxY_, OctreeDepthDetails);
                double verticalCellSize = GetOctreeCellSize(minZ_, maxZ_, OctreeDepthDetails);

                bool ok = PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseListWithAdaptiveSectorCount(
                    surveyList,
                    errorModelType,
                    confidencefactor,
                    scalingFactor,
                    targetPointSpacing,
                    MinEnvelopeMeshSectorCount,
                    MaxEnvelopeMeshSectorCount,
                    null,
                    targetPointSpacing,
                    out List<UncertaintyEllipse>? ellipses,
                    out _);

                HashSet<OctreeCodeLong> leafCodes = new(OctreeCodeLongComparer.Instance);
                if (ok && ellipses is { Count: > 2 })
                {
                    foreach (UncertaintyEllipse ellipse in ellipses)
                    {
                        // We allow for zero ellipse radius here since that is typical for the first ellipse at MD = 0
                        List<SurveyPoint>? ellipseVertices = ellipse.EllipseVertices;
                        if (ellipse.EllipseRadii?[0] is not double ellipseRadius ||
                            !Numeric.GE(ellipseRadius, 0.0) ||
                            ellipseVertices == null)
                        {
                            continue;
                        }

                        // Fill the ellipse coordinates for each well into the corresponding octree
                        foreach (SurveyPoint sp in ellipseVertices) // Previously surveyList.UncertaintyEnvelope[n].EllipseCoordinates)
                        {
                            if (sp.Latitude is double latitude &&
                                sp.Longitude is double longitude &&
                                sp.TVD is double tvd)
                            {
                                AddPointAndNeighbourCodes(
                                    latitude,
                                    longitude,
                                    tvd,
                                    latitudeCellSize,
                                    longitudeCellSize,
                                    verticalCellSize,
                                    leafCodes);
                            }
                        }
                    }
                }

                leaves = CompactLeafCodes(leafCodes, OctreeDepthDetails);
                #endregion
            }
            return leaves ?? [];
        }

        public List<Guid> GetIDs()
        {
            return GetAllTrajectoryIDs(false, true, true);
        }

        public List<OctreeCodeLong> Get(Guid ID)
        {
            return GetDetails(ID);
        }

        public bool AddDetails(Guid ID, List<OctreeCodeLong>? code)
        {
            return AddDetails(code, ID, false, true, true);
        }

        public bool AddInCache(byte[] octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            OctreeCodeLong truncatedCode = new(octreeCode[..octreeDepthCache_]);

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"INSERT OR IGNORE INTO {SqlConnectionManagerOctree.CacheTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID, IsPlanned, IsMeasured, IsDefinitive, OctreeCodeCount, OctreeCodes) VALUES (@cacheHigh, @cacheLow, @trajectoryId, @isPlanned, @isMeasured, @isDefinitive, @codeCount, @codes)";
            AddCacheParameters(command, truncatedCode);
            command.Parameters.AddWithValue("@trajectoryId", Guid.Empty.ToString());
            command.Parameters.AddWithValue("@isPlanned", false);
            command.Parameters.AddWithValue("@isMeasured", false);
            command.Parameters.AddWithValue("@isDefinitive", false);
            command.Parameters.AddWithValue("@codeCount", 0);
            command.Parameters.Add("@codes", SqliteType.Blob).Value = Array.Empty<byte>();

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to add an octree cache entry");
                return false;
            }
        }

        public bool Remove(Guid ID)
        {
            if (!ID.Equals(Guid.Empty))
            {
                return Delete(ID);
            }
            return false;
        }

        public bool Update(Guid ID, List<OctreeCodeLong>? code)
        {
            if (!ID.Equals(Guid.Empty) && code != null)
            {
                return Add(code, ID, false, true, true);
            }
            return false;
        }

        public bool Delete(Guid trajectoryID)
        {
            if (trajectoryID.Equals(Guid.Empty))
            {
                return false;
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            try
            {
                command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.CacheTableName} WHERE TrajectoryID = @trajectoryId";
                command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
                command.ExecuteNonQuery();

                command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE TrajectoryID = @trajectoryId";
                command.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete octree details for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        public bool Add(List<OctreeCodeLong> codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (!trajectoryID.Equals(Guid.Empty) && codes != null)
            {
                bool success = true;
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
                    success &= AddCacheEntries(codes, trajectoryID, isPlanned, isMeasured, isDefinitive);
                }

                return success;
            }
            return false;
        }

        public List<Guid> Search(List<OctreeCodeLong>? codes, bool isPlanned, bool isMeasured, bool isDefinitive, Guid? investigatedTrajectoryID = null)
        {
            List<Guid> trajectoryIDs = [];
            if (codes == null || codes.Count == 0)
            {
                return trajectoryIDs;
            }

            List<OctreeCodeLong> truncatedCodes = GetTruncatedCodes(codes);
            List<Pair<OctreeCodeLong, Guid>> detailedList = GetDetails(truncatedCodes, isPlanned, isMeasured, isDefinitive, investigatedTrajectoryID);
            HashSet<Guid> uniqueTrajectoryIDs = [];
            foreach (OctreeCodeLong code in codes)
            {
                foreach (Pair<OctreeCodeLong, Guid> detail in detailedList)
                {
                    if (code.Intersect(detail.Left) && uniqueTrajectoryIDs.Add(detail.Right))
                    {
                        trajectoryIDs.Add(detail.Right);
                    }
                }
            }

            return trajectoryIDs;
        }

        public bool Clean()
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            try
            {
                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.CacheIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.WellboresTrajectoryIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.WellboresFilterIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.CacheIndexName}Lookup";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.CacheTrajectoryIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP TABLE IF EXISTS {SqlConnectionManagerOctree.CacheTableName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP TABLE IF EXISTS {SqlConnectionManagerOctree.WellboresTableName}";
                command.ExecuteNonQuery();

                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to drop the octree database objects");
                return false;
            }
        }

        private bool ContainsInCache(byte[]? octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            OctreeCodeLong truncatedCode = new(octreeCode![..octreeDepthCache_]);

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM {SqlConnectionManagerOctree.CacheTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow LIMIT 1";
            AddCacheParameters(command, truncatedCode);

            try
            {
                return command.ExecuteScalar() != null;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to check the octree cache");
                return false;
            }
        }

        private List<Guid> GetDetails(OctreeCodeLong truncatedCode)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TrajectoryID FROM {SqlConnectionManagerOctree.CacheTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow";
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
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to retrieve trajectory ids for a truncated octree code");
                return [];
            }

            return results;
        }

        private List<Pair<OctreeCodeLong, Guid>> GetDetails(List<OctreeCodeLong>? truncatedCodes, bool isPlanned, bool isMeasured, bool isDefinitive, Guid? ignoredTrajectoryID = null)
        {
            if (truncatedCodes == null || truncatedCodes.Count == 0)
            {
                return [];
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return [];
            }

            List<Pair<OctreeCodeLong, Guid>> results = [];
            using var transaction = connection.BeginTransaction();
            using (SqliteCommand createTempCommand = connection.CreateCommand())
            {
                createTempCommand.Transaction = transaction;
                createTempCommand.CommandText =
                    """
                    CREATE TEMP TABLE IF NOT EXISTS TempOctreeCacheCodes (
                        OctreeCodeCacheHigh BIGINT NOT NULL,
                        OctreeCodeCacheLow BIGINT NOT NULL,
                        PRIMARY KEY (OctreeCodeCacheHigh, OctreeCodeCacheLow)
                    ) WITHOUT ROWID
                    """;
                createTempCommand.ExecuteNonQuery();
                createTempCommand.CommandText = "DELETE FROM TempOctreeCacheCodes";
                createTempCommand.ExecuteNonQuery();
            }

            using (SqliteCommand insertTempCommand = connection.CreateCommand())
            {
                insertTempCommand.Transaction = transaction;
                insertTempCommand.CommandText =
                    "INSERT OR IGNORE INTO TempOctreeCacheCodes (OctreeCodeCacheHigh, OctreeCodeCacheLow) VALUES (@cacheHigh, @cacheLow)";
                foreach (OctreeCodeLong truncatedCode in truncatedCodes)
                {
                    insertTempCommand.Parameters.Clear();
                    AddCacheParameters(insertTempCommand, truncatedCode);
                    insertTempCommand.ExecuteNonQuery();
                }
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"""
                SELECT cache.TrajectoryID, cache.OctreeCodes
                FROM {SqlConnectionManagerOctree.CacheTableName} cache
                INNER JOIN TempOctreeCacheCodes c
                    ON c.OctreeCodeCacheHigh = cache.OctreeCodeCacheHigh
                   AND c.OctreeCodeCacheLow = cache.OctreeCodeCacheLow
                WHERE cache.IsPlanned = @isPlanned
                  AND cache.IsMeasured = @isMeasured
                  AND cache.IsDefinitive = @isDefinitive
                """;
            command.Parameters.AddWithValue("@isPlanned", isPlanned);
            command.Parameters.AddWithValue("@isMeasured", isMeasured);
            command.Parameters.AddWithValue("@isDefinitive", isDefinitive);
            if (ignoredTrajectoryID != null)
            {
                command.CommandText += " AND cache.TrajectoryID <> @ignoredTrajectoryId";
                command.Parameters.AddWithValue("@ignoredTrajectoryId", ignoredTrajectoryID.Value.ToString());
            }

            try
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Guid trajectoryId = ReadGuid(reader, 0);
                        foreach (OctreeCodeLong code in DeserializeCodes(reader, 1))
                        {
                            results.Add(new Pair<OctreeCodeLong, Guid>(code, trajectoryId));
                        }
                    }
                }
                transaction.Commit();
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to retrieve octree details for truncated codes");
                return [];
            }

            return results;
        }

        private List<Guid> GetAllTrajectoryIDs(bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT DISTINCT TrajectoryID FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE IsPlanned = @isPlanned AND IsMeasured = @isMeasured AND IsDefinitive = @isDefinitive";
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
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to retrieve trajectory ids from the octree database");
                return [];
            }

            return results;
        }

        private List<OctreeCodeLong> GetDetails(Guid trajectoryID)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT OctreeCodes FROM {SqlConnectionManagerOctree.CacheTableName} WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            List<OctreeCodeLong> results = [];
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    results.AddRange(DeserializeCodes(reader, 0));
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to retrieve octree details for trajectory {TrajectoryId}", trajectoryID);
                return [];
            }

            return results;
        }

        private bool AddDetails(List<OctreeCodeLong>? codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (codes == null || codes.Count == 0)
            {
                return false;
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"INSERT OR REPLACE INTO {SqlConnectionManagerOctree.WellboresTableName} (TrajectoryID, IsPlanned, IsMeasured, IsDefinitive) VALUES (@trajectoryId, @isPlanned, @isMeasured, @isDefinitive)";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
            command.Parameters.AddWithValue("@isPlanned", isPlanned);
            command.Parameters.AddWithValue("@isMeasured", isMeasured);
            command.Parameters.AddWithValue("@isDefinitive", isDefinitive);

            try
            {
                int affected = command.ExecuteNonQuery();
                transaction.Commit();
                return affected == 1;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to add octree details for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        private bool AddCacheEntries(List<OctreeCodeLong> codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (codes.Count == 0)
            {
                return true;
            }

            Dictionary<OctreeCodeLong, List<OctreeCodeLong>> codesByTruncatedCode = new(OctreeCodeLongComparer.Instance);
            foreach (OctreeCodeLong code in codes)
            {
                OctreeCodeLong truncatedCode = CreateTruncatedCode(code);
                if (!codesByTruncatedCode.TryGetValue(truncatedCode, out List<OctreeCodeLong>? groupedCodes))
                {
                    groupedCodes = [];
                    codesByTruncatedCode[truncatedCode] = groupedCodes;
                }

                groupedCodes.Add(code);
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"INSERT OR REPLACE INTO {SqlConnectionManagerOctree.CacheTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID, IsPlanned, IsMeasured, IsDefinitive, OctreeCodeCount, OctreeCodes) VALUES (@cacheHigh, @cacheLow, @trajectoryId, @isPlanned, @isMeasured, @isDefinitive, @codeCount, @codes)";

            try
            {
                foreach (KeyValuePair<OctreeCodeLong, List<OctreeCodeLong>> codesByTruncatedCodeEntry in codesByTruncatedCode)
                {
                    command.Parameters.Clear();
                    AddCacheParameters(command, codesByTruncatedCodeEntry.Key);
                    command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
                    command.Parameters.AddWithValue("@isPlanned", isPlanned);
                    command.Parameters.AddWithValue("@isMeasured", isMeasured);
                    command.Parameters.AddWithValue("@isDefinitive", isDefinitive);
                    command.Parameters.AddWithValue("@codeCount", codesByTruncatedCodeEntry.Value.Count);
                    command.Parameters.Add("@codes", SqliteType.Blob).Value = SerializeCodes(codesByTruncatedCodeEntry.Value);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to add octree cache entries");
                return false;
            }
        }

        private bool DeleteInCache(byte[]? octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            OctreeCodeLong truncatedCode = new(octreeCode![..octreeDepthCache_]);

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.CacheTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow";
            AddCacheParameters(command, truncatedCode);

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to delete an octree cache entry");
                return false;
            }
        }

        private bool HasValidCacheCode(byte[]? octreeCode)
        {
            return octreeCode != null && octreeCode.Length >= octreeDepthCache_;
        }

        private void AddCacheParameters(SqliteCommand command, OctreeCodeLong truncatedCode)
        {
            command.Parameters.AddWithValue("@cacheHigh", (long)truncatedCode.CodeHigh);
            command.Parameters.AddWithValue("@cacheLow", (long)truncatedCode.CodeLow);
        }

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return Guid.Parse(reader.GetString(index));
        }

        private static byte[] SerializeCodes(List<OctreeCodeLong> codes)
        {
            const int bytesPerCode = 17;
            byte[] buffer = new byte[codes.Count * bytesPerCode];
            for (int i = 0; i < codes.Count; i++)
            {
                int offset = i * bytesPerCode;
                OctreeCodeLong code = codes[i];
                buffer[offset] = code.Depth;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset + 1, sizeof(ulong)), code.CodeHigh);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset + 1 + sizeof(ulong), sizeof(ulong)), code.CodeLow);
            }

            return buffer;
        }

        private static List<OctreeCodeLong> DeserializeCodes(SqliteDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return [];
            }

            return DeserializeCodes((byte[])reader[index]);
        }

        private static List<OctreeCodeLong> DeserializeCodes(byte[] buffer)
        {
            const int bytesPerCode = 17;
            if (buffer.Length == 0 || buffer.Length % bytesPerCode != 0)
            {
                return [];
            }

            int count = buffer.Length / bytesPerCode;
            List<OctreeCodeLong> codes = new(count);
            for (int i = 0; i < count; i++)
            {
                int offset = i * bytesPerCode;
                byte depth = buffer[offset];
                ulong codeHigh = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(offset + 1, sizeof(ulong)));
                ulong codeLow = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(offset + 1 + sizeof(ulong), sizeof(ulong)));
                codes.Add(new OctreeCodeLong(depth, codeHigh, codeLow));
            }

            return codes;
        }

        private double GetTargetEnvelopePointSpacing(int octreeDepth)
        {
            return GetConservativeOctreeCellSizeMeters(octreeDepth) * EnvelopePointSpacingToCellSizeRatio;
        }

        private static double GetOctreeCellSize(double min, double max, int octreeDepth)
        {
            return (max - min) / Math.Pow(2.0, octreeDepth);
        }

        private double GetConservativeOctreeCellSizeMeters(int octreeDepth)
        {
            double cellCount = Math.Pow(2.0, octreeDepth);
            double latitudeCellSize = EarthRadiusMeters * (maxX_ - minX_) / cellCount;
            double longitudeCellSizeAtEquator = EarthRadiusMeters * (maxY_ - minY_) / cellCount;
            double verticalCellSize = (maxZ_ - minZ_) / cellCount;
            return Math.Min(latitudeCellSize, Math.Min(longitudeCellSizeAtEquator, verticalCellSize));
        }

        private void AddPointAndNeighbourCodes(
            double x,
            double y,
            double z,
            double xCellSize,
            double yCellSize,
            double zCellSize,
            HashSet<OctreeCodeLong> leafCodes)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                double expandedX = x + xOffset * xCellSize;
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    double expandedY = y + yOffset * yCellSize;
                    for (int zOffset = -1; zOffset <= 1; zOffset++)
                    {
                        double expandedZ = z + zOffset * zCellSize;
                        if (TryCreateOctreeCode(expandedX, expandedY, expandedZ, OctreeDepthDetails, out OctreeCodeLong code))
                        {
                            leafCodes.Add(code);
                        }
                    }
                }
            }
        }

        private bool TryCreateOctreeCode(double x, double y, double z, int depth, out OctreeCodeLong code)
        {
            code = default;
            if (depth < 1 || depth > Octree<OctreeCodeLong>.MaxDepthOctreeCodeLong ||
                !IsInsideBounds(x, minX_, maxX_) ||
                !IsInsideBounds(y, minY_, maxY_) ||
                !IsInsideBounds(z, minZ_, maxZ_))
            {
                return false;
            }

            const int reservedForDepth = 5;
            const int depthPivot = (sizeof(ulong) * 8 - reservedForDepth) / 3;

            double minX = minX_;
            double maxX = maxX_;
            double minY = minY_;
            double maxY = maxY_;
            double minZ = minZ_;
            double maxZ = maxZ_;
            ulong codeHigh = 0;
            ulong codeLow = 0;
            int highDepth = Math.Min(depth, depthPivot);
            int lowDepth = depth - depthPivot;

            for (int level = 0; level < depth; level++)
            {
                double middleX = (minX + maxX) / 2.0;
                double middleY = (minY + maxY) / 2.0;
                double middleZ = (minZ + maxZ) / 2.0;
                byte index = 0;

                if (x > middleX)
                {
                    index |= 1;
                    minX = middleX;
                }
                else
                {
                    maxX = middleX;
                }

                if (y > middleY)
                {
                    index |= 2;
                    minY = middleY;
                }
                else
                {
                    maxY = middleY;
                }

                if (z > middleZ)
                {
                    index |= 4;
                    minZ = middleZ;
                }
                else
                {
                    maxZ = middleZ;
                }

                if (level < depthPivot)
                {
                    codeHigh |= (ulong)index << ((highDepth - 1) * 3 - 3 * level);
                }
                else
                {
                    int lowLevel = level - depthPivot;
                    codeLow |= (ulong)index << ((lowDepth - 1) * 3 - 3 * lowLevel);
                }
            }

            code = new OctreeCodeLong((byte)depth, codeHigh, codeLow);
            return true;
        }

        private static bool IsInsideBounds(double value, double min, double max)
        {
            return double.IsFinite(value) && value >= min && value <= max;
        }

        private static List<OctreeCodeLong> CompactLeafCodes(HashSet<OctreeCodeLong> leafCodes, int depth)
        {
            HashSet<OctreeCodeLong> compactedCodes = leafCodes;
            for (int currentDepth = depth; currentDepth > 1; currentDepth--)
            {
                Dictionary<OctreeCodeLong, byte> childMasksByParent = new(OctreeCodeLongComparer.Instance);
                foreach (OctreeCodeLong code in compactedCodes)
                {
                    if (code.Depth != currentDepth)
                    {
                        continue;
                    }

                    OctreeCodeLong parent = FastTruncate(code, (byte)(currentDepth - 1));
                    byte childMask = (byte)(1 << GetLastChildIndex(code));
                    childMasksByParent[parent] = (byte)(childMasksByParent.GetValueOrDefault(parent) | childMask);
                }

                HashSet<OctreeCodeLong> fullParents = new(OctreeCodeLongComparer.Instance);
                foreach (KeyValuePair<OctreeCodeLong, byte> childMaskByParent in childMasksByParent)
                {
                    if (childMaskByParent.Value == byte.MaxValue)
                    {
                        fullParents.Add(childMaskByParent.Key);
                    }
                }

                if (fullParents.Count == 0)
                {
                    continue;
                }

                HashSet<OctreeCodeLong> nextCodes = new(compactedCodes.Count, OctreeCodeLongComparer.Instance);
                foreach (OctreeCodeLong code in compactedCodes)
                {
                    if (code.Depth == currentDepth &&
                        fullParents.Contains(FastTruncate(code, (byte)(currentDepth - 1))))
                    {
                        continue;
                    }

                    nextCodes.Add(code);
                }

                foreach (OctreeCodeLong parent in fullParents)
                {
                    nextCodes.Add(parent);
                }

                compactedCodes = nextCodes;
            }

            return compactedCodes
                .OrderBy(code => code.Depth)
                .ThenBy(code => code.CodeHigh)
                .ThenBy(code => code.CodeLow)
                .ToList();
        }

        private static byte GetLastChildIndex(OctreeCodeLong code)
        {
            return (byte)(code.Depth > 19 ? code.CodeLow & 7UL : code.CodeHigh & 7UL);
        }

        private static OctreeCodeLong FastTruncate(OctreeCodeLong code, byte depth)
        {
            if (code.Depth <= depth)
            {
                return code;
            }

            ulong codeHigh = code.CodeHigh;
            ulong codeLow = code.CodeLow;
            if (code.Depth > 19 && depth > 19)
            {
                codeLow >>= 3 * (code.Depth - depth);
            }
            else if (code.Depth > 19)
            {
                codeLow = 0;
                codeHigh >>= 3 * (19 - depth);
            }
            else
            {
                codeHigh >>= 3 * (code.Depth - depth);
            }

            return new OctreeCodeLong(depth, codeHigh, codeLow);
        }

        private sealed class OctreeCodeLongComparer : IEqualityComparer<OctreeCodeLong>
        {
            public static readonly OctreeCodeLongComparer Instance = new();

            public bool Equals(OctreeCodeLong x, OctreeCodeLong y)
            {
                return x.Depth == y.Depth &&
                    x.CodeHigh == y.CodeHigh &&
                    x.CodeLow == y.CodeLow;
            }

            public int GetHashCode(OctreeCodeLong obj)
            {
                return HashCode.Combine(obj.Depth, obj.CodeHigh, obj.CodeLow);
            }
        }

        private OctreeCodeLong CreateTruncatedCode(OctreeCodeLong code)
        {
            return FastTruncate(code, (byte)octreeDepthCache_);
        }

        private List<OctreeCodeLong> GetTruncatedCodes(List<OctreeCodeLong> codes)
        {
            HashSet<OctreeCodeLong> truncatedCodeSet = new(OctreeCodeLongComparer.Instance);
            foreach (OctreeCodeLong code in codes)
            {
                truncatedCodeSet.Add(CreateTruncatedCode(code));
            }
            return truncatedCodeSet
                .OrderBy(code => code.CodeHigh)
                .ThenBy(code => code.CodeLow)
                .ToList();
        }
    }
}
