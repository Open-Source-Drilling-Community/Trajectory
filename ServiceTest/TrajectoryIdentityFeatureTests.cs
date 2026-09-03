using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace ServiceTest;

[TestFixture]
public sealed class TrajectoryIdentityFeatureTests
{
    private string directory = null!;
    private TrajectoryIdentityManager identities = null!;
    private TrajectoryFeatureCategoryManager categories = null!;
    private TrajectoryAssignmentValidator validator = null!;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "TrajectoryIdentityFeatureTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var main = new SqlConnectionManagerTrajectory(Path.Combine(directory, "Trajectory.db"), NullLogger<SqlConnectionManagerTrajectory>.Instance);
        identities = new(main);
        categories = new(main);
        validator = new(identities, categories);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void Defaults_match_the_shared_contract()
    {
        Assert.That(identities.GetAll().Select(value => value.Name), Is.EquivalentTo(new[]
        {
            "NameForPlanning", "NameForCompanyReporting", "NameForRegulatoryReporting", "Nickname", "NameForOperationReporting"
        }));

        Dictionary<string, TrajectoryFeatureCategory> actual = categories.GetAll().ToDictionary(value => value.Name!);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Count.EqualTo(13));
            Assert.That(actual["SurveyContext"].Options, Has.Count.EqualTo(18));
            Assert.That(actual["MeasurementCondition"].Options, Has.Count.EqualTo(23));
            Assert.That(actual["SurveyReferenceStatus"].IsExclusive, Is.True);
            Assert.That(actual["SurveyReferenceStatus"].HasValidityPeriod, Is.True);
            Assert.That(actual["SurveyStationDensity"].IsExclusive, Is.True);
            Assert.That(actual["SurveyStationDensity"].HasValidityPeriod, Is.False);
            Assert.That(actual["CorrectionApplied"].Options!.Select(value => value.Name), Does.Contain("None"));
        });
    }

    [Test]
    public void Validator_applies_the_same_catalog_rules_to_survey_runs_and_trajectories()
    {
        TrajectoryIdentity identity = identities.GetAll().First();
        TrajectoryFeatureCategory context = categories.GetAll().Single(value => value.Name == "SurveyContext");
        TrajectoryFeatureCategory density = categories.GetAll().Single(value => value.Name == "SurveyStationDensity");

        var identityAssignment = new TrajectoryIdentityAssignment { ID = Guid.NewGuid(), IdentityID = identity.MetaInfo!.ID, Value = "Example" };
        var contextAssignments = context.Options!.Take(2).Select(option => new TrajectoryFeatureAssignment
        {
            ID = Guid.NewGuid(), FeatureCategoryID = context.MetaInfo!.ID, FeatureOptionID = option.ID
        }).ToList();
        var surveyRun = new SurveyRun { SurveyRunIdentityAssignments = [identityAssignment], SurveyRunFeatureAssignments = contextAssignments };
        var trajectory = new Trajectory { TrajectoryIdentityAssignments = [identityAssignment], TrajectoryFeatureAssignments = contextAssignments };

        Assert.Multiple(() =>
        {
            Assert.That(validator.Validate(surveyRun), Is.True);
            Assert.That(validator.Validate(trajectory), Is.True);
        });

        trajectory.TrajectoryFeatureAssignments = density.Options!.Take(2).Select(option => new TrajectoryFeatureAssignment
        {
            ID = Guid.NewGuid(), FeatureCategoryID = density.MetaInfo!.ID, FeatureOptionID = option.ID
        }).ToList();
        Assert.That(validator.Validate(trajectory), Is.False);
    }
}
