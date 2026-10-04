using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.WebPages;

/// <summary>
/// Loads reference lists on demand for hosts that do not provide a shared cache.
/// </summary>
public sealed class DirectTrajectoryReferenceDataProvider(
    ITrajectoryAPIUtils api,
    ILogger<DirectTrajectoryReferenceDataProvider> logger) : ITrajectoryReferenceDataCache
{
    private readonly SemaphoreSlim refreshLock_ = new(1, 1);
    private TrajectoryReferenceDataSnapshot snapshot_ = TrajectoryReferenceDataSnapshot.Empty;

    public TrajectoryReferenceDataSnapshot Current => Volatile.Read(ref snapshot_);

    public async Task<TrajectoryReferenceDataSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        // This is deliberately an on-demand provider rather than a cache. A host that
        // wants snapshots shared across pages can replace it with its own implementation.
        await RefreshAsync(cancellationToken);
        return Current;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await refreshLock_.WaitAsync(cancellationToken);
        try
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
            Volatile.Write(ref snapshot_, Current with { Trajectories = trajectories });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to reload trajectories directly; retaining the last successfully loaded list");
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
            Volatile.Write(ref snapshot_, Current with { SurveyRuns = surveyRuns });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to reload survey runs directly; retaining the last successfully loaded list");
        }
        finally
        {
            refreshLock_.Release();
        }
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
}
