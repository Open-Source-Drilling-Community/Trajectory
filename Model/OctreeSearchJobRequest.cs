using System;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Requests asynchronous overlap discovery from one current trajectory octree index.</summary>
public sealed class OctreeSearchJobRequest
{
    public Guid ReferenceTrajectoryID { get; set; }
    public bool IncludePlanned { get; set; } = true;
    public bool IncludeActual { get; set; } = true;
    public bool DefinitiveOnly { get; set; } = true;
}
