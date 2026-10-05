using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model;

public static class DirectionalControlEvaluationCalculator
{
    public static bool Calculate(
        DirectionalControlEvaluationCase value,
        Trajectory reference,
        Trajectory actual,
        Action<double, string?>? progress = null)
    {
        List<string> errors = DirectionalControlEvaluationValidation.Validate(value);
        if (errors.Count > 0) return Fail(value, string.Join(", ", errors));
        if (reference.WellBoreID == Guid.Empty || actual.WellBoreID == Guid.Empty ||
            reference.WellBoreID != actual.WellBoreID)
            return Fail(value, "The reference and actual trajectories must belong to the same wellbore.");
        if (!TrajectoryExtrapolationCalculator.TryGetOrderedCompleteStations(reference, out List<SurveyStation> referenceStations) ||
            !TrajectoryExtrapolationCalculator.TryGetOrderedCompleteStations(actual, out List<SurveyStation> actualStations))
            return Fail(value, "Both trajectories require complete calculated survey stations.");

        double availableStart = actualStations[0].MD!.Value;
        double availableEnd = actualStations[^1].MD!.Value;
        double startMd = Math.Max(value.StartActualMD ?? availableStart, availableStart);
        double endMd = Math.Min(value.EndActualMD ?? availableEnd, availableEnd);
        if (endMd - startMd < value.EvaluationInterval)
            return Fail(value, "The selected actual-MD range is shorter than one evaluation interval.");

        value.ReferenceTrajectoryRevision = reference.LastModificationDate;
        value.ActualTrajectoryRevision = actual.LastModificationDate;
        value.SampleList = [];
        value.BundleList = [];
        value.CalculationState = CalculationState.Running;
        value.CalculationProgress = 0.02;
        value.CalculationMessage = "Preparing directional-control evaluation";
        progress?.Invoke(value.CalculationProgress, value.CalculationMessage);

        int count = (int)Math.Floor((endMd - startMd) / value.EvaluationInterval + 1e-9);
        ReconnectTrajectoryExtrapolationSpecification reconnect = new()
        {
            ReferenceTrajectoryID = value.ReferenceTrajectoryID,
            ReferenceMDAdvance = value.ReferenceMDAdvance,
            CurveType = value.CurveType,
            AzimuthBranch = value.AzimuthBranch,
            JunctionCurvatureRatio = value.JunctionCurvatureRatio,
            LeadInLength = 0.0
        };

        for (int index = 0; index < count; index++)
        {
            double md = startMd + index * value.EvaluationInterval;
            double nextMd = md + value.EvaluationInterval;
            DirectionalControlEvaluationSample sample = new() { ActualMD = md, ActualEndMD = nextMd };
            value.SampleList.Add(sample);

            if (!SurveyStation.InterpolateAtAbscissa(actualStations, md, out SurveyStation? start,
                    actual.CalculationType) || start == null ||
                !SurveyStation.InterpolateAtAbscissa(actualStations, nextMd, out SurveyStation? end,
                    actual.CalculationType) || end == null)
            {
                Reject(sample, "actual_interpolation_failed", "The actual response interval could not be interpolated.");
            }
            else if (!TrajectoryExtrapolationCalculator.TrySolveReconnectFromStation(start, reference, reconnect,
                         out ReconnectTrajectorySolution? solution, out string? failure))
            {
                Reject(sample, "reconnect_failed", failure ?? "The reconnect solution failed.");
            }
            else if (!TrajectoryExtrapolationCalculator.TryFitIntervalControls(start, end, value.CurveType,
                         out TrajectoryExtrapolationSolvedSection? actualSection) || actualSection == null)
            {
                Reject(sample, "actual_fit_failed", "The actual response interval could not be fitted with the selected curve family.");
            }
            else
            {
                sample.ClosestReferenceMD = solution!.ClosestReferenceMD;
                sample.TargetReferenceMD = solution.TargetReferenceMD;
                PopulateControls(sample, solution.UpstreamSection, actualSection, value.CurveType);
                sample.IsValid = true;
            }

            if (index % 10 == 0 || index == count - 1)
            {
                value.CalculationProgress = 0.05 + 0.75 * (index + 1.0) / count;
                value.CalculationMessage = $"Evaluating actual interval {index + 1} of {count}";
                progress?.Invoke(value.CalculationProgress, value.CalculationMessage);
            }
        }

        value.CalculationProgress = 0.85;
        value.CalculationMessage = "Bundling directional-response discrepancies";
        progress?.Invoke(value.CalculationProgress, value.CalculationMessage);
        value.BundleList = DirectionalControlBundling.CreateBundles(value);
        value.CalculationState = CalculationState.Completed;
        value.CalculationProgress = 1.0;
        value.CalculationMessage = value.SampleList.Any(sample => sample.IsValid)
            ? null
            : "No evaluation interval produced a valid reconnect and actual-response fit.";
        progress?.Invoke(1.0, value.CalculationMessage);
        return value.SampleList.Any(sample => sample.IsValid);
    }

