using System;
using System.Collections.Generic;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.GlobalAntiCollision;
namespace OSDC.Drilling.Trajectory.Model;
public sealed class SurveyStationEllipseCalculationLight
{
    public MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
    public double ConfidenceFactor { get; set; }
    public Guid? SurveyInstrumentID { get; set; }
    public string? CalculationMessage { get; set; }
}
public sealed class GlobalAntiCollisionLight
{
    public string ID { get; set; } = "";
    public Guid ReferenceTrajectoryID { get; set; }
    public Guid ReferenceWellPathID { get; set; }
    public double ConfidenceFactor { get; set; }
}
public sealed class OctreeLight { public Guid ID { get; set; } }
public sealed class SurveyStationEllipseCalculationDiscoveryPage
{
    public List<SurveyStationEllipseCalculationLight> Items { get; set; } = [];
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
public sealed class FieldAntiCollisionPolicyAssignmentDiscoveryPage
{
    public List<FieldAntiCollisionPolicyAssignment> Items { get; set; } = [];
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
public sealed class GlobalAntiCollisionsDiscoveryPage
{
    public List<GlobalAntiCollisionLight> Items { get; set; } = [];
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
public sealed class OctreesDiscoveryPage
{
    public List<OctreeLight> Items { get; set; } = [];
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
