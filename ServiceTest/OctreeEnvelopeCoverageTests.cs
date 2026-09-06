using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Octree;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class OctreeEnvelopeCoverageTests
{
    [Test]
    public void Generated_codes_use_depth_22_and_never_compact_below_the_cache_depth()
    {
        WithManager(manager =>
        {
            List<OctreeCodeLong> codes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(0.0, 0.0, 0.0, 300.0, 30.0));

            Assert.Multiple(() =>
            {
                Assert.That(manager.OctreeDepthDetails, Is.EqualTo(22));
                Assert.That(codes, Is.Not.Empty);
                Assert.That(codes.All(code => code.Depth >= SqlConnectionManagerOctree.OctreeDepthCache), Is.True);
                Assert.That(codes.All(code => code.Depth <= manager.OctreeDepthDetails), Is.True);
            });
        });
    }

    [Test]
    public void Crossing_uncertainty_tubes_are_returned_as_candidates()
    {
        WithManager(manager =>
        {
            List<OctreeCodeLong> referenceCodes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(-73.0, 0.0, 0.0, 200.0, 5.0));
            List<OctreeCodeLong> comparisonCodes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(0.0, -117.0, Math.PI / 2.0, 200.0, 5.0));
            Guid referenceId = Guid.NewGuid();
            Guid comparisonId = Guid.NewGuid();

            Assert.Multiple(() =>
            {
                Assert.That(referenceCodes, Is.Not.Empty);
                Assert.That(comparisonCodes, Is.Not.Empty);
                Assert.That(manager.Add(referenceCodes, referenceId, false, true, true), Is.True);
                Assert.That(manager.Add(comparisonCodes, comparisonId, false, true, true), Is.True);
            });

            Assert.That(
                manager.Search(referenceCodes, Model.TrajectoryType.Actual, true, referenceId),
                Does.Contain(comparisonId));
        });
    }

    [Test]
    public void Solid_index_detects_complete_concentric_containment()
    {
        WithManager(manager =>
        {
            List<OctreeCodeLong> outerCodes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(0.0, 0.0, 0.0, 300.0, 30.0));
            List<OctreeCodeLong> innerCodes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(50.0, 0.0, 0.0, 200.0, 2.0));
            Guid outerId = Guid.NewGuid();
            Guid innerId = Guid.NewGuid();

            Assert.Multiple(() =>
            {
                Assert.That(outerCodes, Is.Not.Empty);
                Assert.That(innerCodes, Is.Not.Empty);
                Assert.That(manager.Add(outerCodes, outerId, false, true, true), Is.True);
                Assert.That(manager.Add(innerCodes, innerId, false, true, true), Is.True);
            });

            Assert.That(
                manager.Search(outerCodes, Model.TrajectoryType.Actual, true, outerId),
                Does.Contain(innerId));
        });
    }

    [Test]
    public void Solid_index_detects_entry_through_the_upper_end()
    {
        WithManager(manager =>
        {
            List<OctreeCodeLong> enclosingCodes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(50.0, 0.0, 0.0, 200.0, 30.0));
            List<OctreeCodeLong> enteringCodes = manager.GetLeavesFromSurveyList(
                CreateHorizontalTrajectory(0.0, 0.0, 0.0, 200.0, 2.0));
            Guid enclosingId = Guid.NewGuid();
            Guid enteringId = Guid.NewGuid();

            Assert.Multiple(() =>
            {
                Assert.That(enclosingCodes, Is.Not.Empty);
                Assert.That(enteringCodes, Is.Not.Empty);
                Assert.That(manager.Add(enclosingCodes, enclosingId, false, true, true), Is.True);
                Assert.That(manager.Add(enteringCodes, enteringId, false, true, true), Is.True);
            });

            Assert.That(
                manager.Search(enclosingCodes, Model.TrajectoryType.Actual, true, enclosingId),
                Does.Contain(enteringId));
        });
    }

    [Test]
    public void Relevant_md_range_spans_the_depths_between_separate_overlap_regions()
    {
        List<SurveyStation> reference = CreateHorizontalTrajectory(
            0.0, 0.0, 0.0, 200.0, 15.0, 1.0, 15.0);
        List<SurveyStation> comparison = CreateHorizontalTrajectory(
            0.0, 20.0, 0.0, 200.0, 15.0, 1.0, 15.0);

        bool found = RelevantMdRangeCalculator.TryGetRelevantMdRanges(
            reference,
            comparison,
            OctreeManager.ConfidenceFactor,
            out MeasuredDepthRange? referenceRange,
            out MeasuredDepthRange? comparisonRange);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(referenceRange, Is.Not.Null);
            Assert.That(comparisonRange, Is.Not.Null);
            Assert.That(referenceRange!.StartMD, Is.LessThanOrEqualTo(0.0));
            Assert.That(referenceRange.EndMD, Is.GreaterThanOrEqualTo(200.0));
            Assert.That(referenceRange.StartMD, Is.LessThan(100.0));
            Assert.That(referenceRange.EndMD, Is.GreaterThan(100.0));
        });
    }

    private static List<SurveyStation> CreateHorizontalTrajectory(
        double startNorth,
        double startEast,
        double azimuth,
        double length,
        double boreholeRadius) => CreateHorizontalTrajectory(
            startNorth,
            startEast,
            azimuth,
            length,
            boreholeRadius,
            boreholeRadius,
            boreholeRadius);

    private static List<SurveyStation> CreateHorizontalTrajectory(
        double startNorth,
        double startEast,
        double azimuth,
        double length,
        double startRadius,
        double middleRadius,
        double endRadius)
    {
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            ReferenceError = 0.0,
            DrillStringMag = 0.0,
            GyroCompassError = 0.0,
            TrueInclination = 0.0,
            Misalignment = 0.0,
            RelDepthError = 0.0
        };

        return
        [
            CreateStation(0.0, startNorth, startEast, azimuth, startRadius, instrument),
            CreateStation(length / 2.0, null, null, azimuth, middleRadius, instrument),
            CreateStation(length, null, null, azimuth, endRadius, instrument)
        ];
    }

    private static SurveyStation CreateStation(
        double md,
        double? north,
        double? east,
        double azimuth,
        double boreholeRadius,
        SurveyInstrument instrument) => new()
    {
        MD = md,
        RiemannianNorth = north,
        RiemannianEast = east,
        TVD = md == 0.0 ? 0.0 : null,
        Inclination = Math.PI / 2.0,
        Azimuth = azimuth,
        BoreholeRadius = boreholeRadius,
        SurveyTool = instrument
    };

    private static void WithManager(Action<OctreeManager> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-octree-envelope", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "GlobalAntiCollision.db");
            var connection = new SqlConnectionManagerOctree(path, NullLogger<SqlConnectionManagerOctree>.Instance);
            var manager = new OctreeManager(NullLogger<OctreeManager>.Instance, connection);
            test(manager);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
