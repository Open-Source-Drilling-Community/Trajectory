using OSDC.Drilling.Trajectory.ModelShared;
using OSDC.Drilling.Trajectory.WebPages;

namespace OSDC.Drilling.Trajectory.WebApp;

public sealed class TrajectoryReferenceDataCache(
    ITrajectoryAPIUtils api,
    ILogger<TrajectoryReferenceDataCache> logger) : BackgroundService, ITrajectoryReferenceDataCache
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
    private readonly SemaphoreSlim refreshLock_ = new(1, 1);
    private TrajectoryReferenceDataSnapshot snapshot_ = TrajectoryReferenceDataSnapshot.Empty;

    public TrajectoryReferenceDataSnapshot Current => Volatile.Read(ref snapshot_);

    public async Task<TrajectoryReferenceDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (Current.RefreshedAtUtc == null)
        {
            await refreshLock_.WaitAsync(cancellationToken);
            try
            {
                // The startup worker may have completed while this caller was waiting.
                if (Current.RefreshedAtUtc == null)
                {
                    await RefreshCoreAsync(cancellationToken);
                }
            }
            finally
            {
                refreshLock_.Release();
            }
        }

        return Current;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await refreshLock_.WaitAsync(cancellationToken);
        try
        {
            await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            refreshLock_.Release();
        }
    }

    public async Task RefreshTrajectoriesAsync(CancellationToken cancellationToken = default)
    {
        await refreshLock_.WaitAsync(cancellationToken);
        try
        {
            IReadOnlyList<TrajectoryLight> trajectories = await LoadTrajectoriesAsync(cancellationToken);
            Volatile.Write(ref snapshot_, Current with
            {
                Trajectories = trajectories
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to refresh shared trajectories; retaining the last successful trajectory snapshot");
        }
        finally
        {
            refreshLock_.Release();
        }
    }

    public async Task RefreshSurveyRunsAsync(CancellationToken cancellationToken = default)
    {
        await refreshLock_.WaitAsync(cancellationToken);
        try
        {
            IReadOnlyList<SurveyRunLight> surveyRuns = await LoadSurveyRunsAsync(cancellationToken);
            Volatile.Write(ref snapshot_, Current with
            {
                SurveyRuns = surveyRuns
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to refresh shared survey runs; retaining the last successful survey-run snapshot");
        }
        finally
        {
            refreshLock_.Release();
        }
    }

    private async Task RefreshCoreAsync(CancellationToken cancellationToken)
    {
        var fieldTask = api.ClientField.GetAllFieldAsync(cancellationToken);
        var clusterTask = api.ClientCluster.GetAllClusterAsync(cancellationToken);
        var wellTask = api.ClientWell.GetAllWellAsync(cancellationToken);
        var wellBoreTask = api.ClientWellBore.GetAllWellBoreAsync(cancellationToken);
        var rigTask = api.GetAllRigReferencesAsync(cancellationToken);
        var surveyInstrumentTask = api.ClientSurveyInstrument.GetAllSurveyInstrumentLightAsync(cancellationToken);
        var wellBoreArchitectureTask = api.ClientWellBoreArchitecture.GetAllWellBoreArchitectureLightAsync(cancellationToken);
        Task<IReadOnlyList<TrajectoryLight>> trajectoryTask = LoadTrajectoriesAsync(cancellationToken);
        Task<IReadOnlyList<SurveyRunLight>> surveyRunTask = LoadSurveyRunsAsync(cancellationToken);

        await Task.WhenAll(
            fieldTask,
            clusterTask,
            wellTask,
            wellBoreTask,
            rigTask,
            surveyInstrumentTask,
            wellBoreArchitectureTask,
            trajectoryTask,
            surveyRunTask);

        TrajectoryReferenceDataSnapshot refreshed = new(
            fieldTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            clusterTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            wellTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            wellBoreTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            rigTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            surveyInstrumentTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            wellBoreArchitectureTask.Result.Where(item => item?.MetaInfo != null).OrderBy(item => item.Name).ToArray(),
            trajectoryTask.Result,
            surveyRunTask.Result,
            DateTimeOffset.UtcNow);

        Volatile.Write(ref snapshot_, refreshed);
        logger.LogDebug(
            "Refreshed shared reference data: {FieldCount} fields, {ClusterCount} clusters, {WellCount} wells, {WellBoreCount} wellbores, {RigCount} rigs, {SurveyInstrumentCount} survey instruments, {ArchitectureCount} wellbore architectures, {TrajectoryCount} trajectories, and {SurveyRunCount} survey runs",
            refreshed.Fields.Count,
            refreshed.Clusters.Count,
            refreshed.Wells.Count,
            refreshed.WellBores.Count,
            refreshed.Rigs.Count,
            refreshed.SurveyInstruments.Count,
            refreshed.WellBoreArchitectures.Count,
            refreshed.Trajectories.Count,
            refreshed.SurveyRuns.Count);
    }

    private async Task<IReadOnlyList<TrajectoryLight>> LoadTrajectoriesAsync(CancellationToken cancellationToken) =>
        (await api.ClientTrajectory.GetAllTrajectoryLightAsync(cancellationToken: cancellationToken))
            .Where(item => item?.MetaInfo != null)
            .OrderBy(item => item.Name)
            .ToArray();

    private async Task<IReadOnlyList<SurveyRunLight>> LoadSurveyRunsAsync(CancellationToken cancellationToken) =>
        (await api.ClientTrajectory.GetAllSurveyRunLightAsync(cancellationToken: cancellationToken))
            .Where(item => item?.MetaInfo != null)
            .OrderBy(item => item.Name)
            .ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryRefreshAsync(stoppingToken);

        using PeriodicTimer timer = new(RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TryRefreshAsync(stoppingToken);
        }
    }

    private async Task TryRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to refresh shared reference data; retaining the last successful snapshot");
        }
    }
}
