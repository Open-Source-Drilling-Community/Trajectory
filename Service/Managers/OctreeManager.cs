using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.Drilling.Surveying;
using Microsoft.Data.Sqlite;
using System.Linq;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
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
                #region Calculate the uncertainty envelope at confidencefactor 0.999 and scalingFactor = 1.0 with 0.1m spacing between intermediate ellipses and 720 point for each ellipse
                double confidencefactor = 0.999;
                double scalingFactor = 1.0;

                UncertaintyEnvelope uncertaintyEnvelope = new()
                {
                    ErrorModel = errorModelType,
                    SurveyStationList = surveyList,
                    MeshSectorCount = 720,
                    MeshLongitudinalLength = 0.1,
                };
                uncertaintyEnvelope.ConfidenceFactor = confidencefactor;
                uncertaintyEnvelope.ScalingFactor = scalingFactor;
                bool ok = uncertaintyEnvelope.Calculate();
                List<UncertaintyEllipse>? ellipses = ok ? uncertaintyEnvelope.MeshedEllipseList : null;

                // Note that TVD is positive downwards, but we correct for that when we convert to Point3D which are being plotted. We also add some additional margins to make sure we can plot the lower part of the envelope
                Octree<OctreeCodeLong> octree = new Octree<OctreeCodeLong>(minX_, maxX_, minY_, maxY_, minZ_, maxZ_);
                if (ellipses is { Count: > 2 })
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
                                octree.Add(latitude, longitude, tvd, OctreeDepthDetails);
                            }
                        }
                    }
                }

                // Extract the leaves of each octree
                List<OctreeCodeLong>? octreeLeaves = octree.GetLeaves(OctreeDepthDetails);
                leaves = octreeLeaves ?? [];
                // Now we don't need the octree anymore
                octree.DeleteRootNodes();
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

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            string[] cacheColumns = CreateCacheLevelColumns();
            command.CommandText =
                $"INSERT INTO {SqlConnectionManagerOctree.CacheTableName} ({string.Join(", ", cacheColumns)}) VALUES ({string.Join(", ", cacheColumns.Select((_, i) => $"@level{i}"))})";
            AddCacheParameters(command, octreeCode);

            try
            {
                return command.ExecuteNonQuery() == 1;
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
                return DeleteDetails(ID);
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
            if (!trajectoryID.Equals(Guid.Empty))
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
            return false;
        }

        public bool Add(List<OctreeCodeLong> codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (!trajectoryID.Equals(Guid.Empty) && codes != null)
            {
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
            return false;
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

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.WellboresIndexName}";
                command.ExecuteNonQuery();

                command.CommandText = $"DROP INDEX IF EXISTS {SqlConnectionManagerOctree.WellboresIndexName2}";
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
            byte[] validCode = octreeCode!;

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {SqlConnectionManagerOctree.CacheTableName} WHERE {BuildCacheMatchClause()}";
            AddCacheParameters(command, validCode);

            try
            {
                return Convert.ToInt64(command.ExecuteScalar()) > 0;
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
            command.CommandText = $"SELECT TrajectoryID FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow";
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
            foreach (OctreeCodeLong truncatedCode in truncatedCodes)
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"SELECT OctreeDepth, OctreeCodeHigh, OctreeCodeLow, TrajectoryID FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE OctreeCodeCacheHigh = @cacheHigh AND OctreeCodeCacheLow = @cacheLow AND IsPlanned = @isPlanned AND IsMeasured = @isMeasured AND IsDefinitive = @isDefinitive";
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
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to retrieve octree details for truncated codes");
                    return [];
                }
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
            command.CommandText = $"SELECT OctreeDepth, OctreeCodeHigh, OctreeCodeLow FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE TrajectoryID = @trajectoryId";
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
                $"INSERT INTO {SqlConnectionManagerOctree.WellboresTableName} (OctreeCodeCacheHigh, OctreeCodeCacheLow, OctreeDepth, OctreeCodeHigh, OctreeCodeLow, TrajectoryID, IsPlanned, IsMeasured, IsDefinitive) VALUES (@cacheHigh, @cacheLow, @depth, @high, @low, @trajectoryId, @isPlanned, @isMeasured, @isDefinitive)";

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
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to add octree details for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        private bool DeleteInCache(byte[]? octreeCode)
        {
            if (!HasValidCacheCode(octreeCode))
            {
                return false;
            }
            byte[] validCode = octreeCode!;

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.CacheTableName} WHERE {BuildCacheMatchClause()}";
            AddCacheParameters(command, validCode);

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

        private bool DeleteDetails(Guid trajectoryID)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to delete octree details for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        private bool DeleteDetails(OctreeCodeLong code, Guid trajectoryID)
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText =
                $"DELETE FROM {SqlConnectionManagerOctree.WellboresTableName} WHERE OctreeDepth = @depth AND OctreeCodeHigh = @high AND OctreeCodeLow = @low AND TrajectoryID = @trajectoryId";
            command.Parameters.AddWithValue("@depth", code.Depth);
            command.Parameters.AddWithValue("@high", (long)code.CodeHigh);
            command.Parameters.AddWithValue("@low", (long)code.CodeLow);
            command.Parameters.AddWithValue("@trajectoryId", trajectoryID.ToString());

            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to delete octree code for trajectory {TrajectoryId}", trajectoryID);
                return false;
            }
        }

        private string[] CreateCacheLevelColumns()
        {
            return Enumerable.Range(0, octreeDepthCache_)
                .Select(i => $"Level{i:00}")
                .ToArray();
        }

        private bool HasValidCacheCode(byte[]? octreeCode)
        {
            return octreeCode != null && octreeCode.Length >= octreeDepthCache_;
        }

        private string BuildCacheMatchClause()
        {
            return string.Join(" AND ", CreateCacheLevelColumns().Select((column, index) => $"{column} = @level{index}"));
        }

        private void AddCacheParameters(SqliteCommand command, byte[] octreeCode)
        {
            for (int i = 0; i < octreeDepthCache_; i++)
            {
                command.Parameters.AddWithValue($"@level{i}", octreeCode[i]);
            }
        }

        private void AddDetailParameters(SqliteCommand command, OctreeCodeLong code, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
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

        private static OctreeCodeLong ReadCode(SqliteDataReader reader, int startIndex)
        {
            return new OctreeCodeLong(reader.GetByte(startIndex), (ulong)reader.GetInt64(startIndex + 1), (ulong)reader.GetInt64(startIndex + 2));
        }

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return Guid.Parse(reader.GetString(index));
        }

        private OctreeCodeLong CreateTruncatedCode(OctreeCodeLong code)
        {
            OctreeCodeLong truncatedCode = new OctreeCodeLong(code);
            truncatedCode.Truncate((byte)octreeDepthCache_);
            return truncatedCode;
        }

        private List<OctreeCodeLong> GetTruncatedCodes(List<OctreeCodeLong> codes)
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
    }
}
