using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

public enum TargetLandingTargetType
{
    DrillerTarget = 0,
    GeologicalTarget = 1
}

public enum TargetLandingAttitudeMode
{
    Free = 0,
    PerpendicularToTargetPlane = 1
}

public enum TargetLandingSampleState
{
    Reachable = 0,
    OutsideUncertaintySafeTarget = 1,
    ExceedsMaximumLandingCurvature = 2,
    NoGeometricSolution = 3
}

/// <summary>One point in the target plane's canonical Cartesian coordinate system, in SI metres.</summary>
public class TargetPlanePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// An oriented target plane in canonical local NED coordinates. The normal is stored as drilling
/// inclination from WGS84 downward vertical and true-north azimuth, both in SI radians.
/// </summary>
public class TargetPlaneDefinition
{
    /// <summary>
    /// Plane origin and oriented forward normal. X/RiemannianNorth and Y/RiemannianEast are SI metres;
    /// Z/TVD is WGS84 depth in SI metres; Latitude and Longitude are WGS84 radians. Inclination is from
    /// downward WGS84 vertical and azimuth is positive east of true north.
    /// </summary>
    public CurvilinearPoint3D Plane { get; set; } = new();
    public List<TargetPlanePoint> Polygon { get; set; } = [];
}

public class TargetLandingCaseLight
{
    public MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
    public Guid SourceTrajectoryID { get; set; }
    public TargetLandingTargetType TargetType { get; set; }
    public ExtrapolationCurveType CurveType { get; set; }
    public TargetLandingAttitudeMode AttitudeMode { get; set; }
    public CalculationState CalculationState { get; set; } = CalculationState.NotCalculated;
    public double CalculationProgress { get; set; }
    public string? CalculationMessage { get; set; }
    public bool IsStale { get; set; }
}

public class TargetLandingCase : TargetLandingCaseLight
{
    public const double DefaultLeadLength = 30.0;
    public const double DefaultMaximumLandingCurvature = Math.PI / 1800.0;
    public const double DefaultConfidenceFactor = 0.95;

    public TargetPlaneDefinition Target { get; set; } = new();
    public double LeadLength { get; set; } = DefaultLeadLength;
    public double ConfidenceFactor { get; set; } = DefaultConfidenceFactor;

    /// <summary>
    /// Optional hard limit applied only to newly designed landing sections, in SI radians per metre.
    /// The default is 3 degrees per 30 metres.
    /// </summary>
    public double? MaximumLandingCurvature { get; set; } = DefaultMaximumLandingCurvature;

    public DateTimeOffset? SourceTrajectoryRevision { get; set; }
    public string? CalculationFingerprint { get; set; }
    /// <summary>The terminal survey station of the source trajectory used by the calculation.</summary>
    public SurveyStation? SourceEndStation { get; set; }
    public SurveyStation? SteeringStartStation { get; set; }
    public List<TargetPlanePoint>? GeologicalTargetBoundary { get; set; }
    public List<TargetPlanePoint>? DrillerTargetBoundary { get; set; }
    public List<TargetPlanePoint>? ReachableTargetBoundary { get; set; }
    public List<List<TargetPlanePoint>>? DrillerTargetContourList { get; set; }
    public List<List<TargetPlanePoint>>? ReachableTargetContourList { get; set; }
    public List<TargetLandingSample>? SampleList { get; set; }
    public List<TargetLandingMeshTriangle>? MeshTriangleList { get; set; }
}

public class TargetLandingSample
{
    public Guid SampleID { get; set; } = Guid.NewGuid();
    public double PlaneX { get; set; }
    public double PlaneY { get; set; }
    public double PolarRadius { get; set; }
    public double PolarAngle { get; set; }
    public double North { get; set; }
    public double East { get; set; }
    public double TVD { get; set; }
    public TargetLandingSampleState State { get; set; }
    public bool? IsUncertaintySafe { get; set; }
    public string? Message { get; set; }
    public double? TotalLandingLength { get; set; }
    public double? PeakLandingCurvature { get; set; }
    public SurveyStation? LandingStation { get; set; }
    public SurveyStationEllipse? LandingEllipseInTargetPlane { get; set; }
    public List<TrajectoryExtrapolationSolvedSection>? SolvedSectionList { get; set; }
    public List<SurveyStation>? SurveyStationList { get; set; }
}

public class TargetLandingMeshTriangle
{
    public Guid FirstSampleID { get; set; }
    public Guid SecondSampleID { get; set; }
    public Guid ThirdSampleID { get; set; }
}
