using System.Net.Http.Headers;
using NORCE.Drilling.Trajectory.ModelShared;

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

        //[Test]
        //public async Task Test_Trajectory_GET()
        //{
        //    #region post a Trajectory
        //    // Create 1st instance of surveyStation
        //    SurveyStation surveyStation1 = PseudoConstructors.ConstructSurveyStation();
        //    // Create 2nd instance of surveyStation
        //    SurveyStation surveyStation2 = PseudoConstructors.ConstructSurveyStation();
        //    // Create instance of surveyStation
        //    Trajectory trajectory = PseudoConstructors.ConstructTrajectory();

        //    //Extract metainfo
        //    MetaInfo metaInfo = trajectory.MetaInfo;
        //    // Create 1st instance of surveyStation
        //    MetaInfo metaInfoSurveyStation1 = surveyStation1.MetaInfo;
        //    // Create 2nd instance of surveyStation           
        //    MetaInfo metaInfoSurveyStation2 = surveyStation2.MetaInfo;
        //    Guid guid = metaInfo.ID;

        //    try
        //    {
        //        await nSwagClient.PostTrajectoryAsync(trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST given Trajectory\n" + ex.Message);
        //    }
        //    #endregion

        //    #region GetAllTrajectoryId
        //    List<Guid> idList = [];
        //    try
        //    {
        //        idList = (List<Guid>)await nSwagClient.GetAllTrajectoryIdAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET all Trajectory ids\n" + ex.Message);
        //    }
        //    Assert.That(idList, Is.Not.Null);
        //    Assert.That(idList, Does.Contain(guid));
        //    #endregion

        //    #region GetAllTrajectoryMetaInfo
        //    List<MetaInfo> metaInfoList = [];
        //    try
        //    {
        //        metaInfoList = (List<MetaInfo>)await nSwagClient.GetAllTrajectoryMetaInfoAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET all Trajectory metainfos\n" + ex.Message);
        //    }
        //    Assert.That(metaInfoList, Is.Not.Null);
        //    IEnumerable<MetaInfo> metaInfoList2 =
        //        from elt in metaInfoList
        //        where elt.ID == guid
        //        select elt;
        //    Assert.That(metaInfoList2, Is.Not.Null);
        //    Assert.That(metaInfoList2, Is.Not.Empty);
        //    #endregion

        //    #region GetAllTrajectoryById
        //    Trajectory? trajectory2 = null;
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Not.Null);
        //    Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
        //    #endregion

        //    #region GetAllTrajectoryLight
        //    List<TrajectoryLight> trajectoryLightList = [];
        //    try
        //    {
        //        trajectoryLightList = (List<TrajectoryLight>)await nSwagClient.GetAllTrajectoryLightAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the list of TrajectoryLight\n" + ex.Message);
        //    }
        //    Assert.That(trajectoryLightList, Is.Not.Null);
        //    Assert.That(trajectoryLightList, Is.Not.Empty);
        //    IEnumerable<TrajectoryLight> trajectoryLightList2 =
        //        from elt in trajectoryLightList
        //        where elt.Name == trajectory.Name
        //        select elt;
        //    Assert.That(trajectoryLightList2, Is.Not.Null);
        //    Assert.That(trajectoryLightList2, Is.Not.Empty);
        //    #endregion

        //    #region GetAllTrajectory
        //    List<Trajectory> trajectoryList = new();
        //    try
        //    {
        //        trajectoryList = (List<Trajectory>)await nSwagClient.GetAllTrajectoryAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the list of Trajectory\n" + ex.Message);
        //    }
        //    Assert.That(trajectoryList, Is.Not.Null);
        //    IEnumerable<Trajectory> trajectoryList2 =
        //        from elt in trajectoryList
        //        where elt.Name == trajectory.Name
        //        select elt;
        //    Assert.That(trajectoryList2, Is.Not.Null);
        //    Assert.That(trajectoryList2, Is.Not.Empty);
        //    #endregion

        //    #region finally delete the new ID
        //    trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_Trajectory_POST()
        //{
        //    #region trying to post an empty guid
        //    // Create instance of surveyStation
        //    Trajectory trajectory = PseudoConstructors.ConstructTrajectory();

        //    //Extract metainfo
        //    MetaInfo metaInfo = trajectory.MetaInfo;
        //    Guid guid = metaInfo.ID;
        //    Trajectory? trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.PostTrajectoryAsync(trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(400));
        //        TestContext.WriteLine("Impossible to POST Trajectory with empty Guid\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(Guid.Empty);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(400));
        //        TestContext.WriteLine("Impossible to GET Trajectory identified by an empty Guid\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Null);
        //    #endregion

        //    #region post some corrupted data
        //    // post data with missing input that fails the calculation process
        //    #endregion

        //    #region posting a new ID in a valid state
        //    guid = Guid.NewGuid();
        //    metaInfo = new() { ID = guid };
        //    trajectory.MetaInfo = metaInfo;
        //    try
        //    {
        //        await nSwagClient.PostTrajectoryAsync(trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST Trajectory although it is in a valid state\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(guid));
        //    Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
        //    #endregion

        //    #region trying to repost the same ID
        //    bool conflict = false;
        //    try
        //    {
        //        await nSwagClient.PostTrajectoryAsync(trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        conflict = true;
        //        Assert.That(ex.StatusCode, Is.EqualTo(409));
        //        TestContext.WriteLine("Impossible to POST existing Trajectory\n" + ex.Message);
        //    }
        //    Assert.That(conflict, Is.True);
        //    #endregion

        //    #region finally delete the new ID
        //    trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET deleted Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_Trajectory_PUT()
        //{
        //    #region posting a new ID
        //    // Create 1st instance of surveyStation
        //    SurveyStation surveyStation1 = PseudoConstructors.ConstructSurveyStation();
        //    // Create 2nd instance of surveyStation
        //    SurveyStation surveyStation2 = PseudoConstructors.ConstructSurveyStation();
        //    // Create instance of surveyStation
        //    Trajectory trajectory = PseudoConstructors.ConstructTrajectory();

        //    //Extract metainfo
        //    MetaInfo metaInfo = trajectory.MetaInfo;
        //    // Create 1st instance of surveyStation
        //    MetaInfo metaInfoSurveyStation1 = surveyStation1.MetaInfo;
        //    // Create 2nd instance of surveyStation           
        //    MetaInfo metaInfoSurveyStation2 = surveyStation2.MetaInfo;
        //    Guid guid = metaInfo.ID;
        //    Trajectory? trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.PostTrajectoryAsync(trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST Trajectory\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(guid));
        //    Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
        //    #endregion

        //    #region updating the new Id
        //    trajectory.Name = "My test Trajectory with modified name";
        //    trajectory.LastModificationDate = DateTimeOffset.UtcNow;
        //    try
        //    {
        //        await nSwagClient.PutTrajectoryByIdAsync(trajectory.MetaInfo.ID, trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to PUT Trajectory of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the updated Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(trajectory.MetaInfo.ID));
        //    Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
        //    #endregion

        //    #region finally delete the new ID
        //    trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET deleted Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_Trajectory_DELETE()
        //{
        //    #region posting a new ID
        //    // Create instance of surveyStation
        //    Trajectory trajectory = PseudoConstructors.ConstructTrajectory();
        //    //Extract metainfo
        //    MetaInfo metaInfo = trajectory.MetaInfo;
        //    Guid guid = metaInfo.ID;
        //    Trajectory? trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.PostTrajectoryAsync(trajectory);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST Trajectory\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo, Is.Not.Null);
        //    Assert.That(trajectory2.MetaInfo.ID, Is.EqualTo(trajectory.MetaInfo.ID));
        //    Assert.That(trajectory2.Name, Is.EqualTo(trajectory.Name));
        //    #endregion

        //    #region finally delete the new ID
        //    trajectory2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE Trajectory of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        trajectory2 = await nSwagClient.GetTrajectoryByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET deleted Trajectory of given Id\n" + ex.Message);
        //    }
        //    Assert.That(trajectory2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_SurveyStation_GET()
        //{
        //    #region post a SurveyStation
        //    // Create instance of surveyStation
        //    SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
        //    MetaInfo metaInfo = surveyStation.MetaInfo;
        //    Guid guid = metaInfo.ID;

        //    try
        //    {
        //        await nSwagClient.PostSurveyStationAsync(surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST given SurveyStation\n" + ex.Message);
        //    }
        //    #endregion

        //    #region GetAllSurveyStationId
        //    List<Guid?> idList = [];
        //    try
        //    {
        //        idList = (List<Guid?>)await nSwagClient.GetAllSurveyStationIdAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET all SurveyStation ids\n" + ex.Message);
        //    }
        //    Assert.That(idList, Is.Not.Null);
        //    Assert.That(idList, Does.Contain(guid));
        //    #endregion

        //    #region GetAllSurveyStationMetaInfo
        //    List<MetaInfo> metaInfoList = [];
        //    try
        //    {
        //        metaInfoList = (List<MetaInfo>)await nSwagClient.GetAllSurveyStationMetaInfoAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET all SurveyStation metainfos\n" + ex.Message);
        //    }
        //    Assert.That(metaInfoList, Is.Not.Null);
        //    IEnumerable<MetaInfo> metaInfoList2 =
        //        from elt in metaInfoList
        //        where elt.ID == guid
        //        select elt;
        //    Assert.That(metaInfoList2, Is.Not.Null);
        //    Assert.That(metaInfoList2, Is.Not.Empty);
        //    #endregion

        //    #region GetAllSurveyStationById
        //    SurveyStation? surveyStation2 = null;
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo.ID, Is.EqualTo(guid));
        //    Assert.That(surveyStation2.Name, Is.EqualTo(surveyStation.Name));
        //    #endregion

        //    #region GetAllSurveyStation
        //    List<SurveyStation> surveyStationList = [];
        //    try
        //    {
        //        surveyStationList = (List<SurveyStation>)await nSwagClient.GetAllSurveyStationAsync();
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the list of SurveyStation\n" + ex.Message);
        //    }
        //    Assert.That(surveyStationList, Is.Not.Null);
        //    IEnumerable<SurveyStation> surveyStationList2 =
        //        from elt in surveyStationList
        //        where elt.Name == surveyStation.Name
        //        select elt;
        //    Assert.That(surveyStationList2, Is.Not.Null);
        //    Assert.That(surveyStationList2, Is.Not.Empty);
        //    #endregion

        //    #region finally delete the new ID
        //    surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE SurveyStation of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_SurveyStation_POST()
        //{
        //    #region trying to post an empty guid
        //    // Create instance of surveyStation
        //    SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
        //    MetaInfo metaInfo = surveyStation.MetaInfo;
        //    Guid guid = metaInfo.ID;

        //    SurveyStation? surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.PostSurveyStationAsync(surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(400));
        //        TestContext.WriteLine("Impossible to POST SurveyStation with empty Guid\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(Guid.Empty);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(400));
        //        TestContext.WriteLine("Impossible to GET SurveyStation identified by an empty Guid\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Null);
        //    #endregion

        //    #region posting a new ID in a valid state
        //    guid = Guid.NewGuid();
        //    metaInfo = new() { ID = guid };
        //    surveyStation.MetaInfo = metaInfo;
        //    try
        //    {
        //        await nSwagClient.PostSurveyStationAsync(surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST SurveyStation although it is in a valid state\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo.ID, Is.EqualTo(guid));
        //    Assert.That(surveyStation2.Name, Is.EqualTo(surveyStation.Name));
        //    #endregion

        //    #region trying to repost the same ID
        //    bool conflict = false;
        //    try
        //    {
        //        await nSwagClient.PostSurveyStationAsync(surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        conflict = true;
        //        Assert.That(ex.StatusCode, Is.EqualTo(409));
        //        TestContext.WriteLine("Impossible to POST existing SurveyStation\n" + ex.Message);
        //    }
        //    Assert.That(conflict, Is.True);
        //    #endregion

        //    #region finally delete the new ID
        //    surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE SurveyStation of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET deleted SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_SurveyStation_PUT()
        //{
        //    #region posting a new ID
        //    // Create instance of surveyStation
        //    SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
        //    MetaInfo metaInfo = surveyStation.MetaInfo;
        //    Guid guid = metaInfo.ID;

        //    SurveyStation? surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.PostSurveyStationAsync(surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST SurveyStation\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(surveyStation.MetaInfo.ID);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo.ID, Is.EqualTo(surveyStation.MetaInfo.ID));
        //    Assert.That(surveyStation2.Name, Is.EqualTo(surveyStation.Name));
        //    #endregion

        //    #region updating the new Id
        //    surveyStation.Name = "My test SurveyStation with modified name";
        //    surveyStation.LastModificationDate = DateTimeOffset.UtcNow;
        //    try
        //    {
        //        await nSwagClient.PutSurveyStationByIdAsync(surveyStation.MetaInfo.ID, surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to PUT SurveyStation of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(surveyStation.MetaInfo.ID);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the updated SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo.ID, Is.EqualTo(surveyStation.MetaInfo.ID));
        //    Assert.That(surveyStation2.Name, Is.EqualTo(surveyStation.Name));
        //    #endregion

        //    #region finally delete the new ID
        //    surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE SurveyStation of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(surveyStation.MetaInfo.ID);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET deleted SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Null);
        //    #endregion
        //}

        //[Test]
        //public async Task Test_SurveyStation_DELETE()
        //{
        //    #region posting a new ID
        //    // Create instance of surveyStation
        //    SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
        //    MetaInfo metaInfo = surveyStation.MetaInfo;
        //    Guid guid = metaInfo.ID;

        //    SurveyStation? surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.PostSurveyStationAsync(surveyStation);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to POST SurveyStation\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to GET the SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo, Is.Not.Null);
        //    Assert.That(surveyStation2.MetaInfo.ID, Is.EqualTo(guid));
        //    Assert.That(surveyStation2.Name, Is.EqualTo(surveyStation.Name));
        //    #endregion

        //    #region finally delete the new ID
        //    surveyStation2 = null;
        //    try
        //    {
        //        await nSwagClient.DeleteSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        TestContext.WriteLine("Impossible to DELETE SurveyStation of given Id\n" + ex.Message);
        //    }
        //    try
        //    {
        //        surveyStation2 = await nSwagClient.GetSurveyStationByIdAsync(guid);
        //    }
        //    catch (ApiException ex)
        //    {
        //        Assert.That(ex.StatusCode, Is.EqualTo(404));
        //        TestContext.WriteLine("Impossible to GET deleted SurveyStation of given Id\n" + ex.Message);
        //    }
        //    Assert.That(surveyStation2, Is.Null);
        //    #endregion
        //}

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient?.Dispose();
        }
    }
}