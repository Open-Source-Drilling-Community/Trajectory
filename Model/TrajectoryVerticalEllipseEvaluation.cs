using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Trajectory.Model;

[Semantic(Concepts.SurveyStationUncertaintyEllipse)]
public sealed class TrajectoryVerticalEllipseEvaluation
{
    [Semantic(Concepts.AlongHoleDepth, Reference = Concepts.Wgs84AlongHoleOrigin)]
    public double AlongHoleDepth { get; set; }
    [Semantic(Concepts.ConfidenceFactor)]
    public double ConfidenceFactor { get; set; }
    [Semantic(Concepts.VerticalSectionAzimuth, Reference = Concepts.TrueNorthClockwise)]
    public double VerticalSectionAzimuth { get; set; }
    [Semantic(Concepts.VerticalUncertaintyEllipse)]
    public VerticalSurveyUncertaintyEllipse VerticalEllipse { get; set; } = new();
}

[Semantic(Concepts.VerticalUncertaintyEllipse)]
public sealed class VerticalSurveyUncertaintyEllipse
{
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.MajorAxis)]
    public double MajorAxis { get; set; }
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.MinorAxis)]
    public double MinorAxis { get; set; }
    [Semantic(Concepts.UncertaintyEllipseOrientation, Reference = Concepts.VerticalEllipseAxisConvention)]
    public double OrientationAngle { get; set; }
}

[Semantic(Concepts.SurveyStationUncertaintyEllipse)]
public sealed class TrajectoryHorizontalEllipseEvaluation
{
    [Semantic(Concepts.AlongHoleDepth, Reference = Concepts.Wgs84AlongHoleOrigin)]
    public double AlongHoleDepth { get; set; }
    [Semantic(Concepts.ConfidenceFactor)]
    public double ConfidenceFactor { get; set; }
    [Semantic(Concepts.HorizontalUncertaintyEllipse)]
    public HorizontalSurveyUncertaintyEllipse HorizontalEllipse { get; set; } = new();
}

[Semantic(Concepts.HorizontalUncertaintyEllipse)]
public sealed class HorizontalSurveyUncertaintyEllipse
{
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.MajorAxis)]
    public double MajorAxis { get; set; }
    [Semantic(Concepts.PhysicalLengthExtent, Role = Concepts.MinorAxis)]
    public double MinorAxis { get; set; }
    [Semantic(Concepts.UncertaintyEllipseOrientation, Reference = Concepts.TrueNorthClockwise)]
    public double OrientationAngle { get; set; }
}
