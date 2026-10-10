using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.Service;

internal static class TrajectoryStationEvaluation
{
    internal static bool TryEvaluateReferenced(Model.Trajectory trajectory, double depth, double originDepth,
        out Model.ReferencedTrajectoryStation? result)
    {
        result = null;
        if (!double.IsFinite(depth) || !double.IsFinite(originDepth) || trajectory.SurveyStationList is not { Count: > 1 } stations)
            return false;
        // Restrict this operator to a verified monotone path. A looping path requires
        // an explicitly selected intersection; never silently choose one or extrapolate.
        if (stations[0].Z is not double firstZ || !double.IsFinite(firstZ) || stations[^1].Z is not double lastZ || !double.IsFinite(lastZ)) return false;
        var direction = Math.Sign((stations[^1].Z ?? double.NaN) - (stations[0].Z ?? double.NaN));
        if (direction == 0 || stations.Any(s => s.Z is null || !double.IsFinite(s.Z.Value) ||
            s.Inclination is null || !double.IsFinite(s.Inclination.Value) || direction * Math.Cos(s.Inclination.Value) <= 1e-12)) return false;
        if (stations.Zip(stations.Skip(1)).Any(p => direction * (p.Second.Z!.Value - p.First.Z!.Value) <= 0)) return false;
        if (direction * (originDepth - stations[0].Z!.Value) < 0 || direction * (stations[^1].Z!.Value - originDepth) < 0) return false;
        double lower = stations[0].MD ?? stations[0].Abscissa ?? double.NaN;
        double upper = stations[^1].MD ?? stations[^1].Abscissa ?? double.NaN;
        double originMd = double.NaN;
        for (int iteration = 0; iteration < 80; iteration++)
        {
            double middle = lower + (upper - lower) / 2;
            if (!TryEvaluate(trajectory, middle, out var point) || point?.Z is not double z || !double.IsFinite(z)) return false;
            if (Math.Abs(z - originDepth) <= 1e-8) { originMd = middle; break; }
            if (direction * (z - originDepth) < 0) lower = middle; else upper = middle;
        }
        if (!double.IsFinite(originMd) || !TryEvaluate(trajectory, originMd + depth, out var evaluated)) return false;
        result = new() { AlongHoleDepth = depth, OriginWgs84Depth = originDepth, OriginNativeAlongHoleDepth = originMd, Station = evaluated! };
        return true;
    }

    internal static bool TryEvaluateVerticalEllipse(Model.Trajectory trajectory, double depth, double confidence,
        out Model.TrajectoryVerticalEllipseEvaluation? result)
    {
        result = null;
        if (!double.IsFinite(confidence) || confidence <= 0 || confidence > Model.SurveyStationEllipseCalculation.MaximumConfidenceFactor ||
            !TryEvaluate(trajectory, depth, out var station)) return false;
        var stations = trajectory.SurveyStationList!.Select(s => new SurveyStation(s)).ToList();
        if (!stations.Any(s => (s.MD ?? s.Abscissa) == depth)) stations.Add(station!);
        stations = stations.OrderBy(s => s.MD ?? s.Abscissa).ToList();
        var plane = Model.SurveyStationEllipseCalculation.ResolveVerticalSectionAzimuth(stations);
        var calculation = new Model.SurveyStationEllipseCalculation { ConfidenceFactor = confidence, SurveyStationList = stations };
        if (plane is null || !calculation.Calculate()) return false;
        var ellipse = calculation.SurveyStationEllipseResultList?.SingleOrDefault(s => s.MD == depth)?.VerticalEllipse;
        if (ellipse?.SemiMajorAxis is not double major || ellipse.SemiMinorAxis is not double minor || ellipse.OrientationAngle is not double angle ||
            !double.IsFinite(major) || !double.IsFinite(minor) || !double.IsFinite(angle) || major < minor || minor < 0) return false;
        result = new() { AlongHoleDepth = depth, ConfidenceFactor = confidence, VerticalSectionAzimuth = plane.Value,
            VerticalEllipse = new() { MajorAxis = 2 * major, MinorAxis = 2 * minor, OrientationAngle = angle } };
        return true;
    }

    internal static bool TryEvaluateHorizontalEllipse(Model.Trajectory trajectory, double depth, double confidence,
        out Model.TrajectoryHorizontalEllipseEvaluation? result)
    {
        result = null;
        if (!double.IsFinite(confidence) || confidence <= 0 || confidence > Model.SurveyStationEllipseCalculation.MaximumConfidenceFactor ||
            !TryEvaluate(trajectory, depth, out var station)) return false;
        var ellipsoid = new UncertaintyEllipsoid { EllipsoidSurveyStation = station, ConfidenceFactor = confidence, ScalingFactor = 1.0,
            CalculateHorizontalEllipse = true };
        if (!ellipsoid.CalculateHorizontalEllipseParameters()) return false;
        var ellipse = ellipsoid.HorizontalEllipse;
        if (ellipse?.EllipseRadii?.X is not double major || ellipse.EllipseRadii.Y is not double minor || ellipse.EllipseOrientationAngle is not double angle ||
            !double.IsFinite(major) || !double.IsFinite(minor) || !double.IsFinite(angle) || major < minor || minor < 0) return false;
        result = new() { AlongHoleDepth = depth, ConfidenceFactor = confidence,
            HorizontalEllipse = new() { MajorAxis = 2 * major, MinorAxis = 2 * minor, OrientationAngle = angle } };
        return true;
    }

    internal static bool TryEvaluate(Model.Trajectory trajectory, double alongHoleDepth, out SurveyStation? station)
    {
        station = null;
        if(trajectory.CalculationState!=Model.CalculationState.Completed || !double.IsFinite(alongHoleDepth) || trajectory.SurveyStationList is not {Count: > 0} stations) return false;
        var depths = stations.Select(s => s.MD ?? s.Abscissa).ToArray();
        if(depths.Any(d => d is null || !double.IsFinite(d.Value)) ||
            depths.Zip(depths.Skip(1)).Any(p => p.First >= p.Second) ||
            alongHoleDepth < depths[0] || alongHoleDepth > depths[^1]) return false;
        if(!SurveyStation.InterpolateAtAbscissa(stations, alongHoleDepth, out station, trajectory.CalculationType) ||
            station is null || (station.MD ?? station.Abscissa) is not {} evaluated ||
            !double.IsFinite(evaluated) || Math.Abs(evaluated - alongHoleDepth)>1e-8)return false;
        // Survey interpolation may populate canonical Riemannian X/Y without
        // updating its optional geographic cache. Expose the equivalent WGS84
        // coordinates using the authoritative shared conversion implementation.
        if(station.X is {} north && station.Y is {} east && double.IsFinite(north) && double.IsFinite(east))
            station.SetLatitudeLongitude(north,east);
        return true;
    }
}
