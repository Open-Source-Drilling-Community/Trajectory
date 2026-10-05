using OSDC.DotnetLibraries.General.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model;

internal static class DirectionalControlBundling
{
    public static List<DirectionalControlEvaluationBundle> CreateBundles(DirectionalControlEvaluationCase value)
    {
        List<DirectionalControlEvaluationSample> all = value.SampleList?.OrderBy(sample => sample.ActualMD).ToList() ?? [];
        List<DirectionalControlEvaluationSample> valid = all.Where(HasResidualPair).ToList();
        if (valid.Count == 0) return [];

        double? circularCenter = value.CurveType == ExtrapolationCurveType.ConstantBuildAndTurn
            ? null
            : Math.Atan2(valid.Sum(sample => Math.Sin(sample.ToolfaceResidual!.Value)),
                valid.Sum(sample => Math.Cos(sample.ToolfaceResidual!.Value)));
        (double firstScale, double secondScale) = RobustScales(valid, value.CurveType, circularCenter);
        List<List<DirectionalControlEvaluationSample>> blocks = SplitAtHardGaps(valid, value.MaximumInvalidGap);
        List<(int Start, int End, List<DirectionalControlEvaluationSample> Samples)> segments = [];
        foreach (List<DirectionalControlEvaluationSample> block in blocks)
        {
            foreach ((int start, int end) in Segment(block, value, firstScale, secondScale, circularCenter))
                segments.Add((start, end, block));
        }

        List<DirectionalControlEvaluationBundle> result = [];
        for (int index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            List<DirectionalControlEvaluationSample> samples = segment.Samples
                .Skip(segment.Start).Take(segment.End - segment.Start + 1).ToList();
            double startMd = samples[0].ActualMD;
            double endMd = samples[^1].ActualEndMD;
            List<DirectionalControlEvaluationSample> attempted = all
                .Where(sample => sample.ActualMD >= startMd - 1e-9 && sample.ActualMD < endMd - 1e-9)
                .ToList();
            result.Add(CreateBundle(index, startMd, endMd, samples, attempted, value.CurveType));
        }
        return result;
    }

    private static List<List<DirectionalControlEvaluationSample>> SplitAtHardGaps(
        List<DirectionalControlEvaluationSample> valid,
        double maximumGap)
    {
        List<List<DirectionalControlEvaluationSample>> result = [];
        List<DirectionalControlEvaluationSample> current = [];
        foreach (DirectionalControlEvaluationSample sample in valid)
        {
            if (current.Count > 0 && sample.ActualMD - current[^1].ActualEndMD > maximumGap)
            {
                result.Add(current);
                current = [];
            }
            current.Add(sample);
        }
        if (current.Count > 0) result.Add(current);
        return result;
    }

    private static List<(int Start, int End)> Segment(
        List<DirectionalControlEvaluationSample> samples,
        DirectionalControlEvaluationCase value,
        double firstScale,
        double secondScale,
        double? circularCenter)
    {
        int count = samples.Count;
        if (count < 2 * value.MinimumBundleSampleCount) return [(0, count - 1)];
        double[] first = samples.Select(sample => Pair(sample, value.CurveType, circularCenter).First / firstScale).ToArray();
        double[] second = samples.Select(sample => Pair(sample, value.CurveType, circularCenter).Second / secondScale).ToArray();
        PrefixMoments firstMoments = new(first);
        PrefixMoments secondMoments = new(second);
        double[] best = Enumerable.Repeat(double.PositiveInfinity, count + 1).ToArray();
        int[] previous = Enumerable.Repeat(-1, count + 1).ToArray();
        best[0] = -value.BundlingPenalty;
        for (int end = 1; end <= count; end++)
        {
            for (int start = 0; start < end; start++)
            {
                int sampleCount = end - start;
                if (sampleCount < value.MinimumBundleSampleCount) continue;
                double length = samples[end - 1].ActualEndMD - samples[start].ActualMD;
                if (length + 1e-9 < value.MinimumBundleLength && !(start == 0 && end == count)) continue;
                if (double.IsPositiveInfinity(best[start])) continue;
                double candidate = best[start] + value.BundlingPenalty +
                    firstMoments.SumSquaredDeviation(start, end) +
                    secondMoments.SumSquaredDeviation(start, end);
                if (candidate < best[end])
                {
                    best[end] = candidate;
                    previous[end] = start;
                }
            }
        }
        if (previous[count] < 0) return [(0, count - 1)];
        List<(int Start, int End)> result = [];
        for (int end = count; end > 0;)
        {
            int start = previous[end];
            result.Add((start, end - 1));
            end = start;
        }
        result.Reverse();
        return result;
    }

