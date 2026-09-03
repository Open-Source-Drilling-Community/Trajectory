using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>An identity definition shared by survey runs and trajectories.</summary>
public class TrajectoryIdentity : IIdentity
{
    public MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
}
