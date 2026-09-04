using System;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Describes whether the derived spatial index for a trajectory is usable and current.</summary>
public enum OctreeIndexState
{
    Missing,
    NotIndexable,
    Stale,
    Current
}

/// <summary>
/// Lightweight operational status for one trajectory's derived uncertainty-envelope octree.
/// The trajectory remains authoritative; this record only describes the rebuildable index.
/// </summary>
public sealed class OctreeIndexStatus
{
    public Guid TrajectoryID { get; set; }
    public OctreeIndexState State { get; set; }
    public bool HasIndex { get; set; }
    public bool IsCurrent { get; set; }
    public TrajectoryType TrajectoryType { get; set; }
    public bool IsDefinitive { get; set; }
    public int SurveyStationCount { get; set; }
    public int BucketCount { get; set; }
    public long OctreeCodeCount { get; set; }
    public DateTimeOffset? SourceLastModificationDate { get; set; }
    public int? IndexSchemaVersion { get; set; }
    public double? ConfidenceFactor { get; set; }
    public string? CalculationParametersHash { get; set; }
}
