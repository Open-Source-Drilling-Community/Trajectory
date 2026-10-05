using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Managers;

public sealed class DirectionalControlEvaluationCaseManager
{
    private const int SampleChunkSize = 250;
    private static DirectionalControlEvaluationCaseManager? instance_;
    private readonly ILogger<DirectionalControlEvaluationCaseManager> logger_;
    private readonly SqlConnectionManager connectionManager_;
    private readonly TrajectoryManager trajectoryManager_;

    internal DirectionalControlEvaluationCaseManager(
        ILogger<DirectionalControlEvaluationCaseManager> logger,
        SqlConnectionManager connectionManager)
    {
        logger_ = logger;
        connectionManager_ = connectionManager;
        trajectoryManager_ = TrajectoryManager.GetInstance(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance,
            connectionManager);
    }

    public static DirectionalControlEvaluationCaseManager GetInstance(
        ILogger<DirectionalControlEvaluationCaseManager> logger,
        SqlConnectionManager connectionManager) => instance_ ??= new(logger, connectionManager);

    public List<Guid>? GetAllIds()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ID FROM DirectionalControlEvaluationCaseTable ORDER BY CreationDate";
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            List<Guid> result = [];
            while (reader.Read())
                if (Guid.TryParse(reader.GetString(0), out Guid id)) result.Add(id);
            return result;
        }
        catch (SqliteException ex)
        {
            logger_.LogError(ex, "Unable to list directional-control evaluation case IDs");
            return null;
        }
    }

    public List<DirectionalControlEvaluationCaseLight>? GetAllLight()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = LightSelect + " ORDER BY c.CreationDate";
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            List<DirectionalControlEvaluationCaseLight> result = [];
            while (reader.Read()) result.Add(ReadLight(reader));
            return result;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to list light directional-control evaluation cases");
            return null;
        }
    }

    public DirectionalControlEvaluationCaseLight? GetLightById(Guid id)
    {
        if (id == Guid.Empty) return null;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = LightSelect + " WHERE c.ID=@id";
        command.Parameters.AddWithValue("@id", id.ToString());
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            return reader.Read() ? ReadLight(reader) : null;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to read directional-control evaluation status {CaseId}", id);
            return null;
        }
    }

    public DirectionalControlEvaluationCase? GetById(Guid id)
    {
        if (id == Guid.Empty) return null;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT DirectionalControlEvaluationCase FROM DirectionalControlEvaluationCaseTable WHERE ID=@id";
        command.Parameters.AddWithValue("@id", id.ToString());
        try
        {
            string? json = command.ExecuteScalar() as string;
            DirectionalControlEvaluationCase? value = json == null
                ? null
                : JsonSerializer.Deserialize<DirectionalControlEvaluationCase>(json, JsonSettings.Options);
            RefreshStale(id, value);
            return value;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to read directional-control evaluation case {CaseId}", id);
            return null;
        }
    }

    public int? GetSampleChunkCount(Guid caseId)
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM DirectionalControlEvaluationSampleChunkTable WHERE CaseID=@id";
        command.Parameters.AddWithValue("@id", caseId.ToString());
        try { return Convert.ToInt32(command.ExecuteScalar()); }
        catch (SqliteException ex)
        {
            logger_.LogError(ex, "Unable to count directional-control sample chunks for {CaseId}", caseId);
            return null;
        }
    }

    public DirectionalControlEvaluationSampleChunk? GetSampleChunk(Guid caseId, int chunkIndex)
    {
        if (caseId == Guid.Empty || chunkIndex < 0) return null;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT SampleCount,StartActualMD,EndActualMD,DirectionalControlEvaluationSamples
            FROM DirectionalControlEvaluationSampleChunkTable
            WHERE CaseID=@id AND ChunkIndex=@chunk
            """;
        command.Parameters.AddWithValue("@id", caseId.ToString());
        command.Parameters.AddWithValue("@chunk", chunkIndex);
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return new DirectionalControlEvaluationSampleChunk
            {
                ChunkIndex = chunkIndex,
                SampleCount = reader.GetInt32(0),
                StartActualMD = reader.IsDBNull(1) ? null : reader.GetDouble(1),
                EndActualMD = reader.IsDBNull(2) ? null : reader.GetDouble(2),
                SampleList = JsonSerializer.Deserialize<List<DirectionalControlEvaluationSample>>(
                    reader.GetString(3), JsonSettings.Options) ?? []
            };
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to read directional-control sample chunk {ChunkIndex} for {CaseId}", chunkIndex, caseId);
            return null;
        }
    }

    public List<string> ValidateReferences(DirectionalControlEvaluationCase value)
    {
        List<string> errors = [];
        Dictionary<Guid, Guid> wellBoreIds = GetTrajectoryWellBoreIds(
            value.ReferenceTrajectoryID, value.ActualTrajectoryID);
        bool hasReference = wellBoreIds.TryGetValue(value.ReferenceTrajectoryID, out Guid referenceWellBoreId);
        bool hasActual = wellBoreIds.TryGetValue(value.ActualTrajectoryID, out Guid actualWellBoreId);
        if (!hasReference) errors.Add("reference_trajectory_not_found");
        if (!hasActual) errors.Add("actual_trajectory_not_found");
        if (hasReference && hasActual &&
            (referenceWellBoreId == Guid.Empty || referenceWellBoreId != actualWellBoreId))
            errors.Add("trajectories_must_belong_to_same_wellbore");
        return errors;
    }

    private Dictionary<Guid, Guid> GetTrajectoryWellBoreIds(Guid firstId, Guid secondId)
    {
        Dictionary<Guid, Guid> result = [];
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return result;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ID,WellBoreID FROM TrajectoryTable WHERE ID=@first OR ID=@second";
        command.Parameters.AddWithValue("@first", firstId.ToString());
        command.Parameters.AddWithValue("@second", secondId.ToString());
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read() && !reader.IsDBNull(0) && !reader.IsDBNull(1))
            {
                if (Guid.TryParse(reader.GetString(0), out Guid id) &&
                    Guid.TryParse(reader.GetString(1), out Guid wellBoreId))
                    result[id] = wellBoreId;
            }
        }
        catch (SqliteException ex)
        {
            logger_.LogError(ex, "Unable to validate directional-control trajectory references");
        }
        return result;
    }

    public Task<bool> AddAsync(DirectionalControlEvaluationCase value)
    {
        if (DirectionalControlEvaluationValidation.Validate(value).Count > 0 ||
            ValidateReferences(value).Count > 0 || GetLightById(value.MetaInfo!.ID) != null)
            return Task.FromResult(false);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        value.CreationDate = now;
        value.LastModificationDate = now;
        PrepareQueued(value);
        return Task.FromResult(SaveCase(value, update: false));
    }

    public Task<bool> UpdateAsync(Guid id, DateTimeOffset expectedRevision, DirectionalControlEvaluationCase value)
    {
        DirectionalControlEvaluationCase? existing = GetById(id);
        if (existing == null || value.MetaInfo?.ID != id ||
            DirectionalControlEvaluationValidation.Validate(value).Count > 0 || ValidateReferences(value).Count > 0)
            return Task.FromResult(false);
        value.CreationDate = existing.CreationDate;
        value.LastModificationDate = DateTimeOffset.UtcNow;
        PrepareQueued(value);
        return Task.FromResult(SaveCase(value, update: true, expectedRevision));
    }

    public bool Delete(Guid id, DateTimeOffset expectedRevision)
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return false;
        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand deleteChunks = connection.CreateCommand();
        deleteChunks.Transaction = transaction;
        deleteChunks.CommandText = "DELETE FROM DirectionalControlEvaluationSampleChunkTable WHERE CaseID=@id";
        deleteChunks.Parameters.AddWithValue("@id", id.ToString());
        deleteChunks.ExecuteNonQuery();
        using SqliteCommand deleteCase = connection.CreateCommand();
        deleteCase.Transaction = transaction;
        deleteCase.CommandText = "DELETE FROM DirectionalControlEvaluationCaseTable WHERE ID=@id AND LastModificationDate=@expected";
        deleteCase.Parameters.AddWithValue("@id", id.ToString());
        deleteCase.Parameters.AddWithValue("@expected", expectedRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        if (deleteCase.ExecuteNonQuery() != 1) { transaction.Rollback(); return false; }
        transaction.Commit();
        return true;
    }

    public List<(Guid Id, DateTimeOffset Revision)> PrepareInterruptedCalculationsForResume()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return [];
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT ID,LastModificationDate FROM DirectionalControlEvaluationCaseTable
            WHERE CalculationState=@queued OR CalculationState=@running
            """;
        command.Parameters.AddWithValue("@queued", CalculationState.Queued.ToString());
        command.Parameters.AddWithValue("@running", CalculationState.Running.ToString());
        List<(Guid, DateTimeOffset)> result = [];
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                if (Guid.TryParse(reader.GetString(0), out Guid id) &&
                    DateTimeOffset.TryParse(reader.GetString(1), out DateTimeOffset revision))
                    result.Add((id, revision));
        }
        catch (SqliteException ex)
        {
            logger_.LogError(ex, "Unable to find interrupted directional-control calculations");
        }
        return result;
    }

    public int RepairMissingTrajectoryRevisions()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return 0;
        using SqliteCommand read = connection.CreateCommand();
        read.CommandText = """
            SELECT c.ID,c.DirectionalControlEvaluationCase,c.CalculationFingerprint,
                   reference.LastModificationDate,actual.LastModificationDate
            FROM DirectionalControlEvaluationCaseTable c
            LEFT JOIN TrajectoryTable reference ON reference.ID=c.ReferenceTrajectoryID
            LEFT JOIN TrajectoryTable actual ON actual.ID=c.ActualTrajectoryID
            WHERE c.CalculationState=@completed
              AND (c.ReferenceTrajectoryRevision IS NULL OR c.ActualTrajectoryRevision IS NULL)
              AND c.CalculationFingerprint IS NOT NULL
            """;
        read.Parameters.AddWithValue("@completed", CalculationState.Completed.ToString());
        List<(Guid Id, DateTimeOffset ReferenceRevision, DateTimeOffset ActualRevision, string Fingerprint)> repairs = [];
        try
        {
            using (SqliteDataReader reader = read.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!Guid.TryParse(reader.GetString(0), out Guid id) || reader.IsDBNull(1) || reader.IsDBNull(2))
                        continue;
                    DateTimeOffset? referenceRevision = ReadDate(reader, 3);
                    DateTimeOffset? actualRevision = ReadDate(reader, 4);
                    DirectionalControlEvaluationCase? value = JsonSerializer.Deserialize<DirectionalControlEvaluationCase>(
                        reader.GetString(1), JsonSettings.Options);
                    string fingerprint = reader.GetString(2);
                    if (value != null && referenceRevision.HasValue && actualRevision.HasValue &&
                        string.Equals(fingerprint,
                            CreateFingerprint(value, referenceRevision.Value, actualRevision.Value),
                            StringComparison.Ordinal))
                        repairs.Add((id, referenceRevision.Value, actualRevision.Value, fingerprint));
                }
            }

            if (repairs.Count == 0) return 0;
            using SqliteTransaction transaction = connection.BeginTransaction();
            int repaired = 0;
            foreach ((Guid id, DateTimeOffset referenceRevision, DateTimeOffset actualRevision, string fingerprint) in repairs)
            {
                using SqliteCommand update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE DirectionalControlEvaluationCaseTable
                    SET ReferenceTrajectoryRevision=@reference,ActualTrajectoryRevision=@actual
                    WHERE ID=@id AND CalculationFingerprint=@fingerprint
                      AND (ReferenceTrajectoryRevision IS NULL OR ActualTrajectoryRevision IS NULL)
                    """;
                update.Parameters.AddWithValue("@reference", referenceRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                update.Parameters.AddWithValue("@actual", actualRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                update.Parameters.AddWithValue("@id", id.ToString());
                update.Parameters.AddWithValue("@fingerprint", fingerprint);
                repaired += update.ExecuteNonQuery();
            }
            transaction.Commit();
            return repaired;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to repair missing directional-control trajectory revisions");
            return 0;
        }
    }

    internal async Task RecalculateAsync(Guid id, DateTimeOffset queuedRevision, CancellationToken cancellationToken)
    {
        await Task.Yield();
        Stopwatch elapsed = Stopwatch.StartNew();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            DirectionalControlEvaluationCase? value = GetById(id);
            if (value?.LastModificationDate != queuedRevision) return;
            Mark(value, CalculationState.Running, 0.01, "Loading reference and actual trajectories");
            if (!UpdateProgress(value, queuedRevision)) return;

            Model.Trajectory? reference = trajectoryManager_.GetTrajectoryById(value.ReferenceTrajectoryID, includeCalculatedStations: true);
            Model.Trajectory? actual = trajectoryManager_.GetTrajectoryById(value.ActualTrajectoryID, includeCalculatedStations: true);
            if (reference == null || actual == null)
            {
                Mark(value, CalculationState.Failed, 1.0, "The reference or actual trajectory no longer exists");
            }
            else if (reference.WellBoreID == Guid.Empty || reference.WellBoreID != actual.WellBoreID)
            {
                Mark(value, CalculationState.Failed, 1.0, "The reference and actual trajectories must belong to the same wellbore");
            }
            else
            {
                value.ReferenceTrajectoryRevision = reference.LastModificationDate;
                value.ActualTrajectoryRevision = actual.LastModificationDate;
                value.CalculationFingerprint = CreateFingerprint(value, reference, actual);
                bool calculated = DirectionalControlEvaluationCalculator.Calculate(value, reference, actual, (progress, message) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    value.CalculationState = CalculationState.Running;
                    value.CalculationProgress = Math.Clamp(0.03 + 0.95 * progress, 0.03, 0.98);
                    value.CalculationMessage = message;
                    UpdateProgress(value, queuedRevision);
                });
                Mark(value, calculated ? CalculationState.Completed : CalculationState.Failed, 1.0, value.CalculationMessage);
            }

            if (GetById(id)?.LastModificationDate != queuedRevision) return;
            value.LastModificationDate = DateTimeOffset.UtcNow;
            if (!SaveTerminal(value, queuedRevision))
            {
                logger_.LogError("Unable to persist completed directional-control evaluation {CaseId}", id);
                return;
            }
            logger_.LogInformation(
                "Directional-control evaluation {CaseId} finished in {ElapsedMilliseconds} ms with {SampleCount} samples and {BundleCount} bundles",
                id, elapsed.ElapsedMilliseconds, value.SampleList?.Count ?? 0, value.BundleList?.Count ?? 0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DirectionalControlEvaluationCase? value = GetById(id);
            if (value?.LastModificationDate == queuedRevision)
            {
                Mark(value, CalculationState.Queued, value.CalculationProgress, "Calculation interrupted by service shutdown");
                UpdateProgress(value, queuedRevision);
            }
        }
        catch (Exception ex)
        {
            logger_.LogError(ex, "Unexpected directional-control evaluation failure for {CaseId}", id);
            DirectionalControlEvaluationCase? value = GetById(id);
            if (value?.LastModificationDate == queuedRevision)
            {
                Mark(value, CalculationState.Failed, 1.0, "Directional-control evaluation failed");
                value.LastModificationDate = DateTimeOffset.UtcNow;
                SaveTerminal(value, queuedRevision);
            }
        }
    }

    private const string LightSelect = """
        SELECT c.MetaInfo,c.Name,c.Description,c.CreationDate,c.LastModificationDate,
               c.ReferenceTrajectoryID,c.ActualTrajectoryID,c.CurveType,c.CalculationState,
               c.CalculationProgress,c.CalculationMessage,c.ReferenceTrajectoryRevision,
               c.ActualTrajectoryRevision,c.CalculationFingerprint,
               reference.LastModificationDate,actual.LastModificationDate
        FROM DirectionalControlEvaluationCaseTable c INDEXED BY DirectionalControlEvaluationCaseLightIndex
        LEFT JOIN TrajectoryTable reference ON reference.ID=c.ReferenceTrajectoryID
        LEFT JOIN TrajectoryTable actual ON actual.ID=c.ActualTrajectoryID
        """;

    private static DirectionalControlEvaluationCaseLight ReadLight(SqliteDataReader reader)
    {
        CalculationState state = Enum.TryParse(reader.GetString(8), out CalculationState parsed)
            ? parsed : CalculationState.NotCalculated;
        bool stale = state == CalculationState.Completed &&
            (reader.IsDBNull(13) || string.IsNullOrWhiteSpace(reader.GetString(13)) ||
             !SameStoredRevision(ReadDate(reader, 11), ReadDate(reader, 14)) ||
             !SameStoredRevision(ReadDate(reader, 12), ReadDate(reader, 15)));
        return new DirectionalControlEvaluationCaseLight
        {
            MetaInfo = reader.IsDBNull(0) ? null : JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options),
            Name = reader.IsDBNull(1) ? null : reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            CreationDate = ReadDate(reader, 3),
            LastModificationDate = ReadDate(reader, 4),
            ReferenceTrajectoryID = Guid.TryParse(reader.GetString(5), out Guid referenceId) ? referenceId : Guid.Empty,
            ActualTrajectoryID = Guid.TryParse(reader.GetString(6), out Guid actualId) ? actualId : Guid.Empty,
            CurveType = Enum.TryParse(reader.GetString(7), out ExtrapolationCurveType curve) ? curve : default,
            CalculationState = state,
            CalculationProgress = reader.IsDBNull(9) ? 0.0 : reader.GetDouble(9),
            CalculationMessage = reader.IsDBNull(10) ? null : reader.GetString(10),
            IsStale = stale
        };
    }

    internal bool SaveCase(DirectionalControlEvaluationCase value, bool update, DateTimeOffset? expectedRevision = null)
    {
        if (value.MetaInfo == null) return false;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return false;
        using SqliteCommand command = CreateSaveCommand(connection, null, value, update, expectedRevision);
        try { return command.ExecuteNonQuery() == 1; }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to save directional-control evaluation case {CaseId}", value.MetaInfo.ID);
            return false;
        }
    }

    internal bool SaveTerminal(DirectionalControlEvaluationCase value, DateTimeOffset expectedRevision)
    {
        if (value.MetaInfo == null) return false;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return false;
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            using SqliteCommand save = CreateSaveCommand(connection, transaction, value, update: true, expectedRevision);
            if (save.ExecuteNonQuery() != 1) { transaction.Rollback(); return false; }
            using SqliteCommand delete = connection.CreateCommand();
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM DirectionalControlEvaluationSampleChunkTable WHERE CaseID=@id";
            delete.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
            delete.ExecuteNonQuery();

            List<DirectionalControlEvaluationSample> samples = value.SampleList ?? [];
            for (int offset = 0, chunkIndex = 0; offset < samples.Count; offset += SampleChunkSize, chunkIndex++)
            {
                List<DirectionalControlEvaluationSample> chunk = samples.Skip(offset).Take(SampleChunkSize).ToList();
                using SqliteCommand insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO DirectionalControlEvaluationSampleChunkTable
                    (ID,CaseID,ChunkIndex,SampleCount,StartActualMD,EndActualMD,DirectionalControlEvaluationSamples)
                    VALUES (@id,@case,@chunk,@count,@start,@end,@data)
                    """;
                insert.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                insert.Parameters.AddWithValue("@case", value.MetaInfo.ID.ToString());
                insert.Parameters.AddWithValue("@chunk", chunkIndex);
                insert.Parameters.AddWithValue("@count", chunk.Count);
                insert.Parameters.AddWithValue("@start", chunk[0].ActualMD);
                insert.Parameters.AddWithValue("@end", chunk[^1].ActualEndMD);
                insert.Parameters.AddWithValue("@data", JsonSerializer.Serialize(chunk, JsonSettings.Options));
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
            return true;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger_.LogError(ex, "Unable to save directional-control results for {CaseId}", value.MetaInfo.ID);
            return false;
        }
    }

    private static SqliteCommand CreateSaveCommand(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        DirectionalControlEvaluationCase value,
        bool update,
        DateTimeOffset? expectedRevision)
    {
        SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = update
            ? "UPDATE DirectionalControlEvaluationCaseTable SET MetaInfo=@meta,Name=@name,Description=@description,CreationDate=@created,LastModificationDate=@modified,ReferenceTrajectoryID=@reference,ActualTrajectoryID=@actual,CurveType=@curve,CalculationState=@state,CalculationProgress=@progress,CalculationMessage=@message,ReferenceTrajectoryRevision=@referenceRevision,ActualTrajectoryRevision=@actualRevision,CalculationFingerprint=@fingerprint,DirectionalControlEvaluationCase=@data WHERE ID=@id AND LastModificationDate=@expected"
            : "INSERT INTO DirectionalControlEvaluationCaseTable (ID,MetaInfo,Name,Description,CreationDate,LastModificationDate,ReferenceTrajectoryID,ActualTrajectoryID,CurveType,CalculationState,CalculationProgress,CalculationMessage,ReferenceTrajectoryRevision,ActualTrajectoryRevision,CalculationFingerprint,DirectionalControlEvaluationCase) VALUES (@id,@meta,@name,@description,@created,@modified,@reference,@actual,@curve,@state,@progress,@message,@referenceRevision,@actualRevision,@fingerprint,@data)";
        command.Parameters.AddWithValue("@id", value.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("@name", (object?)value.Name ?? DBNull.Value);
        command.Parameters.AddWithValue("@description", (object?)value.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@created", (object?)value.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@modified", (object?)value.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@reference", value.ReferenceTrajectoryID.ToString());
        command.Parameters.AddWithValue("@actual", value.ActualTrajectoryID.ToString());
        command.Parameters.AddWithValue("@curve", value.CurveType.ToString());
        command.Parameters.AddWithValue("@state", value.CalculationState.ToString());
        command.Parameters.AddWithValue("@progress", value.CalculationProgress);
        command.Parameters.AddWithValue("@message", (object?)value.CalculationMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@referenceRevision", (object?)value.ReferenceTrajectoryRevision?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@actualRevision", (object?)value.ActualTrajectoryRevision?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@fingerprint", (object?)value.CalculationFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("@data", SerializeCompact(value));
        if (update)
            command.Parameters.AddWithValue("@expected", expectedRevision!.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        return command;
    }

    private bool UpdateProgress(DirectionalControlEvaluationCase value, DateTimeOffset expectedRevision)
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null || value.MetaInfo == null) return false;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE DirectionalControlEvaluationCaseTable SET CalculationState=@state,CalculationProgress=@progress,CalculationMessage=@message WHERE ID=@id AND LastModificationDate=@expected";
        command.Parameters.AddWithValue("@state", value.CalculationState.ToString());
        command.Parameters.AddWithValue("@progress", value.CalculationProgress);
        command.Parameters.AddWithValue("@message", (object?)value.CalculationMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
        command.Parameters.AddWithValue("@expected", expectedRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        return command.ExecuteNonQuery() == 1;
    }

    private static string SerializeCompact(DirectionalControlEvaluationCase value)
    {
        List<DirectionalControlEvaluationSample>? samples = value.SampleList;
        try
        {
            value.SampleList = [];
            return JsonSerializer.Serialize(value, JsonSettings.Options);
        }
        finally { value.SampleList = samples; }
    }

    private static void PrepareQueued(DirectionalControlEvaluationCase value)
    {
        value.ReferenceTrajectoryRevision = null;
        value.ActualTrajectoryRevision = null;
        value.CalculationFingerprint = null;
        value.SampleList = [];
        value.BundleList = [];
        Mark(value, CalculationState.Queued, 0.0, "Calculation queued");
    }

    private static void Mark(DirectionalControlEvaluationCase value, CalculationState state, double progress, string? message)
    {
        value.CalculationState = state;
        value.CalculationProgress = progress;
        value.CalculationMessage = message;
    }

    private static string CreateFingerprint(
        DirectionalControlEvaluationCase value,
        Model.Trajectory reference,
        Model.Trajectory actual) =>
        CreateFingerprint(value, reference.LastModificationDate, actual.LastModificationDate);

    internal static string CreateFingerprint(
        DirectionalControlEvaluationCase value,
        DateTimeOffset? referenceRevision,
        DateTimeOffset? actualRevision)
    {
        string input = string.Join('|',
            value.ReferenceTrajectoryID, value.ActualTrajectoryID, value.CurveType,
            value.EvaluationInterval, value.StartActualMD, value.EndActualMD,
            value.ReferenceMDAdvance, value.JunctionCurvatureRatio,
            value.MaximumInvalidGap, value.MinimumBundleLength, value.MinimumBundleSampleCount,
            value.BundlingPenalty, referenceRevision, actualRevision);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    private static DateTimeOffset? ReadDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) || !DateTimeOffset.TryParse(reader.GetString(ordinal), out DateTimeOffset value)
            ? null : value;

    internal static bool SameStoredRevision(DateTimeOffset? left, DateTimeOffset? right) =>
        left.HasValue && right.HasValue && left.Value.ToUnixTimeSeconds() == right.Value.ToUnixTimeSeconds();

    private void RefreshStale(Guid id, DirectionalControlEvaluationCase? value)
    {
        if (value == null || value.CalculationState != CalculationState.Completed) return;
        // Keep the editor payload and the list/status endpoints on one authoritative
        // staleness calculation. Separate queries previously disagreed in production
        // even when both stored trajectory revisions matched exactly.
        value.IsStale = GetLightById(id)?.IsStale ?? true;
    }
}
