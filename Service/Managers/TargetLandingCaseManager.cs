using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Managers;

public sealed class TargetLandingCaseManager
{
    private static TargetLandingCaseManager? instance_;
    private readonly ILogger<TargetLandingCaseManager> logger_;
    private readonly SqlConnectionManager connectionManager_;
    private readonly TrajectoryManager trajectoryManager_;

    private TargetLandingCaseManager(ILogger<TargetLandingCaseManager> logger, SqlConnectionManager connectionManager)
    {
        logger_ = logger;
        connectionManager_ = connectionManager;
        trajectoryManager_ = TrajectoryManager.GetInstance(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TrajectoryManager>.Instance, connectionManager);
    }

    public static TargetLandingCaseManager GetInstance(ILogger<TargetLandingCaseManager> logger,
        SqlConnectionManager connectionManager) => instance_ ??= new(logger, connectionManager);

    public List<Guid>? GetAllIds()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ID FROM TargetLandingCaseTable ORDER BY CreationDate";
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            List<Guid> values = [];
            while (reader.Read()) if (Guid.TryParse(reader.GetString(0), out Guid id)) values.Add(id);
            return values;
        }
        catch (SqliteException ex) { logger_.LogError(ex, "Unable to list target landing case IDs"); return null; }
    }
    public List<MetaInfo>? GetAllMetaInfo()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT MetaInfo FROM TargetLandingCaseTable ORDER BY CreationDate";
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            List<MetaInfo> values = [];
            while (reader.Read())
            {
                MetaInfo? value = reader.IsDBNull(0) ? null : JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options);
                if (value != null) values.Add(value);
            }
            return values;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException) { logger_.LogError(ex, "Unable to list target landing case metadata"); return null; }
    }
    public List<TargetLandingCaseLight>? GetAllLight()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = LightSelect + " ORDER BY c.CreationDate";
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            List<TargetLandingCaseLight> values = [];
            while (reader.Read()) values.Add(ReadLight(reader));
            return values;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException) { logger_.LogError(ex, "Unable to list light target landing cases"); return null; }
    }
    public List<TargetLandingCase>? GetAll() => ReadAll();
    public TargetLandingCaseLight? GetLightById(Guid id)
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
            if (!reader.Read()) return null;
            return ReadLight(reader);
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to read target landing status {CaseId}", id);
            return null;
        }
    }

    public TargetLandingCase? GetById(Guid id)
        => ReadById(id, includeSamples: true);

    public TargetLandingCase? GetEditById(Guid id)
        => ReadById(id, includeSamples: false);

    public TargetLandingCase? GetDisplayById(Guid id)
    {
        TargetLandingCase? value = ReadById(id, includeSamples: true, refreshStale: false);
        if (value == null) return null;
        value.SampleList = SelectReachableBoundarySamples(value);
        foreach (TargetLandingSample sample in value.SampleList)
        {
            sample.SurveyStationList = sample.SurveyStationList?.Select(ToDisplayStation).ToList();
            sample.LandingStation = sample.LandingStation == null ? null : ToDisplayStation(sample.LandingStation);
        }
        value.LeadSurveyStationList = value.LeadSurveyStationList?.Select(ToDisplayStation).ToList();
        value.SourceEndStation = value.SourceEndStation == null ? null : ToDisplayStation(value.SourceEndStation);
        value.SteeringStartStation = value.SteeringStartStation == null ? null : ToDisplayStation(value.SteeringStartStation);
        value.MeshTriangleList = [];
        return value;
    }

    public SurveyStationEllipseCalculation? GetUncertaintyDisplayById(Guid id)
    {
        TargetLandingCase? value = ReadById(id, includeSamples: true, refreshStale: false);
        if (value == null) return null;
        Model.Trajectory? source = trajectoryManager_.GetTrajectoryById(value.SourceTrajectoryID, includeCalculatedStations: true);
        if (source?.SurveyStationList is not { Count: > 0 } sourceStations) return null;

        List<SurveyStation> stations = sourceStations
            .OrderBy(station => station.MD ?? station.Abscissa ?? double.MaxValue)
            .Select(station => new SurveyStation(station))
            .ToList();
        foreach (SurveyStation station in value.LeadSurveyStationList ?? [])
        {
            double? md = station.MD ?? station.Abscissa;
            if (md.HasValue && stations.Any(existing =>
                    Math.Abs((existing.MD ?? existing.Abscissa ?? double.MaxValue) - md.Value) <= 1e-9))
                continue;
            stations.Add(new SurveyStation(station));
        }

        SurveyStationEllipseCalculation calculation = new()
        {
            ConfidenceFactor = value.ConfidenceFactor,
            SurveyStationList = stations
        };
        calculation.CalculatePerpendicularOnly();

        // Positions are already available from the chunked source and compact landing display
        // payloads. Return only MD-keyed ellipse parameters to avoid duplicating the trajectory,
        // including when calculation cannot produce an ellipse and returns a diagnostic message.
        calculation.SurveyStationList = null;
        return calculation;
    }

    internal static List<TargetLandingSample> SelectReachableBoundarySamples(TargetLandingCase value,
        int maximumSampleCount = 250)
    {
        if (maximumSampleCount <= 0) return [];
        List<TargetLandingSample> reachable = (value.SampleList ?? [])
            .Where(sample => sample.State == TargetLandingSampleState.Reachable &&
                sample.SolvedSectionList is { Count: > 0 })
            .ToList();
        if (reachable.Count == 0) return [];

        Dictionary<(double X, double Y), TargetLandingSample> reachableByPosition = reachable
            .GroupBy(sample => (sample.PlaneX, sample.PlaneY))
            .ToDictionary(group => group.Key, group => group.First());
        HashSet<Guid> selectedIds = [];
        List<TargetLandingSample> selected = [];
        foreach (TargetPlanePoint point in (value.ReachableTargetContourList ?? []).SelectMany(contour => contour))
        {
            TargetLandingSample? nearest = reachableByPosition.GetValueOrDefault((point.X, point.Y));
            nearest ??= reachable.MinBy(sample => SquaredDistance(point, sample));
            if (nearest != null && selectedIds.Add(nearest.SampleID)) selected.Add(nearest);
            if (selected.Count >= maximumSampleCount) break;
        }
        return selected;
    }

    private TargetLandingCase? ReadById(Guid id, bool includeSamples, bool refreshStale = true)
    {
        if (id == Guid.Empty) return null;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = includeSamples
            ? "SELECT TargetLandingCase FROM TargetLandingCaseTable WHERE ID=@id"
            : "SELECT TargetLandingCaseEditData FROM TargetLandingCaseTable WHERE ID=@id";
        command.Parameters.AddWithValue("@id", id.ToString());
        try
        {
            string? json = command.ExecuteScalar() as string;
            TargetLandingCase? value = json == null ? null : JsonSerializer.Deserialize<TargetLandingCase>(json, JsonSettings.Options);
            if (refreshStale) RefreshStaleLight(value);
            return value;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to read target landing case {CaseId}", id);
            return null;
        }
    }

    internal const string LightSelect = """
        SELECT c.MetaInfo,c.CreationDate,c.LastModificationDate,c.SourceTrajectoryID,c.TargetType,c.CurveType,c.AttitudeMode,
               c.CalculationState,c.CalculationProgress,c.CalculationMessage,
               c.Name,c.Description,c.SourceTrajectoryRevision,c.CalculationFingerprint,
               t.LastModificationDate
        FROM TargetLandingCaseTable c INDEXED BY TargetLandingCaseLightCoveringIndex
        LEFT JOIN TrajectoryTable t ON t.ID=c.SourceTrajectoryID
        """;

    private static TargetLandingCaseLight ReadLight(SqliteDataReader reader)
    {
        CalculationState state = Enum.TryParse(reader.GetString(7), out CalculationState parsedState) ? parsedState : CalculationState.NotCalculated;
        DateTimeOffset? sourceRevision = ReadJsonDate(reader, 12);
        DateTimeOffset? currentSourceRevision = ReadDate(reader, 14);
        bool stale = state == CalculationState.Completed &&
            (reader.IsDBNull(13) || string.IsNullOrWhiteSpace(reader.GetString(13)) ||
                !SameStoredRevision(sourceRevision, currentSourceRevision));
        return new TargetLandingCaseLight
        {
            MetaInfo = reader.IsDBNull(0) ? null : JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options),
            CreationDate = ReadDate(reader, 1), LastModificationDate = ReadDate(reader, 2),
            SourceTrajectoryID = Guid.TryParse(reader.GetString(3), out Guid sourceId) ? sourceId : Guid.Empty,
            TargetType = Enum.TryParse(reader.GetString(4), out TargetLandingTargetType targetType) ? targetType : default,
            CurveType = Enum.TryParse(reader.GetString(5), out ExtrapolationCurveType curveType) ? curveType : default,
            AttitudeMode = Enum.TryParse(reader.GetString(6), out TargetLandingAttitudeMode attitudeMode) ? attitudeMode : default,
            CalculationState = state, CalculationProgress = reader.IsDBNull(8) ? 0.0 : reader.GetDouble(8),
            CalculationMessage = reader.IsDBNull(9) ? null : reader.GetString(9),
            Name = reader.IsDBNull(10) ? null : reader.GetString(10), Description = reader.IsDBNull(11) ? null : reader.GetString(11),
            IsStale = stale
        };
    }

    public List<(Guid Id, DateTimeOffset Revision)> PrepareInterruptedCalculationsForResume()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return [];
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT ID FROM TargetLandingCaseTable
            WHERE CalculationState=@queued OR CalculationState=@running
            """;
        command.Parameters.AddWithValue("@queued", CalculationState.Queued.ToString());
        command.Parameters.AddWithValue("@running", CalculationState.Running.ToString());

        List<Guid> interruptedIds = [];
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                if (Guid.TryParse(reader.GetString(0), out Guid id)) interruptedIds.Add(id);
        }
        catch (SqliteException ex)
        {
            logger_.LogError(ex, "Unable to find interrupted target landing calculations");
            return [];
        }

        List<(Guid Id, DateTimeOffset Revision)> requests = [];
        foreach (Guid id in interruptedIds)
        {
            // Edit data excludes the potentially very large calculated sample payload.
            TargetLandingCase? value = GetEditById(id);
            if (value?.MetaInfo == null || !value.LastModificationDate.HasValue) continue;
            DateTimeOffset revision = value.LastModificationDate!.Value;
            Mark(value, CalculationState.Queued, value.CalculationProgress, "Calculation resumed after service restart");
            if (UpdateProgress(value, revision)) requests.Add((value.MetaInfo.ID, revision));
        }
        return requests;
    }

    public Task<bool> AddAsync(TargetLandingCase value)
    {
        if (TargetLandingCalculator.Validate(value).Count > 0 || GetLightById(value.MetaInfo!.ID) != null)
            return Task.FromResult(false);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        value.CreationDate = now;
        value.LastModificationDate = now;
        Mark(value, CalculationState.Queued, 0.0, "Calculation queued");
        bool saved = Save(value, false);
        return Task.FromResult(saved);
    }

    public Task<bool> UpdateAsync(Guid id, DateTimeOffset expectedRevision, TargetLandingCase value)
    {
        TargetLandingCase? existing = GetEditById(id);
        if (existing == null || value.MetaInfo?.ID != id || TargetLandingCalculator.Validate(value).Count > 0)
            return Task.FromResult(false);
        value.CreationDate = existing.CreationDate;
        value.LastModificationDate = DateTimeOffset.UtcNow;
        Mark(value, CalculationState.Queued, 0.0, "Calculation queued");
        bool saved = Save(value, true, expectedRevision);
        return Task.FromResult(saved);
    }

    public bool Delete(Guid id, DateTimeOffset expectedRevision)
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return false;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM TargetLandingCaseTable WHERE ID=@id AND LastModificationDate=@expected";
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@expected", expectedRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        return command.ExecuteNonQuery() == 1;
    }

    internal async Task RecalculateAsync(Guid id, DateTimeOffset queuedRevision, CancellationToken cancellationToken)
    {
        await Task.Yield();
        Stopwatch elapsed = Stopwatch.StartNew();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            TargetLandingCase? value = GetById(id);
            if (value?.LastModificationDate != queuedRevision) return;
            Mark(value, CalculationState.Running, 0.01, "Preparing target landing calculation");
            if (!Save(value, true, queuedRevision))
            {
                logger_.LogWarning("Target landing calculation {CaseId} could not enter the running state", id);
                return;
            }

            Mark(value, CalculationState.Running, 0.03, "Loading source trajectory");
            UpdateProgress(value, queuedRevision);
            Model.Trajectory? source = trajectoryManager_.GetTrajectoryById(value.SourceTrajectoryID, includeCalculatedStations: true);
            Mark(value, CalculationState.Running, 0.05, "Reconstructing source trajectory and uncertainty");
            UpdateProgress(value, queuedRevision);
            source = await trajectoryManager_.CalculateTrajectoryAsync(
                source,
                recalculateSurveyRunUncertainty: true);
            if (source == null)
            {
                Mark(value, CalculationState.Failed, 1.0,
                    "Source trajectory uncertainty lineage could not be reconstructed");
            }
            else
            {
                Mark(value, CalculationState.Running, 0.08, "Preparing target plane sampling");
                UpdateProgress(value, queuedRevision);
                bool calculated = TargetLandingCalculator.Calculate(value, source, (progress, message) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // The calculator owns the result state, but Completed must not become visible
                    // until the corresponding large result has been persisted successfully below.
                    value.CalculationState = CalculationState.Running;
                    value.CalculationProgress = Math.Clamp(0.08 + 0.90 * progress, 0.08, 0.98);
                    value.CalculationMessage = message;
                    // Keep the lightweight polling representation useful during mesh sampling.
                    // LastModificationDate remains the queued revision until the terminal write.
                    UpdateProgress(value, queuedRevision);
                });
                Mark(value, calculated ? CalculationState.Completed : CalculationState.Failed, 1.0,
                    value.CalculationMessage);
            }
            TargetLandingCase? current = GetById(id);
            if (current?.LastModificationDate != queuedRevision) return;
            value.LastModificationDate = DateTimeOffset.UtcNow;
            if (!Save(value, true, queuedRevision))
            {
                logger_.LogError("Unable to persist the completed target landing calculation {CaseId}", id);
                value.LastModificationDate = queuedRevision;
                Mark(value, CalculationState.Failed, 1.0,
                    "Target landing calculation completed, but its result could not be saved");
                UpdateProgress(value, queuedRevision);
                return;
            }

            logger_.LogInformation(
                "Target landing calculation {CaseId} finished in {ElapsedMilliseconds} ms with {SampleCount} samples and {ContourPointCount} reachable contour points",
                id, elapsed.ElapsedMilliseconds, value.SampleList?.Count ?? 0,
                value.ReachableTargetContourList?.Sum(contour => contour.Count) ?? 0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TargetLandingCase? value = GetById(id);
            if (value?.LastModificationDate == queuedRevision)
            {
                Mark(value, CalculationState.Queued, value.CalculationProgress, "Calculation interrupted by service shutdown");
                Save(value, true, queuedRevision);
            }
        }
        catch (Exception ex)
        {
            logger_.LogError(ex, "Unexpected target landing calculation failure for {CaseId}", id);
            TargetLandingCase? value = GetById(id);
            if (value?.LastModificationDate == queuedRevision)
            {
                Mark(value, CalculationState.Failed, 1.0, "Target landing calculation failed");
                Save(value, true, queuedRevision);
            }
        }
    }

    private List<TargetLandingCase>? ReadAll()
    {
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return null;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT TargetLandingCase FROM TargetLandingCaseTable ORDER BY CreationDate";
        try
        {
            using SqliteDataReader reader = command.ExecuteReader();
            List<TargetLandingCase> values = [];
            while (reader.Read())
            {
                TargetLandingCase? value = JsonSerializer.Deserialize<TargetLandingCase>(reader.GetString(0), JsonSettings.Options);
                if (value != null) { RefreshStale(value); values.Add(value); }
            }
            return values;
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to list target landing cases");
            return null;
        }
    }

    private bool Save(TargetLandingCase value, bool update, DateTimeOffset? expectedRevision = null)
    {
        if (value.MetaInfo == null) return false;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return false;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = update
            ? "UPDATE TargetLandingCaseTable SET MetaInfo=@meta,CreationDate=@created,LastModificationDate=@modified,SourceTrajectoryID=@source,TargetType=@targetType,CurveType=@curveType,AttitudeMode=@attitude,CalculationState=@state,CalculationProgress=@progress,CalculationMessage=@message,TargetLandingCase=@data,Name=@name,Description=@description,SourceTrajectoryRevision=@sourceRevision,CalculationFingerprint=@fingerprint,TargetLandingCaseEditData=@editData WHERE ID=@id AND LastModificationDate=@expected"
            : "INSERT INTO TargetLandingCaseTable (ID,MetaInfo,CreationDate,LastModificationDate,SourceTrajectoryID,TargetType,CurveType,AttitudeMode,CalculationState,CalculationProgress,CalculationMessage,TargetLandingCase,Name,Description,SourceTrajectoryRevision,CalculationFingerprint,TargetLandingCaseEditData) VALUES (@id,@meta,@created,@modified,@source,@targetType,@curveType,@attitude,@state,@progress,@message,@data,@name,@description,@sourceRevision,@fingerprint,@editData)";
        command.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
        command.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("@created", (object?)value.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@modified", (object?)value.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@source", value.SourceTrajectoryID.ToString());
        command.Parameters.AddWithValue("@targetType", value.TargetType.ToString());
        command.Parameters.AddWithValue("@curveType", value.CurveType.ToString());
        command.Parameters.AddWithValue("@attitude", value.AttitudeMode.ToString());
        command.Parameters.AddWithValue("@state", value.CalculationState.ToString());
        command.Parameters.AddWithValue("@progress", value.CalculationProgress);
        command.Parameters.AddWithValue("@message", (object?)value.CalculationMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(value, JsonSettings.Options));
        command.Parameters.AddWithValue("@name", (object?)value.Name ?? DBNull.Value);
        command.Parameters.AddWithValue("@description", (object?)value.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@sourceRevision", (object?)value.SourceTrajectoryRevision?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? DBNull.Value);
        command.Parameters.AddWithValue("@fingerprint", (object?)value.CalculationFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("@editData", SerializeEditData(value));
        if (update)
        {
            if (!expectedRevision.HasValue) return false;
            command.Parameters.AddWithValue("@expected", expectedRevision.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        }
        try { return command.ExecuteNonQuery() == 1; }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            logger_.LogError(ex, "Unable to save target landing case {CaseId}", value.MetaInfo.ID);
            return false;
        }
    }

    private static string SerializeEditData(TargetLandingCase value)
    {
        List<TargetLandingSample>? samples = value.SampleList;
        List<TargetLandingMeshTriangle>? triangles = value.MeshTriangleList;
        try
        {
            value.SampleList = [];
            value.MeshTriangleList = [];
            return JsonSerializer.Serialize(value, JsonSettings.Options);
        }
        finally
        {
            value.SampleList = samples;
            value.MeshTriangleList = triangles;
        }
    }

    private bool UpdateProgress(TargetLandingCase value, DateTimeOffset expectedRevision)
    {
        if (value.MetaInfo == null) return false;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) return false;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE TargetLandingCaseTable SET CalculationState=@state,CalculationProgress=@progress,CalculationMessage=@message WHERE ID=@id AND LastModificationDate=@expected";
        command.Parameters.AddWithValue("@id", value.MetaInfo.ID.ToString());
        command.Parameters.AddWithValue("@expected", expectedRevision.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        command.Parameters.AddWithValue("@state", value.CalculationState.ToString());
        command.Parameters.AddWithValue("@progress", value.CalculationProgress);
        command.Parameters.AddWithValue("@message", (object?)value.CalculationMessage ?? DBNull.Value);
        try { return command.ExecuteNonQuery() == 1; }
        catch (SqliteException ex)
        {
            logger_.LogError(ex, "Unable to update target landing calculation progress for {CaseId}", value.MetaInfo.ID);
            return false;
        }
    }

    private void RefreshStale(TargetLandingCase? value) => RefreshStaleLight(value);

    private void RefreshStaleLight(TargetLandingCase? value)
    {
        if (value == null || value.CalculationState != CalculationState.Completed) return;
        using SqliteConnection? connection = connectionManager_.GetConnection();
        if (connection == null) { value.IsStale = true; return; }
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT LastModificationDate FROM TrajectoryTable WHERE ID=@id";
        command.Parameters.AddWithValue("@id", value.SourceTrajectoryID.ToString());
        object? result = command.ExecuteScalar();
        DateTimeOffset? currentRevision = result is string text && DateTimeOffset.TryParse(text, out DateTimeOffset parsed) ? parsed : null;
        value.IsStale = currentRevision == null || string.IsNullOrWhiteSpace(value.CalculationFingerprint) ||
            !SameStoredRevision(value.SourceTrajectoryRevision, currentRevision);
    }

    internal static bool SameStoredRevision(DateTimeOffset? calculatedRevision, DateTimeOffset? currentRevision) =>
        calculatedRevision.HasValue && currentRevision.HasValue &&
        calculatedRevision.Value.ToUnixTimeSeconds() == currentRevision.Value.ToUnixTimeSeconds();

    private static TargetLandingCaseLight ToLight(TargetLandingCase value) => new()
    {
        MetaInfo = value.MetaInfo, Name = value.Name, Description = value.Description,
        CreationDate = value.CreationDate, LastModificationDate = value.LastModificationDate,
        SourceTrajectoryID = value.SourceTrajectoryID, TargetType = value.TargetType,
        CurveType = value.CurveType, AttitudeMode = value.AttitudeMode,
        CalculationState = value.CalculationState, CalculationProgress = value.CalculationProgress,
        CalculationMessage = value.CalculationMessage, IsStale = value.IsStale
    };

    private static DateTimeOffset? ReadDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) || !DateTimeOffset.TryParse(reader.GetString(ordinal), out DateTimeOffset value)
            ? null
            : value;

    private static DateTimeOffset? ReadJsonDate(SqliteDataReader reader, int ordinal) => ReadDate(reader, ordinal);

    private static double SquaredDistance(TargetPlanePoint point, TargetLandingSample sample)
    {
        double dx = point.X - sample.PlaneX;
        double dy = point.Y - sample.PlaneY;
        return dx * dx + dy * dy;
    }

    private static SurveyStation ToDisplayStation(SurveyStation source) => new()
    {
        MD = source.MD, X = source.X, Y = source.Y, Z = source.Z,
        RiemannianNorth = source.RiemannianNorth, RiemannianEast = source.RiemannianEast, TVD = source.TVD,
        Latitude = source.Latitude, Longitude = source.Longitude,
        Inclination = source.Inclination, Azimuth = source.Azimuth,
        Curvature = source.Curvature, Toolface = source.Toolface, BUR = source.BUR, TUR = source.TUR,
        VerticalSection = source.VerticalSection, Abscissa = source.Abscissa
    };

    private static void Mark(TargetLandingCase value, CalculationState state, double progress, string? message)
    {
        value.CalculationState = state;
        value.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
        value.CalculationMessage = message;
    }
}
