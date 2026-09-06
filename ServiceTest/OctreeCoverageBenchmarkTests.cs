using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Octree;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[Explicit("Read-only benchmark against the development Trajectory API; run deliberately, not in CI.")]
[NonParallelizable]
public sealed class OctreeCoverageBenchmarkTests
{
    private const string TrajectoryApi = "https://dev.digiwells.no/Trajectory/api/";
    private const double EarthRadiusMeters = 6_000_000.0;
    private const double MinLatitude = -Math.PI / 2.0;
    private const double MaxLatitude = Math.PI / 2.0;
    private const double MinLongitude = -Math.PI;
    private const double MaxLongitude = Math.PI;
    private const double MinTvd = -6_000_000.0;
    private const double MaxTvd = 34_000_000.0;

    [Test]
    public void Padded_solid_covers_detect_containment_and_upper_end_entry()
    {
        WithManager(manager =>
        {
            foreach (int depth in new[] { 20, 21, 22, 23 })
            {
                HashSet<OctreeCodeLong> outer = BuildSolidAabbCover(
                    manager, CreateHorizontalTrajectory(0.0, 0.0, 300.0, 30.0), depth);
                HashSet<OctreeCodeLong> contained = BuildSolidAabbCover(
                    manager, CreateHorizontalTrajectory(50.0, 0.0, 200.0, 2.0), depth);
                HashSet<OctreeCodeLong> enclosing = BuildSolidAabbCover(
                    manager, CreateHorizontalTrajectory(50.0, 0.0, 200.0, 30.0), depth);
                HashSet<OctreeCodeLong> entering = BuildSolidAabbCover(
                    manager, CreateHorizontalTrajectory(0.0, 0.0, 200.0, 2.0), depth);

                Assert.Multiple(() =>
                {
                    Assert.That(outer.Overlaps(contained), Is.True, $"Containment was missed at depth {depth}.");
                    Assert.That(enclosing.Overlaps(entering), Is.True, $"Upper-end entry was missed at depth {depth}.");
                });
            }
        });
    }

