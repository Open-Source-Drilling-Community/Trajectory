using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NORCE.Drilling.GlobalAntiCollision;
using NORCE.Drilling.Trajectory.Service;
using NORCE.Drilling.Trajectory.Service.Controllers;
using NORCE.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Octree;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using GlobalAntiCollisionModel = NORCE.Drilling.GlobalAntiCollision.GlobalAntiCollision;
using SurveyStationChunkModel = NORCE.Drilling.Trajectory.Model.SurveyStationChunk;
using TrajectoryModel = NORCE.Drilling.Trajectory.Model.Trajectory;
using WellBore = NORCE.Drilling.Trajectory.ModelShared.WellBore;
using WellBoreArchitecture = NORCE.Drilling.Trajectory.ModelShared.WellBoreArchitecture;

internal static class Program
{
    private const string ExternalServiceRoot = "https://dev.digiwells.no/";
    private static readonly Guid DuplicateTrajectoryNamespaceId = new("9c64b88e-bfd6-4c87-bf37-27c1eb2fa9f2");
    private static readonly Uri ExternalServiceBaseAddress = new(ExternalServiceRoot);
    private static readonly Uri RemoteTrajectoryBaseAddress = new(ExternalServiceBaseAddress, "Trajectory/api/");
    private static readonly JsonSerializerOptions JsonOptions = JsonSettings.Options;

