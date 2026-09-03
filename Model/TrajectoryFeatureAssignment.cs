using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>A feature option assigned to a survey run or trajectory.</summary>
public class TrajectoryFeatureAssignment : IFeatureAssignment
{
    public Guid ID { get; set; }
    public Guid? FeatureCategoryID { get; set; }
    public Guid? FeatureOptionID { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
