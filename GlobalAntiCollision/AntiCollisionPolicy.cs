using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.GlobalAntiCollision;

public enum AntiCollisionHierarchyLevel { Cluster, Slot, Well, WellBore, Trajectory }
public enum AntiCollisionComparisonOperator { LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual }
public enum AntiCollisionIdentityMatchOperator { Exact, StartsWith, EndsWith, Contains, Glob }
public enum AntiCollisionFeatureTemporalOperator
{
    ActiveAtEvaluationTime,
    ActiveAtOldestMeasurementTime,
    OverlapsMeasurementInterval,
    ActiveAtSpecifiedTime,
    OverlapsSpecifiedInterval
}
public enum AntiCollisionPolicyEvaluationState { Matched, NoMatch, Indeterminate }
public enum AntiCollisionClassification { Normal, Alert, Alarm, Indeterminate }

/// <summary>An immutable revision of an anti-collision policy.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AntiCollisionPolicyRevision
{
    /// <summary>Revision identity.</summary>
    public MetaInfo? MetaInfo { get; set; }
    /// <summary>Stable identity shared by every revision of the same policy.</summary>
    public Guid PolicyID { get; set; }
    /// <summary>Server-derived, monotonically increasing revision number within PolicyID.</summary>
    public int RevisionNumber { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    /// <summary>Confidence proportion in (0, 0.999].</summary>
    public double ConfidenceFactor { get; set; } = GlobalAntiCollision.DefaultConfidenceFactor;
    /// <summary>Rules in deterministic priority order; exactly one unconditional final rule is required.</summary>
    public List<AntiCollisionPolicyRule> Rules { get; set; } = [];
}

/// <summary>Caller-owned fields for creating a new immutable policy revision.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AntiCollisionPolicyRevisionCreate
{
    public MetaInfo? MetaInfo { get; set; }
    public Guid PolicyID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public double ConfidenceFactor { get; set; } = GlobalAntiCollision.DefaultConfidenceFactor;
    public List<AntiCollisionPolicyRule> Rules { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AntiCollisionPolicyRule
{
    public Guid RuleID { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    /// <summary>Unique priority greater than or equal to one; lower values are evaluated first.</summary>
    public int Priority { get; set; }
    /// <summary>Dimensionless separation-factor Alert threshold.</summary>
    public double AlertThreshold { get; set; }
    /// <summary>Dimensionless separation-factor Alarm threshold; must be lower than AlertThreshold.</summary>
    public double AlarmThreshold { get; set; }
    /// <summary>All conditions must match. An empty list is the required default rule.</summary>
    public List<AntiCollisionPolicyCondition> Conditions { get; set; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ConditionType")]
[JsonDerivedType(typeof(AntiCollisionTrajectoryAgeCondition), "TrajectoryAge")]
[JsonDerivedType(typeof(AntiCollisionIdentityCondition), "Identity")]
[JsonDerivedType(typeof(AntiCollisionFeatureCondition), "Feature")]
public abstract class AntiCollisionPolicyCondition
{
    public Guid ConditionID { get; set; } = Guid.NewGuid();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AntiCollisionTrajectoryAgeCondition : AntiCollisionPolicyCondition
{
    public AntiCollisionComparisonOperator Operator { get; set; }
    /// <summary>Age threshold in canonical SI seconds.</summary>
    public double AgeThreshold { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AntiCollisionIdentityCondition : AntiCollisionPolicyCondition
{
    public AntiCollisionHierarchyLevel ResourceLevel { get; set; }
    public Guid IdentityDefinitionID { get; set; }
    public AntiCollisionIdentityMatchOperator MatchOperator { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AntiCollisionFeatureCondition : AntiCollisionPolicyCondition
{
    public AntiCollisionHierarchyLevel ResourceLevel { get; set; }
    public Guid FeatureCategoryID { get; set; }
    public Guid FeatureOptionID { get; set; }
    public AntiCollisionFeatureTemporalOperator TemporalOperator { get; set; }
    public DateTimeOffset? SpecifiedTimeUtc { get; set; }
    public DateTimeOffset? SpecifiedFromUtc { get; set; }
    public DateTimeOffset? SpecifiedToUtc { get; set; }
}

/// <summary>Effective-dated selection of one immutable policy revision for a Field.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class FieldAntiCollisionPolicyAssignment
{
    public MetaInfo? MetaInfo { get; set; }
    public Guid FieldID { get; set; }
    public Guid PolicyRevisionID { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset? ValidToUtc { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>Caller-owned fields for creating or replacing a Field policy assignment.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class FieldAntiCollisionPolicyAssignmentMutation
{
    public MetaInfo? MetaInfo { get; set; }
    public Guid FieldID { get; set; }
    public Guid PolicyRevisionID { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset? ValidToUtc { get; set; }
}

public sealed class AntiCollisionResourceContextSnapshot
{
    public AntiCollisionHierarchyLevel ResourceLevel { get; set; }
    public Guid ResourceID { get; set; }
    public string? Name { get; set; }
    public bool IsAvailable { get; set; } = true;
    public List<AntiCollisionIdentityValueSnapshot> Identities { get; set; } = [];
    public List<AntiCollisionFeatureValueSnapshot> Features { get; set; } = [];
    public bool IdentityCatalogAvailable { get; set; } = true;
    public string? UnavailableReason { get; set; }
}

public sealed class AntiCollisionIdentityValueSnapshot
{
    public Guid IdentityDefinitionID { get; set; }
    public string? Value { get; set; }
}

public sealed class AntiCollisionFeatureValueSnapshot
{
    public Guid FeatureCategoryID { get; set; }
    public Guid FeatureOptionID { get; set; }
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
}

/// <summary>Frozen comparison-side evidence and deterministic policy decision.</summary>
public sealed class AntiCollisionPolicyEvaluation
{
    public Guid ComparisonTrajectoryID { get; set; }
    public AntiCollisionPolicyEvaluationState State { get; set; }
    public string? Message { get; set; }
    public Guid? MatchedRuleID { get; set; }
    public int? MatchedRulePriority { get; set; }
    public double? AlertThreshold { get; set; }
    public double? AlarmThreshold { get; set; }
    public DateTimeOffset? OldestEvidenceUtc { get; set; }
    public DateTimeOffset? NewestEvidenceUtc { get; set; }
    public double? TrajectoryAge { get; set; }
    public AntiCollisionClassification WorstClassification { get; set; } = AntiCollisionClassification.Indeterminate;
    public List<AntiCollisionResourceContextSnapshot> Context { get; set; } = [];
}

public static class AntiCollisionPolicyValidation
{
    public static List<string> Validate(AntiCollisionPolicyRevision? value)
    {
        List<string> errors = [];
        if (value?.MetaInfo == null || value.MetaInfo.ID == Guid.Empty) errors.Add("policy_revision_id_required");
        if (value?.PolicyID == Guid.Empty) errors.Add("policy_id_required");
        if (value == null || !GlobalAntiCollision.IsConfidenceFactorSupported(value.ConfidenceFactor)) errors.Add("confidence_factor_invalid");
        if (value?.Rules is not { Count: > 0 }) { errors.Add("policy_rule_required"); return errors; }
        if (string.IsNullOrWhiteSpace(value.Name)) errors.Add("policy_name_required");
        if (value.Rules.Any(rule => rule == null)) { errors.Add("policy_rules_must_not_contain_null"); return errors; }
        if (value.Rules.Select(rule => rule.RuleID).Any(id => id == Guid.Empty) || value.Rules.Select(rule => rule.RuleID).Distinct().Count() != value.Rules.Count)
            errors.Add("rule_ids_must_be_non_empty_and_unique");
        if (value.Rules.Any(rule => rule.Priority < 1)) errors.Add("rule_priorities_must_be_at_least_one");
        if (value.Rules.Select(rule => rule.Priority).Distinct().Count() != value.Rules.Count) errors.Add("rule_priorities_must_be_unique");
        List<AntiCollisionPolicyRule> ordered = value.Rules.OrderBy(rule => rule.Priority).ToList();
        if (ordered.Any(rule => rule.Conditions == null)) { errors.Add("rule_conditions_required"); return errors; }
        if (ordered.Count(rule => rule.Conditions.Count == 0) != 1 || ordered[^1].Conditions.Count != 0) errors.Add("one_final_unconditional_rule_required");
        foreach (AntiCollisionPolicyRule rule in value.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Name)) errors.Add($"rule_{rule.RuleID}_name_required");
            if (!double.IsFinite(rule.AlarmThreshold) || !double.IsFinite(rule.AlertThreshold) ||
                rule.AlarmThreshold <= 0 || rule.AlertThreshold <= rule.AlarmThreshold)
                errors.Add($"rule_{rule.RuleID}_thresholds_invalid");
            if (rule.Conditions.Any(condition => condition == null)) { errors.Add($"rule_{rule.RuleID}_conditions_must_not_contain_null"); continue; }
            if (rule.Conditions.Select(condition => condition.ConditionID).Any(id => id == Guid.Empty) ||
                rule.Conditions.Select(condition => condition.ConditionID).Distinct().Count() != rule.Conditions.Count)
                errors.Add($"rule_{rule.RuleID}_condition_ids_invalid");
            foreach (AntiCollisionPolicyCondition condition in rule.Conditions) ValidateCondition(rule.RuleID, condition, errors);
        }
        return errors;
    }

    public static List<string> Validate(FieldAntiCollisionPolicyAssignment? value)
    {
        List<string> errors = [];
        if (value?.MetaInfo == null || value.MetaInfo.ID == Guid.Empty) errors.Add("assignment_id_required");
        if (value?.FieldID == Guid.Empty) errors.Add("field_id_required");
        if (value?.PolicyRevisionID == Guid.Empty) errors.Add("policy_revision_id_required");
        if (value != null && value.ValidFromUtc == default) errors.Add("valid_from_required");
        if (value != null && value.ValidFromUtc.Offset != TimeSpan.Zero) errors.Add("valid_from_must_be_utc");
        if (value?.ValidToUtc is { } utcTo && utcTo.Offset != TimeSpan.Zero) errors.Add("valid_to_must_be_utc");
        if (value?.ValidToUtc is { } to && to <= value.ValidFromUtc) errors.Add("validity_interval_invalid");
        return errors;
    }

    private static void ValidateCondition(Guid ruleId, AntiCollisionPolicyCondition condition, List<string> errors)
    {
        switch (condition)
        {
            case AntiCollisionTrajectoryAgeCondition age when !double.IsFinite(age.AgeThreshold) || age.AgeThreshold < 0:
                errors.Add($"rule_{ruleId}_age_threshold_invalid");
                break;
            case AntiCollisionIdentityCondition identity when identity.IdentityDefinitionID == Guid.Empty || string.IsNullOrWhiteSpace(identity.Pattern):
                errors.Add($"rule_{ruleId}_identity_condition_invalid");
                break;
            case AntiCollisionFeatureCondition feature:
                if (feature.FeatureCategoryID == Guid.Empty || feature.FeatureOptionID == Guid.Empty) errors.Add($"rule_{ruleId}_feature_condition_invalid");
                if (feature.TemporalOperator == AntiCollisionFeatureTemporalOperator.ActiveAtSpecifiedTime && feature.SpecifiedTimeUtc == null)
                    errors.Add($"rule_{ruleId}_specified_time_required");
                if (feature.TemporalOperator == AntiCollisionFeatureTemporalOperator.OverlapsSpecifiedInterval &&
                    (feature.SpecifiedFromUtc == null || feature.SpecifiedToUtc == null || feature.SpecifiedFromUtc >= feature.SpecifiedToUtc))
                    errors.Add($"rule_{ruleId}_specified_interval_invalid");
                if (feature.SpecifiedTimeUtc is { Offset: not { Ticks: 0 } } ||
                    feature.SpecifiedFromUtc is { Offset: not { Ticks: 0 } } ||
                    feature.SpecifiedToUtc is { Offset: not { Ticks: 0 } })
                    errors.Add($"rule_{ruleId}_specified_times_must_be_utc");
                if (feature.TemporalOperator != AntiCollisionFeatureTemporalOperator.ActiveAtSpecifiedTime && feature.SpecifiedTimeUtc != null)
                    errors.Add($"rule_{ruleId}_specified_time_forbidden");
                if (feature.TemporalOperator != AntiCollisionFeatureTemporalOperator.OverlapsSpecifiedInterval &&
                    (feature.SpecifiedFromUtc != null || feature.SpecifiedToUtc != null))
                    errors.Add($"rule_{ruleId}_specified_interval_forbidden");
                break;
            case AntiCollisionTrajectoryAgeCondition or AntiCollisionIdentityCondition:
                break;
            default:
                errors.Add($"rule_{ruleId}_condition_type_invalid");
                break;
        }
    }
}
