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
    internal static class SurveyStationChunkStore
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
            command.CommandText = "SELECT COUNT(*) FROM SurveyStationChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            try
            {
                object? result = command.ExecuteScalar();
                return result is long count ? (int)count : 0;
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get calculated survey station chunk count");
                return 0;
            }
        }

        public static SurveyStationChunk? GetChunk(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, string ownerType, int chunkIndex)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyStationChunk FROM SurveyStationChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType AND ChunkIndex = @chunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    return JsonSerializer.Deserialize<SurveyStationChunk>(reader.GetString(0), JsonSettings.Options);
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get calculated survey station chunk");
            }

            return null;
        }

        public static List<SurveyStation>? GetStations(ILogger logger, SqlConnectionManager connectionManager, Guid ownerId, string ownerType)
        {
            using SqliteConnection? connection = connectionManager.GetConnection();
            if (connection == null)
            {
                logger.LogWarning("Impossible to access the SQLite database");
                return null;
            }

            List<SurveyStation> stations = [];
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT SurveyStationChunk FROM SurveyStationChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType ORDER BY ChunkIndex";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    SurveyStationChunk? chunk = JsonSerializer.Deserialize<SurveyStationChunk>(reader.GetString(0), JsonSettings.Options);
                    if (chunk?.SurveyStationList is { Count: > 0 } chunkStations)
                    {
                        stations.AddRange(chunkStations);
                    }
                }
            }
            catch (SqliteException ex)
            {
                logger.LogError(ex, "Impossible to get calculated survey station chunks");
                return null;
            }

            return stations.Count > 0 ? stations : null;
        }

        public static bool ReplaceChunks(SqliteConnection connection, SqliteTransaction transaction, Guid ownerId, string ownerType, List<SurveyStation>? stations)
        {
            SqliteCommand deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM SurveyStationChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType";
            deleteCommand.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            deleteCommand.Parameters.AddWithValue("@ownerType", ownerType);
            deleteCommand.ExecuteNonQuery();

            if (stations is not { Count: > 0 })
            {
                return true;
            }

            int chunkIndex = 0;
            foreach (List<SurveyStation> stationChunk in stations.Chunk(DefaultChunkSize).Select(chunk => chunk.ToList()))
            {
                SurveyStationChunk chunk = new()
                {
                    OwnerID = ownerId,
                    OwnerType = ownerType,
                    ChunkIndex = chunkIndex++,
                    SurveyStationList = stationChunk
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
            command.CommandText = "DELETE FROM SurveyStationChunkTable WHERE OwnerID = @ownerId AND OwnerType = @ownerType";
            command.Parameters.AddWithValue("@ownerId", ownerId.ToString());
            command.Parameters.AddWithValue("@ownerType", ownerType);
            command.ExecuteNonQuery();
            return true;
        }

        private static bool UpsertChunk(SqliteConnection connection, SqliteTransaction transaction, SurveyStationChunk chunk)
        {
            string chunkId = $"{chunk.OwnerType}:{chunk.OwnerID:N}:{chunk.ChunkIndex:D10}";
            string data = JsonSerializer.Serialize(chunk, JsonSettings.Options);
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO SurveyStationChunkTable " +
                "(ID, OwnerID, OwnerType, ChunkIndex, StationCount, StartMD, EndMD, SurveyStationChunk) " +
                "VALUES (@id, @ownerId, @ownerType, @chunkIndex, @stationCount, @startMd, @endMd, @surveyStationChunk) " +
                "ON CONFLICT(ID) DO UPDATE SET " +
                "OwnerID = excluded.OwnerID, OwnerType = excluded.OwnerType, ChunkIndex = excluded.ChunkIndex, " +
                "StationCount = excluded.StationCount, StartMD = excluded.StartMD, EndMD = excluded.EndMD, SurveyStationChunk = excluded.SurveyStationChunk";
            command.Parameters.AddWithValue("@id", chunkId);
            command.Parameters.AddWithValue("@ownerId", chunk.OwnerID.ToString());
            command.Parameters.AddWithValue("@ownerType", chunk.OwnerType ?? string.Empty);
            command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("@stationCount", chunk.StationCount);
            command.Parameters.AddWithValue("@startMd", (object?)chunk.StartMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@endMd", (object?)chunk.EndMD ?? DBNull.Value);
            command.Parameters.AddWithValue("@surveyStationChunk", data);
            return command.ExecuteNonQuery() == 1;
        }
    }
}
