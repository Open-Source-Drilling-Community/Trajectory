using System.Net.Http.Headers;
using System.Text.Json;
using NORCE.Drilling.Trajectory.ModelShared;
using NORCE.Drilling.Trajectory.ServiceTest;

namespace ServiceTest
{
    public class Tests
    {
        // testing outside Visual Studio requires using http port (https faces authentication issues both in console and on github)
        private static string host = "http://localhost:8080/";
        //private static string host = "https://localhost:5001/";
        //private static string host = "https://localhost:44368/";
        //private static string host = "http://localhost:54949/";
        private static HttpClient httpClient;
        private static Client nSwagClient;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }; // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
            httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(host + "Trajectory/api/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            nSwagClient = new Client(httpClient.BaseAddress.ToString(), httpClient);
        }

        [Test]
        public async Task Test_Trajectory_GET()
        {
            #region post a Trajectory
            // Create instance of Trajectory
            Trajectory trajectory = PseudoConstructors.ConstructTrajectory();
            trajectory.MDStep = 10;
            trajectory.SurveyStationList = ConstructSurveyStationList();
            //Extract metainfo
            MetaInfo metaInfo = trajectory.MetaInfo;
            Guid guid = metaInfo.ID;
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to POST given Trajectory\n" + ex.Message);
            }
            #endregion

            #region GetAllTrajectoryId
            List<Guid> idList = [];
            try
            {
                idList = (List<Guid>)await nSwagClient.GetAllTrajectoryIdAsync();
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET all Trajectory ids\n" + ex.Message);
            }
            Assert.That(idList, Is.Not.Null);
            Assert.That(idList, Does.Contain(guid));
            #endregion

            #region GetAllTrajectoryMetaInfo
            List<MetaInfo> metaInfoList = [];
            try
            {
                metaInfoList = (List<MetaInfo>)await nSwagClient.GetAllTrajectoryMetaInfoAsync();
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET all Trajectory metainfos\n" + ex.Message);
            }
            Assert.That(metaInfoList, Is.Not.Null);
            IEnumerable<MetaInfo> metaInfoList2 =
                from elt in metaInfoList
                where elt.ID == guid
                select elt;
            Assert.That(metaInfoList2, Is.Not.Null);
            Assert.That(metaInfoList2, Is.Not.Empty);
            #endregion

