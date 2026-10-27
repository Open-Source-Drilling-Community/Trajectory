using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.Drilling.Trajectory.ModelShared;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class BoreholeRadiusPreparationTests
{
    private static WellBoreArchitecture Architecture() => JsonSerializer.Deserialize<WellBoreArchitecture>("""
        {"CasingSections":[{"TopDepth":{"GaussianValue":{"Mean":0}},"Length":{"GaussianValue":{"Mean":100}},
          "CasingSectionSizeTable":[{"HoleSize":{"GaussianValue":{"Mean":0.4}},"Length":{"GaussianValue":{"Mean":100}}}]}]}
        """, JsonSettings.Options)!;

    [Test]
    public async Task MissingAndLegacyZeroAreDerivedWithoutOverwritingKnownRadius()
    {
        Guid wellBore = Guid.NewGuid();
        var trajectory = new TrajectoryModel { WellBoreID = wellBore, SurveyStationList = [
            new SurveyStation { MD = 10, BoreholeRadius = null },
            new SurveyStation { MD = 20, BoreholeRadius = 0 },
            new SurveyStation { MD = 30, BoreholeRadius = 0.35 },
            new SurveyStation { MD = 150, BoreholeRadius = 0 }
        ] };
        int requests = 0;
        int defined = await TrajectoryManager.PrepareBoreholeRadiiAsync(trajectory, id =>
        {
            Assert.That(id, Is.EqualTo(wellBore));
            requests++;
            return Task.FromResult<WellBoreArchitecture?>(Architecture());
        });
        Assert.Multiple(() =>
        {
            Assert.That(requests, Is.EqualTo(1));
            Assert.That(defined, Is.EqualTo(3));
            Assert.That(trajectory.SurveyStationList.Select(s => s.BoreholeRadius), Is.EqualTo(new double?[] { 0.2, 0.2, 0.35, null }));
            Assert.That(TrajectoryManager.HasCompleteBoreholeRadii(trajectory.SurveyStationList), Is.False);
        });
    }

    [Test]
    public async Task MissingArchitectureKeepsUnknownNullAndKnownValuesIntact()
    {
        var trajectory = new TrajectoryModel { WellBoreID = Guid.NewGuid(), SurveyStationList = [
            new SurveyStation { MD = 10, BoreholeRadius = 0 }, new SurveyStation { MD = 20, BoreholeRadius = 0.2 }
        ] };
        int count = await TrajectoryManager.PrepareBoreholeRadiiAsync(trajectory, _ => Task.FromResult<WellBoreArchitecture?>(null));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(trajectory.SurveyStationList.Select(s => s.BoreholeRadius), Is.EqualTo(new double?[] { null, 0.2 }));
        Assert.That(TrajectoryManager.HasCompleteBoreholeRadii(trajectory.SurveyStationList), Is.False);
    }

    [Test]
    public async Task CompletePositiveRadiiDoNotFetchArchitecture()
    {
        var trajectory = new TrajectoryModel { WellBoreID = Guid.NewGuid(), SurveyStationList = [
            new SurveyStation { MD = 10, BoreholeRadius = 0.2 }, new SurveyStation { MD = 20, BoreholeRadius = 0.3 }
        ] };
        int count = await TrajectoryManager.PrepareBoreholeRadiiAsync(trajectory, _ => throw new InvalidOperationException("Unexpected lookup"));
        Assert.That(count, Is.EqualTo(2));
        Assert.That(TrajectoryManager.HasCompleteBoreholeRadii(trajectory.SurveyStationList), Is.True);
    }

    [Test]
    public void NewStationChunksPersistUnknownAsNullAndPositiveValuesUnchanged()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "radius-test-" + Guid.NewGuid().ToString("N") + ".db");
        var database = new SqlConnectionManagerTrajectory(path, NullLogger<SqlConnectionManagerTrajectory>.Instance);
        Guid owner = Guid.NewGuid();
        using (var connection = database.GetConnection()!)
        using (var transaction = connection.BeginTransaction())
        {
            Assert.That(SurveyStationChunkStore.ReplaceChunks(connection, transaction, owner, "Trajectory", [
                new SurveyStation { MD = 1, BoreholeRadius = 0 }, new SurveyStation { MD = 2, BoreholeRadius = null },
                new SurveyStation { MD = 3, BoreholeRadius = 0.25 }
            ]), Is.True);
            transaction.Commit();
        }
        var stored = SurveyStationChunkStore.GetStations(NullLogger.Instance, database, owner, "Trajectory");
        Assert.That(stored!.Select(s => s.BoreholeRadius), Is.EqualTo(new double?[] { null, null, 0.25 }));
    }

    [TestCase(null)]
    [TestCase(0.0)]
    public void UnknownRadiusLeavesClearanceUnresolvedWhileRetainingCenterDistance(double? radius)
    {
        List<SurveyStation> reference = [Station(0, 0, radius), Station(10, 0, radius)];
        List<SurveyStation> comparison = [Station(0, 10, 0.2), Station(10, 10, 0.2)];
        var result = CenterlineMinimumDistanceEngine.Calculate(reference,
            [new CenterlineComparisonSource(Guid.NewGuid(), comparison)], true, 4, 8).Single();
        Assert.That(result.CenterToCenterDistance, Is.EqualTo(10).Within(1e-6));
        Assert.That(result.ReferenceBoreholeDiameter, Is.Null);
        Assert.That(result.ComparisonBoreholeDiameter, Is.EqualTo(0.4).Within(1e-6));
        Assert.That(result.ClearanceDistance, Is.Null);
    }

    private static SurveyStation Station(double md, double east, double? radius) => new()
    {
        MD = md, Abscissa = md, RiemannianNorth = 0, RiemannianEast = east, TVD = md,
        Inclination = 0, Azimuth = 0, BoreholeRadius = radius
    };
}
