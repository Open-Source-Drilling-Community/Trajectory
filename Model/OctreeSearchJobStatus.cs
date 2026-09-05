using System;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Lightweight polling status for an asynchronous octree overlap search.</summary>
public sealed class OctreeSearchJobStatus
{
    public Guid JobID { get; set; }
    public Guid ReferenceTrajectoryID { get; set; }
    public CalculationState CalculationState { get; set; } = CalculationState.Queued;
    public double CalculationProgress { get; set; }
    public string? CalculationMessage { get; set; }
    public int? CandidateCount { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
}