            #region GetAllTrajectoryById
            Trajectory? trajectory2 = null;
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Not.Null);
            Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
            #endregion

            #region GetAllTrajectoryLight
            List<TrajectoryLight> trajectoryLightList = [];
            try
            {
                trajectoryLightList = (List<TrajectoryLight>)await nSwagClient.GetAllTrajectoryLightAsync();
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET the list of TrajectoryLight\n" + ex.Message);
            }
            Assert.That(trajectoryLightList, Is.Not.Null);
            Assert.That(trajectoryLightList, Is.Not.Empty);
            IEnumerable<TrajectoryLight> trajectoryLightList2 =
                from elt in trajectoryLightList
                where elt.Name == trajectory.Name
                select elt;
            Assert.That(trajectoryLightList2, Is.Not.Null);
            Assert.That(trajectoryLightList2, Is.Not.Empty);
            #endregion

            #region GetAllTrajectory
            List<Trajectory> trajectoryList = new();
            try
            {
                trajectoryList = (List<Trajectory>)await nSwagClient.GetAllTrajectoryAsync();
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET the list of Trajectory\n" + ex.Message);
            }
            Assert.That(trajectoryList, Is.Not.Null);
            IEnumerable<Trajectory> trajectoryList2 =
                from elt in trajectoryList
                where elt.Name == trajectory.Name
                select elt;
            Assert.That(trajectoryList2, Is.Not.Null);
            Assert.That(trajectoryList2, Is.Not.Empty);
            #endregion

            #region finally delete the new ID
            trajectory2 = null;
            try
            {
                await nSwagClient.DeleteTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
                TestContext.WriteLine("Impossible to GET Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Null);
            #endregion
        }

        [Test]
        public async Task Test_Trajectory_POST()
        {
            #region trying to post an empty guid
            // Create instance of trajectory
            Trajectory trajectory = PseudoConstructors.ConstructTrajectory();
            trajectory.MDStep = 10;
            trajectory.MetaInfo.ID = Guid.Empty;
            trajectory.SurveyStationList = ConstructSurveyStationList();
            Trajectory? trajectory2 = null;
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                TestContext.WriteLine("Impossible to POST Trajectory with empty Guid\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(Guid.Empty);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                TestContext.WriteLine("Impossible to GET Trajectory identified by an empty Guid\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Null);
            #endregion

            #region post some corrupted data with valid guid
            Guid guid = Guid.NewGuid(); // fixing the empty guid issue
            MetaInfo metaInfo = new() { ID = guid };
            trajectory.MetaInfo = metaInfo;
            trajectory.MDStep = 10;
            trajectory.SurveyStationList = ConstructCorruptedSurveyStationList(); // introducing a corrupted input data issue
            trajectory2 = null;
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(500));
                TestContext.WriteLine("Impossible to POST Trajectory with corrupted input data\n" + ex.Message);
            }
            #endregion

            #region posting valid data with valid guid
            metaInfo = new() { ID = guid };
            trajectory.MetaInfo = metaInfo;
            trajectory.MDStep = 10;
            trajectory.SurveyStationList = ConstructSurveyStationList();
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to POST Trajectory although it is in a valid state\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET the Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(guid));
            Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
            #endregion

            #region trying to repost the same ID
            bool conflict = false;
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                conflict = true;
                Assert.That(ex.StatusCode, Is.EqualTo(409));
                TestContext.WriteLine("Impossible to POST existing Trajectory\n" + ex.Message);
            }
            Assert.That(conflict, Is.True);
            #endregion

            #region finally delete the new ID
            trajectory2 = null;
            try
            {
                await nSwagClient.DeleteTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
                TestContext.WriteLine("Impossible to GET deleted Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Null);
            #endregion
        }

        [Test]
        public async Task Test_Trajectory_PUT()
        {
            #region posting a new ID
            // Create instance of trajectory
            Trajectory trajectory = PseudoConstructors.ConstructTrajectory();
            trajectory.MDStep = 10;
            trajectory.SurveyStationList = ConstructSurveyStationList();
            //Extract metainfo
            MetaInfo metaInfo = trajectory.MetaInfo;
            Guid guid = metaInfo.ID;
            Trajectory? trajectory2 = null;
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to POST Trajectory\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET the Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(guid));
            Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
            #endregion

            #region updating the new Id
            trajectory.Name = "My test Trajectory with modified name";
            trajectory.LastModificationDate = DateTimeOffset.UtcNow;
            try
            {
                await nSwagClient.PutTrajectoryByIdAsync(trajectory.MetaInfo.ID, trajectory);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to PUT Trajectory of given Id\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET the updated Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(trajectory.MetaInfo.ID));
            Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
            #endregion

            #region finally delete the new ID
            trajectory2 = null;
            try
            {
                await nSwagClient.DeleteTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
                TestContext.WriteLine("Impossible to GET deleted Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Null);
            #endregion
        }

        [Test]
        public async Task Test_Trajectory_DELETE()
        {
            #region posting a new ID
            // Create instance of trajectory
            Trajectory trajectory = PseudoConstructors.ConstructTrajectory();
            trajectory.MDStep = 10;
            trajectory.SurveyStationList = ConstructSurveyStationList();
            //Extract metainfo
            MetaInfo metaInfo = trajectory.MetaInfo;
            Guid guid = metaInfo.ID;
            Trajectory? trajectory2 = null;
            try
            {
                await nSwagClient.PostTrajectoryAsync(trajectory);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to POST Trajectory\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to GET the Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo, Is.Not.Null);
            Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(trajectory.MetaInfo.ID));
            Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
            #endregion

            #region finally delete the new ID
            trajectory2 = null;
            try
            {
                await nSwagClient.DeleteTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
            }
            try
            {
                trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
                TestContext.WriteLine("Impossible to GET deleted Trajectory of given Id\n" + ex.Message);
            }
            Assert.That(trajectory2, Is.Null);
            #endregion
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient?.Dispose();
        }

        private static List<SurveyStation> ConstructSurveyStationList()
        {
            string jsonString =
                            "[{ \"MD\": 0.0, \"Azimuth\": 0.000000, \"Inclination\": 0.000000, \"RiemannianNorth\": 0.0, \"RiemannianEast\": 0.0, \"TVD\":0.0 }, " + // first survey station is complete
                            "{ \"MD\": 90.0, \"Azimuth\": 3.051883, \"Inclination\": 0.056374 }, " +
                            "{ \"MD\": 109.0, \"Azimuth\": 2.923426, \"Inclination\": 0.111701 }, " + // at least 3 survey stations are needed to complete the survey
                            "{ \"MD\": 160.0, \"Azimuth\": 2.894806, \"Inclination\": 0.237486 }, " +
                            "{ \"MD\": 216.0, \"Azimuth\": 3.078990, \"Inclination\": 0.314985 }, " +
                            "{ \"MD\": 243.0, \"Azimuth\": 2.971742, \"Inclination\": 0.315332 }, " +
                            "{ \"MD\": 316.0, \"Azimuth\": 3.054304, \"Inclination\": 0.381440 }, " +
                            "{ \"MD\": 347.0, \"Azimuth\": 3.063636, \"Inclination\": 0.388621 }, " +
                            "{ \"MD\": 404.0, \"Azimuth\": 3.002276, \"Inclination\": 0.570171 }, " +
                            "{ \"MD\": 470.0, \"Azimuth\": 2.938753, \"Inclination\": 0.732573 }, " +
                            "{ \"MD\": 519.2, \"Azimuth\": 2.997911, \"Inclination\": 0.776672 }, " +
                            "{ \"MD\": 546.8, \"Azimuth\": 3.007962, \"Inclination\": 0.783520 }, " +
                            "{ \"MD\": 602.7, \"Azimuth\": 2.979085, \"Inclination\": 0.830482 }, " +
                            "{ \"MD\": 659.4, \"Azimuth\": 2.962783, \"Inclination\": 0.932835 }, " +
                            "{ \"MD\": 716.7, \"Azimuth\": 3.002276, \"Inclination\": 1.056256 }, " +
                            "{ \"MD\": 745.2, \"Azimuth\": 3.024437, \"Inclination\": 1.125621 }, " +
                            "{ \"MD\": 805.4, \"Azimuth\": 3.064820, \"Inclination\": 1.284859 }, " +
                            "{ \"MD\": 859.2, \"Azimuth\": 3.103693, \"Inclination\": 1.331733 }, " +
                            "{ \"MD\": 910.7, \"Azimuth\": 3.146106, \"Inclination\": 1.411521 }, " +
                            "{ \"MD\": 958.4, \"Azimuth\": 3.157970, \"Inclination\": 1.415886 }, " +
                            "{ \"MD\": 1006.7, \"Azimuth\": 3.159725, \"Inclination\": 1.402615 }, " +
                            "{ \"MD\": 1016.3, \"Azimuth\": 3.163219, \"Inclination\": 1.402615 }]";
            List<SurveyStation> surveyStationList = [];
            var list = JsonSerializer.Deserialize<List<SurveyStation>>(jsonString, JsonSettings.Options);
            if (list is { })
            {
                foreach (var station in list)
                {
                    SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
                    surveyStation.MD = station.MD;
                    surveyStation.Azimuth = station.Azimuth;
                    surveyStation.Inclination = station.Inclination;
                    surveyStation.RiemannianNorth = station.RiemannianNorth;
                    surveyStation.RiemannianEast = station.RiemannianEast;
                    surveyStation.TVD = station.TVD;
                    surveyStationList.Add(surveyStation);
                }
            }
            return surveyStationList;
        }

        private static List<SurveyStation> ConstructCorruptedSurveyStationList()
        {
            string jsonString =
                            "[{ \"MD\": 0.0, \"Azimuth\": 0.000000, \"Inclination\": 0.000000 }, " + // first survey station should be complete
                            "{ \"MD\": 90.0, \"Azimuth\": 3.051883, \"Inclination\": 0.056374 }, " +
                            "{ \"MD\": 109.0, \"Azimuth\": 2.923426, \"Inclination\": 0.111701 }, " +
                            "{ \"MD\": 160.0, \"Azimuth\": 2.894806, \"Inclination\": 0.237486 }, " +
                            "{ \"MD\": 216.0, \"Azimuth\": 3.078990, \"Inclination\": 0.314985 }, " +
                            "{ \"MD\": 243.0, \"Azimuth\": 2.971742, \"Inclination\": 0.315332 }, " +
                            "{ \"MD\": 316.0, \"Azimuth\": 3.054304, \"Inclination\": 0.381440 }, " +
                            "{ \"MD\": 347.0, \"Azimuth\": 3.063636, \"Inclination\": 0.388621 }, " +
                            "{ \"MD\": 404.0, \"Azimuth\": 3.002276, \"Inclination\": 0.570171 }, " +
                            "{ \"MD\": 470.0, \"Azimuth\": 2.938753, \"Inclination\": 0.732573 }, " +
                            "{ \"MD\": 519.2, \"Azimuth\": 2.997911, \"Inclination\": 0.776672 }, " +
                            "{ \"MD\": 546.8, \"Azimuth\": 3.007962, \"Inclination\": 0.783520 }, " +
                            "{ \"MD\": 602.7, \"Azimuth\": 2.979085, \"Inclination\": 0.830482 }, " +
                            "{ \"MD\": 659.4, \"Azimuth\": 2.962783, \"Inclination\": 0.932835 }, " +
                            "{ \"MD\": 716.7, \"Azimuth\": 3.002276, \"Inclination\": 1.056256 }, " +
                            "{ \"MD\": 745.2, \"Azimuth\": 3.024437, \"Inclination\": 1.125621 }, " +
                            "{ \"MD\": 805.4, \"Azimuth\": 3.064820, \"Inclination\": 1.284859 }, " +
                            "{ \"MD\": 859.2, \"Azimuth\": 3.103693, \"Inclination\": 1.331733 }, " +
                            "{ \"MD\": 910.7, \"Azimuth\": 3.146106, \"Inclination\": 1.411521 }, " +
                            "{ \"MD\": 958.4, \"Azimuth\": 3.157970, \"Inclination\": 1.415886 }, " +
                            "{ \"MD\": 1006.7, \"Azimuth\": 3.159725, \"Inclination\": 1.402615 }, " +
                            "{ \"MD\": 1016.3, \"Azimuth\": 3.163219, \"Inclination\": 1.402615 }]";
            List<SurveyStation> surveyStationList = [];
            var list = JsonSerializer.Deserialize<List<SurveyStation>>(jsonString, JsonSettings.Options);
            if (list is { })
            {
                SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
                foreach (var station in list)
                {
                    surveyStation.Abscissa = station.Abscissa;
                    surveyStation.Azimuth = station.Azimuth;
                    surveyStation.Inclination = station.Inclination;
                    surveyStation.RiemannianNorth = station.RiemannianNorth;
                    surveyStation.RiemannianEast = station.RiemannianEast;
                    surveyStation.TVD = station.TVD;
                    surveyStationList.Add(surveyStation);
                }
            }
            return surveyStationList;
        }
    }
}