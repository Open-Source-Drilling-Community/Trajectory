using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.GlobalAntiCollision;

public static class AntiCollisionPolicyEvaluator
{
    public static AntiCollisionPolicyEvaluation Evaluate(
        AntiCollisionPolicyRevision policy,
        Guid comparisonTrajectoryId,
        DateTimeOffset evaluationUtc,
        DateTimeOffset? oldestEvidenceUtc,
        DateTimeOffset? newestEvidenceUtc,
        List<AntiCollisionResourceContextSnapshot> context,
        IEnumerable<double>? separationFactors = null)
    {
        AntiCollisionPolicyEvaluation result = new()
        {
            ComparisonTrajectoryID = comparisonTrajectoryId,
            OldestEvidenceUtc = oldestEvidenceUtc,
            NewestEvidenceUtc = newestEvidenceUtc,
            Context = context
        };
        if (oldestEvidenceUtc is { } oldest)
        {
            double age = (evaluationUtc - oldest).TotalSeconds;
            if (age < 0)
            {
                result.State = AntiCollisionPolicyEvaluationState.Indeterminate;
                result.Message = "The oldest trajectory evidence is later than the policy evaluation time.";
                return result;
            }
            result.TrajectoryAgeSeconds = age;
        }

        foreach (AntiCollisionPolicyRule rule in policy.Rules.OrderBy(rule => rule.Priority))
        {
            bool noMatch = false;
            string? indeterminate = null;
            foreach (AntiCollisionPolicyCondition condition in rule.Conditions)
            {
                (AntiCollisionPolicyEvaluationState state, string? message) = EvaluateCondition(
                    condition, evaluationUtc, result.TrajectoryAgeSeconds, oldestEvidenceUtc, newestEvidenceUtc, context);
                if (state == AntiCollisionPolicyEvaluationState.NoMatch) { noMatch = true; break; }
                if (state == AntiCollisionPolicyEvaluationState.Indeterminate) indeterminate ??= message;
            }
            if (noMatch) continue;
            if (indeterminate != null)
            {
                result.State = AntiCollisionPolicyEvaluationState.Indeterminate;
                result.Message = $"Rule {rule.RuleID} could not be evaluated: {indeterminate}";
                return result;
            }

            result.State = AntiCollisionPolicyEvaluationState.Matched;
            result.MatchedRuleID = rule.RuleID;
            result.MatchedRulePriority = rule.Priority;
            result.AlertThreshold = rule.AlertThreshold;
            result.AlarmThreshold = rule.AlarmThreshold;
            result.WorstClassification = ClassifyWorst(separationFactors, rule.AlertThreshold, rule.AlarmThreshold);
            return result;
        }

        result.State = AntiCollisionPolicyEvaluationState.NoMatch;
        result.Message = "No policy rule matched. A valid policy should have an unconditional final rule.";
        return result;
    }

    private static (AntiCollisionPolicyEvaluationState State, string? Message) EvaluateCondition(
        AntiCollisionPolicyCondition condition,
        DateTimeOffset evaluationUtc,
        double? ageSeconds,
        DateTimeOffset? oldestEvidenceUtc,
        DateTimeOffset? newestEvidenceUtc,
        List<AntiCollisionResourceContextSnapshot> context)
    {
        switch (condition)
        {
            case AntiCollisionTrajectoryAgeCondition age:
                if (!ageSeconds.HasValue) return (AntiCollisionPolicyEvaluationState.Indeterminate, "No survey acquisition or measurement date is available.");
                return (Compare(ageSeconds.Value, age.AgeThresholdSeconds, age.Operator)
                    ? AntiCollisionPolicyEvaluationState.Matched : AntiCollisionPolicyEvaluationState.NoMatch, null);

            case AntiCollisionIdentityCondition identity:
            {
                AntiCollisionResourceContextSnapshot? resource = context.FirstOrDefault(item => item.ResourceLevel == identity.ResourceLevel);
                if (resource == null) return (AntiCollisionPolicyEvaluationState.Indeterminate, $"The {identity.ResourceLevel} context is unavailable.");
                if (!resource.IsAvailable) return (AntiCollisionPolicyEvaluationState.Indeterminate, resource.UnavailableReason ?? $"The {identity.ResourceLevel} context is unavailable.");
                if (!resource.IdentityCatalogAvailable) return (AntiCollisionPolicyEvaluationState.Indeterminate,
                    resource.UnavailableReason ?? $"The {identity.ResourceLevel} identity catalog is unavailable.");
                IEnumerable<string> values = resource.Identities
                    .Where(value => value.IdentityDefinitionID == identity.IdentityDefinitionID && value.Value != null)
                    .Select(value => value.Value!);
                return (values.Any(value => MatchIdentity(value, identity.Pattern, identity.MatchOperator, identity.CaseSensitive))
                    ? AntiCollisionPolicyEvaluationState.Matched : AntiCollisionPolicyEvaluationState.NoMatch, null);
            }

            case AntiCollisionFeatureCondition feature:
            {
                AntiCollisionResourceContextSnapshot? resource = context.FirstOrDefault(item => item.ResourceLevel == feature.ResourceLevel);
                if (resource == null) return (AntiCollisionPolicyEvaluationState.Indeterminate, $"The {feature.ResourceLevel} context is unavailable.");
                if (!resource.IsAvailable) return (AntiCollisionPolicyEvaluationState.Indeterminate, resource.UnavailableReason ?? $"The {feature.ResourceLevel} context is unavailable.");
                List<AntiCollisionFeatureValueSnapshot> candidates = resource.Features.Where(value =>
                    value.FeatureCategoryID == feature.FeatureCategoryID && value.FeatureOptionID == feature.FeatureOptionID).ToList();
                foreach (AntiCollisionFeatureValueSnapshot candidate in candidates)
                {
                    (bool? active, string? message) = EvaluateFeatureTime(feature, candidate, evaluationUtc, oldestEvidenceUtc, newestEvidenceUtc);
                    if (active == true) return (AntiCollisionPolicyEvaluationState.Matched, null);
                    if (active == null) return (AntiCollisionPolicyEvaluationState.Indeterminate, message);
                }
                return (AntiCollisionPolicyEvaluationState.NoMatch, null);
            }

            default:
                return (AntiCollisionPolicyEvaluationState.Indeterminate, "The condition type is unsupported.");
        }
    }