    public static async Task<int> Main()
    {
        HttpClient? remoteTrajectoryClient = null;
        LocalHarness? harness = null;
        int exitCode;

        bool deleteOctreesAfterRun = false; // Set to true to delete cached octrees at the end of the run.
        int referenceTrajectoryIndex = 0; // Zero-based index into the loaded trajectory list.
        string referenceTrajectoryName = "U3"; // Exact names are preferred; otherwise the first containing name is used.
        List<string> comparisonTrajectoryNameFilters = [""]; // If non-empty, only trajectories whose names contain one of these values will be used as comparisons.
        bool forceSymmetricSeparationFactorCalculation = false; // Set to true for slower two-direction calculations with less direction-dependent minima.
        bool fillBoreholeRadiusFromWellboreArchitecture = true; // Set to false to use the remote trajectory source exactly as returned.
        bool allowBoreholeRadiusArchitectureFallbackByTrajectoryName = false; // Set to true to try similarly named trajectories when a WellBoreID has no architecture. This changes anti-collision inputs.
        bool printSeparationFactorProfilesForPossibleCollisions = true; // Set to false to skip full profile output for comparisons with minimum separation factor between 0.01 and 1.0.

        List<TestTrajectory> trajectories = [];
        string globalAntiCollisionId = Guid.NewGuid().ToString();

        try
        {
            ConfigureRemoteServiceHosts();
            GlobalAntiCollisionModel.UseSymmetricSeparationFactorCalculation = forceSymmetricSeparationFactorCalculation;

            remoteTrajectoryClient = CreateHttpClient(RemoteTrajectoryBaseAddress);

            string runtimeRoot = InitializeLocalRuntimeDirectory();
            harness = CreateLocalHarness();

            trajectories = await LoadTestTrajectoriesAsync(remoteTrajectoryClient);
            if (fillBoreholeRadiusFromWellboreArchitecture)
            {
                await FillBoreholeRadiusFromWellboreArchitectureAsync(trajectories, allowBoreholeRadiusArchitectureFallbackByTrajectoryName);
            }

            referenceTrajectoryIndex = ResolveReferenceTrajectoryIndex(trajectories, referenceTrajectoryIndex, referenceTrajectoryName);

            TestTrajectory referenceTrajectory = trajectories[referenceTrajectoryIndex];

            Console.WriteLine($"Using remote trajectory source: {RemoteTrajectoryBaseAddress}");
            Console.WriteLine($"Using local controller runtime root: {runtimeRoot}");
            Console.WriteLine($"Using local database root: {Path.GetFullPath(SqlConnectionManager.HOME_DIRECTORY)}");
            Console.WriteLine(deleteOctreesAfterRun
                ? "Octree cleanup mode: enabled. Cached octrees will be deleted at the end of the run.\n"
                : "Octree cleanup mode: disabled by default. Cached octrees will be reused across runs.\n");
            Console.WriteLine(forceSymmetricSeparationFactorCalculation
                ? "Separation factor calculation mode: symmetric. Reverse-direction points will be merged into each profile.\n"
                : "Separation factor calculation mode: fast. Results may retain reference-direction sampling asymmetry.\n");
            Console.WriteLine(fillBoreholeRadiusFromWellboreArchitecture
                ? "Borehole radius source: WellBoreArchitecture. Survey stations are hydrated before local test seeding.\n"
                : "Borehole radius source: remote trajectory payload. Survey stations are not modified by the test harness.\n");
            if (fillBoreholeRadiusFromWellboreArchitecture)
            {
                Console.WriteLine(allowBoreholeRadiusArchitectureFallbackByTrajectoryName
                    ? "Borehole radius fallback: enabled. Missing WellBoreID architecture matches may use a similarly named trajectory.\n"
                    : "Borehole radius fallback: disabled. Missing WellBoreID architecture matches are left unchanged.\n");
            }
            Console.WriteLine(printSeparationFactorProfilesForPossibleCollisions
                ? "Separation factor profile printing: enabled for comparison trajectories with minimum separation factor in (0.01, 1.0).\n"
                : "Separation factor profile printing: disabled.\n");
            Console.WriteLine($"Reference trajectory name filter: \"{referenceTrajectoryName}\"");
            Console.WriteLine(comparisonTrajectoryNameFilters.Count > 0
                ? $"Comparison trajectory name filters: {string.Join(", ", comparisonTrajectoryNameFilters.Select(filter => $"\"{filter}\""))}"
                : "Comparison trajectory name filters: <none>");
            Console.WriteLine($"Reference trajectory: {FormatTrajectoryLabel(referenceTrajectory, includeGuid: true)}");
            Console.WriteLine("Using the following trajectories for the local test run:");
            for (int i = 0; i < trajectories.Count; i++)
            {
                TestTrajectory trajectory = trajectories[i];
                string referenceLabel = i == referenceTrajectoryIndex ? " [reference]" : string.Empty;
                Console.WriteLine($"\t[{i}] {FormatTrajectoryLabel(trajectory, includeGuid: true)}{FormatDuplicateLabel(trajectory)}{referenceLabel}");
            }

            SeedLocalTrajectoryDatabase(harness.TrajectoryConnectionManager, harness.TrajectoryManager, trajectories);

            await RunOctreeControllerAndManagerTestAsync(harness.OctreesController, harness.OctreeManager, trajectories, referenceTrajectory);
            await RunGlobalAntiCollisionControllerAndManagerTestAsync(
                harness.GlobalAntiCollisionsController,
                harness.GlobalAntiCollisionManager,
                trajectories,
                referenceTrajectory,
                comparisonTrajectoryNameFilters,
                printSeparationFactorProfilesForPossibleCollisions,
                globalAntiCollisionId);

            if (deleteOctreesAfterRun)
            {
                await RunOctreeControllerAndManagerDeleteTestAsync(harness.OctreesController, harness.OctreeManager, trajectories);
                Console.WriteLine("\tThe next run will need to post all octrees again because the cached octree entries were deleted.");
            }

            Console.WriteLine("All in-process controller/manager tests completed successfully.");
            exitCode = 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Test run failed.");
            Console.Error.WriteLine(ex);
            exitCode = 1;
        }
        finally
        {
            if (harness != null && !string.IsNullOrWhiteSpace(globalAntiCollisionId))
            {
                harness.GlobalAntiCollisionManager.Remove(globalAntiCollisionId);
            }

            remoteTrajectoryClient?.Dispose();
        }

        Environment.Exit(exitCode);
        return exitCode;
    }

    private static async Task RunOctreeControllerAndManagerTestAsync(
        OctreesController octreesController,
        OctreeManager octreeManager,
        IReadOnlyList<TestTrajectory> trajectories,
        TestTrajectory referenceTrajectory)
    {
        Console.WriteLine("Running in-process OctreeManager + OctreesController test...");

        HashSet<Guid> expectedTrajectoryIds = trajectories.Select(trajectory => trajectory.Id).ToHashSet();
        List<Guid> existingOctreeIds = octreesController.Get().ToList();
        List<Guid> staleOctreeIds = existingOctreeIds
            .Where(id => !expectedTrajectoryIds.Contains(id))
            .ToList();

        if (staleOctreeIds.Count > 0)
        {
            Console.WriteLine($"\tRemoving {staleOctreeIds.Count} stale octree entries that are not part of the current trajectory set...");
            foreach (Guid staleOctreeId in staleOctreeIds)
            {
                octreesController.Delete(staleOctreeId);
            }
        }

        Console.WriteLine("\tChecking whether each trajectory already exists in the octree cache...");
        Stopwatch postStopwatch = Stopwatch.StartNew();
        foreach (TestTrajectory trajectory in trajectories)
        {
            EnsureTrajectoryPostedToOctree(octreesController, octreeManager, trajectory);
        }
        postStopwatch.Stop();
        Console.WriteLine($"\tFinished octree cache verification in {postStopwatch.Elapsed.TotalSeconds:F0} s.\n");

        List<Guid> idsFromController = octreesController.Get().ToList();
        foreach (TestTrajectory trajectory in trajectories)
        {
            Ensure(idsFromController.Contains(trajectory.Id), $"OctreesController GET should include {FormatTrajectoryLabel(trajectory)}.");
            Ensure(octreeManager.Contains(trajectory.Id), $"OctreeManager should contain {FormatTrajectoryLabel(trajectory)}.");
        }

        List<OctreeCodeLong> storedReferenceCodes = octreeManager.Get(referenceTrajectory.Id);
        Ensure(storedReferenceCodes.Count > 0, $"OctreeManager should return stored codes for {FormatTrajectoryLabel(referenceTrajectory)}.");

        List<Guid> searchResults = octreeManager.Search(storedReferenceCodes, false, true, true, referenceTrajectory.Id);
        Dictionary<Guid, TestTrajectory> trajectoryLookup = trajectories.ToDictionary(trajectory => trajectory.Id);
        Console.WriteLine($"\tOctreeManager overlap search using reference trajectory {FormatTrajectoryLabel(referenceTrajectory)} returned {searchResults.Count} trajectories.");

        foreach (Guid trajectoryId in searchResults)
        {
            if (trajectoryLookup.TryGetValue(trajectoryId, out TestTrajectory trajectory))
            {
                Console.WriteLine($"\t{FormatTrajectoryLabel(trajectory)} overlaps with the reference trajectory.");
            }
            else
            {
                Console.WriteLine($"\tTrajectory {trajectoryId} overlaps with the reference trajectory.");
            }
        }

        List<OctreeCodeLong> details = octreesController.Get(referenceTrajectory.Id);
        Ensure(details.Count > 0, $"OctreesController GET by id should return the stored code list for {FormatTrajectoryLabel(referenceTrajectory)}.");

        Console.WriteLine("In-process octree controller/manager test passed.\n");
        await Task.CompletedTask;
    }

    private static async Task RunOctreeControllerAndManagerDeleteTestAsync(
        OctreesController octreesController,
        OctreeManager octreeManager,
        IReadOnlyList<TestTrajectory> trajectories)
    {
        Console.WriteLine("Running in-process OctreeManager + OctreesController Delete test...");
        Stopwatch deleteStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < trajectories.Count; i++)
        {
            TestTrajectory trajectory = trajectories[i];
            Console.WriteLine($"\tDeleting octree entry {i + 1}/{trajectories.Count}: {FormatTrajectoryLabel(trajectory)}...");
            Stopwatch singleDeleteStopwatch = Stopwatch.StartNew();
            octreesController.Delete(trajectory.Id);
            singleDeleteStopwatch.Stop();
            Ensure(!octreeManager.Contains(trajectory.Id), $"OctreesController DELETE should remove {FormatTrajectoryLabel(trajectory)} from the manager view.");

            List<Guid> idsAfterDelete = octreesController.Get().ToList();
            Ensure(!idsAfterDelete.Contains(trajectory.Id), $"OctreesController DELETE should remove {FormatTrajectoryLabel(trajectory)}.");

            foreach (TestTrajectory remainingTrajectory in trajectories.Skip(i + 1))
            {
                Ensure(idsAfterDelete.Contains(remainingTrajectory.Id),
                    $"OctreesController DELETE should leave {FormatTrajectoryLabel(remainingTrajectory)} untouched until it is deleted.");
            }

            Console.WriteLine($"\tFinished deleting {FormatTrajectoryLabel(trajectory)} in {singleDeleteStopwatch.Elapsed.TotalSeconds:F2} s.");
        }

        deleteStopwatch.Stop();
        Console.WriteLine($"\tFinished OctreeManager + OctreesController Delete test in {deleteStopwatch.Elapsed.TotalSeconds:F2} s.");
        Console.WriteLine("In-process octree controller/manager Delete test passed.\n");
        await Task.CompletedTask;
    }

    private static async Task RunGlobalAntiCollisionControllerAndManagerTestAsync(
        GlobalAntiCollisionsController globalAntiCollisionsController,
        GlobalAntiCollisionManager globalAntiCollisionManager,
        IReadOnlyList<TestTrajectory> trajectories,
        TestTrajectory referenceTrajectory,
        IReadOnlyList<string> comparisonTrajectoryNameFilters,
        bool printSeparationFactorProfilesForPossibleCollisions,
        string globalAntiCollisionId)
    {
        Console.WriteLine("Running in-process GlobalAntiCollisionManager + GlobalAntiCollisionsController test...");

        Dictionary<Guid, TestTrajectory> trajectoryLookup = trajectories.ToDictionary(trajectory => trajectory.Id);
        List<TestTrajectory> configuredComparisonTrajectories = trajectories
            .Where(trajectory => trajectory.Id != referenceTrajectory.Id)
            .Where(trajectory => MatchesComparisonTrajectoryNameFilter(trajectory, comparisonTrajectoryNameFilters))
            .ToList();

        if (comparisonTrajectoryNameFilters.Count > 0)
        {
            if (configuredComparisonTrajectories.Count > 0)
            {
                Console.WriteLine("\tUsing the following comparison trajectories after name filtering:");
                foreach (TestTrajectory comparisonTrajectory in configuredComparisonTrajectories)
                {
                    Console.WriteLine($"\t\t{FormatTrajectoryLabel(comparisonTrajectory, includeGuid: true)}");
                }
            }
            else
            {
                Console.WriteLine("\tNo comparison trajectories matched the configured name filters.");
            }
        }

        GlobalAntiCollisionModel postPayload = CreateGlobalAntiCollision(globalAntiCollisionId, referenceTrajectory, configuredComparisonTrajectories, 0.999);
        Console.WriteLine($"\tStarting GlobalAntiCollisionsController.Post for reference trajectory {FormatTrajectoryLabel(referenceTrajectory)}...");
        Stopwatch postStopwatch = Stopwatch.StartNew();
        await globalAntiCollisionsController.Post(postPayload);
        postStopwatch.Stop();
        Console.WriteLine($"\tFinished GlobalAntiCollisionsController.Post in {postStopwatch.Elapsed.TotalSeconds:F2} s.");

        Ensure(globalAntiCollisionManager.Contains(globalAntiCollisionId), "GlobalAntiCollisionManager should contain the inserted payload.");

        GlobalAntiCollisionModel? storedByManager = globalAntiCollisionManager.Get(globalAntiCollisionId);
        Ensure(storedByManager != null, "GlobalAntiCollisionManager should retrieve the inserted payload.");
        Ensure(storedByManager!.ReferenceTrajectoryID == referenceTrajectory.Id,
            $"Stored GlobalAntiCollision payload should keep the reference trajectory id for {FormatTrajectoryLabel(referenceTrajectory)}.");
        ValidateComparisonResults(storedByManager, trajectoryLookup, $"\tStored by manager (confidence factor {storedByManager.ConfidenceFactor:F3})");
        await ValidateSidetrackTieInFilteringAsync(
            storedByManager,
            referenceTrajectory,
            configuredComparisonTrajectories);

        List<string> ids = globalAntiCollisionsController.Get().ToList();
        Ensure(ids.Contains(globalAntiCollisionId), "GlobalAntiCollisionsController GET should include the inserted id.");

        GlobalAntiCollisionModel? storedByController = globalAntiCollisionsController.Get(globalAntiCollisionId);
        Ensure(storedByController != null, "GlobalAntiCollisionsController GET by id should return the stored payload.");
        Ensure(Math.Abs(storedByController!.ConfidenceFactor - postPayload.ConfidenceFactor) < 1e-9,
            "GlobalAntiCollisionsController GET by id should return the stored confidence factor.");
        Ensure(storedByController.ReferenceTrajectoryID == referenceTrajectory.Id,
            $"GlobalAntiCollisionsController GET by id should return the reference trajectory id for {FormatTrajectoryLabel(referenceTrajectory)}.");
        ValidateComparisonResults(storedByController, trajectoryLookup, $"\tStored by controller (confidence factor {storedByController.ConfidenceFactor:F3})");

        bool runUpdate = false;
        if (runUpdate)
        {
            GlobalAntiCollisionModel putPayload = CreateGlobalAntiCollision(globalAntiCollisionId, referenceTrajectory, configuredComparisonTrajectories, 0.95);
            Console.WriteLine();
            Console.WriteLine($"\tStarting GlobalAntiCollisionsController.Put for reference trajectory {FormatTrajectoryLabel(referenceTrajectory)}...");
            Stopwatch putStopwatch = Stopwatch.StartNew();
            await globalAntiCollisionsController.Put(globalAntiCollisionId, putPayload);
            putStopwatch.Stop();
            Console.WriteLine($"\tFinished GlobalAntiCollisionsController.Put in {putStopwatch.Elapsed.TotalSeconds:F2} s.");

            GlobalAntiCollisionModel? updatedByManager = globalAntiCollisionManager.Get(globalAntiCollisionId);
            Ensure(updatedByManager != null && Math.Abs(updatedByManager.ConfidenceFactor - putPayload.ConfidenceFactor) < 1e-9,
                "GlobalAntiCollisionManager should observe the updated payload.");

            GlobalAntiCollisionModel? updatedByController = globalAntiCollisionsController.Get(globalAntiCollisionId);
            Ensure(updatedByController != null, "GlobalAntiCollisionsController GET by id should return the updated payload.");
            Ensure(Math.Abs(updatedByController!.ConfidenceFactor - putPayload.ConfidenceFactor) < 1e-9,
                "GlobalAntiCollisionsController PUT should update the confidence factor.");
            ValidateComparisonResults(updatedByController, trajectoryLookup, $"\tUpdated by controller (confidence factor {updatedByController.ConfidenceFactor:F3})");
            EnsureUpdatedProfilesImproved(
                storedByController.SeparationFactorResults,
                updatedByController.SeparationFactorResults,
                storedByController.ConfidenceFactor,
                updatedByController.ConfidenceFactor,
                trajectoryLookup);
        }

        PrintPossibleCollisionSummary(storedByController, trajectoryLookup, 1.0);
        if (printSeparationFactorProfilesForPossibleCollisions)
        {
            PrintSeparationFactorProfilesForPossibleCollisions(storedByController, trajectoryLookup, 0.01, 1.0);
        }

        globalAntiCollisionsController.Delete(globalAntiCollisionId);
        Ensure(!globalAntiCollisionManager.Contains(globalAntiCollisionId),
            "GlobalAntiCollisionsController DELETE should remove the payload from the manager view.");

        List<string> idsAfterDelete = globalAntiCollisionsController.Get().ToList();
        Ensure(!idsAfterDelete.Contains(globalAntiCollisionId), "GlobalAntiCollisionsController DELETE should remove the payload id.");

        Console.WriteLine("In-process global anti-collision controller/manager test passed.\n");
        await Task.CompletedTask;
    }

    private static GlobalAntiCollisionModel CreateGlobalAntiCollision(
        string id,
        TestTrajectory referenceTrajectory,
        IReadOnlyList<TestTrajectory> comparisonTrajectories,
        double confidenceFactor)
    {
        return new GlobalAntiCollisionModel
        {
            ID = id,
            ConfidenceFactor = confidenceFactor,
            ReferenceTrajectoryID = referenceTrajectory.Id,
            ReferenceWellPathID = Guid.Empty,
            ComparisonTrajectoryIDs = comparisonTrajectories.Select(trajectory => trajectory.Id).ToList(),
            SeparationFactorResults =
            [
            ]
        };
    }

    private static async Task<List<TestTrajectory>> LoadTestTrajectoriesAsync(HttpClient remoteTrajectoryClient)
    {
        List<Guid> remoteIds = await GetJsonAsync<List<Guid>>(remoteTrajectoryClient, "Trajectory");
        Ensure(remoteIds.Count > 0, "No trajectories were found on the remote dev trajectory service.");

        List<TestTrajectory> trajectories = [];
        foreach (Guid remoteId in remoteIds)
        {
            TrajectoryModel trajectory = await GetJsonAsync<TrajectoryModel>(remoteTrajectoryClient, $"Trajectory/{remoteId}");
            await LoadRemoteSurveyStationChunksAsync(remoteTrajectoryClient, remoteId, trajectory);
            trajectories.Add(new TestTrajectory(remoteId, trajectory, false));
        }

        TestTrajectory firstTrajectory = trajectories[0];
        string[] duplicateRoles = ["second", "third"];
        while (trajectories.Count < 3)
        {
            string duplicateRole = duplicateRoles[trajectories.Count - 1];
            trajectories.Add(CreateDuplicateTestTrajectory(firstTrajectory.Id, firstTrajectory.Trajectory, duplicateRole));
        }

        return trajectories;
    }

    private static async Task LoadRemoteSurveyStationChunksAsync(HttpClient remoteTrajectoryClient, Guid remoteId, TrajectoryModel trajectory)
    {
        if (trajectory.SurveyStationList is { Count: > 0 })
        {
            return;
        }

        int chunkCount;
        try
        {
            chunkCount = await GetJsonAsync<int>(remoteTrajectoryClient, $"Trajectory/{remoteId}/SurveyStations/ChunkCount");
        }
        catch
        {
            return;
        }

        if (chunkCount <= 0)
        {
            return;
        }

        List<SurveyStation> stations = [];
        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            SurveyStationChunkModel? chunk = await GetJsonAsync<SurveyStationChunkModel>(
                remoteTrajectoryClient,
                $"Trajectory/{remoteId}/SurveyStations/Chunks/{chunkIndex}");
            if (chunk?.SurveyStationList is { Count: > 0 } chunkStations)
            {
                stations.AddRange(chunkStations);
            }
        }

        if (stations.Count > 0)
        {
            trajectory.SurveyStationList = stations;
        }
    }

    private static async Task FillBoreholeRadiusFromWellboreArchitectureAsync(
        IReadOnlyList<TestTrajectory> trajectories,
        bool allowFallbackByTrajectoryName)
    {
        Console.WriteLine("Filling survey station borehole radii from WellBoreArchitecture...");
        Stopwatch stopwatch = Stopwatch.StartNew();

        ICollection<WellBoreArchitecture> architectures = await APIUtils.ClientWellBoreArchitecture.GetAllWellBoreArchitectureAsync();
        Dictionary<Guid, WellBoreArchitecture> architecturesByWellBoreId = architectures
            .Where(architecture => architecture?.WellBoreID is Guid wellBoreId && wellBoreId != Guid.Empty)
            .GroupBy(architecture => architecture.WellBoreID!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        Dictionary<string, TestTrajectory> architectureFallbackTrajectoriesByName = BuildArchitectureFallbackTrajectoriesByName(
            trajectories,
            architecturesByWellBoreId);

        int filledTrajectoryCount = 0;
        int filledStationCount = 0;
        int missingArchitectureCount = 0;
        int missingIntervalCount = 0;
        int fallbackArchitectureCount = 0;

        foreach (TestTrajectory testTrajectory in trajectories)
        {
            Guid wellBoreId = testTrajectory.Trajectory.WellBoreID;
            if (!TryGetArchitectureForTrajectory(
                testTrajectory,
                architecturesByWellBoreId,
                architectureFallbackTrajectoriesByName,
                allowFallbackByTrajectoryName,
                out WellBoreArchitecture? architecture,
                out TestTrajectory? fallbackTrajectory))
            {
                missingArchitectureCount++;
                Console.WriteLine($"\tNo WellBoreArchitecture found for {FormatTrajectoryLabel(testTrajectory)} with WellBoreID {wellBoreId}.");
                continue;
            }

            if (fallbackTrajectory.HasValue)
            {
                fallbackArchitectureCount++;
                Console.WriteLine(
                    $"\tUsing WellBoreArchitecture from {FormatTrajectoryLabel(fallbackTrajectory.Value)} " +
                    $"for {FormatTrajectoryLabel(testTrajectory)} because WellBoreID {wellBoreId} has no direct architecture match.");
            }

            int filledCount = TrajectoryManager.FillBoreholeRadiusFromArchitecture(testTrajectory.Trajectory, architecture!);
            if (filledCount == 0)
            {
                missingIntervalCount++;
                Console.WriteLine($"\tNo borehole radius intervals matched {FormatTrajectoryLabel(testTrajectory)}.");
                continue;
            }

            filledTrajectoryCount++;
            filledStationCount += filledCount;
        }

        stopwatch.Stop();
        Console.WriteLine(
            $"\tFilled borehole radius for {filledStationCount} survey stations across {filledTrajectoryCount}/{trajectories.Count} trajectories " +
            $"in {stopwatch.Elapsed.TotalSeconds:F2} s.");
        if (missingArchitectureCount > 0 || missingIntervalCount > 0)
        {
            Console.WriteLine($"\tMissing architecture: {missingArchitectureCount}; no matching intervals: {missingIntervalCount}.");
        }
        if (fallbackArchitectureCount > 0)
        {
            Console.WriteLine($"\tFallback architecture matches used: {fallbackArchitectureCount}.");
        }

        Console.WriteLine();
    }

    private static Dictionary<string, TestTrajectory> BuildArchitectureFallbackTrajectoriesByName(
        IReadOnlyList<TestTrajectory> trajectories,
        IReadOnlyDictionary<Guid, WellBoreArchitecture> architecturesByWellBoreId)
    {
        Dictionary<string, TestTrajectory> fallbackTrajectories = [];
        foreach (TestTrajectory trajectory in trajectories)
        {
            if (!architecturesByWellBoreId.ContainsKey(trajectory.Trajectory.WellBoreID))
            {
                continue;
            }

            string key = GetArchitectureFallbackKey(trajectory);
            if (string.IsNullOrWhiteSpace(key) || fallbackTrajectories.ContainsKey(key))
            {
                continue;
            }

            fallbackTrajectories[key] = trajectory;
        }

        return fallbackTrajectories;
    }

    private static bool TryGetArchitectureForTrajectory(
        TestTrajectory testTrajectory,
        IReadOnlyDictionary<Guid, WellBoreArchitecture> architecturesByWellBoreId,
        IReadOnlyDictionary<string, TestTrajectory> architectureFallbackTrajectoriesByName,
        bool allowFallbackByTrajectoryName,
        out WellBoreArchitecture? architecture,
        out TestTrajectory? fallbackTrajectory)
    {
        fallbackTrajectory = null;
        if (architecturesByWellBoreId.TryGetValue(testTrajectory.Trajectory.WellBoreID, out architecture))
        {
            return true;
        }

        if (!allowFallbackByTrajectoryName)
        {
            return false;
        }

        string fallbackKey = GetArchitectureFallbackKey(testTrajectory);
        if (!string.IsNullOrWhiteSpace(fallbackKey) &&
            architectureFallbackTrajectoriesByName.TryGetValue(fallbackKey, out TestTrajectory candidateTrajectory) &&
            candidateTrajectory.Id != testTrajectory.Id &&
            architecturesByWellBoreId.TryGetValue(candidateTrajectory.Trajectory.WellBoreID, out architecture))
        {
            fallbackTrajectory = candidateTrajectory;
            return true;
        }

        return false;
    }

    private static string GetArchitectureFallbackKey(TestTrajectory trajectory)
    {
        string[] ignoredTokens = ["MIA", "EXTRAPOLATED"];
        string[] tokens = FormatTrajectoryName(trajectory)
            .Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(tokens
            .Where(token => !ignoredTokens.Contains(token, StringComparer.OrdinalIgnoreCase)))
            .ToUpperInvariant();
    }

    private static TestTrajectory CreateDuplicateTestTrajectory(Guid seedId, TrajectoryModel trajectory, string duplicateRole)
    {
        return new TestTrajectory(CreateDeterministicGuid($"{seedId:D}:{duplicateRole}"), trajectory, true);
    }

    private static int ResolveReferenceTrajectoryIndex(
        IReadOnlyList<TestTrajectory> trajectories,
        int fallbackIndex,
        string referenceTrajectoryName)
    {
        Ensure(fallbackIndex >= 0 && fallbackIndex < trajectories.Count,
            $"The reference trajectory index {fallbackIndex} is outside the valid range [0, {trajectories.Count - 1}].");

        if (!string.IsNullOrWhiteSpace(referenceTrajectoryName))
        {
            var indexedTrajectories = trajectories
                .Select((trajectory, index) => new { trajectory, index })
                .ToList();

            int matchedIndex = indexedTrajectories
                .FirstOrDefault(x => FormatTrajectoryName(x.trajectory).Equals(referenceTrajectoryName, StringComparison.OrdinalIgnoreCase))
                ?.index ?? -1;
            if (matchedIndex == -1)
            {
                matchedIndex = indexedTrajectories
                    .FirstOrDefault(x => FormatTrajectoryName(x.trajectory).Contains(referenceTrajectoryName, StringComparison.OrdinalIgnoreCase))
                    ?.index ?? -1;
            }
            if (matchedIndex >= 0)
            {
                return matchedIndex;
            }
        }

        return fallbackIndex;
    }

    private static bool MatchesComparisonTrajectoryNameFilter(
        TestTrajectory trajectory,
        IReadOnlyList<string> comparisonTrajectoryNameFilters)
    {
        List<string> activeFilters = comparisonTrajectoryNameFilters
            .Where(filter => !string.IsNullOrWhiteSpace(filter))
            .ToList();
        if (activeFilters.Count == 0)
        {
            return true;
        }

        string trajectoryName = FormatTrajectoryName(trajectory);
        return activeFilters.Any(filter =>
            trajectoryName.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    private static void ConfigureRemoteServiceHosts()
    {
        string host = ExternalServiceBaseAddress.ToString();
        ServiceConfiguration.FieldHostURL = host;
        ServiceConfiguration.ClusterHostURL = host;
        ServiceConfiguration.WellHostURL = host;
        ServiceConfiguration.WellBoreHostURL = host;
        ServiceConfiguration.WellBoreArchitectureHostURL = host;
        ServiceConfiguration.SurveyInstrumentHostURL = host;
    }

    private static string InitializeLocalRuntimeDirectory()
    {
        string runtimeRoot = Path.Combine(AppContext.BaseDirectory, "testruns", "shared");
        string workingDirectory = Path.Combine(runtimeRoot, "work");
        Directory.CreateDirectory(workingDirectory);
        Directory.SetCurrentDirectory(workingDirectory);
        return runtimeRoot;
    }

    private static LocalHarness CreateLocalHarness()
    {
        SqlConnectionManagerTrajectory trajectoryConnectionManager = new(NullLogger<SqlConnectionManagerTrajectory>.Instance);
        SqlConnectionManagerOctree octreeConnectionManager = new(NullLogger<SqlConnectionManagerOctree>.Instance);
        SqlConnectionManagerSeparationFactorResults separationConnectionManager = new(NullLogger<SqlConnectionManagerSeparationFactorResults>.Instance);

        TrajectoryManager trajectoryManager = TrajectoryManager.GetInstance(NullLogger<TrajectoryManager>.Instance, trajectoryConnectionManager);
        OctreeManager octreeManager = OctreeManager.GetInstance(NullLogger<OctreeManager>.Instance, octreeConnectionManager);
        GlobalAntiCollisionManager globalAntiCollisionManager = GlobalAntiCollisionManager.GetInstance(NullLogger<GlobalAntiCollisionManager>.Instance, separationConnectionManager);

        OctreesController octreesController = new(
            NullLogger<TrajectoryManager>.Instance,
            NullLogger<OctreeManager>.Instance,
            trajectoryConnectionManager,
            octreeConnectionManager);

        GlobalAntiCollisionsController globalAntiCollisionsController = new(
            NullLogger<TrajectoryManager>.Instance,
            NullLogger<GlobalAntiCollisionManager>.Instance,
            NullLogger<OctreeManager>.Instance,
            trajectoryConnectionManager,
            separationConnectionManager,
            octreeConnectionManager);

        return new LocalHarness(
            trajectoryManager,
            trajectoryConnectionManager,
            octreeManager,
            globalAntiCollisionManager,
            octreesController,
            globalAntiCollisionsController);
    }

    private static void SeedLocalTrajectoryDatabase(
        SqlConnectionManagerTrajectory trajectoryConnectionManager,
        TrajectoryManager trajectoryManager,
        IEnumerable<TestTrajectory> trajectories)
    {
        Console.WriteLine("Seeding local Trajectory.db for controller lookups...\n");

        foreach (TestTrajectory testTrajectory in trajectories)
        {
            Ensure(testTrajectory.Id != Guid.Empty, "Local test trajectory ID must not be empty.");
            Ensure(testTrajectory.Trajectory.MetaInfo != null, "Remote trajectory metadata must not be null.");

            TrajectoryModel storedTrajectory = CloneTrajectoryWithLocalId(testTrajectory.Trajectory, testTrajectory.Id);
            UpsertTrajectoryRow(trajectoryConnectionManager, storedTrajectory);

            TrajectoryModel? loadedTrajectory = trajectoryManager.GetTrajectoryById(testTrajectory.Id);
            Ensure(loadedTrajectory != null, $"Local Trajectory.db should contain {FormatTrajectoryLabel(testTrajectory)}.");
            Ensure(loadedTrajectory!.MetaInfo?.ID == testTrajectory.Id,
                $"Local Trajectory.db should return {FormatTrajectoryLabel(testTrajectory)} with a matching MetaInfo.ID.");
        }
    }

    private static void EnsureTrajectoryPostedToOctree(
        OctreesController octreesController,
        OctreeManager octreeManager,
        TestTrajectory trajectory)
    {
        if (octreeManager.Contains(trajectory.Id))
        {
            Console.WriteLine($"\t{FormatTrajectoryLabel(trajectory)} is already present in the octree cache. Skipping OctreesController.Post.");
            return;
        }

        Console.WriteLine($"\t{FormatTrajectoryLabel(trajectory)} is not present in the octree cache. Posting it now...");
        Stopwatch stopwatch = Stopwatch.StartNew();
        octreesController.Post(trajectory.Id);
        stopwatch.Stop();

        Ensure(octreeManager.Contains(trajectory.Id),
            $"OctreeManager should contain {FormatTrajectoryLabel(trajectory)} after posting it.");
        Console.WriteLine($"\tFinished OctreesController.Post for {FormatTrajectoryLabel(trajectory)} in {stopwatch.Elapsed.TotalSeconds:F0} s.");
    }

    private static void ValidateComparisonResults(
        GlobalAntiCollisionModel payload,
        IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup,
        string label)
    {
        Console.WriteLine($"\n{label}:");

        if (payload.ComparisonTrajectoryIDs == null || payload.ComparisonTrajectoryIDs.Count == 0)
        {
            Ensure(payload.SeparationFactorResults.Count == 0, $"{label} should not contain separation factor results when there are no comparison trajectories.");
            Console.WriteLine("\tNo nearby comparison trajectories were returned for the reference trajectory.");
            return;
        }

        Ensure(payload.SeparationFactorResults.Count <= payload.ComparisonTrajectoryIDs.Count,
            $"{label} should not contain more separation factor results than comparison trajectory ids.");

        foreach (SeparationFactorResult result in payload.SeparationFactorResults)
        {
            Ensure(payload.ComparisonTrajectoryIDs.Contains(result.ComparisonTrajectoryID),
                $"{label} contains a separation factor result for an unexpected trajectory id {result.ComparisonTrajectoryID}.");

            if (!TryGetMinimumSeparationPoint(result, out SeparationFactorPoint minimumPoint))
            {
                Console.WriteLine(
                    $"\t{FormatTrajectoryLabel(result.ComparisonTrajectoryID, trajectoryLookup)} has an empty separation factor profile " +
                    $"for reference MD range {FormatMeasuredDepthRange(result.ReferenceMDRange)} and comparison MD range {FormatMeasuredDepthRange(result.ComparisonMDRange)}.");
            }
            else
            {
                Console.WriteLine(
                    $"\tMinimum separation factor for {FormatTrajectoryLabel(result.ComparisonTrajectoryID, trajectoryLookup)} " +
                    $"is {minimumPoint.SeparationFactor:F3} at reference MD {minimumPoint.ReferenceMD:F2} and comparison MD {minimumPoint.ComparisonMD:F2}.");
            }
        }
    }

    private static async Task ValidateSidetrackTieInFilteringAsync(
        GlobalAntiCollisionModel payload,
        TestTrajectory referenceTrajectory,
        IReadOnlyList<TestTrajectory> comparisonTrajectories)
    {
        WellBore referenceWellBore = await APIUtils.ClientWellBore.GetWellBoreByIdAsync(
            referenceTrajectory.Trajectory.WellBoreID);
        Dictionary<Guid, SeparationFactorResult> resultsByTrajectoryId = payload.SeparationFactorResults
            .ToDictionary(result => result.ComparisonTrajectoryID);

        foreach (TestTrajectory comparisonTrajectory in comparisonTrajectories)
        {
            WellBore comparisonWellBore = await APIUtils.ClientWellBore.GetWellBoreByIdAsync(
                comparisonTrajectory.Trajectory.WellBoreID);
            if (!TryGetExpectedSidetrackMinimumMDs(
                referenceTrajectory,
                referenceWellBore,
                comparisonTrajectory,
                comparisonWellBore,
                out double referenceMinimumMD,
                out double comparisonMinimumMD) ||
                !resultsByTrajectoryId.TryGetValue(comparisonTrajectory.Id, out SeparationFactorResult? result))
            {
                continue;
            }

            const double tolerance = 1e-6;
            Ensure(result.ReferenceMDRange != null &&
                result.ReferenceMDRange.StartMD >= referenceMinimumMD - tolerance,
                $"{FormatTrajectoryLabel(referenceTrajectory)} vs. {FormatTrajectoryLabel(comparisonTrajectory)} " +
                $"should start the reference safety-factor range at or after the tie-in MD {referenceMinimumMD:F2}.");
            Ensure(result.ComparisonMDRange != null &&
                result.ComparisonMDRange.StartMD >= comparisonMinimumMD - tolerance,
                $"{FormatTrajectoryLabel(referenceTrajectory)} vs. {FormatTrajectoryLabel(comparisonTrajectory)} " +
                $"should start the comparison safety-factor range at or after the tie-in MD {comparisonMinimumMD:F2}.");

            foreach (SeparationFactorPoint point in result.SeparationFactorProfile)
            {
                Ensure(point.ReferenceMD >= referenceMinimumMD - tolerance,
                    $"Reference safety factor MD {point.ReferenceMD:F6} is above the sidetrack tie-in MD {referenceMinimumMD:F6}.");
                Ensure(IsUndefinedComparisonMD(point) || point.ComparisonMD >= comparisonMinimumMD - tolerance,
                    $"Comparison safety factor MD {point.ComparisonMD:F6} is above the sidetrack tie-in MD {comparisonMinimumMD:F6}.");
            }

            Console.WriteLine(
                $"\tVerified sidetrack filtering for {FormatTrajectoryLabel(referenceTrajectory)} vs. " +
                $"{FormatTrajectoryLabel(comparisonTrajectory)}: safety factors start at reference MD " +
                $"{referenceMinimumMD:F2} and comparison MD {comparisonMinimumMD:F2}.");
        }
    }

    private static bool TryGetExpectedSidetrackMinimumMDs(
        TestTrajectory referenceTrajectory,
        WellBore referenceWellBore,
        TestTrajectory comparisonTrajectory,
        WellBore comparisonWellBore,
        out double referenceMinimumMD,
        out double comparisonMinimumMD)
    {
        referenceMinimumMD = 0;
        comparisonMinimumMD = 0;

        if (TryGetParentTieInMD(referenceWellBore, comparisonTrajectory.Trajectory.WellBoreID, out double parentTieInMD))
        {
            referenceMinimumMD = GetTrajectoryTieInMD(referenceTrajectory, parentTieInMD);
            comparisonMinimumMD = parentTieInMD;
            return true;
        }

        if (TryGetParentTieInMD(comparisonWellBore, referenceTrajectory.Trajectory.WellBoreID, out parentTieInMD))
        {
            referenceMinimumMD = parentTieInMD;
            comparisonMinimumMD = GetTrajectoryTieInMD(comparisonTrajectory, parentTieInMD);
            return true;
        }

        return false;
    }

    private static bool TryGetParentTieInMD(WellBore possibleSidetrack, Guid parentWellBoreId, out double tieInMD)
    {
        tieInMD = 0;
        if (!possibleSidetrack.IsSidetrack ||
            possibleSidetrack.ParentWellBoreID != parentWellBoreId ||
            possibleSidetrack.TieInPointAlongHoleDepth?.GaussianValue?.Mean is not double candidateTieInMD ||
            !double.IsFinite(candidateTieInMD))
        {
            return false;
        }

        tieInMD = candidateTieInMD;
        return true;
    }

    private static double GetTrajectoryTieInMD(TestTrajectory trajectory, double fallbackTieInMD)
    {
        double? trajectoryTieInMD = trajectory.Trajectory.TieInPoint?.MD ??
            trajectory.Trajectory.TieInPoint?.Abscissa;
        return trajectoryTieInMD is double tieInMD && double.IsFinite(tieInMD)
            ? tieInMD
            : fallbackTieInMD;
    }

    private static void EnsureUpdatedProfilesImproved(
        IReadOnlyList<SeparationFactorResult> storedResults,
        IReadOnlyList<SeparationFactorResult> updatedResults,
        double storedConfidenceFactor,
        double updatedConfidenceFactor,
        IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup)
    {
        if (storedResults.Count == 0 || updatedResults.Count == 0)
        {
            return;
        }

        Console.WriteLine($"\n\tComparing minimum separation factor results at confidence factor {storedConfidenceFactor:F3} vs. {updatedConfidenceFactor:F3}:");
        Dictionary<Guid, SeparationFactorResult> storedById = storedResults.ToDictionary(result => result.ComparisonTrajectoryID);
        foreach (SeparationFactorResult updatedResult in updatedResults)
        {
            if (!storedById.TryGetValue(updatedResult.ComparisonTrajectoryID, out SeparationFactorResult? storedResult))
            {
                continue;
            }

            if (storedResult.SeparationFactorProfile.Count == 0 || updatedResult.SeparationFactorProfile.Count == 0)
            {
                continue;
            }

            bool hasStoredMinimum = TryGetMinimumSeparationPoint(storedResult, out SeparationFactorPoint storedMinimumPoint);
            bool hasUpdatedMinimum = TryGetMinimumSeparationPoint(updatedResult, out SeparationFactorPoint updatedMinimumPoint);
            if (!hasStoredMinimum || !hasUpdatedMinimum)
            {
                continue;
            }

            Console.WriteLine(
                $"\t{FormatTrajectoryLabel(updatedResult.ComparisonTrajectoryID, trajectoryLookup)} " +
                $"{storedMinimumPoint.SeparationFactor:F3} at reference MD {storedMinimumPoint.ReferenceMD:F2} / comparison MD {storedMinimumPoint.ComparisonMD:F2} " +
                $"vs. {updatedMinimumPoint.SeparationFactor:F3} at reference MD {updatedMinimumPoint.ReferenceMD:F2} / comparison MD {updatedMinimumPoint.ComparisonMD:F2}.");
            Ensure(updatedMinimumPoint.SeparationFactor >= storedMinimumPoint.SeparationFactor,
                $"GlobalAntiCollisionsController PUT should improve the minimum separation factor for {FormatTrajectoryLabel(updatedResult.ComparisonTrajectoryID, trajectoryLookup)}.");
        }
    }

    private static void PrintPossibleCollisionSummary(
        GlobalAntiCollisionModel payload,
        IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup,
        double threshold)
    {
        Console.WriteLine($"\n\tPossible collisions at confidence factor {payload.ConfidenceFactor:F3} (minimum separation factor <= {threshold:F2}):");

        if (payload.SeparationFactorResults.Count == 0)
        {
            Console.WriteLine("\tNo comparison trajectories produced separation factor results.");
            return;
        }

        string referenceLabel = FormatTrajectoryLabel(payload.ReferenceTrajectoryID, trajectoryLookup);
        List<SeparationFactorResult> possibleCollisions = payload.SeparationFactorResults
            .Where(result => TryGetMinimumSeparationPoint(result, out SeparationFactorPoint minimumPoint) && minimumPoint.SeparationFactor <= threshold)
            .ToList();

        if (possibleCollisions.Count == 0)
        {
            Console.WriteLine("\tNo possible collisions were found.");
            return;
        }

        foreach (SeparationFactorResult result in possibleCollisions)
        {
            bool hasMinimum = TryGetMinimumSeparationPoint(result, out SeparationFactorPoint minimumPoint);
            if (!hasMinimum)
            {
                continue;
            }

            Console.WriteLine(
                $"\tReference well {referenceLabel} vs. comparison well {FormatTrajectoryLabel(result.ComparisonTrajectoryID, trajectoryLookup)}: " +
                $"minimum separation factor {minimumPoint.SeparationFactor:F3} at reference MD {minimumPoint.ReferenceMD:F2} and comparison MD {minimumPoint.ComparisonMD:F2}.");
        }
    }

    private static void PrintSeparationFactorProfilesForPossibleCollisions(
        GlobalAntiCollisionModel payload,
        IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup,
        double exclusiveLowerBound,
        double exclusiveUpperBound)
    {
        Console.WriteLine(
            $"\n\tSeparation factor profiles for comparisons with minimum separation factor > {exclusiveLowerBound:F2} " +
            $"and < {exclusiveUpperBound:F2}:");

        if (payload.SeparationFactorResults.Count == 0)
        {
            Console.WriteLine("\tNo comparison trajectories produced separation factor results.");
            return;
        }

        List<(SeparationFactorResult Result, SeparationFactorPoint MinimumPoint)> selectedResults = [];
        if (payload.SeparationFactorResults.Count == 1)
        {
            if (TryGetMinimumSeparationPoint(payload.SeparationFactorResults[0], out SeparationFactorPoint minimumPoint))
            {
                selectedResults.Add((payload.SeparationFactorResults[0], minimumPoint));
            }
        }
        else
        {
            foreach (SeparationFactorResult result in payload.SeparationFactorResults)
            {
                if (TryGetMinimumSeparationPoint(result, out SeparationFactorPoint minimumPoint) &&
                    minimumPoint.SeparationFactor > exclusiveLowerBound &&
                    minimumPoint.SeparationFactor < exclusiveUpperBound)
                {
                    selectedResults.Add((result, minimumPoint));
                }
            }
        }

        if (selectedResults.Count == 0)
        {
            Console.WriteLine("\tNo comparison trajectories matched the configured profile-printing range.");
            return;
        }

        string referenceLabel = FormatTrajectoryLabel(payload.ReferenceTrajectoryID, trajectoryLookup);
        foreach ((SeparationFactorResult result, SeparationFactorPoint minimumPoint) in selectedResults)
        {
            BoreholeRadiusCoverage referenceRadiusCoverage = GetBoreholeRadiusCoverage(trajectoryLookup, payload.ReferenceTrajectoryID);
            BoreholeRadiusCoverage comparisonRadiusCoverage = GetBoreholeRadiusCoverage(trajectoryLookup, result.ComparisonTrajectoryID);
            Console.WriteLine(
                $"\n\tReference well {referenceLabel} vs. comparison well {FormatTrajectoryLabel(result.ComparisonTrajectoryID, trajectoryLookup)} " +
                $"minimum separation factor {minimumPoint.SeparationFactor:F6} at reference MD {minimumPoint.ReferenceMD:F2} " +
                $"and comparison MD {minimumPoint.ComparisonMD:F2}:");
            Console.WriteLine(
                $"\t\tBorehole radius coverage: reference {FormatBoreholeRadiusCoverage(referenceRadiusCoverage)}, " +
                $"comparison {FormatBoreholeRadiusCoverage(comparisonRadiusCoverage)}.");
            Console.WriteLine("\t\tIndex\tReferenceMD\tComparisonMD\tCenterlineDistance\tClearanceDistance\tSeparationFactor");

            for (int i = 0; i < result.SeparationFactorProfile.Count; i++)
            {
                SeparationFactorPoint point = result.SeparationFactorProfile[i];
                ProfileDistance profileDistance = CalculateProfileDistance(
                    trajectoryLookup,
                    payload.ReferenceTrajectoryID,
                    result.ComparisonTrajectoryID,
                    point);
                Console.WriteLine(
                    $"\t\t{i}\t{point.ReferenceMD:F6}\t{point.ComparisonMD:F6}\t" +
                    $"{FormatNullableDistance(profileDistance.CenterlineDistance)}\t" +
                    $"{FormatNullableDistance(profileDistance.ClearanceDistance)}\t" +
                    $"{point.SeparationFactor:F9}");
            }
        }
    }

    private static ProfileDistance CalculateProfileDistance(
        IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup,
        Guid referenceTrajectoryId,
        Guid comparisonTrajectoryId,
        SeparationFactorPoint point)
    {
        if (IsUndefinedComparisonMD(point))
        {
            return new(null, null);
        }

        if (!trajectoryLookup.TryGetValue(referenceTrajectoryId, out TestTrajectory referenceTrajectory) ||
            !trajectoryLookup.TryGetValue(comparisonTrajectoryId, out TestTrajectory comparisonTrajectory))
        {
            return new(null, null);
        }

        if (!TryInterpolateSurveySample(referenceTrajectory.Trajectory.SurveyStationList, point.ReferenceMD, out SurveySample3D referenceSample) ||
            !TryInterpolateSurveySample(comparisonTrajectory.Trajectory.SurveyStationList, point.ComparisonMD, out SurveySample3D comparisonSample))
        {
            return new(null, null);
        }

        double centerlineDistance = referenceSample.Point.DistanceTo(comparisonSample.Point);
        double? clearanceDistance = null;
        if (referenceSample.BoreholeRadius is double referenceRadius &&
            comparisonSample.BoreholeRadius is double comparisonRadius &&
            double.IsFinite(referenceRadius) &&
            double.IsFinite(comparisonRadius))
        {
            clearanceDistance = centerlineDistance - referenceRadius - comparisonRadius;
        }

        return new(centerlineDistance, clearanceDistance);
    }

    private static bool IsUndefinedComparisonMD(SeparationFactorPoint point)
    {
        const double tolerance = 1e-9;
        return Math.Abs(point.ComparisonMD - -1.0) <= tolerance &&
            Math.Abs(point.SeparationFactor - SeparationFactorCalculations.MaxSeparationFactor) <= tolerance;
    }

    private static bool TryInterpolateSurveySample(List<SurveyStation>? surveyStations, double md, out SurveySample3D sample)
    {
        sample = default;
        if (surveyStations is not { Count: > 0 } || !double.IsFinite(md))
        {
            return false;
        }

        List<SurveyStation> stations = surveyStations
            .Where(station => station?.MD is double stationMd && double.IsFinite(stationMd))
            .OrderBy(station => station.MD)
            .ToList();
        if (stations.Count == 0)
        {
            return false;
        }

        const double tolerance = 1e-6;
        if (md < stations[0].MD!.Value - tolerance || md > stations[^1].MD!.Value + tolerance)
        {
            return false;
        }

        for (int i = 0; i < stations.Count; i++)
        {
            if (Math.Abs(stations[i].MD!.Value - md) <= tolerance)
            {
                if (!TryGetSurveyPoint3D(stations[i], out SurveyPoint3D point))
                {
                    return false;
                }

                sample = new SurveySample3D(point, GetFiniteNullableValue(stations[i].BoreholeRadius));
                return true;
            }
        }

        for (int i = 0; i < stations.Count - 1; i++)
        {
            SurveyStation start = stations[i];
            SurveyStation end = stations[i + 1];
            double startMd = start.MD!.Value;
            double endMd = end.MD!.Value;
            if (md + tolerance < startMd || md - tolerance > endMd)
            {
                continue;
            }

            if (!TryGetSurveyPoint3D(start, out SurveyPoint3D startPoint) ||
                !TryGetSurveyPoint3D(end, out SurveyPoint3D endPoint))
            {
                return false;
            }

            double denominator = endMd - startMd;
            double ratio = Math.Abs(denominator) <= tolerance ? 0.0 : (md - startMd) / denominator;
            sample = new SurveySample3D(
                SurveyPoint3D.Interpolate(startPoint, endPoint, Math.Clamp(ratio, 0.0, 1.0)),
                GetConservativeBoreholeRadius(start, end));
            return true;
        }

        return false;
    }

    private static bool TryGetSurveyPoint3D(SurveyStation station, out SurveyPoint3D point)
    {
        point = default;
        double? x = station.X ?? station.RiemannianNorth;
        double? y = station.Y ?? station.RiemannianEast;
        double? z = station.Z ?? station.TVD;
        if (x is not double xValue || y is not double yValue || z is not double zValue ||
            !double.IsFinite(xValue) || !double.IsFinite(yValue) || !double.IsFinite(zValue))
        {
            return false;
        }

        point = new SurveyPoint3D(xValue, yValue, zValue);
        return true;
    }

    private static string FormatNullableDistance(double? distance) => distance.HasValue ? $"{distance.Value:F6}" : "N/A";

    private static string FormatMeasuredDepthRange(MeasuredDepthRange? range) =>
        range == null ? "<full trajectory>" : $"[{range.StartMD:F2}, {range.EndMD:F2}]";

    private static BoreholeRadiusCoverage GetBoreholeRadiusCoverage(
        IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup,
        Guid trajectoryId)
    {
        if (!trajectoryLookup.TryGetValue(trajectoryId, out TestTrajectory trajectory) ||
            trajectory.Trajectory.SurveyStationList is not { Count: > 0 } surveyStations)
        {
            return new(0, 0);
        }

        int filledCount = surveyStations.Count(station => GetFiniteNullableValue(station.BoreholeRadius).HasValue);
        return new(filledCount, surveyStations.Count);
    }

    private static string FormatBoreholeRadiusCoverage(BoreholeRadiusCoverage coverage) =>
        $"{coverage.FilledStationCount}/{coverage.TotalStationCount} stations";

    private static double? GetConservativeBoreholeRadius(SurveyStation start, SurveyStation end)
    {
        double? startRadius = GetFiniteNullableValue(start.BoreholeRadius);
        double? endRadius = GetFiniteNullableValue(end.BoreholeRadius);
        if (startRadius.HasValue && endRadius.HasValue)
        {
            return Math.Max(startRadius.Value, endRadius.Value);
        }

        return startRadius ?? endRadius;
    }

    private static double? GetFiniteNullableValue(double? value) =>
        value is double finiteValue && double.IsFinite(finiteValue) ? finiteValue : null;

    private static bool TryGetMinimumSeparationPoint(SeparationFactorResult result, out SeparationFactorPoint minimumPoint)
    {
        if (result.SeparationFactorProfile == null || result.SeparationFactorProfile.Count == 0)
        {
            minimumPoint = default;
            return false;
        }

        minimumPoint = result.SeparationFactorProfile[0];
        for (int i = 1; i < result.SeparationFactorProfile.Count; i++)
        {
            SeparationFactorPoint point = result.SeparationFactorProfile[i];
            if (point.SeparationFactor < minimumPoint.SeparationFactor)
            {
                minimumPoint = point;
            }
        }

        return true;
    }

    private static string FormatDuplicateLabel(TestTrajectory trajectory)
    {
        return trajectory.IsDuplicate ? " (duplicate)" : string.Empty;
    }

    private static string FormatTrajectoryName(TestTrajectory trajectory)
    {
        return string.IsNullOrWhiteSpace(trajectory.Trajectory.Name) ? "<unnamed>" : trajectory.Trajectory.Name;
    }

    private static string FormatTrajectoryLabel(TestTrajectory trajectory, bool includeGuid = false)
    {
        string id = includeGuid ? $" ({trajectory.Id})" : string.Empty;
        return $"\"{FormatTrajectoryName(trajectory)}\"{id}";
    }

    private static string FormatTrajectoryLabel(Guid trajectoryId, IReadOnlyDictionary<Guid, TestTrajectory> trajectoryLookup)
    {
        return trajectoryLookup.TryGetValue(trajectoryId, out TestTrajectory trajectory)
            ? FormatTrajectoryLabel(trajectory)
            : $"trajectory {trajectoryId}";
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        byte[] namespaceBytes = DuplicateTrajectoryNamespaceId.ToByteArray();
        byte[] nameBytes = Encoding.UTF8.GetBytes(value);
        byte[] inputBytes = new byte[namespaceBytes.Length + nameBytes.Length];

        Buffer.BlockCopy(namespaceBytes, 0, inputBytes, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, inputBytes, namespaceBytes.Length, nameBytes.Length);

        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(inputBytes);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    private static TrajectoryModel CloneTrajectoryWithLocalId(TrajectoryModel trajectory, Guid localId)
    {
        string json = JsonSerializer.Serialize(trajectory, JsonOptions);
        TrajectoryModel clonedTrajectory =
            JsonSerializer.Deserialize<TrajectoryModel>(json, JsonOptions) ??
            throw new InvalidOperationException("Failed to clone the remote trajectory payload.");

        clonedTrajectory.MetaInfo ??= new OSDC.DotnetLibraries.General.DataManagement.MetaInfo();
        clonedTrajectory.MetaInfo.ID = localId;
        return clonedTrajectory;
    }

    private static void UpsertTrajectoryRow(SqlConnectionManagerTrajectory trajectoryConnectionManager, TrajectoryModel trajectory)
    {
        using SqliteConnection? connection = trajectoryConnectionManager.GetConnection();
        Ensure(connection != null, "Could not open the local trajectory database.");
        SqliteConnection openedConnection = connection!;

        using SqliteTransaction transaction = openedConnection.BeginTransaction();
        using SqliteCommand command = openedConnection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT OR REPLACE INTO TrajectoryTable
            (ID, MetaInfo, CreationDate, LastModificationDate, FieldID, ClusterID, WellID, WellBoreID, Trajectory)
            VALUES
            (@id, @metaInfo, @creationDate, @lastModificationDate, @fieldId, @clusterId, @wellId, @wellBoreId, @trajectory)
            """;

        command.Parameters.AddWithValue("@id", trajectory.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("@metaInfo", JsonSerializer.Serialize(trajectory.MetaInfo, JsonOptions));
        command.Parameters.AddWithValue("@creationDate", FormatDate(trajectory.CreationDate));
        command.Parameters.AddWithValue("@lastModificationDate", FormatDate(trajectory.LastModificationDate));
        command.Parameters.AddWithValue("@fieldId", ToDatabaseValue(trajectory.FieldID));
        command.Parameters.AddWithValue("@clusterId", ToDatabaseValue(trajectory.ClusterID));
        command.Parameters.AddWithValue("@wellId", ToDatabaseValue(trajectory.WellID));
        command.Parameters.AddWithValue("@wellBoreId", trajectory.WellBoreID.ToString());
        command.Parameters.AddWithValue("@trajectory", JsonSerializer.Serialize(trajectory, JsonOptions));

        int affectedRows = command.ExecuteNonQuery();
        Ensure(affectedRows == 1, $"Expected a single local trajectory row for {trajectory.MetaInfo.ID}, got {affectedRows}.");
        transaction.Commit();
    }

    private static object ToDatabaseValue(Guid? value)
    {
        return value is Guid guid && guid != Guid.Empty ? guid.ToString() : DBNull.Value;
    }

    private static object FormatDate(DateTimeOffset? value)
    {
        return value is DateTimeOffset dateTimeOffset
            ? dateTimeOffset.ToString(SqlConnectionManager.DATE_TIME_FORMAT)
            : DBNull.Value;
    }

    private static HttpClient CreateHttpClient(Uri baseAddress)
    {
        HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        return new HttpClient(handler)
        {
            BaseAddress = baseAddress
        };
    }

    private static async Task<T> GetJsonAsync<T>(HttpClient client, string relativeUrl)
    {
        using HttpResponseMessage response = await client.GetAsync(relativeUrl);
        await EnsureSuccessStatusCodeAsync(response, $"GET {relativeUrl}");
        T? payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return payload ?? throw new InvalidOperationException($"GET {relativeUrl} returned an empty payload.");
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{operation} failed with status {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record LocalHarness(
        TrajectoryManager TrajectoryManager,
        SqlConnectionManagerTrajectory TrajectoryConnectionManager,
        OctreeManager OctreeManager,
        GlobalAntiCollisionManager GlobalAntiCollisionManager,
        OctreesController OctreesController,
        GlobalAntiCollisionsController GlobalAntiCollisionsController);

    private readonly record struct TestTrajectory(Guid Id, TrajectoryModel Trajectory, bool IsDuplicate);

    private readonly record struct BoreholeRadiusCoverage(int FilledStationCount, int TotalStationCount);

    private readonly record struct ProfileDistance(double? CenterlineDistance, double? ClearanceDistance);

    private readonly record struct SurveySample3D(SurveyPoint3D Point, double? BoreholeRadius);

    private readonly record struct SurveyPoint3D(double X, double Y, double Z)
    {
        public static SurveyPoint3D Interpolate(SurveyPoint3D start, SurveyPoint3D end, double ratio) =>
            new(
                start.X + (end.X - start.X) * ratio,
                start.Y + (end.Y - start.Y) * ratio,
                start.Z + (end.Z - start.Z) * ratio);

        public double DistanceTo(SurveyPoint3D other) =>
            Math.Sqrt(
                Math.Pow(X - other.X, 2.0) +
                Math.Pow(Y - other.Y, 2.0) +
                Math.Pow(Z - other.Z, 2.0));
    }
}
