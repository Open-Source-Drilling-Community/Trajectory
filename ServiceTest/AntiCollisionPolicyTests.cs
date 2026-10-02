using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class AntiCollisionPolicyTests
{
    [Test]
    public void Validation_requires_ordered_thresholds_and_final_default()
    {
        AntiCollisionPolicyRevision policy = Policy();
        policy.Rules[0].AlarmThreshold = policy.Rules[0].AlertThreshold;
        policy.Rules.Add(new AntiCollisionPolicyRule { RuleID = Guid.NewGuid(), Name = "Age", Priority = 200, AlertThreshold = 2, AlarmThreshold = 1,
            Conditions = [new AntiCollisionTrajectoryAgeCondition { AgeThresholdSeconds = 10 }] });

        List<string> errors = AntiCollisionPolicyValidation.Validate(policy);

        Assert.Multiple(() =>
        {
            Assert.That(errors.Any(error => error.Contains("thresholds_invalid")), Is.True);
            Assert.That(errors, Does.Contain("one_final_unconditional_rule_required"));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validation_rejects_rule_priority_below_one(int priority)
    {
        AntiCollisionPolicyRevision policy = Policy();
        policy.Rules[0].Priority = priority;

        Assert.That(AntiCollisionPolicyValidation.Validate(policy),
            Does.Contain("rule_priorities_must_be_at_least_one"));
    }

    [Test]
    public void Evaluation_uses_first_matching_rule_and_classifies_alarm()
    {
        Guid identityId = Guid.NewGuid();
        AntiCollisionPolicyRevision policy = Policy();
        policy.Rules.Insert(0, new AntiCollisionPolicyRule
        {
            RuleID = Guid.NewGuid(), Name = "Identity", Priority = 1, AlertThreshold = 3, AlarmThreshold = 2,
            Conditions = [new AntiCollisionIdentityCondition
            {
                ResourceLevel = AntiCollisionHierarchyLevel.Well,
                IdentityDefinitionID = identityId,
                MatchOperator = AntiCollisionIdentityMatchOperator.Glob,
                Pattern = "U*-PROD"
            }]
        });
        AntiCollisionResourceContextSnapshot well = new()
        {
            ResourceLevel = AntiCollisionHierarchyLevel.Well,
            ResourceID = Guid.NewGuid(),
            Identities = [new AntiCollisionIdentityValueSnapshot { IdentityDefinitionID = identityId, Value = "u4-prod" }]
        };

        AntiCollisionPolicyEvaluation result = AntiCollisionPolicyEvaluator.Evaluate(policy, Guid.NewGuid(),
            DateTimeOffset.Parse("2026-10-01T00:00:00Z"), DateTimeOffset.Parse("2020-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2020-01-02T00:00:00Z"), [well], [2.5, 1.5]);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(AntiCollisionPolicyEvaluationState.Matched));
            Assert.That(result.MatchedRulePriority, Is.EqualTo(1));
            Assert.That(result.WorstClassification, Is.EqualTo(AntiCollisionClassification.Alarm));
        });
    }

    [Test]
    public void Missing_age_is_indeterminate_and_does_not_fall_through()
    {
        AntiCollisionPolicyRevision policy = Policy();
        policy.Rules.Insert(0, new AntiCollisionPolicyRule
        {
            RuleID = Guid.NewGuid(), Name = "Age", Priority = 1, AlertThreshold = 3, AlarmThreshold = 2,
            Conditions = [new AntiCollisionTrajectoryAgeCondition { Operator = AntiCollisionComparisonOperator.GreaterThan, AgeThresholdSeconds = 1 }]
        });

        AntiCollisionPolicyEvaluation result = AntiCollisionPolicyEvaluator.Evaluate(policy, Guid.NewGuid(), DateTimeOffset.UtcNow,
            null, null, [], [5]);

        Assert.That(result.State, Is.EqualTo(AntiCollisionPolicyEvaluationState.Indeterminate));
    }

    [Test]
    public void Feature_temporal_variant_rejects_fields_from_another_variant()
    {
        AntiCollisionPolicyRevision policy = Policy();
        policy.Rules.Insert(0, new AntiCollisionPolicyRule
        {
            RuleID = Guid.NewGuid(), Name = "Feature", Priority = 1, AlertThreshold = 2, AlarmThreshold = 1,
            Conditions = [new AntiCollisionFeatureCondition
            {
                FeatureCategoryID = Guid.NewGuid(), FeatureOptionID = Guid.NewGuid(),
                TemporalOperator = AntiCollisionFeatureTemporalOperator.ActiveAtEvaluationTime,
                SpecifiedTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
            }]
        });

        Assert.That(AntiCollisionPolicyValidation.Validate(policy),
            Has.Some.Contains("specified_time_forbidden"));
    }

    [Test]
    public void Assignment_requires_utc_and_a_forward_interval()
    {
        FieldAntiCollisionPolicyAssignment assignment = Assignment(Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        List<string> errors = AntiCollisionPolicyValidation.Validate(assignment);

        Assert.Multiple(() =>
        {
            Assert.That(errors, Does.Contain("valid_from_must_be_utc"));
            Assert.That(errors, Does.Contain("validity_interval_invalid"));
        });
    }

    [Test]
    public void Manager_derives_revisions_and_rejects_overlapping_field_assignments()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-policy", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "Trajectory.db");
        try
        {
            SqlConnectionManagerTrajectory database = new(path, NullLogger<SqlConnectionManagerTrajectory>.Instance);
            AntiCollisionPolicyManager manager = new(NullLogger<AntiCollisionPolicyManager>.Instance, database);
            AntiCollisionPolicyRevision first = Policy();
            AntiCollisionPolicyRevision second = Policy(first.PolicyID);
            AntiCollisionPolicyRevision unusedFirst = Policy();
            AntiCollisionPolicyRevision unusedSecond = Policy(unusedFirst.PolicyID);
            Assert.That(manager.AddRevision(first), Is.True);
            Assert.That(manager.AddRevision(second), Is.True);
            Assert.That(manager.AddRevision(unusedFirst), Is.True);
            Assert.That(manager.AddRevision(unusedSecond), Is.True);
            Guid fieldId = Guid.NewGuid();
            FieldAntiCollisionPolicyAssignment assignment = Assignment(fieldId, first.MetaInfo!.ID,
                DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2027-01-01T00:00:00Z"));
            FieldAntiCollisionPolicyAssignment overlap = Assignment(fieldId, second.MetaInfo!.ID,
                DateTimeOffset.Parse("2026-06-01T00:00:00Z"), null);

            Assert.Multiple(() =>
            {
                Assert.That(first.RevisionNumber, Is.EqualTo(1));
                Assert.That(second.RevisionNumber, Is.EqualTo(2));
                Assert.That(manager.DeletePolicy(unusedFirst.PolicyID, unusedFirst.MetaInfo!.ID),
                    Is.EqualTo(AntiCollisionPolicyDeleteResult.Stale));
                Assert.That(manager.DeletePolicy(unusedFirst.PolicyID, unusedSecond.MetaInfo!.ID),
                    Is.EqualTo(AntiCollisionPolicyDeleteResult.Deleted));
                Assert.That(manager.GetRevisions(unusedFirst.PolicyID), Is.Empty);
                Assert.That(manager.AddAssignment(assignment), Is.True);
                Assert.That(manager.AddAssignment(overlap), Is.False);
                Assert.That(manager.DeletePolicy(first.PolicyID, second.MetaInfo!.ID),
                    Is.EqualTo(AntiCollisionPolicyDeleteResult.InUse));
                Assert.That(manager.GetEffectiveAssignment(fieldId, DateTimeOffset.Parse("2026-03-01T00:00:00Z"))?.MetaInfo?.ID,
                    Is.EqualTo(assignment.MetaInfo!.ID));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Requested_assignment_is_optional_and_must_belong_to_the_reference_field()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-policy-selection", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            SqlConnectionManagerTrajectory database = new(Path.Combine(directory, "Trajectory.db"),
                NullLogger<SqlConnectionManagerTrajectory>.Instance);
            AntiCollisionPolicyManager manager = new(NullLogger<AntiCollisionPolicyManager>.Instance, database);
            AntiCollisionPolicyRevision policy = Policy();
            Guid fieldId = Guid.NewGuid();
            FieldAntiCollisionPolicyAssignment assignment = Assignment(fieldId, policy.MetaInfo!.ID,
                DateTimeOffset.Parse("2020-01-01T00:00:00Z"), null);
            Assert.That(manager.AddRevision(policy), Is.True);
            Assert.That(manager.AddAssignment(assignment), Is.True);

            var noPolicy = new OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision { ConfidenceFactor = 0.8 };
            var selected = new OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision
            {
                ConfidenceFactor = 0.8,
                RequestedPolicyAssignmentID = assignment.MetaInfo!.ID
            };
            DateTimeOffset evaluated = DateTimeOffset.Parse("2026-10-02T10:00:00Z");

            Assert.Multiple(() =>
            {
                Assert.That(GlobalAntiCollisionCalculationWorker.TryApplyRequestedPolicy(manager, noPolicy,
                    new OSDC.Drilling.Trajectory.Model.Trajectory { FieldID = fieldId }, evaluated, out string? noPolicyError), Is.True);
                Assert.That(noPolicyError, Is.Null);
                Assert.That(noPolicy.PolicySnapshot, Is.Null);

                Assert.That(GlobalAntiCollisionCalculationWorker.TryApplyRequestedPolicy(manager, selected,
                    new OSDC.Drilling.Trajectory.Model.Trajectory { FieldID = fieldId }, evaluated, out string? selectedError), Is.True);
                Assert.That(selectedError, Is.Null);
                Assert.That(selected.PolicyAssignmentID, Is.EqualTo(assignment.MetaInfo.ID));
                Assert.That(selected.PolicySnapshot?.MetaInfo?.ID, Is.EqualTo(policy.MetaInfo.ID));
                Assert.That(selected.ConfidenceFactor, Is.EqualTo(policy.ConfidenceFactor));

                Assert.That(GlobalAntiCollisionCalculationWorker.TryApplyRequestedPolicy(manager, selected,
                    new OSDC.Drilling.Trajectory.Model.Trajectory { FieldID = Guid.NewGuid() }, evaluated, out string? wrongFieldError), Is.False);
                Assert.That(wrongFieldError, Does.Contain("does not belong"));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static AntiCollisionPolicyRevision Policy(Guid? policyId = null) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, PolicyID = policyId ?? Guid.NewGuid(), Name = "Policy", ConfidenceFactor = 0.95,
        Rules = [new AntiCollisionPolicyRule { RuleID = Guid.NewGuid(), Name = "Default", Priority = 100, AlertThreshold = 2, AlarmThreshold = 1 }]
    };

    private static FieldAntiCollisionPolicyAssignment Assignment(Guid fieldId, Guid revisionId, DateTimeOffset from, DateTimeOffset? to) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, FieldID = fieldId, PolicyRevisionID = revisionId, ValidFromUtc = from, ValidToUtc = to
    };
}
