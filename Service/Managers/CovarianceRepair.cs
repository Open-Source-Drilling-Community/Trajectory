using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
namespace OSDC.Drilling.Trajectory.Service.Managers;

/// <summary>Offline administrative repair. Never queues geometry calculation or rewrites observations.</summary>
public static class CovarianceRepair
{
    public const string AlgorithmVersion = "absolute-reference-v1";
    private static readonly string[] UncertaintyFields = ["Covariance", "Bias", "EigenValues", "EigenVectors"];
    private sealed record Row(string Table, string Column, string Id, string Json);
    private static List<Row> Read(SqlConnectionManager db)
    {
        List<Row> rows = []; using var connection = db.GetConnection()!; using var tx = connection.BeginTransaction(deferred: true);
        foreach (var (table, column) in new[] { ("SurveyRunTable", "SurveyRun"), ("TrajectoryTable", "Trajectory"), ("SurveyStationChunkTable", "SurveyStationChunk"), ("SurveyRunMeasurementChunkTable", "SurveyMeasurementChunk") })
        {
            using var cmd = connection.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = $"SELECT ID,{column} FROM {table} ORDER BY ID"; using var reader = cmd.ExecuteReader(); while (reader.Read()) rows.Add(new(table, column, reader.GetString(0), reader.GetString(1)));
        }
        return rows;
    }
    private static JsonNode? Normalize(JsonNode? node, bool geometry)
    {
        if (node is JsonObject obj) { var result = new JsonObject(); foreach (var pair in obj.OrderBy(p => p.Key, StringComparer.Ordinal)) if (!geometry || (!UncertaintyFields.Contains(pair.Key) && pair.Key != "LastModificationDate")) result[pair.Key] = Normalize(pair.Value, geometry); return result; }
        if (node is JsonArray array) return new JsonArray(array.Select(n => Normalize(n, geometry)).ToArray()); return node?.DeepClone();
    }
    private static string Hash(IEnumerable<Row> rows, bool geometry) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", rows.Select(r => r.Table + "/" + r.Id + "/" + Normalize(JsonNode.Parse(r.Json), geometry)!.ToJsonString())))));
    private static void Patch(JsonObject target, SurveyStation source)
    {
        if (!UncertaintyPropagation.IsUsable(source.Covariance)) throw new InvalidOperationException("Invalid candidate covariance");
        var candidate = JsonSerializer.SerializeToNode(source, JsonSettings.Options)!.AsObject(); foreach (string field in UncertaintyFields) target[field] = candidate[field]?.DeepClone();
    }
    public static Task<object> RunAsync(SqlConnectionManagerTrajectory db, bool apply, string auditDirectory)
    {
        if (apply && (!Path.GetFullPath(auditDirectory).StartsWith("/home/.deployment-backups/", StringComparison.Ordinal) || !File.Exists(Path.Combine(auditDirectory, "snapshot-manifest.json")))) throw new InvalidOperationException("Apply requires a verified in-cluster snapshot directory.");
        if (apply)
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(auditDirectory, "snapshot-manifest.json")));
            var file = manifest.RootElement.GetProperty("Files").EnumerateArray().Single(f => f.GetProperty("Path").GetString() == "Trajectory.db");
            using var stream = File.OpenRead(Path.Combine(auditDirectory, "Trajectory.db"));
            if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(file.GetProperty("SHA256").GetString(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Independent backup hash verification failed");
        }
        return RunCoreAsync(db, apply, auditDirectory);
    }
    internal static async Task<object> RunCoreAsync(SqlConnectionManagerTrajectory db, bool apply, string auditDirectory)
    {
        Directory.CreateDirectory(auditDirectory); var original = Read(db); string beforeGeometry = Hash(original, true); var planned = original.ToDictionary(r => r.Table + "/" + r.Id); var details = new List<object>(); using var dependencies = new UncertaintyDependencies();
        var runs = SurveyRunManager.GetInstance(NullLogger<SurveyRunManager>.Instance, db); var trajectories = TrajectoryManager.GetInstance(NullLogger<TrajectoryManager>.Instance, db);
        foreach (var row in original.Where(r => r.Table is "SurveyRunTable" or "TrajectoryTable"))
        {
            var metadata = JsonNode.Parse(row.Json)!.AsObject(); if (metadata["CalculationState"]?.GetValue<string>() != "Completed") { details.Add(new { entity = row.Column, id = row.Id, status = "unchanged_not_completed" }); continue; }
            Guid id = Guid.Parse(row.Id); List<SurveyStation>? candidate = row.Table == "SurveyRunTable" ? (await runs.GetSurveyRunWithRecalculatedUncertaintyAsync(id))?.SurveyStationList : (await trajectories.GetTrajectoryWithRecalculatedUncertaintyAsync(id))?.SurveyStationList;
            if (candidate is not { Count: > 0 }) throw new InvalidOperationException("Cannot reconstruct uncertainty without geometry changes: " + row.Column + "/" + row.Id);
            var chunks = original.Where(r => r.Table == "SurveyStationChunkTable").Select(r => (row: r, node: JsonNode.Parse(r.Json)!.AsObject())).Where(p => p.node["OwnerID"]!.GetValue<string>().Equals(row.Id, StringComparison.OrdinalIgnoreCase) && p.node["OwnerType"]!.GetValue<string>() == row.Column).OrderBy(p => p.node["ChunkIndex"]!.GetValue<int>()).ToList();
            int index = 0; bool changed = false;
            if (chunks.Count > 0) foreach (var chunk in chunks)
                {
                    foreach (var station in chunk.node["SurveyStationList"]!.AsArray()) { if (index >= candidate.Count) throw new InvalidOperationException("Station count mismatch"); Patch(station!.AsObject(), candidate[index++]); }
                    string json = chunk.node.ToJsonString(); changed |= Normalize(JsonNode.Parse(json), false)!.ToJsonString() != Normalize(JsonNode.Parse(chunk.row.Json), false)!.ToJsonString(); planned[chunk.row.Table + "/" + chunk.row.Id] = chunk.row with { Json = json };
                }
            if (metadata["SurveyStationList"] is JsonArray inline)
            {
                int inlineIndex=0;foreach(var station in inline){if(inlineIndex>=candidate.Count)throw new InvalidOperationException("Inline station count mismatch");Patch(station!.AsObject(),candidate[inlineIndex++]);}
                if(inlineIndex!=candidate.Count)throw new InvalidOperationException("Inline station count mismatch");
                if(chunks.Count==0)index=inlineIndex;
            }
            if (index != candidate.Count) throw new InvalidOperationException("Retained station count mismatch");
            if (metadata["TieInPoint"] is JsonObject tie) Patch(tie, candidate[0]);
            string updated = metadata.ToJsonString(); changed |= Normalize(JsonNode.Parse(updated), false)!.ToJsonString() != Normalize(JsonNode.Parse(row.Json), false)!.ToJsonString();
            if (changed) { metadata["LastModificationDate"] = DateTimeOffset.UtcNow; updated = metadata.ToJsonString(); } else updated = row.Json;
            planned[row.Table + "/" + row.Id] = row with { Json = updated }; details.Add(new { entity = row.Column, id = row.Id, status = changed ? "repair" : "already_current", stations = index });
        }
        var final = original.Select(r => planned[r.Table + "/" + r.Id]).ToList(); if (Hash(final, true) != beforeGeometry) throw new InvalidOperationException("Geometry or measurements changed in the repair plan"); await dependencies.RevalidateAsync();
        var plan = new { algorithmVersion = AlgorithmVersion, beforeGeometryHash = beforeGeometry, sourceHashes = dependencies.Hashes, records = details }; File.WriteAllText(Path.Combine(auditDirectory, "uncertainty-repair-plan.json"), JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
        if (apply)
        {
            using var connection = db.GetConnection()!; using var tx = connection.BeginTransaction();
            // Compare every retained input/result row under the write lock before changing any record.
            foreach (var row in original) { using var check = connection.CreateCommand(); check.Transaction = tx; check.CommandText = $"SELECT {row.Column} FROM {row.Table} WHERE ID=@id"; check.Parameters.AddWithValue("@id", row.Id); if (check.ExecuteScalar() as string != row.Json) throw new InvalidOperationException("A source row changed during repair"); }
            foreach (var row in final)
            {
                var old = original.Single(r => r.Table == row.Table && r.Id == row.Id); if (old.Json == row.Json) continue;
                using var cmd = connection.CreateCommand(); cmd.Transaction = tx; bool metadata = row.Table is "SurveyRunTable" or "TrajectoryTable";
                cmd.CommandText = $"UPDATE {row.Table} SET {row.Column}=@json" + (metadata ? ", LastModificationDate=@modified" : "") + " WHERE ID=@id"; cmd.Parameters.AddWithValue("@json", row.Json); cmd.Parameters.AddWithValue("@id", row.Id);
                if (metadata) cmd.Parameters.AddWithValue("@modified", DateTimeOffset.Parse(JsonNode.Parse(row.Json)!["LastModificationDate"]!.GetValue<string>()).ToString(SqlConnectionManager.DATE_TIME_FORMAT)); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Repair target disappeared");
            }
            await dependencies.RevalidateAsync(); tx.Commit();
        }
        var after = Read(db); if (Hash(after, true) != beforeGeometry) throw new InvalidOperationException("Post-repair geometry/measurement verification failed"); if (apply && Hash(after, false) != Hash(final, false)) throw new InvalidOperationException("Persisted uncertainty differs from approved plan");
        var receipt = new { passed = true, applied = apply, algorithmVersion = AlgorithmVersion, geometryAndMeasurementsUnchanged = true, geometryHash = beforeGeometry, records = details, sourceHashes = dependencies.Hashes, changedRows = final.Count(r => r.Json != original.Single(o => o.Table == r.Table && o.Id == r.Id).Json) };
        File.WriteAllText(Path.Combine(auditDirectory, apply ? "uncertainty-repair-applied.json" : "uncertainty-repair-dry-run.json"), JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true })); return receipt;
    }
}