    private static DirectionalControlEvaluationBundle CreateBundle(
        int index,
        double startMd,
        double endMd,
        List<DirectionalControlEvaluationSample> valid,
        List<DirectionalControlEvaluationSample> attempted,
        ExtrapolationCurveType curveType)
    {
        DirectionalControlEvaluationBundle result = new()
        {
            BundleIndex = index,
            StartActualMD = startMd,
            EndActualMD = endMd,
            AttemptedSampleCount = attempted.Count,
            ValidSampleCount = valid.Count,
            InvalidSampleCount = attempted.Count - valid.Count,
            ValidCoverageRatio = attempted.Count == 0 ? 0.0 : (double)valid.Count / attempted.Count,
            MaximumInvalidGap = MaximumInvalidGap(attempted)
        };
        if (curveType == ExtrapolationCurveType.ConstantBuildAndTurn)
        {
            result.BuildRateResidual = Summarize(valid.Select(sample => sample.BuildRateResidual!.Value), false);
            result.TurnRateResidual = Summarize(valid.Select(sample => sample.TurnRateResidual!.Value), false);
        }
        else
        {
            result.CurvatureResidual = Summarize(valid.Select(sample => sample.CurvatureResidual!.Value), false);
            result.ToolfaceResidual = Summarize(valid.Select(sample => sample.ToolfaceResidual!.Value), true);
        }
        return result;
    }

    private static DirectionalControlDistributionSummary Summarize(IEnumerable<double> source, bool circular)
    {
        List<double> raw = source.Where(Numeric.IsDefined).ToList();
        double center = circular
            ? Math.Atan2(raw.Sum(Math.Sin), raw.Sum(Math.Cos))
            : 0.0;
        List<double> values = circular
            ? raw.Select(value => center + DirectionalControlEvaluationCalculator.WrapAngle(value - center)).Order().ToList()
            : raw.Order().ToList();
        double mean = circular ? center : values.Average();
        double standardDeviation = circular
            ? Math.Sqrt(values.Average(value => Math.Pow(value - center, 2.0)))
            : Math.Sqrt(values.Average(value => Math.Pow(value - mean, 2.0)));
        double median = Quantile(values, 0.5);
        List<double> absolute = values.Select(value => Math.Abs(value - median)).Order().ToList();
        DirectionalControlDistributionSummary result = new()
        {
            Count = values.Count,
            P10 = Normalize(Quantile(values, 0.1), circular),
            P50 = Normalize(median, circular),
            P90 = Normalize(Quantile(values, 0.9), circular),
            Mean = Normalize(mean, circular),
            StandardDeviation = standardDeviation,
            MedianAbsoluteDeviation = Quantile(absolute, 0.5),
            Minimum = Normalize(values[0], circular),
            Maximum = Normalize(values[^1], circular),
            IsCircular = circular
        };
        result.Histogram = Histogram(values, circular);
        return result;
    }

    private static List<DirectionalControlHistogramBin> Histogram(List<double> values, bool circular)
    {
        int binCount = Math.Clamp((int)Math.Ceiling(Math.Sqrt(values.Count)), 1, 20);
        double low = values[0];
        double high = values[^1];
        if (Math.Abs(high - low) < 1e-12)
        {
            double halfWidth = circular ? Math.PI / 180.0 : Math.Max(Math.Abs(low) * 0.05, 1e-9);
            low -= halfWidth;
            high += halfWidth;
        }
        double width = (high - low) / binCount;
        int[] counts = new int[binCount];
        foreach (double value in values)
        {
            int index = Math.Min((int)((value - low) / width), binCount - 1);
            counts[Math.Max(index, 0)]++;
        }
        return Enumerable.Range(0, binCount).Select(index => new DirectionalControlHistogramBin
        {
            // Keep circular bins on the continuous branch selected around the circular centre.
            // Normalizing each edge independently would make a bin crossing +/-pi appear near zero.
            LowerBound = low + index * width,
            UpperBound = low + (index + 1) * width,
            Count = counts[index]
        }).ToList();
    }

