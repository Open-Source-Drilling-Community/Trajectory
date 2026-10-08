using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.Service;

internal static class TrajectoryStationEvaluation
{
    internal static bool TryEvaluate(Model.Trajectory trajectory, double alongHoleDepth, out SurveyStation? station)
    {
        station = null;
        if(trajectory.CalculationState!=Model.CalculationState.Completed || !double.IsFinite(alongHoleDepth) || trajectory.SurveyStationList is not {Count: > 0} stations) return false;
        var depths = stations.Select(s => s.MD ?? s.Abscissa).ToArray();
        if(depths.Any(d => d is null || !double.IsFinite(d.Value)) ||
            depths.Zip(depths.Skip(1)).Any(p => p.First >= p.Second) ||
            alongHoleDepth < depths[0] || alongHoleDepth > depths[^1]) return false;
        return SurveyStation.InterpolateAtAbscissa(stations, alongHoleDepth, out station, trajectory.CalculationType) &&
            station is not null && (station.MD ?? station.Abscissa) is {} evaluated &&
            double.IsFinite(evaluated) && Math.Abs(evaluated - alongHoleDepth) <= 1e-8;
    }
}
