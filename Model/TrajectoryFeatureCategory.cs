using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>A feature category shared by survey runs and trajectories.</summary>
public class TrajectoryFeatureCategory : IFeatureCategory
{
    public MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public bool IsExclusive { get; set; }
    public bool HasValidityPeriod { get; set; }
    public List<TrajectoryFeatureOption>? Options { get; set; }
    List<IFeatureOption>? IFeatureCategory.Options
    {
        get => Options?.Cast<IFeatureOption>().ToList();
        set => Options = value?.Select(option => option is TrajectoryFeatureOption typed
            ? typed
            : new TrajectoryFeatureOption { ID = option.ID, Name = option.Name }).ToList();
    }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
}