    [Test]
    public async Task Compare_production_solid_cover_with_alternative_depths()
    {
        using HttpClient client = new() { BaseAddress = new Uri(TrajectoryApi), Timeout = TimeSpan.FromMinutes(5) };
        List<Guid> ids = await client.GetFromJsonAsync<List<Guid>>("Trajectory", JsonSettings.Options) ?? [];
        Assert.That(ids, Is.Not.Empty, "The development Trajectory API returned no trajectories.");

        List<(Guid Id, TrajectoryModel Value)> trajectories = [];
        foreach (Guid id in ids)
        {
            TrajectoryModel? trajectory = await client.GetFromJsonAsync<TrajectoryModel>($"Trajectory/{id}", JsonSettings.Options);
            if (trajectory == null)
            {
                continue;
            }

            if (trajectory.SurveyStationList is not { Count: > 1 })
            {
                int chunkCount = await client.GetFromJsonAsync<int>($"Trajectory/{id}/SurveyStations/ChunkCount", JsonSettings.Options);
                List<SurveyStation> stations = [];
                for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
                {
                    SurveyStationChunk? chunk = await client.GetFromJsonAsync<SurveyStationChunk>(
                        $"Trajectory/{id}/SurveyStations/Chunks/{chunkIndex}", JsonSettings.Options);
                    if (chunk?.SurveyStationList is { Count: > 0 })
                    {
                        stations.AddRange(chunk.SurveyStationList);
                    }
                }
                trajectory.SurveyStationList = stations;
            }

            if (trajectory.SurveyStationList is { Count: >= 3 })
            {
                trajectories.Add((id, trajectory));
            }
        }

        Assert.That(trajectories, Is.Not.Empty, "No trajectory had enough stations for envelope generation.");

        WithManager(manager =>
        {
            Dictionary<Guid, List<OctreeCodeLong>> productionCovers = [];
            Dictionary<int, Dictionary<Guid, HashSet<OctreeCodeLong>>> solidCovers = new()
            {
                [20] = [],
                [21] = [],
                [22] = [],
                [23] = []
            };
            Dictionary<int, long> elapsedMilliseconds = solidCovers.Keys.ToDictionary(depth => depth, _ => 0L);

            Stopwatch productionWatch = Stopwatch.StartNew();
            foreach ((Guid id, TrajectoryModel trajectory) in trajectories)
            {
                List<SurveyStation> source = CloneStations(trajectory.SurveyStationList!);
                List<OctreeCodeLong> cover = manager.GetLeavesFromSurveyList(source);
                if (cover.Count > 0)
                {
                    productionCovers[id] = cover;
                }
                TestContext.Progress.WriteLine($"Production depth {manager.OctreeDepthDetails} {productionCovers.Count}/{trajectories.Count}: {id}, {cover.Count:N0} codes");
            }
            productionWatch.Stop();

            foreach (int depth in solidCovers.Keys)
            {
                Stopwatch watch = Stopwatch.StartNew();
                foreach ((Guid id, TrajectoryModel trajectory) in trajectories)
                {
                    HashSet<OctreeCodeLong> cover = BuildSolidAabbCover(
                        manager,
                        CloneStations(trajectory.SurveyStationList!),
                        depth);
                    cover = new HashSet<OctreeCodeLong>(
                        OctreeManager.CompactLeafCodes(
                            cover,
                            depth,
                            Math.Min(depth, SqlConnectionManagerOctree.OctreeDepthCache)),
                        OctreeCodeComparer.Instance);
                    if (cover.Count > 0)
                    {
                        solidCovers[depth][id] = cover;
                    }
                    TestContext.Progress.WriteLine(
                        $"Solid depth {depth} {solidCovers[depth].Count}/{trajectories.Count}: {id}, {cover.Count:N0} codes");
                }
                watch.Stop();
                elapsedMilliseconds[depth] = watch.ElapsedMilliseconds;
            }

            PersistedIndexMetrics productionMetrics = MeasurePersistedIndex(
                productionCovers.ToDictionary(pair => pair.Key, pair => (IReadOnlyCollection<OctreeCodeLong>)pair.Value));
            HashSet<(Guid Left, Guid Right)> productionPairs = productionMetrics.Pairs;
            TestContext.Progress.WriteLine($"Trajectories returned: {ids.Count}; benchmarked: {productionCovers.Count}");
            TestContext.Progress.WriteLine(
                $"Production solid AABB depth {manager.OctreeDepthDetails}: {productionCovers.Values.Sum(value => value.Count):N0} compacted codes, " +
                $"{productionMetrics.BucketMemberships:N0} bucket memberships, {productionMetrics.DatabaseBytes / 1024.0:F0} KiB database, " +
                $"{productionWatch.ElapsedMilliseconds:N0} ms generation, {productionMetrics.InsertMilliseconds:N0} ms insertion, " +
                $"{productionMetrics.SearchMilliseconds:N0} ms persisted pair search, {productionPairs.Count:N0} candidate pairs");

            foreach (int depth in solidCovers.Keys.Order())
            {
                Dictionary<Guid, HashSet<OctreeCodeLong>> covers = solidCovers[depth];
                PersistedIndexMetrics metrics = MeasurePersistedIndex(
                    covers.ToDictionary(pair => pair.Key, pair => (IReadOnlyCollection<OctreeCodeLong>)pair.Value));
                HashSet<(Guid Left, Guid Right)> coverPairs = metrics.Pairs;
                int additionalPairs = coverPairs.Count(pair => !productionPairs.Contains(pair));
                int missingPairs = productionPairs.Count(pair => !coverPairs.Contains(pair));
                double codeRatio = productionCovers.Values.Sum(value => value.Count) == 0
                    ? 0.0
                    : (double)covers.Values.Sum(value => value.Count) / productionCovers.Values.Sum(value => value.Count);
                TestContext.Progress.WriteLine(
                    $"Solid AABB depth {depth}: {covers.Values.Sum(value => value.Count):N0} codes " +
                    $"({codeRatio:F2}x production depth {manager.OctreeDepthDetails}), {metrics.BucketMemberships:N0} bucket memberships, " +
                    $"{metrics.DatabaseBytes / 1024.0:F0} KiB database, {elapsedMilliseconds[depth]:N0} ms generation, " +
                    $"{metrics.InsertMilliseconds:N0} ms insertion, {metrics.SearchMilliseconds:N0} ms persisted pair search, " +
                    $"{coverPairs.Count:N0} candidate pairs, {additionalPairs:N0} additional and " +
                    $"{missingPairs:N0} missing versus production depth {manager.OctreeDepthDetails}");
            }

            Assert.That(productionCovers, Is.Not.Empty);
            Assert.That(solidCovers.Values.All(values => values.Count > 0), Is.True);
        });
    }

