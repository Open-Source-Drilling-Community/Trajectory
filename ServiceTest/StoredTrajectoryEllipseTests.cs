using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;

namespace ServiceTest;

[TestFixture, NonParallelizable]
public sealed class StoredTrajectoryEllipseTests
{
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    public async Task Stored_geometry_is_retained_only_when_authoritative_survey_attitudes_match(bool mismatch, bool separateTieIn, bool legacyTieIn)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"StoredEllipse_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        using var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        int port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        string address = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(address);
        listener.Start();
        var configuration = typeof(ServiceConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Static);
        var originalConfiguration = configuration.ToDictionary(property => property, property => property.GetValue(null));
        foreach (var property in configuration)
            if (property.GetValue(null) is null) property.SetValue(null, "http://localhost/");
        var clients = typeof(APIUtils).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OSDC.Drilling.Trajectory.ModelShared.Client))
            .Select(field => (OSDC.Drilling.Trajectory.ModelShared.Client)field.GetValue(null)!).ToArray();
        var originalUrls = clients.ToDictionary(client => client, client => client.BaseUrl);
        foreach (var client in clients) client.BaseUrl = address;
        Task serve = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    if (context.Request.Url!.AbsolutePath.StartsWith("/SurveyInstrument/", StringComparison.Ordinal))
                    {
                        context.Response.ContentType = "application/json";
                        await JsonSerializer.SerializeAsync(context.Response.OutputStream, new
                        {
                            ModelType = "MWD_WolffDeWardt", Misalignment = .01, RelDepthError = .001,
                            TrueInclination = .005, ReferenceError = .01, DrillStringMag = .002
                        });
                    }
                    else if(context.Request.Url!.AbsolutePath.StartsWith("/WellBore/"))
                        await JsonSerializer.SerializeAsync(context.Response.OutputStream,new { WellID=Guid.Parse("11111111-1111-1111-1111-111111111111") });
                    else if(context.Request.Url!.AbsolutePath.StartsWith("/Well/"))
                        await JsonSerializer.SerializeAsync(context.Response.OutputStream,new { ClusterID=Guid.Parse("11111111-1111-1111-1111-111111111111"), SlotID=Guid.Parse("11111111-1111-1111-1111-111111111111") });
                    else if(context.Request.Url!.AbsolutePath.StartsWith("/Cluster/"))
                        await JsonSerializer.SerializeAsync(context.Response.OutputStream,new { FieldID=Guid.NewGuid(),ReferencePoint=new { TVD=0 },Slots=new Dictionary<string,object>{["11111111-1111-1111-1111-111111111111"]=new { Latitude=new { GaussianValue=new { Mean=0,StandardDeviation=1e-8 } },Longitude=new { GaussianValue=new { Mean=0,StandardDeviation=1e-8 } } }} });
                    else context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException) { }
        });
        ServiceTestHost.ResetManagerSingletons();
        try
        {
            var database = new SqlConnectionManagerTrajectory(Path.Combine(directory, "Trajectory.db"),
                NullLogger<SqlConnectionManagerTrajectory>.Instance);
            Guid runId = Guid.NewGuid(), trajectoryId = Guid.NewGuid(), wellBoreId = Guid.NewGuid();
            List<SurveyStation> geometry = [Station(0, .2), Station(30, .25), Station(60, .3)];
            var run = new SurveyRun
            {
                MetaInfo = new MetaInfo { ID = runId }, WellBoreID = wellBoreId,
                SurveyInstrumentID = Guid.NewGuid(), SurveyStationList = geometry.Select(station => new SurveyStation(station)).ToList()
            };
            if (legacyTieIn)
            {
                geometry[0] = Station(-.76, 0);
                geometry[0].Azimuth = 0;
                run.SurveyStationList[0] = new SurveyStation(geometry[0]);
            }
            if (separateTieIn)
            {
                geometry.Add(Station(90, .35));
                run.SurveyStationList.Add(Station(90, .35));
                geometry[0].Inclination = 0;
                geometry[0].Azimuth = 0;
                run.SurveyStationList[0] = Station(-.76, 0);
            }
            if (mismatch) run.SurveyStationList[1].Inclination = .4;
            var trajectory = new OSDC.Drilling.Trajectory.Model.Trajectory
            {
                MetaInfo = new MetaInfo { ID = trajectoryId }, WellBoreID = wellBoreId,
                CalculationState = CalculationState.Completed, SurveyStationList = geometry,
                SurveyRunSectionList = [new() { SurveyRunID = runId, StartAbscissa = legacyTieIn ? -.76 : 0 }],
                // Geometry is already complete; ellipse reads must not require a new geometry calculation.
                TieInPoint = separateTieIn || legacyTieIn ? new SurveyStation(geometry[0]) : null,
                MDStep = 0
            };
            using (var connection = database.GetConnection()!)
            {
                foreach (var row in new[] { ("SurveyRun", runId, (object)run), ("Trajectory", trajectoryId, (object)trajectory) })
                {
                    using var insert = connection.CreateCommand();
                    insert.CommandText = $"INSERT INTO {row.Item1}Table (ID,{row.Item1}) VALUES (@id,@json)";
                    insert.Parameters.AddWithValue("@id", row.Item2.ToString());
                    insert.Parameters.AddWithValue("@json", JsonSerializer.Serialize(row.Item3, JsonSettings.Options));
                    insert.ExecuteNonQuery();
                }
            }
            var manager = SurveyStationEllipseCalculationManager.GetInstance(
                NullLogger<SurveyStationEllipseCalculationManager>.Instance, database);
            var request = new SurveyStationEllipseCalculation { ConfidenceFactor = .95 };
            var result = await manager.AddSurveyStationEllipseCalculationAsync(request, null, trajectoryId);
            if (mismatch)
            {
                Assert.That(result, Is.Null);
                Assert.That(request.CalculationMessage, Does.Contain("lineage"));
            }
            else
            {
                Assert.That(result, Is.Not.Null, request.CalculationMessage);
                Assert.That(result!.SurveyStationEllipseResultList, Has.Count.EqualTo(geometry.Count));
                Assert.That(result.SurveyStationEllipseResultList![2].HorizontalEllipse, Is.Not.Null);
                Assert.That(result.SurveyStationList![2].X, Is.EqualTo(geometry[2].X));
                Assert.That(result.SurveyStationList[2].TVD, Is.EqualTo(geometry[2].TVD));
                Assert.That(manager.GetSurveyStationEllipseCalculationById(result.MetaInfo!.ID), Is.Not.Null);
            }
        }
        finally
        {
            ServiceTestHost.ResetManagerSingletons();
            foreach (var client in clients) client.BaseUrl = originalUrls[client];
            foreach (var property in configuration) property.SetValue(null, originalConfiguration[property]);
            listener.Stop();
            await serve;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    private static SurveyStation Station(double md, double inclination) => new()
    {
        MD = md, Abscissa = md, Inclination = inclination, Azimuth = .3,
        X = md * .2, Y = md * .1, Z = md * .95
    };
}
