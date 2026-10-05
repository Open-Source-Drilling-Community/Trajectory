using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.Model;

internal sealed record ReconnectTrajectorySolution(
    double ClosestReferenceMD,
    double TargetReferenceMD,
    SurveyStation TargetStation,
    ArcSection Pair,
    TrajectoryExtrapolationSolvedSection UpstreamSection);
