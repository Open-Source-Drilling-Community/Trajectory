using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.Model;

[Semantic(Concepts.SurveyStation)]
public sealed class ReferencedTrajectoryStation
{
    [Semantic(Concepts.AlongHoleDepth)]
    public double AlongHoleDepth { get; set; }
    [Semantic(Concepts.EllipsoidalDepth, Role = Concepts.SourceReference, Reference = Concepts.Wgs84)]
    public double OriginWgs84Depth { get; set; }
    [Semantic(Concepts.AlongHoleDepth, Reference = Concepts.Wgs84AlongHoleOrigin)]
    public double OriginNativeAlongHoleDepth { get; set; }
    public SurveyStation Station { get; set; } = new();
}
