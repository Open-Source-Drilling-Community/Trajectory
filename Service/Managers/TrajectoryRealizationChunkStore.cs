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
    internal static class TrajectoryRealizationChunkStore
    {
        public const int DefaultRealizationsPerChunk = 25;

        public static int GetChunkCount(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return 0;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM TrajectoryRealizationChunkTable WHERE OwnerID = @ownerId";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            try
            {
                object? result = command.ExecuteScalar();
                return result is long count ? (int)count : 0;
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get trajectory realization chunk count");
                return 0;
            }
        }

        public static TrajectoryRealizationChunk? GetChunk(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, int chunkIndex)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryRealizationChunk FROM TrajectoryRealizationChunkTable WHERE OwnerID = @ownerId AND ChunkIndex = @chunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    return JsonSerializer.Deserialize<TrajectoryRealizationChunk>(reader.GetString(0), JsonSettings.Options);
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get trajectory realization chunk");
            }

            return null;
        }

        public static List<List<SurveyPoint>>? GetRealizations(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            List<List<SurveyPoint>> realizations = [];
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT TrajectoryRealizationChunk FROM TrajectoryRealizationChunkTable WHERE OwnerID = @ownerId ORDER BY ChunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    TrajectoryRealizationChunk? chunk = JsonSerializer.Deserialize<TrajectoryRealizationChunk>(reader.GetString(0), JsonSettings.Options);
                    if (chunk?.RealizationList is { Count: > 0 } chunkRealizations)
                    {
                        realizations.AddRange(chunkRealizations);
                    }
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get trajectory realization chunks");
                return null;
            }

            return realizations.Count > 0 ? realizations : null;
        }

        public static bool ReplaceChunks(SqliteConnection connection, SqliteTransaction transaction, Guid ownerId, List<List<SurveyPoint>>? realizations)
        {
            SqliteCommand deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM TrajectoryRealizationChunkTable WHERE OwnerID = @ownerId";
            deleteCommand.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            deleteCommand.ExecuteNonQuery();

            if (realizations is not { Count: > 0 })
            {
                return true;
            }

            int chunkIndex = 0;
            foreach (List<List<SurveyPoint>> realizationChunk in realizations.Chunk(DefaultRealizationsPerChunk).Select(chunk => chunk.ToList()))
            {
                TrajectoryRealizationChunk chunk = new()
                {
                    OwnerID = ownerId,
                    ChunkIndex = chunkIndex++,
                    RealizationList = realizationChunk
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
            command.CommandText = "DELETE FROM TrajectoryRealizationChunkTable WHERE OwnerID = @ownerId";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.ExecuteNonQuery();
            return true;
        }

        private static bool UpsertChunk(SqliteConnection connection, SqliteTransaction transaction, TrajectoryRealizationChunk chunk)
        {
            string chunkId = $"{chunk.OwnerID:N}:{chunk.ChunkIndex:D10}";
            string data = JsonSerializer.Serialize(chunk, JsonSettings.Options);
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO TrajectoryRealizationChunkTable " +
                "(ID, OwnerID, ChunkIndex, RealizationCount, SurveyPointCount, StartMD, EndMD, TrajectoryRealizationChunk) " +
                "VALUES (@id, @ownerId, @chunkIndex, @realizationCount, @surveyPointCount, @startMd, @endMd, @chunk) " +
                "ON CONFLICT(ID) DO UPDATE SET " +
                "OwnerID = excluded.OwnerID, ChunkIndex = excluded.ChunkIndex, RealizationCount = excluded.RealizationCount, " +
                "SurveyPointCount = excluded.SurveyPointCount, StartMD = excluded.StartMD, EndMD = excluded.EndMD, TrajectoryRealizationChunk = excluded.TrajectoryRealizationChunk";
            command.Parameters.AddWithValue("@id", chunkId);
            command.Parameters.AddWithValue("@ownerId", chunk.OwnerID.ToString());
            command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("@realizationCount", chunk.RealizationCount);
            command.Parameters.AddWithValue("@surveyPointCount", chunk.SurveyPointCount);
            command.Parameters.AddWithValue("@startMd", (object?)chunk.StartMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@endMd", (object?)chunk.EndMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@chunk", data);
            return command.ExecuteNonQuery() == 1;
        }
    }
}
