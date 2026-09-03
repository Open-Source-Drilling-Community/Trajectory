using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>An identity value assigned to a survey run or trajectory.</summary>
public class TrajectoryIdentityAssignment : IIdentityAssignment
{
    public Guid ID { get; set; }
    public Guid? IdentityID { get; set; }
    public string? Value { get; set; }
}
