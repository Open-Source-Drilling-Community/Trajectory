using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>
/// Reconciles the derived octree with durable trajectories after startup. This closes the small
/// cross-database crash window that cannot be covered by a single SQLite transaction.
/// </summary>
public sealed class OctreeReconciliationService(
    ILogger<OctreeReconciliationService> logger,
    ILogger<TrajectoryManager> trajectoryLogger,
    SqlConnectionManager mainDatabase,
    OctreeManager octreeManager) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Reconcile(stoppingToken), stoppingToken);
    }

    private void Reconcile(CancellationToken stoppingToken)
    {
        try
        {
            TrajectoryManager trajectoryManager = TrajectoryManager.GetInstance(
                trajectoryLogger, mainDatabase, octreeManager);
            List<Model.TrajectoryLight> trajectories = trajectoryManager.GetAllTrajectoryLight() ?? [];
            HashSet<Guid> durableIds = trajectories
                .Where(value => value.MetaInfo?.ID is Guid id && id != Guid.Empty)
                .Select(value => value.MetaInfo!.ID)
                .ToHashSet();

            int removed = 0;
            foreach (Guid cachedId in octreeManager.GetIDs().Where(id => !durableIds.Contains(id)))
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (octreeManager.Delete(cachedId)) removed++;
            }

            int rebuilt = 0;
            foreach (Guid trajectoryId in durableIds)
            {
                stoppingToken.ThrowIfCancellationRequested();
                Model.Trajectory? trajectory = trajectoryManager.GetTrajectoryById(trajectoryId);
                if (trajectory?.SurveyStationList is not { Count: >= 2 })
                {
                    if (octreeManager.Contains(trajectoryId) && octreeManager.Delete(trajectoryId)) removed++;
                    continue;
                }
                if (!octreeManager.IsCurrent(trajectory) && octreeManager.Rebuild(trajectory)) rebuilt++;
            }

            logger.LogInformation(
                "Octree reconciliation examined {TrajectoryCount} trajectories, rebuilt {RebuiltCount}, and removed {RemovedCount} stale or orphaned entries",
                durableIds.Count, rebuilt, removed);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Octree reconciliation was cancelled during service shutdown");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Octree reconciliation failed; trajectory APIs remain available and later writes will maintain their own cache entries");
        }
    }
}