    private static (bool? Active, string? Message) EvaluateFeatureTime(
        AntiCollisionFeatureCondition condition,
        AntiCollisionFeatureValueSnapshot assignment,
        DateTimeOffset evaluationUtc,
        DateTimeOffset? oldestEvidenceUtc,
        DateTimeOffset? newestEvidenceUtc)
    {
        return condition.TemporalOperator switch
        {
            AntiCollisionFeatureTemporalOperator.ActiveAtEvaluationTime => (Contains(assignment, evaluationUtc), null),
            AntiCollisionFeatureTemporalOperator.ActiveAtOldestMeasurementTime => oldestEvidenceUtc is { } oldest
                ? (Contains(assignment, oldest), null) : (null, "No oldest measurement time is available."),
            AntiCollisionFeatureTemporalOperator.OverlapsMeasurementInterval => oldestEvidenceUtc is { } from && newestEvidenceUtc is { } to
                ? (Overlaps(assignment, from, to), null) : (null, "No complete measurement interval is available."),
            AntiCollisionFeatureTemporalOperator.ActiveAtSpecifiedTime => condition.SpecifiedTimeUtc is { } specified
                ? (Contains(assignment, specified), null) : (null, "The condition has no specified time."),
            AntiCollisionFeatureTemporalOperator.OverlapsSpecifiedInterval => condition.SpecifiedFromUtc is { } specifiedFrom && condition.SpecifiedToUtc is { } specifiedTo
                ? (Overlaps(assignment, specifiedFrom, specifiedTo), null) : (null, "The condition has no complete specified interval."),
            _ => (null, "The feature temporal operator is unsupported.")
        };
    }

    private static bool Contains(AntiCollisionFeatureValueSnapshot assignment, DateTimeOffset instant) =>
        (!assignment.FromUtc.HasValue || assignment.FromUtc <= instant) &&
        (!assignment.ToUtc.HasValue || instant < assignment.ToUtc);

    private static bool Overlaps(AntiCollisionFeatureValueSnapshot assignment, DateTimeOffset from, DateTimeOffset to) =>
        (!assignment.ToUtc.HasValue || from < assignment.ToUtc) &&
        (!assignment.FromUtc.HasValue || assignment.FromUtc < to);

    private static bool Compare(double left, double right, AntiCollisionComparisonOperator op) => op switch
    {
        AntiCollisionComparisonOperator.LessThan => left < right,
        AntiCollisionComparisonOperator.LessThanOrEqual => left <= right,
        AntiCollisionComparisonOperator.GreaterThan => left > right,
        AntiCollisionComparisonOperator.GreaterThanOrEqual => left >= right,
        _ => false
    };

    private static bool MatchIdentity(string value, string pattern, AntiCollisionIdentityMatchOperator op, bool caseSensitive)
    {
        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return op switch
        {
            AntiCollisionIdentityMatchOperator.Exact => string.Equals(value, pattern, comparison),
            AntiCollisionIdentityMatchOperator.StartsWith => value.StartsWith(pattern, comparison),
            AntiCollisionIdentityMatchOperator.EndsWith => value.EndsWith(pattern, comparison),
            AntiCollisionIdentityMatchOperator.Contains => value.Contains(pattern, comparison),
            AntiCollisionIdentityMatchOperator.Glob => GlobMatch(value, pattern, caseSensitive),
            _ => false
        };
    }

    private static bool GlobMatch(string value, string pattern, bool caseSensitive)
    {
        if (!caseSensitive) { value = value.ToUpperInvariant(); pattern = pattern.ToUpperInvariant(); }
        int valueIndex = 0, patternIndex = 0, star = -1, retry = 0;
        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == value[valueIndex]))
            { valueIndex++; patternIndex++; }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            { star = patternIndex++; retry = valueIndex; }
            else if (star >= 0)
            { patternIndex = star + 1; valueIndex = ++retry; }
            else return false;
        }
        while (patternIndex < pattern.Length && pattern[patternIndex] == '*') patternIndex++;
        return patternIndex == pattern.Length;
    }

    private static AntiCollisionClassification ClassifyWorst(IEnumerable<double>? factors, double alert, double alarm)
    {
        List<double> defined = factors?.Where(double.IsFinite).ToList() ?? [];
        if (defined.Count == 0) return AntiCollisionClassification.Indeterminate;
        double minimum = defined.Min();
        if (minimum < alarm) return AntiCollisionClassification.Alarm;
        if (minimum < alert) return AntiCollisionClassification.Alert;
        return AntiCollisionClassification.Normal;
    }
}
