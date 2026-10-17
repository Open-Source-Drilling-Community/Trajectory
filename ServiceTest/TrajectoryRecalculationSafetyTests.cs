using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using T = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace ServiceTest;

[TestFixture, NonParallelizable]
public sealed class TrajectoryRecalculationSafetyTests
{
    [Test]
    public void Wellhead_conversion_populates_geometry_and_nonzero_SI_covariance()
    {
        const double latitude = 1, longitude = .1, sigma = 1e-8;
        var point = APIUtils.CreateWellheadReferencePoint(latitude, longitude, -91.2, sigma, sigma);
        const double a = 6378137, f = 1 / 298.257223563;
        double e2 = f * (2 - f), q = 1 - e2 * Math.Pow(Math.Sin(latitude), 2);
        double northSigma = a * (1 - e2) / Math.Pow(q, 1.5) * sigma;
        double eastSigma = a * Math.Cos(latitude) / Math.Sqrt(q) * sigma;
        double eastFromLat = longitude * sigma * (-a*Math.Sin(latitude)/Math.Sqrt(q)+a*e2*Math.Cos(latitude)*Math.Cos(latitude)*Math.Sin(latitude)/Math.Pow(q,1.5));
        Assert.Multiple(() =>
        {
            Assert.That(point.X, Is.Not.Null);
            Assert.That(point.Y, Is.Not.Null);
            Assert.That(point.Latitude, Is.EqualTo(latitude));
            Assert.That(point.Longitude, Is.EqualTo(longitude));
            Assert.That(point.Z, Is.EqualTo(-91.2));
            Assert.That(point.Covariance![0, 0], Is.EqualTo(northSigma * northSigma).Within(1e-6));
            Assert.That(point.Covariance[1, 1], Is.EqualTo(eastSigma * eastSigma + eastFromLat * eastFromLat).Within(1e-6));
            Assert.That(APIUtils.CreateWellheadCovariance(latitude, longitude, 0, 0, 2)[2, 2], Is.EqualTo(4));
            Assert.That(APIUtils.CreateWellheadCovariance(latitude, longitude, 0, 0, 0)[0, 0], Is.Zero);
        });
    }

