using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.Drilling.Surveying;
using Microsoft.Data.Sqlite;
using System.Linq;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service.Managers
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
        public const double ConfidenceFactor = 0.999;
        public const int IndexSchemaVersion = 2;
        public const string CalculationParametersHash = "surface-neighbours-compact-depth23-cache21-confidence0.999-scale1-v2";
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

        public OctreeManager(ILogger<OctreeManager> logger, SqlConnectionManagerOctree connectionManager)
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

                command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.TrajectoryStateTableName}";
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
            command.CommandText = $"SELECT COUNT(*) FROM {SqlConnectionManagerOctree.TrajectoryStateTableName} WHERE TrajectoryID = @trajectoryId";
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

        internal List<OctreeCodeLong> GetLeavesFromSurveyList(List<SurveyStation>? surveyList, UncertaintyEnvelope.ErrorModelType? errorModelType = null)
        {
            List<OctreeCodeLong> leaves = new List<OctreeCodeLong>();
            if (surveyList is { Count: >= 2 })
            {
                #region Calculate the uncertainty envelope at confidencefactor 0.999 and scalingFactor = 1.0 with point spacing linked to the octree cell size
                double confidencefactor = ConfidenceFactor;
                double scalingFactor = 1.0;
                double targetPointSpacing = GetTargetEnvelopePointSpacing(OctreeDepthDetails);
                double latitudeCellSize = GetOctreeCellSize(minX_, maxX_, OctreeDepthDetails);
                double longitudeCellSize = GetOctreeCellSize(minY_, maxY_, OctreeDepthDetails);
                double verticalCellSize = GetOctreeCellSize(minZ_, maxZ_, OctreeDepthDetails);

                bool ok = PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseListWithAdaptiveSectorCount(
                    surveyList,
                    errorModelType ?? PerpendicularEllipseEnvelopeBuilder.InferErrorModelType(surveyList),
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

        public List<Guid> GetIDs(TrajectoryType? trajectoryType = null, bool? isDefinitive = null)
        {
            return GetAllTrajectoryIDs(trajectoryType, isDefinitive);
        }

        public List<OctreeCodeLong> Get(Guid ID)
        {
            return GetDetails(ID);
        }

        public OctreeIndexStatus GetStatus(Model.Trajectory trajectory)
        {
            Guid trajectoryId = trajectory.MetaInfo?.ID ?? Guid.Empty;
            var status = new OctreeIndexStatus
            {
                TrajectoryID = trajectoryId,
                TrajectoryType = trajectory.TrajectoryType,
                IsDefinitive = trajectory.IsDefinitive,
                SurveyStationCount = trajectory.SurveyStationList?.Count ?? 0
            };

            if (trajectoryId == Guid.Empty)
            {
                status.State = OctreeIndexState.Missing;
                return status;
            }

            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                throw new InvalidOperationException("The octree database is unavailable.");
            }

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT state.SourceLastModificationDate, state.IndexSchemaVersion,
                       state.ConfidenceFactor, state.CalculationParametersHash,
                       state.TrajectoryType, state.IsDefinitive,
                       COUNT(membership.TrajectoryID), COALESCE(SUM(membership.OctreeCodeCount), 0)
                FROM {SqlConnectionManagerOctree.TrajectoryStateTableName} state
                LEFT JOIN {SqlConnectionManagerOctree.CacheTableName} membership
                  ON membership.TrajectoryID = state.TrajectoryID
                WHERE state.TrajectoryID = @trajectoryId
                GROUP BY state.TrajectoryID
                """;
            command.Parameters.AddWithValue("@trajectoryId", trajectoryId.ToString());

            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    status.State = status.SurveyStationCount < 2
                        ? OctreeIndexState.NotIndexable
                        : OctreeIndexState.Missing;
                    return status;
                }

                status.HasIndex = true;
                string? sourceModified = reader.IsDBNull(0) ? null : reader.GetString(0);
                status.SourceLastModificationDate = reader.IsDBNull(0)
                    ? null
                    : DateTimeOffset.Parse(sourceModified!, System.Globalization.CultureInfo.InvariantCulture);
                status.IndexSchemaVersion = reader.GetInt32(1);
                status.ConfidenceFactor = reader.GetDouble(2);
                status.CalculationParametersHash = reader.GetString(3);
                status.BucketCount = reader.GetInt32(6);
                status.OctreeCodeCount = reader.GetInt64(7);
                status.IsCurrent =
                    string.Equals(reader.GetString(4), trajectory.TrajectoryType.ToString(), StringComparison.Ordinal) &&
                    reader.GetInt64(5) == (trajectory.IsDefinitive ? 1 : 0) &&
                    string.Equals(sourceModified, trajectory.LastModificationDate?.ToString("O"), StringComparison.Ordinal) &&
                    status.IndexSchemaVersion == IndexSchemaVersion &&
                    Math.Abs(status.ConfidenceFactor.Value - ConfidenceFactor) < 1e-12 &&
                    string.Equals(status.CalculationParametersHash, CalculationParametersHash, StringComparison.Ordinal);
                status.State = status.IsCurrent ? OctreeIndexState.Current : OctreeIndexState.Stale;
                return status;
            }
            catch (Exception ex) when (ex is SqliteException or FormatException)
            {
                _logger.LogError(ex, "Impossible to retrieve octree status for trajectory {TrajectoryId}", trajectoryId);
                throw new InvalidOperationException("The octree status could not be read from storage.", ex);
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

        public bool Rebuild(Model.Trajectory? trajectory)
        {
            if (trajectory?.MetaInfo?.ID is not Guid id || id == Guid.Empty)
            {
                return false;
            }

            List<OctreeCodeLong> codes = GetLeavesFromSurveyList(trajectory.SurveyStationList);
            return Replace(codes, id, trajectory.TrajectoryType, trajectory.IsDefinitive,
                trajectory.LastModificationDate);
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

                command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.TrajectoryStateTableName} WHERE TrajectoryID = @trajectoryId";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
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
            TrajectoryType type = isPlanned && !isMeasured ? TrajectoryType.Planned : TrajectoryType.Actual;
            return Replace(codes, trajectoryID, type, isDefinitive, null);
        }

        public List<Guid> Search(List<OctreeCodeLong>? codes, bool isPlanned, bool isMeasured,
            bool isDefinitive, Guid? investigatedTrajectoryID = null)
        {
            TrajectoryType type = isPlanned && !isMeasured ? TrajectoryType.Planned : TrajectoryType.Actual;
            return Search(codes, type, isDefinitive, investigatedTrajectoryID);
        }

        public List<Guid> Search(List<OctreeCodeLong>? codes, TrajectoryType trajectoryType, bool isDefinitive, Guid? investigatedTrajectoryID = null)
        {
            return SearchCore(codes, trajectoryType, isDefinitive, investigatedTrajectoryID);
        }

        public List<Guid> SearchByClassification(List<OctreeCodeLong>? codes, bool includePlanned, bool includeActual,
            bool definitiveOnly, Guid? investigatedTrajectoryID = null,
            Action<double, string>? progress = null)
        {
            if (!includePlanned && !includeActual)
            {
                return [];
            }

            TrajectoryType? trajectoryType = includePlanned == includeActual
                ? null
                : includePlanned ? TrajectoryType.Planned : TrajectoryType.Actual;
            return SearchCore(codes, trajectoryType, definitiveOnly ? true : null, investigatedTrajectoryID, progress);
        }

        private List<Guid> SearchCore(List<OctreeCodeLong>? codes, TrajectoryType? trajectoryType,
            bool? isDefinitive, Guid? investigatedTrajectoryID,
            Action<double, string>? progress = null)
        {
            List<Guid> trajectoryIDs = [];
            if (codes == null || codes.Count == 0)
            {
                return trajectoryIDs;
            }

            progress?.Invoke(0.02, "Preparing reference octree codes");
            List<OctreeCodeLong> truncatedCodes = GetTruncatedCodes(codes);
            progress?.Invoke(0.05, $"Loading {truncatedCodes.Count:N0} overlapping octree buckets");
            List<Pair<OctreeCodeLong, Guid>> detailedList = GetDetails(truncatedCodes, trajectoryType, isDefinitive,
                investigatedTrajectoryID,
                (completed, total) => progress?.Invoke(0.05 + 0.2 * completed / Math.Max(1, total),
                    $"Loaded {completed:N0}/{total:N0} octree buckets"));
            progress?.Invoke(0.25,
                $"Checking {codes.Count:N0} reference codes against {detailedList.Count:N0} candidate codes");
            HashSet<Guid> uniqueTrajectoryIDs = [];
            int progressInterval = Math.Max(1, codes.Count / 100);
            for (int codeIndex = 0; codeIndex < codes.Count; codeIndex++)
            {
                OctreeCodeLong code = codes[codeIndex];
                foreach (Pair<OctreeCodeLong, Guid> detail in detailedList)
                {
                    if (code.Intersect(detail.Left) && uniqueTrajectoryIDs.Add(detail.Right))
                    {
                        trajectoryIDs.Add(detail.Right);
                    }
                }
                int completed = codeIndex + 1;
                if (completed == codes.Count || completed % progressInterval == 0)
                {
                    progress?.Invoke(0.25 + 0.74 * completed / codes.Count,
                        $"Checked {completed:N0}/{codes.Count:N0} reference octree codes");
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

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.CacheTrajectoryIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.StateFilterIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP TABLE IF EXISTS {SqlConnectionManagerOctree.CacheTableName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP TABLE IF EXISTS {SqlConnectionManagerOctree.TrajectoryStateTableName}";
                command.ExecuteNonQuery();

                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to drop the octree database objects");
                return false;
            }
        }

        private List<Pair<OctreeCodeLong, Guid>> GetDetails(List<OctreeCodeLong>? truncatedCodes,
            TrajectoryType? trajectoryType, bool? isDefinitive, Guid? ignoredTrajectoryID = null,
            Action<int, int>? progress = null)
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
                        OctreeCodeCacheDepth INTEGER NOT NULL,
                        OctreeCodeCacheHigh BIGINT NOT NULL,
                        OctreeCodeCacheLow BIGINT NOT NULL,
                        PRIMARY KEY (OctreeCodeCacheDepth, OctreeCodeCacheHigh, OctreeCodeCacheLow)
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
                    "INSERT OR IGNORE INTO TempOctreeCacheCodes (OctreeCodeCacheDepth, OctreeCodeCacheHigh, OctreeCodeCacheLow) VALUES (@cacheDepth, @cacheHigh, @cacheLow)";
                for (int codeIndex = 0; codeIndex < truncatedCodes.Count; codeIndex++)
                {
                    OctreeCodeLong truncatedCode = truncatedCodes[codeIndex];
                    insertTempCommand.Parameters.Clear();
                    AddCacheParameters(insertTempCommand, truncatedCode);
                    insertTempCommand.ExecuteNonQuery();
                    int completed = codeIndex + 1;
                    int progressInterval = Math.Max(1, truncatedCodes.Count / 100);
                    if (completed == truncatedCodes.Count || completed % progressInterval == 0)
                    {
                        progress?.Invoke(completed, truncatedCodes.Count);
                    }
                }
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"""
                SELECT membership.TrajectoryID, membership.OctreeCodes
                FROM {SqlConnectionManagerOctree.CacheTableName} membership
                INNER JOIN TempOctreeCacheCodes c
                    ON c.OctreeCodeCacheDepth = membership.OctreeCodeCacheDepth
                   AND c.OctreeCodeCacheHigh = membership.OctreeCodeCacheHigh
                   AND c.OctreeCodeCacheLow = membership.OctreeCodeCacheLow
                INNER JOIN {SqlConnectionManagerOctree.TrajectoryStateTableName} state
                    ON state.TrajectoryID = membership.TrajectoryID
                WHERE 1 = 1
                """;
            if (trajectoryType.HasValue)
            {
                command.CommandText += " AND state.TrajectoryType = @trajectoryType";
                command.Parameters.AddWithValue("@trajectoryType", trajectoryType.Value.ToString());
            }
            if (isDefinitive.HasValue)
            {
                command.CommandText += " AND state.IsDefinitive = @isDefinitive";
                command.Parameters.AddWithValue("@isDefinitive", isDefinitive.Value);
            }
            if (ignoredTrajectoryID != null)
            {
                command.CommandText += " AND membership.TrajectoryID <> @ignoredTrajectoryId";
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

        public bool UpdateClassification(Guid trajectoryID, TrajectoryType trajectoryType, bool isDefinitive,
            DateTimeOffset? sourceLastModificationDate = null)
        {
            if (trajectoryID == Guid.Empty) return false;
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"UPDATE {SqlConnectionManagerOctree.TrajectoryStateTableName} " +
                "SET TrajectoryType = @trajectoryType, IsDefinitive = @isDefinitive, " +
                "SourceLastModificationDate = COALESCE(@sourceLastModificationDate, SourceLastModificationDate) " +
                "WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
            command.Parameters.AddWithValue("@trajectoryType", trajectoryType.ToString());
            command.Parameters.AddWithValue("@isDefinitive", isDefinitive);
            command.Parameters.AddWithValue("@sourceLastModificationDate",
                (object?)sourceLastModificationDate?.ToString("O") ?? DBNull.Value);

            try
            {
                return command.ExecuteNonQuery() == 1;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to update octree classification for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        public bool IsCurrent(Model.Trajectory? trajectory)
        {
            if (trajectory?.MetaInfo?.ID is not Guid trajectoryId || trajectoryId == Guid.Empty) return false;
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null) return false;
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT TrajectoryType, IsDefinitive, SourceLastModificationDate, " +
                $"IndexSchemaVersion, ConfidenceFactor, CalculationParametersHash FROM {SqlConnectionManagerOctree.TrajectoryStateTableName} " +
                "WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryId.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (!reader.Read()) return false;
                string? sourceModified = reader.IsDBNull(2) ? null : reader.GetString(2);
                return string.Equals(reader.GetString(0), trajectory.TrajectoryType.ToString(), StringComparison.Ordinal) &&
                    reader.GetInt64(1) == (trajectory.IsDefinitive ? 1 : 0) &&
                    string.Equals(sourceModified, trajectory.LastModificationDate?.ToString("O"), StringComparison.Ordinal) &&
                    reader.GetInt32(3) == IndexSchemaVersion &&
                    Math.Abs(reader.GetDouble(4) - ConfidenceFactor) < 1e-12 &&
                    string.Equals(reader.GetString(5), CalculationParametersHash, StringComparison.Ordinal);
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to validate octree provenance for trajectory {TrajectoryId}", trajectoryId);
                return false;
            }
        }

        private List<Guid> GetAllTrajectoryIDs(TrajectoryType? trajectoryType, bool? isDefinitive)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return [];
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TrajectoryID FROM {SqlConnectionManagerOctree.TrajectoryStateTableName} WHERE 1 = 1";
            if (trajectoryType.HasValue)
            {
                command.CommandText += " AND TrajectoryType = @trajectoryType";
                command.Parameters.AddWithValue("@trajectoryType", trajectoryType.Value.ToString());
            }
            if (isDefinitive.HasValue)
            {
                command.CommandText += " AND IsDefinitive = @isDefinitive";
                command.Parameters.AddWithValue("@isDefinitive", isDefinitive.Value);
            }
            command.CommandText += " ORDER BY TrajectoryID";

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

        private bool Replace(List<OctreeCodeLong>? codes, Guid trajectoryID, TrajectoryType trajectoryType,
            bool isDefinitive, DateTimeOffset? sourceLastModificationDate)
        {
            if (trajectoryID == Guid.Empty || codes == null || codes.Count == 0)
            {
                return false;
            }

            Dictionary<OctreeCodeLong, byte[]> serializedGroups = GroupAndSerializeCodes(codes);

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                using (SqliteCommand delete = connection.CreateCommand())
                {
                    delete.Transaction = transaction;
                    delete.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.CacheTableName} WHERE TrajectoryID = @trajectoryId";
                    delete.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
                    delete.ExecuteNonQuery();
                }

                using (SqliteCommand state = connection.CreateCommand())
                {
                    state.Transaction = transaction;
                    state.CommandText = $"""
                        INSERT INTO {SqlConnectionManagerOctree.TrajectoryStateTableName}
                            (TrajectoryID, TrajectoryType, IsDefinitive, SourceLastModificationDate,
                             IndexSchemaVersion, ConfidenceFactor, CalculationParametersHash)
                        VALUES (@trajectoryId, @trajectoryType, @isDefinitive, @sourceLastModificationDate,
                                @indexSchemaVersion, @confidenceFactor, @calculationParametersHash)
                        ON CONFLICT(TrajectoryID) DO UPDATE SET
                            TrajectoryType = excluded.TrajectoryType,
                            IsDefinitive = excluded.IsDefinitive,
                            SourceLastModificationDate = excluded.SourceLastModificationDate,
                            IndexSchemaVersion = excluded.IndexSchemaVersion,
                            ConfidenceFactor = excluded.ConfidenceFactor,
                            CalculationParametersHash = excluded.CalculationParametersHash
                        """;
                    state.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
                    state.Parameters.AddWithValue("@trajectoryType", trajectoryType.ToString());
                    state.Parameters.AddWithValue("@isDefinitive", isDefinitive);
                    state.Parameters.AddWithValue("@sourceLastModificationDate",
                        (object?)sourceLastModificationDate?.ToString("O") ?? DBNull.Value);
                    state.Parameters.AddWithValue("@indexSchemaVersion", IndexSchemaVersion);
                    state.Parameters.AddWithValue("@confidenceFactor", ConfidenceFactor);
                    state.Parameters.AddWithValue("@calculationParametersHash", CalculationParametersHash);
                    if (state.ExecuteNonQuery() != 1) throw new InvalidOperationException("Octree trajectory state was not saved.");
                }

                using SqliteCommand membership = connection.CreateCommand();
                membership.Transaction = transaction;
                membership.CommandText =
                    $"INSERT INTO {SqlConnectionManagerOctree.CacheTableName} " +
                    "(OctreeCodeCacheDepth, OctreeCodeCacheHigh, OctreeCodeCacheLow, TrajectoryID, OctreeCodeCount, OctreeCodes) " +
                    "VALUES (@cacheDepth, @cacheHigh, @cacheLow, @trajectoryId, @codeCount, @codes)";
                foreach ((OctreeCodeLong prefix, byte[] serializedCodes) in serializedGroups)
                {
                    membership.Parameters.Clear();
                    AddCacheParameters(membership, prefix);
                    membership.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());
                    membership.Parameters.AddWithValue("@codeCount", serializedCodes.Length / 17);
                    membership.Parameters.Add("@codes", SqliteType.Blob).Value = serializedCodes;
                    if (membership.ExecuteNonQuery() != 1) throw new InvalidOperationException("Octree bucket membership was not saved.");
                }
                transaction.Commit();
                return true;
            }
            catch (Exception ex) when (ex is SqliteException or InvalidOperationException)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to replace octree details for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        private Dictionary<OctreeCodeLong, byte[]> GroupAndSerializeCodes(List<OctreeCodeLong> codes)
        {
            Dictionary<OctreeCodeLong, List<OctreeCodeLong>> grouped = new(OctreeCodeLongComparer.Instance);
            foreach (OctreeCodeLong code in codes)
            {
                OctreeCodeLong prefix = CreateTruncatedCode(code);
                if (!grouped.TryGetValue(prefix, out List<OctreeCodeLong>? values))
                {
                    values = [];
                    grouped[prefix] = values;
                }
                values.Add(code);
            }
            return grouped.ToDictionary(pair => pair.Key, pair => SerializeCodes(pair.Value),
                new OctreeCodeLongComparer());
        }

        private void AddCacheParameters(SqliteCommand command, OctreeCodeLong truncatedCode)
        {
            command.Parameters.AddWithValue("@cacheDepth", truncatedCode.Depth);
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
