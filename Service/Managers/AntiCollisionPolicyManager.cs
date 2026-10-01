using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Text.Json;

namespace OSDC.Drilling.Trajectory.Service.Managers;

/// <summary>Durable immutable policy revisions and effective-dated Field assignments.</summary>
public sealed class AntiCollisionPolicyManager(
    ILogger<AntiCollisionPolicyManager> logger,
    SqlConnectionManager connectionManager)
{
    public List<Guid>? GetRevisionIds()
    {
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ID FROM AntiCollisionPolicyRevisionTable ORDER BY PolicyID,RevisionNumber";
        return ReadGuids(command, "list anti-collision policy revisions");
    }

    public List<AntiCollisionPolicyRevision>? GetRevisions(Guid? policyId = null)
    {
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT AntiCollisionPolicyRevision FROM AntiCollisionPolicyRevisionTable" +
            (policyId.HasValue ? " WHERE PolicyID=@policyId" : "") + " ORDER BY PolicyID,RevisionNumber";
        if (policyId.HasValue) command.Parameters.AddWithValue("@policyId", policyId.Value.ToString());
        return ReadDocuments<AntiCollisionPolicyRevision>(command, "list anti-collision policy revisions");
    }

    public AntiCollisionPolicyRevision? GetRevision(Guid revisionId)
    {
        if (revisionId == Guid.Empty) return null;
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT AntiCollisionPolicyRevision FROM AntiCollisionPolicyRevisionTable WHERE ID=@id";
        command.Parameters.AddWithValue("@id", revisionId.ToString());
        return ReadDocument<AntiCollisionPolicyRevision>(command, "read anti-collision policy revision");
    }

    public bool AddRevision(AntiCollisionPolicyRevision value)
    {
        if (AntiCollisionPolicyValidation.Validate(value).Count > 0 || value.MetaInfo == null) return false;
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return false;
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            using SqliteCommand duplicate = connection.CreateCommand();
            duplicate.Transaction = transaction;
            duplicate.CommandText = "SELECT COUNT(*) FROM AntiCollisionPolicyRevisionTable WHERE ID=@id";
            duplicate.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
            if ((long)(duplicate.ExecuteScalar() ?? 0L) != 0) return false;

            using SqliteCommand revision = connection.CreateCommand();
            revision.Transaction = transaction;
            revision.CommandText = "SELECT COALESCE(MAX(RevisionNumber),0)+1 FROM AntiCollisionPolicyRevisionTable WHERE PolicyID=@policyId";
            revision.Parameters.AddWithValue("@policyId", value.PolicyID.ToString());
            value.RevisionNumber = Convert.ToInt32((long)(revision.ExecuteScalar() ?? 1L));
            value.CreationDate = DateTimeOffset.UtcNow;
            SortRules(value);

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO AntiCollisionPolicyRevisionTable(ID,PolicyID,RevisionNumber,Name,CreationDate,AntiCollisionPolicyRevision) VALUES(@id,@policyId,@revision,@name,@created,@document)";
            command.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
            command.Parameters.AddWithValue("@policyId", value.PolicyID.ToString());
            command.Parameters.AddWithValue("@revision", value.RevisionNumber);
            command.Parameters.AddWithValue("@name", (object?)value.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@created", value.CreationDate.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            command.Parameters.AddWithValue("@document", JsonSerializer.Serialize(value, JsonSettings.Options));
            bool success = command.ExecuteNonQuery() == 1;
            if (success) transaction.Commit();
            return success;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger.LogError(ex, "Unable to create anti-collision policy revision {RevisionId}", value.MetaInfo.ID);
            return false;
        }
    }

    public List<FieldAntiCollisionPolicyAssignment>? GetAssignments(Guid? fieldId = null)
    {
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT FieldAntiCollisionPolicyAssignment FROM FieldAntiCollisionPolicyAssignmentTable" +
            (fieldId.HasValue ? " WHERE FieldID=@fieldId" : "") + " ORDER BY FieldID,ValidFromUtc";
        if (fieldId.HasValue) command.Parameters.AddWithValue("@fieldId", fieldId.Value.ToString());
        return ReadDocuments<FieldAntiCollisionPolicyAssignment>(command, "list Field anti-collision policy assignments");
    }

    public FieldAntiCollisionPolicyAssignment? GetAssignment(Guid assignmentId)
    {
        if (assignmentId == Guid.Empty) return null;
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT FieldAntiCollisionPolicyAssignment FROM FieldAntiCollisionPolicyAssignmentTable WHERE ID=@id";
        command.Parameters.AddWithValue("@id", assignmentId.ToString());
        return ReadDocument<FieldAntiCollisionPolicyAssignment>(command, "read Field anti-collision policy assignment");
    }

    public FieldAntiCollisionPolicyAssignment? GetEffectiveAssignment(Guid fieldId, DateTimeOffset instant)
    {
        return GetAssignments(fieldId)?.SingleOrDefault(value =>
            value.ValidFromUtc <= instant && (!value.ValidToUtc.HasValue || instant < value.ValidToUtc));
    }

    public bool AddAssignment(FieldAntiCollisionPolicyAssignment value)
    {
        if (AntiCollisionPolicyValidation.Validate(value).Count > 0 || value.MetaInfo == null || GetRevision(value.PolicyRevisionID) == null) return false;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        value.CreationDate = now;
        value.LastModificationDate = now;
        return SaveAssignment(value, false, null);
    }

    public bool UpdateAssignment(FieldAntiCollisionPolicyAssignment value, DateTimeOffset expectedModifiedUtc)
    {
        if (AntiCollisionPolicyValidation.Validate(value).Count > 0 || value.MetaInfo == null || GetRevision(value.PolicyRevisionID) == null) return false;
        FieldAntiCollisionPolicyAssignment? existing = GetAssignment(value.MetaInfo.ID);
        if (existing == null) return false;
        value.CreationDate = existing.CreationDate;
        value.LastModificationDate = DateTimeOffset.UtcNow;
        return SaveAssignment(value, true, expectedModifiedUtc);
    }

    public bool DeleteFutureAssignment(Guid id, DateTimeOffset expectedModifiedUtc, DateTimeOffset now)
    {
        FieldAntiCollisionPolicyAssignment? existing = GetAssignment(id);
        if (existing == null || existing.ValidFromUtc <= now) return false;
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null) return false;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM FieldAntiCollisionPolicyAssignmentTable WHERE ID=@id AND LastModificationDate=@expected";
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@expected", expectedModifiedUtc.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        return command.ExecuteNonQuery() == 1;
    }

    private bool SaveAssignment(FieldAntiCollisionPolicyAssignment value, bool update, DateTimeOffset? expected)
    {
        using SqliteConnection? connection = connectionManager.GetConnection();
        if (connection == null || value.MetaInfo == null) return false;
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            List<FieldAntiCollisionPolicyAssignment> others = ReadAssignments(connection, transaction, value.FieldID, value.MetaInfo.ID);
            if (others.Any(other => Overlaps(value.ValidFromUtc, value.ValidToUtc, other.ValidFromUtc, other.ValidToUtc))) return false;
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = update
                ? "UPDATE FieldAntiCollisionPolicyAssignmentTable SET FieldID=@field,PolicyRevisionID=@policy,ValidFromUtc=@from,ValidToUtc=@to,CreationDate=@created,LastModificationDate=@modified,FieldAntiCollisionPolicyAssignment=@document WHERE ID=@id AND LastModificationDate=@expected"
                : "INSERT INTO FieldAntiCollisionPolicyAssignmentTable(ID,FieldID,PolicyRevisionID,ValidFromUtc,ValidToUtc,CreationDate,LastModificationDate,FieldAntiCollisionPolicyAssignment) VALUES(@id,@field,@policy,@from,@to,@created,@modified,@document)";
            command.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
            command.Parameters.AddWithValue("@field", value.FieldID.ToString());
            command.Parameters.AddWithValue("@policy", value.PolicyRevisionID.ToString());
            command.Parameters.AddWithValue("@from", value.ValidFromUtc.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            command.Parameters.AddWithValue("@to", (object?)value.ValidToUtc?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
            command.Parameters.AddWithValue("@created", value.CreationDate!.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            command.Parameters.AddWithValue("@modified", value.LastModificationDate!.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            command.Parameters.AddWithValue("@document", JsonSerializer.Serialize(value, JsonSettings.Options));
            if (update) command.Parameters.AddWithValue("@expected", expected!.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            bool success = command.ExecuteNonQuery() == 1;
            if (success) transaction.Commit();
            return success;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger.LogError(ex, "Unable to save Field anti-collision policy assignment {AssignmentId}", value.MetaInfo.ID);
            return false;
        }
    }

    private static List<FieldAntiCollisionPolicyAssignment> ReadAssignments(SqliteConnection connection, SqliteTransaction transaction, Guid fieldId, Guid exceptId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT FieldAntiCollisionPolicyAssignment FROM FieldAntiCollisionPolicyAssignmentTable WHERE FieldID=@field AND ID<>@id";
        command.Parameters.AddWithValue("@field", fieldId.ToString());
        command.Parameters.AddWithValue("@id", exceptId.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        List<FieldAntiCollisionPolicyAssignment> values = [];
        while (reader.Read())
        {
            FieldAntiCollisionPolicyAssignment? value = JsonSerializer.Deserialize<FieldAntiCollisionPolicyAssignment>(reader.GetString(0), JsonSettings.Options);
            if (value != null) values.Add(value);
        }
        return values;
    }

    private static bool Overlaps(DateTimeOffset leftFrom, DateTimeOffset? leftTo, DateTimeOffset rightFrom, DateTimeOffset? rightTo) =>
        (!leftTo.HasValue || rightFrom < leftTo) && (!rightTo.HasValue || leftFrom < rightTo);

    private static void SortRules(AntiCollisionPolicyRevision value) => value.Rules = value.Rules.OrderBy(rule => rule.Priority).ToList();

    private List<Guid>? ReadGuids(SqliteCommand command, string operation)
    {
        try { using SqliteDataReader reader = command.ExecuteReader(); List<Guid> values = []; while (reader.Read()) values.Add(reader.GetGuid(0)); return values; }
        catch (SqliteException ex) { logger.LogError(ex, "Unable to {Operation}", operation); return null; }
    }

    private List<T>? ReadDocuments<T>(SqliteCommand command, string operation)
    {
        try
        {
            using SqliteDataReader reader = command.ExecuteReader(); List<T> values = [];
            while (reader.Read()) { T? value = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options); if (value != null) values.Add(value); }
            return values;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException) { logger.LogError(ex, "Unable to {Operation}", operation); return null; }
    }

    private T? ReadDocument<T>(SqliteCommand command, string operation)
    {
        try { string? json = command.ExecuteScalar() as string; return json == null ? default : JsonSerializer.Deserialize<T>(json, JsonSettings.Options); }
        catch (Exception ex) when (ex is SqliteException or JsonException) { logger.LogError(ex, "Unable to {Operation}", operation); return default; }
    }
}