    [TestCase(double.NaN, .1, 0, 1e-8)]
    [TestCase(2, .1, 0, 1e-8)]
    [TestCase(1, double.PositiveInfinity, 0, 1e-8)]
    [TestCase(1, .1, double.NaN, 1e-8)]
    [TestCase(1, .1, 0, -1)]
    [TestCase(1, .1, 0, double.NaN)]
    public void Invalid_wellhead_inputs_are_rejected(double latitude, double longitude, double tvd, double sigma) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => APIUtils.CreateWellheadReferencePoint(latitude, longitude, tvd, sigma, sigma));

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Full_geometry_calculation_from_geographic_wellhead_succeeds(bool replayUncertainty, bool sidetrack)
    {
        await using var fixture = new Fixture(sidetrack);
        var trajectory = fixture.Input();
        var result = await fixture.Manager.CalculateTrajectoryAsync(trajectory, replayUncertainty);
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.SurveyStationList, Has.Count.EqualTo(3));
            Assert.That(result.SurveyStationList![0].X, Is.Not.Null);
            Assert.That(result.SurveyStationList[2].X, Is.Not.Null);
            Assert.That(result.SurveyStationList[2].Z, Is.GreaterThan(0));
        });
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Failed_queued_calculation_preserves_last_geometry(bool inline, bool failSave)
    {
        await using var fixture = new Fixture();
        fixture.SeedCompleted(inline);
        string before = fixture.GeometryJson(inline);
        if (failSave) fixture.Execute($"CREATE TRIGGER RejectGeometry BEFORE INSERT ON SurveyStationChunkTable WHEN NEW.OwnerID='{fixture.TrajectoryId}' BEGIN SELECT RAISE(ABORT,'injected result save failure'); END;");
        var update = fixture.Input();
        if (!failSave) update.SurveyRunSectionList![0].SurveyRunID = Guid.NewGuid();
        Assert.That(await fixture.Manager.UpdateTrajectoryById(fixture.TrajectoryId, update), Is.True);
        await fixture.WaitFor(CalculationState.Failed);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.GeometryJson(inline), Is.EqualTo(before), "Last valid geometry must survive queueing, calculation failure, and transaction rollback.");
            var retained = fixture.Manager.GetTrajectoryById(fixture.TrajectoryId, includeCalculatedStations: true)!;
            Assert.That(retained.SurveyStationList, Has.Count.EqualTo(3));
            Assert.That(retained.SurveyStationList![0].X, Is.EqualTo(1234));
            Assert.That(retained.CalculationMessage, Does.Contain(failSave ? "saving" : "SurveyRun sections"));
        });
    }

    [Test]
    public async Task Successful_recalculation_atomically_replaces_previous_geometry()
    {
        await using var fixture = new Fixture();
        fixture.SeedCompleted(false);
        string before = fixture.GeometryJson(false);
        Assert.That(await fixture.Manager.UpdateTrajectoryById(fixture.TrajectoryId, fixture.Input()), Is.True);
        await fixture.WaitFor(CalculationState.Completed);
        Assert.That(fixture.GeometryJson(false), Is.Not.EqualTo(before));
        Assert.That(fixture.Manager.GetTrajectoryById(fixture.TrajectoryId, true)!.SurveyStationList, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Failed_new_trajectory_does_not_invent_a_previous_result()
    {
        await using var fixture = new Fixture();
        var input = fixture.Input(); input.SurveyRunSectionList![0].SurveyRunID = Guid.NewGuid();
        Assert.That(await fixture.Manager.AddTrajectory(input), Is.True);
        await fixture.WaitFor(CalculationState.Failed);
        Assert.That(fixture.Manager.GetSurveyStationChunkCount(fixture.TrajectoryId), Is.Zero);
        Assert.That(fixture.Manager.GetTrajectoryById(fixture.TrajectoryId, true)!.SurveyStationList, Is.Null);
    }

    [Test]
    public async Task Repair_dry_run_preserves_source_rows_and_pins_dependencies()
    {
        await using var fixture = new Fixture();fixture.SeedCompleted(false);
        string before=fixture.GeometryJson(false);
        var result=await CovarianceRepair.RunAsync(fixture.Database,false,fixture.AuditDirectory);
        var json=JsonSerializer.SerializeToNode(result)!;
        Assert.Multiple(()=>{Assert.That(json["passed"]!.GetValue<bool>(),Is.True);Assert.That(json["geometryAndMeasurementsUnchanged"]!.GetValue<bool>(),Is.True);Assert.That(json["changedRows"]!.GetValue<int>(),Is.GreaterThan(0));Assert.That(fixture.GeometryJson(false),Is.EqualTo(before));});
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SurveyRun_failure_retains_legacy_or_chunked_geometry(bool inline)
    {
        await using var fixture=new Fixture();
        using(var connection=fixture.Database.GetConnection()!)
        {
            using var tx=connection.BeginTransaction();
            if(!inline)Assert.That(SurveyStationChunkStore.ReplaceChunks(connection,tx,fixture.RunId,"SurveyRun",Fixture.Stations()),Is.True);
            tx.Commit();
        }
        var manager=SurveyRunManager.GetInstance(NullLogger<SurveyRunManager>.Instance,fixture.Database);
        if(!inline)fixture.Execute($"UPDATE SurveyRunTable SET SurveyRun=json_set(SurveyRun,'$.SurveyStationList',NULL) WHERE ID='{fixture.RunId}';");
        var before=manager.GetSurveyRunById(fixture.RunId)!.SurveyStationList!;string hash=JsonSerializer.Serialize(before,JsonSettings.Options);
        var update=manager.GetSurveyRunById(fixture.RunId)!;update.SurveyStationList=null;
        update.SurveyMeasurementList=[new(){MD=0,Inclination=.2,Azimuth=.3},new(){MD=30,Inclination=.25,Azimuth=.3},new(){MD=60,Inclination=.3,Azimuth=.3}];
        // Fail only the result insertion after the metadata UPDATE and station DELETE.
        fixture.Execute($"CREATE TRIGGER RejectRunGeometry BEFORE INSERT ON SurveyStationChunkTable WHEN NEW.OwnerID='{fixture.RunId}' AND NEW.OwnerType='SurveyRun' BEGIN SELECT RAISE(ABORT,'injected run result save failure'); END;");
        Assert.That(await manager.UpdateSurveyRunById(fixture.RunId,update),Is.True);
        for(int i=0;i<200&&manager.GetSurveyRunById(fixture.RunId)!.CalculationState!=CalculationState.Failed;i++)await Task.Delay(25);
        var retained=manager.GetSurveyRunById(fixture.RunId)!;
        Assert.That(retained.CalculationState,Is.EqualTo(CalculationState.Failed));
        Assert.That(JsonSerializer.Serialize(retained.SurveyStationList,JsonSettings.Options),Is.EqualTo(hash));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Repair_apply_is_idempotent_and_preserves_geometry(bool duplicateInline)
    {
        await using var fixture=new Fixture();fixture.SeedCompleted(false);
        if(duplicateInline)fixture.Execute($"UPDATE TrajectoryTable SET Trajectory=json_set(Trajectory,'$.SurveyStationList',json((SELECT json_extract(SurveyStationChunk,'$.SurveyStationList') FROM SurveyStationChunkTable WHERE OwnerID='{fixture.TrajectoryId}' AND OwnerType='Trajectory'))) WHERE ID='{fixture.TrajectoryId}'");
        var first=JsonSerializer.SerializeToNode(await CovarianceRepair.RunCoreAsync(fixture.Database,true,fixture.AuditDirectory))!;
        var second=JsonSerializer.SerializeToNode(await CovarianceRepair.RunCoreAsync(fixture.Database,true,fixture.AuditDirectory))!;
        Assert.Multiple(()=>{Assert.That(first["geometryAndMeasurementsUnchanged"]!.GetValue<bool>(),Is.True);Assert.That(second["changedRows"]!.GetValue<int>(),Is.Zero);Assert.That(fixture.Manager.GetTrajectoryById(fixture.TrajectoryId,true)!.SurveyStationList![0].X,Is.EqualTo(1234));});
    }
    [Test]
    public async Task Repair_transaction_rolls_back_all_records_on_chunk_failure()
    {
        await using var fixture=new Fixture();fixture.SeedCompleted(false);string before=fixture.GeometryJson(false);
        fixture.Execute("CREATE TRIGGER RejectRepair BEFORE UPDATE ON SurveyStationChunkTable BEGIN SELECT RAISE(ABORT,'injected repair failure'); END;");
        Assert.ThrowsAsync<SqliteException>(async()=>await CovarianceRepair.RunCoreAsync(fixture.Database,true,fixture.AuditDirectory));
        Assert.That(fixture.GeometryJson(false),Is.EqualTo(before));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "TrajectorySafety_" + Guid.NewGuid().ToString("N"));
        private readonly HttpListener listener = new();
        private readonly Task serve;
        private readonly Dictionary<PropertyInfo, object?> configuration;
        private readonly Dictionary<OSDC.Drilling.Trajectory.ModelShared.Client, string> urls;
        private readonly SqlConnectionManagerTrajectory database;
        public Guid TrajectoryId { get; } = Guid.NewGuid();
        private readonly Guid runId = Guid.NewGuid(), wellBoreId = Guid.NewGuid(), wellId = Guid.NewGuid(), clusterId = Guid.NewGuid(), slotId = Guid.NewGuid(), fieldId = Guid.NewGuid(), parentWellBoreId = Guid.NewGuid();
        public TrajectoryManager Manager { get; }
        public SqlConnectionManagerTrajectory Database => database;
        public string AuditDirectory => Path.Combine(directory,"audit");
        public Guid RunId => runId;

        public Fixture(bool sidetrack = false)
        {
            Directory.CreateDirectory(directory);
            using var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start();
            int port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
            string address = $"http://127.0.0.1:{port}/";
            configuration = typeof(ServiceConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Static).ToDictionary(p => p, p => p.GetValue(null));
            foreach (var p in configuration.Keys) if (p.PropertyType == typeof(string)) p.SetValue(null, address);
            urls = typeof(APIUtils).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(OSDC.Drilling.Trajectory.ModelShared.Client))
                .Select(f => (OSDC.Drilling.Trajectory.ModelShared.Client)f.GetValue(null)!).ToDictionary(c => c, c => c.BaseUrl);
            foreach (var client in urls.Keys) client.BaseUrl = address;
            listener.Prefixes.Add(address); listener.Start();
            serve = Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync();
                        string path = context.Request.Url!.AbsolutePath;
                        object value = path.StartsWith("/SurveyInstrument/") ? new { ModelType = "MWD_WolffDeWardt", Misalignment = .01, RelDepthError = .001, TrueInclination = .005, ReferenceError = .01, DrillStringMag = .002 }
                            : path.StartsWith("/WellBore/") ? (object)new { WellID = wellId, IsSidetrack = sidetrack && path.Contains(wellBoreId.ToString()), ParentWellBoreID = parentWellBoreId, TieInPointAlongHoleDepth = new { GaussianValue = new { Mean = 30.0 } } }
                            : path.StartsWith("/Well/") ? new { ClusterID = clusterId, SlotID = slotId }
                            : path.StartsWith("/Cluster/") ? new { FieldID = fieldId, ReferencePoint = new { TVD = 0 }, Slots = new Dictionary<string, object> { [slotId.ToString()] = new { Latitude = new { GaussianValue = new { Mean = 1, StandardDeviation = 1e-8 } }, Longitude = new { GaussianValue = new { Mean = .1, StandardDeviation = 1e-8 } } } } }
                            : new { Items = Array.Empty<object>(), TotalCount = 0 };
                        context.Response.ContentType = "application/json";
                        await JsonSerializer.SerializeAsync(context.Response.OutputStream, value);
                        context.Response.Close();
                    }
                }
                catch (Exception e) when (e is HttpListenerException or ObjectDisposedException) { }
            });
            ServiceTestHost.ResetManagerSingletons();
            database = new SqlConnectionManagerTrajectory(Path.Combine(directory, "Trajectory.db"), NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Manager = TrajectoryManager.GetInstance(NullLogger<TrajectoryManager>.Instance, database);
            var run = new SurveyRun { MetaInfo = new MetaInfo { ID = runId }, WellBoreID = wellBoreId, SurveyInstrumentID = Guid.NewGuid(), CalculationState = CalculationState.Completed, SurveyStationList = Stations() };
            Insert("SurveyRun", runId, run);
            if (sidetrack)
            {
                Guid parentRunId = Guid.NewGuid();
                var parentRun = new SurveyRun { MetaInfo = new MetaInfo { ID = parentRunId }, WellBoreID = parentWellBoreId, SurveyInstrumentID = run.SurveyInstrumentID, SurveyStationList = Stations() };
                Insert("SurveyRun", parentRunId, parentRun);
                var parent = new T { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, WellBoreID = parentWellBoreId, MDStep = 10, SurveyStationList = Stations(), TieInPoint = APIUtils.CreateWellheadReferencePoint(1, .1, 0, 1e-8, 1e-8), SurveyRunSectionList = [new() { SurveyRunID = parentRunId, StartAbscissa = 0 }] };
                Assert.That(parent.Calculate(), Is.True);
                Insert("Trajectory", parent.MetaInfo.ID, parent);
            }
        }
        public static List<SurveyStation> Stations() => [new() { MD = 0, Inclination = .2, Azimuth = .3, X = 1234, Y = 5678, Z = 0 }, new() { MD = 30, Inclination = .25, Azimuth = .3, X = 1240, Y = 5680, Z = 29 }, new() { MD = 60, Inclination = .3, Azimuth = .3, X = 1247, Y = 5682, Z = 58 }];
        public T Input() => new() { MetaInfo = new MetaInfo { ID = TrajectoryId }, WellBoreID = wellBoreId, MDStep = 10, SurveyRunSectionList = [new() { SurveyRunID = runId, StartAbscissa = 0 }] };
        public void SeedCompleted(bool inline)
        {
            var trajectory = Input(); trajectory.CalculationState = CalculationState.Completed; trajectory.LastModificationDate = DateTimeOffset.UtcNow;
            if (inline) trajectory.SurveyStationList = Stations();
            Insert("Trajectory", TrajectoryId, trajectory);
            if (!inline) { using var connection = database.GetConnection()!; using var transaction = connection.BeginTransaction(); Assert.That(SurveyStationChunkStore.ReplaceChunks(connection, transaction, TrajectoryId, "Trajectory", Stations()), Is.True); transaction.Commit(); }
        }
        private void Insert(string kind, Guid id, object value)
        {
            using var connection = database.GetConnection()!; using var cmd = connection.CreateCommand();
            cmd.CommandText = $"INSERT INTO {kind}Table(ID,WellBoreID,{kind}) VALUES(@id,@wellbore,@json)"; cmd.Parameters.AddWithValue("@wellbore", JsonSerializer.SerializeToNode(value, JsonSettings.Options)!["WellBoreID"]!.GetValue<Guid>().ToString()); cmd.Parameters.AddWithValue("@id", id.ToString()); cmd.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonSettings.Options)); cmd.ExecuteNonQuery();
        }
        public string GeometryJson(bool inline)
        {
            using var connection = database.GetConnection()!; using var cmd = connection.CreateCommand();
            cmd.CommandText = inline ? "SELECT Trajectory FROM TrajectoryTable WHERE ID=@id" : "SELECT SurveyStationChunk FROM SurveyStationChunkTable WHERE OwnerID=@id AND OwnerType='Trajectory' ORDER BY ChunkIndex";
            cmd.Parameters.AddWithValue("@id", TrajectoryId.ToString()); string json = (string)cmd.ExecuteScalar()!;
            return inline ? JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(json, JsonSettings.Options)!.SurveyStationList, JsonSettings.Options) : json;
        }
        public void Execute(string sql) { using var connection = database.GetConnection()!; using var cmd = connection.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
        public async Task WaitFor(CalculationState state)
        {
            for (int i = 0; i < 200; i++) { if (Manager.GetTrajectoryById(TrajectoryId)!.CalculationState == state) return; await Task.Delay(25); }
            Assert.Fail("Background calculation did not reach " + state);
        }
        public async ValueTask DisposeAsync()
        {
            listener.Stop(); await serve; listener.Close();
            foreach (var pair in urls) pair.Key.BaseUrl = pair.Value;
            foreach (var pair in configuration) pair.Key.SetValue(null, pair.Value);
            ServiceTestHost.ResetManagerSingletons(); SqliteConnection.ClearAllPools(); Directory.Delete(directory, true);
        }
    }
}
