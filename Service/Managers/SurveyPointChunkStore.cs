using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    internal static class SurveyPointChunkStore
    {
        public const int DefaultChunkSize = 1000;

        public static int GetChunkCount(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, string ownerType)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return 0;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SurveyPointChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            try
            {
                object? result = command.ExecuteScalar();
                return result is long count ? (int)count : 0;
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get survey point chunk count");
                return 0;
            }
        }

        public static SurveyPointChunk? GetChunk(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, string ownerType, int chunkIndex)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyPointChunk FROM SurveyPointChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType AND ChunkIndex = @chunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    return JsonSerializer.Deserialize<SurveyPointChunk>(reader.GetString(0), JsonSettings.Options);
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get survey point chunk");
            }

            return null;
        }

        public static List<SurveyPoint>? GetPoints(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, string ownerType)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            List<SurveyPoint> points = [];
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyPointChunk FROM SurveyPointChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType ORDER BY ChunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyPointChunk? chunk = JsonSerializer.Deserialize<SurveyPointChunk>(reader.GetString(0), JsonSettings.Options);
                    if (chunk?.SurveyPointList is { Count: > 0 } chunkPoints)
                    {
                        points.AddRange(chunkPoints);
                    }
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get survey point chunks");
                return null;
            }

            return points.Count > 0 ? points : null;
        }

        public static bool ReplaceChunks(SqliteConnection connection, SqliteTransaction transaction, Guid ownerId, string ownerType, List<SurveyPoint>? points)
        {
            DeleteChunks(connection, transaction, ownerId, ownerType);
            if (points is not { Count: > 0 })
            {
                return true;
            }

            int chunkIndex = 0;
            foreach (List<SurveyPoint> pointChunk in points.Chunk(DefaultChunkSize).Select(chunk => chunk.ToList()))
            {
                SurveyPointChunk chunk = new()
                {
                    OwnerID = ownerId,
                    OwnerType = ownerType,
                    ChunkIndex = chunkIndex++,
                    SurveyPointList = pointChunk
                };
                chunk.UpdateMetadata();
                if (!UpsertChunk(connection, transaction, chunk))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool DeleteChunks(SqliteConnection connection, SqliteTransaction transaction, Guid ownerId, string ownerType)
        {
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM SurveyPointChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            command.ExecuteNonQuery();
            return true;
        }

        private static bool UpsertChunk(SqliteConnection connection, SqliteTransaction transaction, SurveyPointChunk chunk)
        {
            string chunkId = $"{chunk.OwnerType}:{chunk.OwnerID:N}:{chunk.ChunkIndex:D10}";
            string data = JsonSerializer.Serialize(chunk, JsonSettings.Options);
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO SurveyPointChunkTable " +
                "(ID, OwnerID, OwnerType, ChunkIndex, PointCount, StartMD, EndMD, SurveyPointChunk) " +
                "VALUES (@id, @ownerId, @ownerType, @chunkIndex, @pointCount, @startMd, @endMd, @chunk) " +
                "ON CONFLICT(ID) DO UPDATE SET " +
                "OwnerID = excluded.OwnerID, OwnerType = excluded.OwnerType, ChunkIndex = excluded.ChunkIndex, " +
                "PointCount = excluded.PointCount, StartMD = excluded.StartMD, EndMD = excluded.EndMD, SurveyPointChunk = excluded.SurveyPointChunk";
            command.Parameters.AddWithValue("@id", chunkId);
            command.Parameters.AddWithValue("@ownerId", chunk.OwnerID.ToString());
            command.Parameters.AddWithValue("@ownerType", chunk.OwnerType ?? string.Empty);
            command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("@pointCount", chunk.PointCount);
            command.Parameters.AddWithValue("@startMd", (object?)chunk.StartMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@endMd", (object?)chunk.EndMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@chunk", data);
            return command.ExecuteNonQuery() == 1;
        }
    }
}
