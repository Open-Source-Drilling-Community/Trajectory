using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    internal static class SurveyRunMinimumDistanceResultChunkStore
    {
        public const int DefaultChunkSize = 1000;

        public static int GetChunkCount(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return 0;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SurveyRunMinimumDistanceResultChunkTable WHERE OwnerID = @ownerId";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            try
            {
                object? result = command.ExecuteScalar();
                return result is long count ? (int)count : 0;
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get survey run minimum distance result chunk count");
                return 0;
            }
        }

        public static SurveyRunMinimumDistanceResultChunk? GetChunk(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, int chunkIndex)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRunMinimumDistanceResultChunk FROM SurveyRunMinimumDistanceResultChunkTable WHERE OwnerID = @ownerId AND ChunkIndex = @chunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    return JsonSerializer.Deserialize<SurveyRunMinimumDistanceResultChunk>(reader.GetString(0), JsonSettings.Options);
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get survey run minimum distance result chunk");
            }

            return null;
        }

        public static List<SurveyRunMinimumDistanceResult>? GetResults(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            List<SurveyRunMinimumDistanceResult> results = [];
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyRunMinimumDistanceResultChunk FROM SurveyRunMinimumDistanceResultChunkTable WHERE OwnerID = @ownerId ORDER BY ChunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyRunMinimumDistanceResultChunk? chunk = JsonSerializer.Deserialize<SurveyRunMinimumDistanceResultChunk>(reader.GetString(0), JsonSettings.Options);
                    if (chunk?.ResultList is { Count: > 0 } chunkResults)
                    {
                        results.AddRange(chunkResults);
                    }
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get survey run minimum distance result chunks");
                return null;
            }

            return results.Count > 0 ? results : null;
        }

        public static bool ReplaceChunks(SqliteConnection connection, SqliteTransaction transaction, Guid ownerId, List<SurveyRunMinimumDistanceResult>? results)
        {
            SqliteCommand deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM SurveyRunMinimumDistanceResultChunkTable WHERE OwnerID = @ownerId";
            deleteCommand.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            deleteCommand.ExecuteNonQuery();

            if (results is not { Count: > 0 })
            {
                return true;
            }

            int chunkIndex = 0;
            foreach (List<SurveyRunMinimumDistanceResult> resultChunk in results.Chunk(DefaultChunkSize).Select(chunk => chunk.ToList()))
            {
                SurveyRunMinimumDistanceResultChunk chunk = new()
                {
                    OwnerID = ownerId,
                    ChunkIndex = chunkIndex++,
                    ResultList = resultChunk
                };
                chunk.UpdateMetadata();
                if (!UpsertChunk(connection, transaction, chunk))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool DeleteChunks(SqliteConnection connection, SqliteTransaction transaction, Guid ownerId)
        {
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM SurveyRunMinimumDistanceResultChunkTable WHERE OwnerID = @ownerId";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.ExecuteNonQuery();
            return true;
        }

        private static bool UpsertChunk(SqliteConnection connection, SqliteTransaction transaction, SurveyRunMinimumDistanceResultChunk chunk)
        {
            string chunkId = $"{chunk.OwnerID:N}:{chunk.ChunkIndex:D10}";
            string data = JsonSerializer.Serialize(chunk, JsonSettings.Options);
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO SurveyRunMinimumDistanceResultChunkTable " +
                "(ID, OwnerID, ChunkIndex, ResultCount, StartReferenceMD, EndReferenceMD, SurveyRunMinimumDistanceResultChunk) " +
                "VALUES (@id, @ownerId, @chunkIndex, @resultCount, @startReferenceMd, @endReferenceMd, @chunk) " +
                "ON CONFLICT(ID) DO UPDATE SET " +
                "OwnerID = excluded.OwnerID, ChunkIndex = excluded.ChunkIndex, ResultCount = excluded.ResultCount, " +
                "StartReferenceMD = excluded.StartReferenceMD, EndReferenceMD = excluded.EndReferenceMD, SurveyRunMinimumDistanceResultChunk = excluded.SurveyRunMinimumDistanceResultChunk";
            command.Parameters.AddWithValue("@id", chunkId);
            command.Parameters.AddWithValue("@ownerId", chunk.OwnerID.ToString());
            command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("@resultCount", chunk.ResultCount);
            command.Parameters.AddWithValue("@startReferenceMd", (object?)chunk.StartReferenceMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@endReferenceMd", (object?)chunk.EndReferenceMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@chunk", data);
            return command.ExecuteNonQuery() == 1;
        }
    }
}