    private static (double First, double Second) RobustScales(
        List<DirectionalControlEvaluationSample> samples,
        ExtrapolationCurveType curveType,
        double? circularCenter)
    {
        List<double> first = samples.Select(sample => Pair(sample, curveType, circularCenter).First).Order().ToList();
        List<double> second = samples.Select(sample => Pair(sample, curveType, circularCenter).Second).Order().ToList();
        return (Scale(first), Scale(second));
    }

    private static double Scale(List<double> values)
    {
        double median = Quantile(values, 0.5);
        List<double> deviations = values.Select(value => Math.Abs(value - median)).Order().ToList();
        double mad = 1.4826 * Quantile(deviations, 0.5);
        if (mad > 1e-12) return mad;
        double rms = Math.Sqrt(values.Average(value => Math.Pow(value - values.Average(), 2.0)));
        return rms > 1e-12 ? rms : 1.0;
    }

    private static (double First, double Second) Pair(
        DirectionalControlEvaluationSample sample,
        ExtrapolationCurveType curveType,
        double? circularCenter)
    {
        if (curveType == ExtrapolationCurveType.ConstantBuildAndTurn)
            return (sample.BuildRateResidual!.Value, sample.TurnRateResidual!.Value);
        double toolface = sample.ToolfaceResidual!.Value;
        if (circularCenter is double center)
            toolface = center + DirectionalControlEvaluationCalculator.WrapAngle(toolface - center);
        return (sample.CurvatureResidual!.Value, toolface);
    }

    private static bool HasResidualPair(DirectionalControlEvaluationSample sample) => sample.IsValid &&
        ((IsDefined(sample.CurvatureResidual) && IsDefined(sample.ToolfaceResidual)) ||
         (IsDefined(sample.BuildRateResidual) && IsDefined(sample.TurnRateResidual)));

    private static double MaximumInvalidGap(List<DirectionalControlEvaluationSample> attempted)
    {
        double current = 0.0;
        double maximum = 0.0;
        foreach (DirectionalControlEvaluationSample sample in attempted)
        {
            if (sample.IsValid) current = 0.0;
            else
            {
                current += sample.ActualEndMD - sample.ActualMD;
                maximum = Math.Max(maximum, current);
            }
        }
        return maximum;
    }

    private static double Quantile(List<double> values, double probability)
    {
        if (values.Count == 1) return values[0];
        double position = probability * (values.Count - 1);
        int lower = (int)Math.Floor(position);
        int upper = (int)Math.Ceiling(position);
        return values[lower] + (position - lower) * (values[upper] - values[lower]);
    }

    private static double Normalize(double value, bool circular) =>
        circular ? DirectionalControlEvaluationCalculator.WrapAngle(value) : value;

    private static bool IsDefined(double? value) => value is double defined && Numeric.IsDefined(defined);

    private sealed class PrefixMoments
    {
        private readonly double[] sums_;
        private readonly double[] squares_;

        public PrefixMoments(double[] values)
        {
            sums_ = new double[values.Length + 1];
            squares_ = new double[values.Length + 1];
            for (int i = 0; i < values.Length; i++)
            {
                sums_[i + 1] = sums_[i] + values[i];
                squares_[i + 1] = squares_[i] + values[i] * values[i];
            }
        }

        public double SumSquaredDeviation(int start, int end)
        {
            int count = end - start;
            double sum = sums_[end] - sums_[start];
            double squares = squares_[end] - squares_[start];
            return Math.Max(0.0, squares - sum * sum / count);
        }
    }
}
