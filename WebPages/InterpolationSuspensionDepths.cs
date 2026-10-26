using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.WebPages;

// Casing-section tops are the architecture's suspension positions. The separate
// wellhead hanger marker remains supported when explicitly populated.
internal static class InterpolationSuspensionDepths
{
    internal static IReadOnlyList<double> Resolve(IEnumerable<double?>? sectionTops, double? wellheadHanger)
    {
        var depths = new List<double>();
        foreach (double value in (sectionTops ?? Enumerable.Empty<double?>()).Append(wellheadHanger)
            .Where(value => value.HasValue && double.IsFinite(value.Value)).Select(value => value!.Value).OrderBy(value => value))
        {
            if (depths.Count == 0 || Math.Abs(value - depths[^1]) >= 1e-6) depths.Add(value);
        }
        return depths;
    }
}