    private static void PopulateControls(
        DirectionalControlEvaluationSample sample,
        TrajectoryExtrapolationSolvedSection expected,
        TrajectoryExtrapolationSolvedSection actual,
        ExtrapolationCurveType curveType)
    {
        switch (curveType)
        {
            case ExtrapolationCurveType.CircularArc:
                sample.ExpectedCurvature = expected.CircularArcCurvature;
                sample.ActualCurvature = actual.CircularArcCurvature;
                sample.CurvatureResidual = Difference(actual.CircularArcCurvature, expected.CircularArcCurvature);
                sample.ExpectedToolface = expected.CircularArcStartToolface;
                sample.ActualToolface = actual.CircularArcStartToolface;
                sample.ToolfaceResidual = AngularDifference(actual.CircularArcStartToolface, expected.CircularArcStartToolface);
                break;
            case ExtrapolationCurveType.ConstantBuildAndTurn:
                sample.ExpectedBuildRate = expected.ConstantBuildRate;
                sample.ActualBuildRate = actual.ConstantBuildRate;
                sample.BuildRateResidual = Difference(actual.ConstantBuildRate, expected.ConstantBuildRate);
                sample.ExpectedTurnRate = expected.ConstantTurnRate;
                sample.ActualTurnRate = actual.ConstantTurnRate;
                sample.TurnRateResidual = Difference(actual.ConstantTurnRate, expected.ConstantTurnRate);
                break;
            case ExtrapolationCurveType.ConstantCurvatureAndToolface:
                sample.ExpectedCurvature = expected.ConstantCurvature;
                sample.ActualCurvature = actual.ConstantCurvature;
                sample.CurvatureResidual = Difference(actual.ConstantCurvature, expected.ConstantCurvature);
                sample.ExpectedToolface = expected.ConstantToolface;
                sample.ActualToolface = actual.ConstantToolface;
                sample.ToolfaceResidual = AngularDifference(actual.ConstantToolface, expected.ConstantToolface);
                break;
        }
    }

    private static double? Difference(double? actual, double? expected) =>
        actual is double a && expected is double e && Numeric.IsDefined(a) && Numeric.IsDefined(e) ? a - e : null;

    internal static double? AngularDifference(double? actual, double? expected) =>
        actual is double a && expected is double e && Numeric.IsDefined(a) && Numeric.IsDefined(e)
            ? WrapAngle(a - e)
            : null;

    internal static double WrapAngle(double value) =>
        Math.Atan2(Math.Sin(value), Math.Cos(value));

    private static void Reject(DirectionalControlEvaluationSample sample, string code, string message)
    {
        sample.IsValid = false;
        sample.FailureCode = code;
        sample.FailureMessage = message;
    }

    private static bool Fail(DirectionalControlEvaluationCase value, string message)
    {
        value.CalculationState = CalculationState.Failed;
        value.CalculationProgress = 1.0;
        value.CalculationMessage = message;
        value.SampleList = [];
        value.BundleList = [];
        return false;
    }
}
