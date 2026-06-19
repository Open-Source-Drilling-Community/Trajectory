using System;

namespace NORCE.Drilling.Trajectory.Model
{
    public class MinimumDistanceAdaptiveRefinementSettings
    {
        public bool Enabled { get; set; }
        public double? PolarDeviationTolerance { get; set; } = 0.5;
        public double? PolarAngularTolerance { get; set; } = Math.PI / 12.0;
        public double? MinimumMDStep { get; set; } = 1.0;
        public int MaximumDepth { get; set; } = 4;
        public int MaximumExtraSamplesPerComparison { get; set; } = 1000;

        public MinimumDistanceAdaptiveRefinementSettings()
        {
        }
    }
}
