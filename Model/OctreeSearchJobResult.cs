using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Completed candidate UUIDs from an asynchronous octree overlap search.</summary>
public sealed class OctreeSearchJobResult
{
    public Guid JobID { get; set; }
    public Guid ReferenceTrajectoryID { get; set; }
    public List<Guid> CandidateTrajectoryIDs { get; set; } = [];
}