    private static HashSet<OctreeCodeLong> BuildSolidAabbCover(
        OctreeManager manager,
        List<SurveyStation> stations,
        int depth)
    {
        double latitudeCellSize = (MaxLatitude - MinLatitude) / Math.Pow(2.0, depth);
        double longitudeCellSize = (MaxLongitude - MinLongitude) / Math.Pow(2.0, depth);
        double verticalCellSize = (MaxTvd - MinTvd) / Math.Pow(2.0, depth);
        double targetSpacing = 0.5 * Math.Min(
            EarthRadiusMeters * latitudeCellSize,
            Math.Min(EarthRadiusMeters * longitudeCellSize, verticalCellSize));

        bool built = PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseListWithAdaptiveSectorCount(
            stations,
            PerpendicularEllipseEnvelopeBuilder.InferErrorModelType(stations),
            OctreeManager.ConfidenceFactor,
            1.0,
            targetSpacing,
            36,
            240,
            null,
            targetSpacing,
            out List<UncertaintyEllipse>? ellipses,
            out _);
        if (!built || ellipses is not { Count: > 1 })
        {
            return [];
        }

        HashSet<OctreeCodeLong> result = [];
        for (int i = 0; i < ellipses.Count - 1; i++)
        {
            IEnumerable<SurveyPoint> points = (ellipses[i].EllipseVertices ?? [])
                .Concat(ellipses[i + 1].EllipseVertices ?? []);
            List<SurveyPoint> bounded = points
                .Where(point => point.Latitude.HasValue && point.Longitude.HasValue && point.TVD.HasValue)
                .ToList();
            if (bounded.Count == 0)
            {
                continue;
            }

            AddAabbCells(
                manager,
                bounded.Min(point => point.Latitude!.Value),
                bounded.Max(point => point.Latitude!.Value),
                bounded.Min(point => point.Longitude!.Value),
                bounded.Max(point => point.Longitude!.Value),
                bounded.Min(point => point.TVD!.Value),
                bounded.Max(point => point.TVD!.Value),
                depth,
                latitudeCellSize,
                longitudeCellSize,
                verticalCellSize,
                result);
        }
        return result;
    }

