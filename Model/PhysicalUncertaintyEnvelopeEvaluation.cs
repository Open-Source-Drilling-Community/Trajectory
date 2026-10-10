using System.Text.Json.Serialization;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Trajectory.Model;

[JsonConverter(typeof(JsonStringEnumConverter<UncertaintyProjectionPlane>))]
public enum UncertaintyProjectionPlane { Horizontal, Vertical, Perpendicular }

public sealed class PhysicalUncertaintyEnvelopeRequest
{
    [Semantic(Concepts.UncertaintyProjectionPlane)]
    public UncertaintyProjectionPlane Projection { get; set; }
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.SemiMajorAxis)]
    public double UncertaintySemiMajorAxis { get; set; }
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.SemiMinorAxis)]
    public double UncertaintySemiMinorAxis { get; set; }
    [Semantic(Concepts.UncertaintyEllipseOrientation)]
    public double UncertaintyOrientationAngle { get; set; }
    [Semantic(Concepts.WellboreInclination, Reference = Concepts.Wgs84DownwardNormal)]
    public double Inclination { get; set; }
    [Semantic(Concepts.WellboreAzimuth, Reference = Concepts.TrueNorthClockwise)]
    public double Azimuth { get; set; }
    [Semantic(Concepts.VerticalSectionAzimuth, Reference = Concepts.TrueNorthClockwise)]
    public double? VerticalSectionAzimuth { get; set; }
    [Semantic(Concepts.OutermostKnownPhysicalEnvelopeDiameter)]
    public double OutermostKnownPhysicalEnvelopeDiameter { get; set; }
}

[Semantic(Concepts.CircularlyDilatedUncertaintyEnvelope)]
public sealed class PhysicalUncertaintyEnvelopeEvaluation
{
    [Semantic(Concepts.UncertaintyProjectionPlane)]
    public UncertaintyProjectionPlane Projection { get; set; }
    [Semantic(Concepts.ProjectedBoreholeCrossSection)]
    public PlanarEllipse ProjectedBoreholeCrossSection { get; set; } = new();
    [Semantic(Concepts.CircularlyDilatedUncertaintyEnvelope)]
    public PlanarEllipse CombinedEnvelope { get; set; } = new();
    [Semantic(Concepts.OutermostKnownPhysicalEnvelopeDiameter)]
    public double AppliedOutermostKnownPhysicalEnvelopeDiameter { get; set; }
    public string ApproximationConvention { get; set; } = Concepts.MinimumDeterminantEllipsoidalOuterBoundConvention;
}

public sealed class PlanarEllipse
{
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.SemiMajorAxis)]
    public double SemiMajorAxis { get; set; }
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.SemiMinorAxis)]
    public double SemiMinorAxis { get; set; }
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.MajorAxis)]
    public double MajorAxis => 2 * SemiMajorAxis;
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.MinorAxis)]
    public double MinorAxis => 2 * SemiMinorAxis;
    [Semantic(Concepts.UncertaintyEllipseOrientation)]
    public double OrientationAngle { get; set; }
}
