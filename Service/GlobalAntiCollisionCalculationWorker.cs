using System.Threading.Channels;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>
/// Executes durable global anti-collision requests outside the initiating HTTP request.
/// Progress and partial results are persisted so clients can poll the normal GET endpoint.
/// </summary>
public sealed class GlobalAntiCollisionCalculationWorker : BackgroundService
{
    private readonly Channel<string> queue_ = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly ConcurrentDictionary<string, GlobalAntiCollisionCalculationStatus> statuses_ = new();
    private readonly ConcurrentDictionary<string, byte> removed_ = new();
    private readonly ILogger<GlobalAntiCollisionCalculationWorker> logger_;
    private readonly ILogger<GlobalAntiCollisionManager> globalLogger_;
    private readonly ILogger<TrajectoryManager> trajectoryLogger_;
    private readonly GlobalAntiCollisionManager globalManager_;
    private readonly TrajectoryManager trajectoryManager_;
    private readonly OctreeManager octreeManager_;

    public GlobalAntiCollisionCalculationWorker(
        ILogger<GlobalAntiCollisionCalculationWorker> logger,
        ILogger<GlobalAntiCollisionManager> globalLogger,
        ILogger<TrajectoryManager> trajectoryLogger,
        SqlConnectionManager connectionManagerTrajectory,
        SqlConnectionManagerSeparationFactorResults connectionManagerGlobalAC,
        OctreeManager octreeManager)
    {
        logger_ = logger;
        globalLogger_ = globalLogger;
        trajectoryLogger_ = trajectoryLogger;
        globalManager_ = GlobalAntiCollisionManager.GetInstance(globalLogger_, connectionManagerGlobalAC);
        trajectoryManager_ = TrajectoryManager.GetInstance(trajectoryLogger_, connectionManagerTrajectory, octreeManager);
        octreeManager_ = octreeManager;
    }

