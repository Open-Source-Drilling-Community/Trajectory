using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>A deterministic bounded page of matching trajectories.</summary>
public sealed class TrajectorySearchResult
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public List<TrajectoryLight> Items { get; set; } = [];
}
