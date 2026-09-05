using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>
/// Runs transient octree overlap searches outside request threads. Jobs are intentionally
/// rebuildable and in-memory; callers can safely submit another search after a service restart.
/// </summary>
public sealed class OctreeSearchJobWorker : BackgroundService
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(1);
    private readonly Channel<Guid> queue_ = Channel.CreateBounded<Guid>(new BoundedChannelOptions(100)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly ConcurrentDictionary<Guid, SearchJob> jobs_ = new();
    private readonly ILogger<OctreeSearchJobWorker> logger_;
    private readonly TrajectoryManager trajectoryManager_;
    private readonly OctreeManager octreeManager_;

    public OctreeSearchJobWorker(
        ILogger<OctreeSearchJobWorker> logger,
        ILogger<TrajectoryManager> trajectoryLogger,
        SqlConnectionManager connectionManagerTrajectory,
        OctreeManager octreeManager)
    {
        logger_ = logger;
        trajectoryManager_ = TrajectoryManager.GetInstance(trajectoryLogger, connectionManagerTrajectory, octreeManager);
        octreeManager_ = octreeManager;
    }

    public OctreeSearchJobStatus? Queue(OctreeSearchJobRequest request)
    {
        RemoveExpiredJobs();
        Guid jobId = Guid.NewGuid();
        DateTimeOffset createdUtc = DateTimeOffset.UtcNow;
        var job = new SearchJob(request, new OctreeSearchJobStatus
        {
            JobID = jobId,
            ReferenceTrajectoryID = request.ReferenceTrajectoryID,
            CalculationState = CalculationState.Queued,
            CalculationProgress = 0.0,
            CalculationMessage = "Octree scan queued",
            CreatedUtc = createdUtc
        });
        if (!jobs_.TryAdd(jobId, job) || !queue_.Writer.TryWrite(jobId))
        {
            jobs_.TryRemove(jobId, out _);
            return null;
        }
        return CloneStatus(job);
    }

    public OctreeSearchJobStatus? GetStatus(Guid jobId) =>
        jobs_.TryGetValue(jobId, out SearchJob? job) ? CloneStatus(job) : null;

    public OctreeSearchJobResult? GetResult(Guid jobId)
    {
        if (!jobs_.TryGetValue(jobId, out SearchJob? job))
        {
            return null;
        }
        lock (job.SyncRoot)
        {
            if (job.Status.CalculationState != CalculationState.Completed || job.CandidateTrajectoryIDs == null)
            {
                return null;
            }
            return new OctreeSearchJobResult
            {
                JobID = jobId,
                ReferenceTrajectoryID = job.Request.ReferenceTrajectoryID,
                CandidateTrajectoryIDs = [.. job.CandidateTrajectoryIDs]
            };
        }
    }

    public bool Contains(Guid jobId) => jobs_.ContainsKey(jobId);

    public bool Delete(Guid jobId) => jobs_.TryRemove(jobId, out _);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid jobId in queue_.Reader.ReadAllAsync(stoppingToken))
        {
            Run(jobId, stoppingToken);
        }
    }

    private void Run(Guid jobId, CancellationToken cancellationToken)
    {
        if (!jobs_.TryGetValue(jobId, out SearchJob? job))
        {
            return;
        }

        try
        {
            Update(job, CalculationState.Running, 0.01, "Validating the reference trajectory");
            cancellationToken.ThrowIfCancellationRequested();
            Model.Trajectory? trajectory = trajectoryManager_.GetTrajectoryById(job.Request.ReferenceTrajectoryID);
            if (trajectory == null)
            {
                Fail(job, "The reference trajectory no longer exists");
                return;
            }

            OctreeIndexStatus indexStatus = octreeManager_.GetStatus(trajectory);
            if (!indexStatus.IsCurrent)
            {
                Fail(job, $"The reference trajectory octree index is {indexStatus.State.ToString().ToLowerInvariant()}");
                return;
            }

            List<Guid> candidates = octreeManager_.SearchByClassification(
                octreeManager_.Get(job.Request.ReferenceTrajectoryID),
                job.Request.IncludePlanned,
                job.Request.IncludeActual,
                job.Request.DefinitiveOnly,
                job.Request.ReferenceTrajectoryID,
                (progress, message) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Update(job, CalculationState.Running, progress, message);
                });

            lock (job.SyncRoot)
            {
                job.CandidateTrajectoryIDs = candidates;
                UpdateCore(job, CalculationState.Completed, 1.0,
                    $"Found {candidates.Count:N0} overlapping trajectories", candidates.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // These jobs are derived and transient. The client can safely resubmit after restart.
            Fail(job, "The octree scan was interrupted by service shutdown");
        }
        catch (Exception ex)
        {
            logger_.LogError(ex, "Octree search job {JobId} failed", jobId);
            Fail(job, "The octree scan failed");
        }
    }

    private static void Update(SearchJob job, CalculationState state, double progress, string message,
        int? candidateCount = null)
    {
        lock (job.SyncRoot)
        {
            UpdateCore(job, state, progress, message, candidateCount);
        }
    }

    private static void UpdateCore(SearchJob job, CalculationState state, double progress, string message,
        int? candidateCount)
    {
        job.Status.CalculationState = state;
        job.Status.CalculationProgress = Math.Clamp(progress, 0.0, 1.0);
        job.Status.CalculationMessage = message;
        job.Status.CandidateCount = candidateCount;
        if (state is CalculationState.Completed or CalculationState.Failed)
        {
            job.Status.CompletedUtc = DateTimeOffset.UtcNow;
        }
    }

    private static void Fail(SearchJob job, string message) =>
        Update(job, CalculationState.Failed, job.Status.CalculationProgress, message);

    private void RemoveExpiredJobs()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - Retention;
        foreach ((Guid id, SearchJob job) in jobs_)
        {
            if (job.Status.CompletedUtc < cutoff)
            {
                jobs_.TryRemove(id, out _);
            }
        }
    }

    private static OctreeSearchJobStatus CloneStatus(SearchJob job)
    {
        lock (job.SyncRoot)
        {
            OctreeSearchJobStatus value = job.Status;
            return new OctreeSearchJobStatus
            {
                JobID = value.JobID,
                ReferenceTrajectoryID = value.ReferenceTrajectoryID,
                CalculationState = value.CalculationState,
                CalculationProgress = value.CalculationProgress,
                CalculationMessage = value.CalculationMessage,
                CandidateCount = value.CandidateCount,
                CreatedUtc = value.CreatedUtc,
                CompletedUtc = value.CompletedUtc
            };
        }
    }

    private sealed class SearchJob(OctreeSearchJobRequest request, OctreeSearchJobStatus status)
    {
        public object SyncRoot { get; } = new();
        public OctreeSearchJobRequest Request { get; } = request;
        public OctreeSearchJobStatus Status { get; } = status;
        public List<Guid>? CandidateTrajectoryIDs { get; set; }
    }
}
