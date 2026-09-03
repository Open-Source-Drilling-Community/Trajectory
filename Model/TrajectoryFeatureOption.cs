using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Trajectory.Model;

public class TrajectoryFeatureOption : IFeatureOption
{
    public Guid ID { get; set; }
    public string? Name { get; set; }
}
