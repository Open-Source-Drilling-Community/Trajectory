using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

public class DirectionalControlEvaluationCaseLight
{
    public MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
    public Guid ReferenceTrajectoryID { get; set; }
    public Guid ActualTrajectoryID { get; set; }
    public ExtrapolationCurveType CurveType { get; set; }
    public CalculationState CalculationState { get; set; } = CalculationState.Queued;
    public double CalculationProgress { get; set; }
    public string? CalculationMessage { get; set; }
    public bool IsStale { get; set; }
}

public class DirectionalControlEvaluationCase : DirectionalControlEvaluationCaseLight
{
    public const double DefaultEvaluationInterval = 10.0;
    public const double DefaultReferenceMDAdvance = 60.0;
    public const double DefaultMaximumInvalidGap = 100.0;
    public const double DefaultMinimumBundleLength = 90.0;
    public const int DefaultMinimumBundleSampleCount = 5;
    public const double DefaultBundlingPenalty = 8.0;

    /// <summary>Evaluation spacing and forward response-window length in SI metres of actual MD.</summary>
    public double EvaluationInterval { get; set; } = DefaultEvaluationInterval;
    /// <summary>Optional inclusive lower evaluation bound in SI metres of actual MD.</summary>
    public double? StartActualMD { get; set; }
    /// <summary>Optional inclusive upper evaluation bound in SI metres of actual MD.</summary>
    public double? EndActualMD { get; set; }
    /// <summary>Correction length in SI metres, measured forward from the closest reference MD.</summary>
    public double ReferenceMDAdvance { get; set; } = DefaultReferenceMDAdvance;
    /// <summary>Junction-curvature ratio used by build-and-turn reconnect pairs.</summary>
    public double JunctionCurvatureRatio { get; set; } = 1.0;
    /// <summary>Invalid MD gaps larger than this SI-metre limit split bundles.</summary>
    public double MaximumInvalidGap { get; set; } = DefaultMaximumInvalidGap;
    /// <summary>Minimum actual-MD extent of a statistically independent bundle candidate, in SI metres.</summary>
    public double MinimumBundleLength { get; set; } = DefaultMinimumBundleLength;
    public int MinimumBundleSampleCount { get; set; } = DefaultMinimumBundleSampleCount;
    /// <summary>Experimental dimensionless change-point penalty; larger values produce fewer bundles.</summary>
    public double BundlingPenalty { get; set; } = DefaultBundlingPenalty;

    public DateTimeOffset? ReferenceTrajectoryRevision { get; set; }
    public DateTimeOffset? ActualTrajectoryRevision { get; set; }
    public string? CalculationFingerprint { get; set; }
    public List<DirectionalControlEvaluationSample>? SampleList { get; set; }
    public List<DirectionalControlEvaluationBundle>? BundleList { get; set; }
}

public class DirectionalControlEvaluationSample
{
    public Guid SampleID { get; set; } = Guid.NewGuid();
    public double ActualMD { get; set; }
    public double ActualEndMD { get; set; }
    public double? ClosestReferenceMD { get; set; }
    public double? TargetReferenceMD { get; set; }
    public bool IsValid { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public double? ExpectedCurvature { get; set; }
    public double? ActualCurvature { get; set; }
    public double? CurvatureResidual { get; set; }
    public double? ExpectedToolface { get; set; }
    public double? ActualToolface { get; set; }
    public double? ToolfaceResidual { get; set; }
    public double? ExpectedBuildRate { get; set; }
    public double? ActualBuildRate { get; set; }
    public double? BuildRateResidual { get; set; }
    public double? ExpectedTurnRate { get; set; }
    public double? ActualTurnRate { get; set; }
    public double? TurnRateResidual { get; set; }
}

/// <summary>A payload-conscious page of directional-control samples.</summary>
public class DirectionalControlEvaluationSampleChunk
{
    public int ChunkIndex { get; set; }
    public int SampleCount { get; set; }
    public double? StartActualMD { get; set; }
    public double? EndActualMD { get; set; }
    public List<DirectionalControlEvaluationSample> SampleList { get; set; } = [];
}

public class DirectionalControlEvaluationBundle
{
    public Guid BundleID { get; set; } = Guid.NewGuid();
    public int BundleIndex { get; set; }
    public double StartActualMD { get; set; }
    public double EndActualMD { get; set; }
    public int AttemptedSampleCount { get; set; }
    public int ValidSampleCount { get; set; }
    public int InvalidSampleCount { get; set; }
    public double ValidCoverageRatio { get; set; }
    public double MaximumInvalidGap { get; set; }
    public DirectionalControlDistributionSummary? CurvatureResidual { get; set; }
    public DirectionalControlDistributionSummary? ToolfaceResidual { get; set; }
    public DirectionalControlDistributionSummary? BuildRateResidual { get; set; }
    public DirectionalControlDistributionSummary? TurnRateResidual { get; set; }
}

public class DirectionalControlDistributionSummary
{
    public int Count { get; set; }
    public double P10 { get; set; }
    public double P50 { get; set; }
    public double P90 { get; set; }
    public double Mean { get; set; }
    public double StandardDeviation { get; set; }
    public double MedianAbsoluteDeviation { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; }
    public bool IsCircular { get; set; }
    public List<DirectionalControlHistogramBin> Histogram { get; set; } = [];
}

public class DirectionalControlHistogramBin
{
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public int Count { get; set; }
}

public static class DirectionalControlEvaluationValidation
{
    public static List<string> Validate(DirectionalControlEvaluationCase? value)
    {
        List<string> errors = [];
        if (value?.MetaInfo == null || value.MetaInfo.ID == Guid.Empty) errors.Add("meta_info_required");
        if (value == null) return errors;
        if (value.ReferenceTrajectoryID == Guid.Empty) errors.Add("reference_trajectory_required");
        if (value.ActualTrajectoryID == Guid.Empty) errors.Add("actual_trajectory_required");
        if (!Positive(value.EvaluationInterval)) errors.Add("evaluation_interval_must_be_positive");
        if (!Positive(value.ReferenceMDAdvance)) errors.Add("reference_advance_must_be_positive");
        if (!Positive(value.JunctionCurvatureRatio)) errors.Add("junction_curvature_ratio_must_be_positive");
        if (!Positive(value.MaximumInvalidGap)) errors.Add("maximum_invalid_gap_must_be_positive");
        if (!Positive(value.MinimumBundleLength)) errors.Add("minimum_bundle_length_must_be_positive");
        if (value.MinimumBundleSampleCount < 2) errors.Add("minimum_bundle_sample_count_must_be_at_least_two");
        if (!Positive(value.BundlingPenalty)) errors.Add("bundling_penalty_must_be_positive");
        if (value.StartActualMD is double start && (!Numeric.IsDefined(start) || start < 0.0)) errors.Add("start_actual_md_invalid");
        if (value.EndActualMD is double end && (!Numeric.IsDefined(end) || end < 0.0)) errors.Add("end_actual_md_invalid");
        if (value.StartActualMD is double lower && value.EndActualMD is double upper && upper <= lower)
            errors.Add("actual_md_range_invalid");
        return errors;
    }

    private static bool Positive(double value) => Numeric.IsDefined(value) && value > 0.0;
}