    private static void AddAabbCells(
        OctreeManager manager,
        double minLatitude,
        double maxLatitude,
        double minLongitude,
        double maxLongitude,
        double minTvd,
        double maxTvd,
        int depth,
        double latitudeCellSize,
        double longitudeCellSize,
        double verticalCellSize,
        HashSet<OctreeCodeLong> result)
    {
        int cellCount = 1 << depth;
        int minX = Math.Max(0, ToCellIndex(minLatitude, MinLatitude, latitudeCellSize, cellCount) - 1);
        int maxX = Math.Min(cellCount - 1, ToCellIndex(maxLatitude, MinLatitude, latitudeCellSize, cellCount) + 1);
        int minY = Math.Max(0, ToCellIndex(minLongitude, MinLongitude, longitudeCellSize, cellCount) - 1);
        int maxY = Math.Min(cellCount - 1, ToCellIndex(maxLongitude, MinLongitude, longitudeCellSize, cellCount) + 1);
        int minZ = Math.Max(0, ToCellIndex(minTvd, MinTvd, verticalCellSize, cellCount) - 1);
        int maxZ = Math.Min(cellCount - 1, ToCellIndex(maxTvd, MinTvd, verticalCellSize, cellCount) + 1);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            if (manager.TryCreateOctreeCode(
                MinLatitude + (x + 0.5) * latitudeCellSize,
                MinLongitude + (y + 0.5) * longitudeCellSize,
                MinTvd + (z + 0.5) * verticalCellSize,
                depth,
                out OctreeCodeLong code))
            {
                result.Add(code);
            }
        }
    }

    private static int ToCellIndex(double value, double minimum, double cellSize, int cellCount) =>
        Math.Clamp((int)Math.Floor((value - minimum) / cellSize), 0, cellCount - 1);

    private static PersistedIndexMetrics MeasurePersistedIndex(
        Dictionary<Guid, IReadOnlyCollection<OctreeCodeLong>> values)
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-octree-persisted-benchmark", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "GlobalAntiCollision.db");
        try
        {
            var connectionManager = new SqlConnectionManagerOctree(path, NullLogger<SqlConnectionManagerOctree>.Instance);
            var manager = new OctreeManager(NullLogger<OctreeManager>.Instance, connectionManager);
            Stopwatch insertWatch = Stopwatch.StartNew();
            foreach ((Guid id, IReadOnlyCollection<OctreeCodeLong> codes) in values)
            {
                Assert.That(manager.Add(codes.ToList(), id, false, true, true), Is.True);
            }
            insertWatch.Stop();

            long bucketMemberships = values.Keys.Sum(id => manager.GetStatus(new TrajectoryModel
            {
                MetaInfo = new MetaInfo { ID = id },
                TrajectoryType = Model.TrajectoryType.Actual,
                IsDefinitive = true
            }).BucketCount);

            HashSet<(Guid Left, Guid Right)> pairs = [];
            Stopwatch searchWatch = Stopwatch.StartNew();
            foreach ((Guid id, IReadOnlyCollection<OctreeCodeLong> codes) in values)
            {
                foreach (Guid comparisonId in manager.Search(codes.ToList(), Model.TrajectoryType.Actual, true, id))
                {
                    pairs.Add(OrderPair(id, comparisonId));
                }
            }
            searchWatch.Stop();

            long databaseBytes;
            using (SqliteConnection connection = new($"Data Source={path}"))
            {
                connection.Open();
                using SqliteCommand pageCountCommand = connection.CreateCommand();
                pageCountCommand.CommandText = "PRAGMA page_count";
                long pageCount = Convert.ToInt64(pageCountCommand.ExecuteScalar());
                using SqliteCommand pageSizeCommand = connection.CreateCommand();
                pageSizeCommand.CommandText = "PRAGMA page_size";
                long pageSize = Convert.ToInt64(pageSizeCommand.ExecuteScalar());
                databaseBytes = pageCount * pageSize;
            }

            return new PersistedIndexMetrics(
                bucketMemberships,
                databaseBytes,
                insertWatch.ElapsedMilliseconds,
                searchWatch.ElapsedMilliseconds,
                pairs);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static (Guid Left, Guid Right) OrderPair(Guid left, Guid right) =>
        left.CompareTo(right) <= 0 ? (left, right) : (right, left);

    private sealed record PersistedIndexMetrics(
        long BucketMemberships,
        long DatabaseBytes,
        long InsertMilliseconds,
        long SearchMilliseconds,
        HashSet<(Guid Left, Guid Right)> Pairs);

    private sealed class OctreeCodeComparer : IEqualityComparer<OctreeCodeLong>
    {
        public static readonly OctreeCodeComparer Instance = new();

        public bool Equals(OctreeCodeLong left, OctreeCodeLong right) =>
            left.Depth == right.Depth && left.CodeHigh == right.CodeHigh && left.CodeLow == right.CodeLow;

        public int GetHashCode(OctreeCodeLong value) =>
            HashCode.Combine(value.Depth, value.CodeHigh, value.CodeLow);
    }

    private static List<SurveyStation> CloneStations(IEnumerable<SurveyStation> stations) =>
        stations.Select(station => new SurveyStation(station)).ToList();

    private static List<SurveyStation> CreateHorizontalTrajectory(
        double startNorth,
        double startEast,
        double length,
        double radius)
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
            CreateStation(0.0, startNorth, startEast, radius, instrument),
            CreateStation(length / 2.0, null, null, radius, instrument),
            CreateStation(length, null, null, radius, instrument)
        ];
    }

    private static SurveyStation CreateStation(
        double md,
        double? north,
        double? east,
        double radius,
        SurveyInstrument instrument) => new()
    {
        MD = md,
        RiemannianNorth = north,
        RiemannianEast = east,
        TVD = md == 0.0 ? 0.0 : null,
        Inclination = Math.PI / 2.0,
        Azimuth = 0.0,
        BoreholeRadius = radius,
        SurveyTool = instrument
    };

    private static void WithManager(Action<OctreeManager> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "trajectory-octree-benchmark", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "GlobalAntiCollision.db");
            var connection = new SqlConnectionManagerOctree(path, NullLogger<SqlConnectionManagerOctree>.Instance);
            test(new OctreeManager(NullLogger<OctreeManager>.Instance, connection));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
