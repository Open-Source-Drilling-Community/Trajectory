using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Calculates sampling-independent curvature characteristics for solved curve sections.</summary>
internal static class CurveCurvatureCharacteristicsCalculator
{
    private const double ContinuityTolerance = 1e-10;

    internal static CurveCurvatureCharacteristics? Calculate(
        IReadOnlyList<TrajectoryExtrapolationSolvedSection> sections)
    {
        TrajectoryExtrapolationSolvedSection[] steering = sections
            .Where(section => section.Role is TrajectoryExtrapolationSectionRole.UpstreamSteeringSection or
                TrajectoryExtrapolationSectionRole.DownstreamSteeringSection)
            .OrderBy(section => section.SectionIndex).ToArray();
        if (steering.Length == 0) return null;

        var ranges = new List<(double Minimum, double Maximum)>();
        foreach (TrajectoryExtrapolationSolvedSection section in steering)
        {
            if (!TryRange(section, out double minimum, out double maximum)) return null;
            ranges.Add((minimum, maximum));
        }

        double? junction = null;
        if (steering.Length == 2 &&
            TryBoundaryCurvature(steering[0], atEnd: true, out double upstream) &&
            TryBoundaryCurvature(steering[1], atEnd: false, out double downstream))
        {
            double scale = Math.Max(1.0, Math.Max(upstream, downstream));
            if (Math.Abs(upstream - downstream) <= ContinuityTolerance * scale)
                junction = 0.5 * (upstream + downstream);
        }

        return new CurveCurvatureCharacteristics
        {
            MinimumCurvature = ranges.Min(value => value.Minimum),
            JunctionCurvature = junction,
            MaximumCurvature = ranges.Max(value => value.Maximum)
        };
    }

    private static bool TryRange(TrajectoryExtrapolationSolvedSection section,
        out double minimum, out double maximum)
    {
        minimum = maximum = double.NaN;
        switch (section.CurveType)
        {
            case ExtrapolationCurveType.CircularArc when Finite(section.CircularArcCurvature):
                minimum = maximum = Math.Abs(section.CircularArcCurvature!.Value);
                return true;
            case ExtrapolationCurveType.ConstantCurvatureAndToolface when Finite(section.ConstantCurvature):
                minimum = maximum = Math.Abs(section.ConstantCurvature!.Value);
                return true;
            case ExtrapolationCurveType.ConstantBuildAndTurn when
                Finite(section.ConstantBuildRate) && Finite(section.ConstantTurnRate) &&
                Finite(section.Start?.Inclination) && Finite(section.End?.Inclination):
                double build = section.ConstantBuildRate!.Value;
                double turn = section.ConstantTurnRate!.Value;
                double start = section.Start!.Inclination!.Value;
                double end = section.End!.Inclination!.Value;
                double minimumSin = Math.Min(Math.Abs(Math.Sin(start)), Math.Abs(Math.Sin(end)));
                double maximumSin = Math.Max(Math.Abs(Math.Sin(start)), Math.Abs(Math.Sin(end)));
                double lower = Math.Min(start, end);
                double upper = Math.Max(start, end);
                if (ContainsInclination(lower, upper, 0.0)) minimumSin = 0.0;
                if (ContainsInclination(lower, upper, Math.PI / 2.0)) maximumSin = 1.0;
                minimum = Curvature(build, turn, minimumSin);
                maximum = Curvature(build, turn, maximumSin);
                return Finite(minimum) && Finite(maximum);
            default:
                return false;
        }
    }

    private static bool TryBoundaryCurvature(TrajectoryExtrapolationSolvedSection section,
        bool atEnd, out double curvature)
    {
        curvature = double.NaN;
        switch (section.CurveType)
        {
            case ExtrapolationCurveType.CircularArc when Finite(section.CircularArcCurvature):
                curvature = Math.Abs(section.CircularArcCurvature!.Value);
                return true;
            case ExtrapolationCurveType.ConstantCurvatureAndToolface when Finite(section.ConstantCurvature):
                curvature = Math.Abs(section.ConstantCurvature!.Value);
                return true;
            case ExtrapolationCurveType.ConstantBuildAndTurn when
                Finite(section.ConstantBuildRate) && Finite(section.ConstantTurnRate):
                double? inclination = atEnd ? section.End?.Inclination : section.Start?.Inclination;
                if (!Finite(inclination)) return false;
                curvature = Curvature(section.ConstantBuildRate!.Value,
                    section.ConstantTurnRate!.Value, Math.Abs(Math.Sin(inclination!.Value)));
                return Finite(curvature);
            default:
                return false;
        }
    }

    private static bool ContainsInclination(double lower, double upper, double phase)
    {
        int first = (int)Math.Ceiling((lower - phase) / Math.PI);
        return phase + first * Math.PI <= upper + 1e-12;
    }

    private static double Curvature(double build, double turn, double absoluteSin) =>
        Math.Sqrt(build * build + turn * turn * absoluteSin * absoluteSin);

    private static bool Finite(double? value) => value.HasValue && double.IsFinite(value.Value);
}