    public bool Queue(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }
        removed_.TryRemove(id, out _);
        statuses_[id] = new GlobalAntiCollisionCalculationStatus
        {
            ID = id,
            CalculationState = GlobalAntiCollisionCalculationState.Queued,
            CalculationProgress = 0.0,
            CalculationMessage = "Calculation queued"
        };
        if (queue_.Writer.TryWrite(id))
        {
            return true;
        }
        statuses_.TryRemove(id, out _);
        return false;
    }

    public GlobalAntiCollisionCalculationStatus? GetStatus(string id)
    {
        if (statuses_.TryGetValue(id, out GlobalAntiCollisionCalculationStatus? status))
        {
            return status;
        }
        GlobalAntiCollision.GlobalAntiCollision? calculation = globalManager_.Get(id);
        return calculation == null ? null : ToStatus(calculation);
    }

    public void Forget(string id)
    {
        removed_[id] = 0;
        statuses_.TryRemove(id, out _);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Resume calculations interrupted by a service restart. Legacy records default to Completed.
        List<string> interruptedIds = [];
        foreach (string id in globalManager_.GetIDs())
        {
            GlobalAntiCollision.GlobalAntiCollision? calculation = globalManager_.Get(id);
            if (calculation?.CalculationState is GlobalAntiCollisionCalculationState.Queued or GlobalAntiCollisionCalculationState.Running)
            {
                interruptedIds.Add(id);
            }
        }

        foreach (string id in interruptedIds)
        {
            await CalculateAsync(id, stoppingToken);
        }

        await foreach (string id in queue_.Reader.ReadAllAsync(stoppingToken))
        {
            await CalculateAsync(id, stoppingToken);
        }
    }

    private async Task CalculateAsync(string id, CancellationToken cancellationToken)
    {
        GlobalAntiCollision.GlobalAntiCollision? value = globalManager_.Get(id);
        if (value == null)
        {
            return;
        }

        try
        {
            UpdateState(value, GlobalAntiCollisionCalculationState.Running, 0.02, "Preparing reference trajectory", true);
            Model.Trajectory? referenceTrajectory = PrepareCalculationInput(value, out List<SurveyStation>? referenceSurveyList);
            if (referenceSurveyList is not { Count: > 1 })
            {
                UpdateState(value, GlobalAntiCollisionCalculationState.Failed, 0.0,
                    "The reference trajectory has no calculated survey stations", true);
                return;
            }

            List<Model.Trajectory> comparisonTrajectories = GetComparisonTrajectories(value.ComparisonTrajectoryIDs);
            if (comparisonTrajectories.Count == 0)
            {
                value.SeparationFactorResults = [];
                UpdateState(value, GlobalAntiCollisionCalculationState.Completed, 1.0,
                    "No valid overlapping comparison trajectory was found", true);
                return;
            }

            UpdateState(value, GlobalAntiCollisionCalculationState.Running, 0.06, "Preparing borehole radii");
            await trajectoryManager_.EnsureBoreholeRadiiAsync(referenceTrajectory);
            for (int i = 0; i < comparisonTrajectories.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await trajectoryManager_.EnsureBoreholeRadiiAsync(comparisonTrajectories[i]);
                UpdateState(value, GlobalAntiCollisionCalculationState.Running,
                    0.06 + 0.09 * (i + 1) / comparisonTrajectories.Count,
                    $"Prepared {i + 1:N0}/{comparisonTrajectories.Count:N0} comparison trajectories");
            }

            List<List<SurveyStation>> comparisonSurveyLists = comparisonTrajectories
                .Select(trajectory => trajectory.SurveyStationList!)
                .ToList();

            if (Numeric.IsUndefined(value.ConfidenceFactor) || value.ConfidenceFactor <= 0 || value.ConfidenceFactor > 0.999)
            {
                value.ConfidenceFactor = 0.999;
            }

            List<MeasuredDepthRange?> referenceMdRanges = [];
            List<MeasuredDepthRange?> comparisonMdRanges = [];
            BuildRelevantMdRanges(referenceSurveyList, comparisonSurveyLists, value.ConfidenceFactor,
                referenceMdRanges, comparisonMdRanges);

            UpdateState(value, GlobalAntiCollisionCalculationState.Running, 0.18, "Resolving sidetrack constraints");
            List<AntiCollisionPairMdConstraints> pairMdConstraints =
                await SidetrackRelationshipResolver.GetAntiCollisionPairMdConstraintsAsync(
                    referenceTrajectory, comparisonTrajectories, globalLogger_);

            cancellationToken.ThrowIfCancellationRequested();
            value.Calculate(
                referenceSurveyList,
                comparisonSurveyLists,
                referenceMdRanges,
                comparisonMdRanges,
                pairMdConstraints.Select(constraints => constraints.ReferenceMinimumMD).ToList(),
                pairMdConstraints.Select(constraints => constraints.ComparisonMinimumMD).ToList(),
                (completed, total) => UpdateState(value, GlobalAntiCollisionCalculationState.Running,
                    0.2 + 0.78 * completed / total,
                    $"Calculated {completed:N0}/{total:N0} comparison trajectories"));

            UpdateState(value, GlobalAntiCollisionCalculationState.Completed, 1.0, null, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Leave the record queued so it is resumed after a normal service restart.
            UpdateState(value, GlobalAntiCollisionCalculationState.Queued, value.CalculationProgress,
                "Calculation interrupted; waiting to resume", true);
        }
        catch (Exception ex)
        {
            logger_.LogError(ex, "Unable to calculate global anti-collision request {Id}", id);
            UpdateState(value, GlobalAntiCollisionCalculationState.Failed, value.CalculationProgress,
                "The separation-factor calculation failed", true);
        }
    }

    private void UpdateState(GlobalAntiCollision.GlobalAntiCollision value,
        GlobalAntiCollisionCalculationState state, double progress, string? message, bool persist = false)
    {
        if (removed_.ContainsKey(value.ID))
        {
            return;
        }
        value.CalculationState = state;
        value.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
        value.CalculationMessage = message;
        statuses_[value.ID] = ToStatus(value);
        if (persist && !globalManager_.Update(value.ID, value))
        {
            logger_.LogWarning("Unable to persist progress for global anti-collision request {Id}", value.ID);
        }
    }

    private static GlobalAntiCollisionCalculationStatus ToStatus(GlobalAntiCollision.GlobalAntiCollision value) => new()
    {
        ID = value.ID,
        CalculationState = value.CalculationState,
        CalculationProgress = value.CalculationProgress,
        CalculationMessage = value.CalculationMessage
    };

    private Model.Trajectory? PrepareCalculationInput(
        GlobalAntiCollision.GlobalAntiCollision value,
        out List<SurveyStation>? referenceSurveyList)
    {
        Model.Trajectory? referenceTrajectory = null;
        referenceSurveyList = null;
        List<Guid>? requestedComparisonTrajectoryIds = value.ComparisonTrajectoryIDs is { Count: > 0 }
            ? [.. value.ComparisonTrajectoryIDs.Where(id => id != Guid.Empty)]
            : null;

        if (value.ReferenceWellPathID != Guid.Empty)
        {
            List<OctreeCodeLong>? leaves = referenceSurveyList != null
                ? octreeManager_.GetLeavesFromSurveyList(referenceSurveyList)
                : null;
            value.ComparisonTrajectoryIDs = FilterComparisonTrajectoryIds(
                octreeManager_.Search(leaves, Model.TrajectoryType.Actual, true, null),
                requestedComparisonTrajectoryIds);
            value.ReferenceTrajectoryID = Guid.Empty;
        }
        else if (value.ReferenceTrajectoryID != Guid.Empty)
        {
            referenceTrajectory = trajectoryManager_.GetTrajectoryById(value.ReferenceTrajectoryID);
            referenceSurveyList = referenceTrajectory?.SurveyStationList;
            List<Guid> candidateTrajectoryIds = requestedComparisonTrajectoryIds is { Count: > 0 }
                ? octreeManager_.SearchByClassification(octreeManager_.Get(value.ReferenceTrajectoryID), true, true, false,
                    value.ReferenceTrajectoryID)
                : octreeManager_.Search(octreeManager_.Get(value.ReferenceTrajectoryID), Model.TrajectoryType.Actual, true,
                    value.ReferenceTrajectoryID);
            value.ComparisonTrajectoryIDs = FilterComparisonTrajectoryIds(candidateTrajectoryIds,
                requestedComparisonTrajectoryIds);
            value.ReferenceWellPathID = Guid.Empty;
        }

        return referenceTrajectory;
    }

    private static List<Guid> FilterComparisonTrajectoryIds(List<Guid>? candidateTrajectoryIds,
        List<Guid>? requestedComparisonTrajectoryIds)
    {
        if (candidateTrajectoryIds == null || candidateTrajectoryIds.Count == 0)
        {
            return [];
        }
        if (requestedComparisonTrajectoryIds == null || requestedComparisonTrajectoryIds.Count == 0)
        {
            return candidateTrajectoryIds;
        }
        HashSet<Guid> requestedIds = [.. requestedComparisonTrajectoryIds];
        return candidateTrajectoryIds.Where(requestedIds.Contains).ToList();
    }

    private List<Model.Trajectory> GetComparisonTrajectories(List<Guid>? comparisonTrajectoryIds)
    {
        if (comparisonTrajectoryIds == null || comparisonTrajectoryIds.Count == 0)
        {
            return [];
        }

        List<Model.Trajectory>? values = trajectoryManager_.GetListOfTrajectoryById(comparisonTrajectoryIds);
        Dictionary<Guid, Model.Trajectory> byId = values?
            .Where(trajectory => trajectory?.MetaInfo?.ID is Guid && trajectory.SurveyStationList is { Count: > 1 })
            .ToDictionary(trajectory => trajectory.MetaInfo!.ID) ?? [];
        List<Model.Trajectory> filtered = [];
        List<Guid> filteredIds = [];
        foreach (Guid id in comparisonTrajectoryIds)
        {
            if (byId.TryGetValue(id, out Model.Trajectory? trajectory))
            {
                filtered.Add(trajectory);
                filteredIds.Add(id);
            }
        }
        comparisonTrajectoryIds.Clear();
        comparisonTrajectoryIds.AddRange(filteredIds);
        return filtered;
    }

    private static void BuildRelevantMdRanges(List<SurveyStation> referenceSurveyList,
        List<List<SurveyStation>> comparisonSurveyLists, double confidenceFactor,
        List<MeasuredDepthRange?> referenceMdRanges, List<MeasuredDepthRange?> comparisonMdRanges)
    {
        foreach (List<SurveyStation> comparisonSurveyList in comparisonSurveyLists)
        {
            if (RelevantMdRangeCalculator.TryGetRelevantMdRanges(referenceSurveyList, comparisonSurveyList,
                confidenceFactor, out MeasuredDepthRange? referenceRange, out MeasuredDepthRange? comparisonRange))
            {
                referenceMdRanges.Add(referenceRange);
                comparisonMdRanges.Add(comparisonRange);
            }
            else
            {
                referenceMdRanges.Add(null);
                comparisonMdRanges.Add(null);
            }
        }
    }
}
