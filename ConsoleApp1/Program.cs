using ConsoleApp1;
using NORCE.Drilling.Trajectory.ModelShared;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

string host = "https://localhost:5001/";
//private static string host = "https://localhost:44368/";
//private static string host = "http://localhost:54949/";
HttpClient httpClient;
Client nSwagClient;
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; } // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
};
httpClient = new HttpClient(handler)
{
    BaseAddress = new Uri(host + "Trajectory/api/")
};
httpClient.DefaultRequestHeaders.Accept.Clear();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
nSwagClient = new Client(httpClient.BaseAddress.ToString(), httpClient);


#region post a Trajectory
List<SurveyStation> surveyStationList = [];
SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
surveyStation.Abscissa = 0;
surveyStation.Azimuth = 0;
surveyStation.Inclination = 0;
surveyStation.X = 0;
surveyStation.Y = 0;
surveyStation.Z = 0;
surveyStationList.Add(surveyStation);
SurveyStation surveyStation2 = PseudoConstructors.ConstructSurveyStation();
surveyStation2.Abscissa = 90;
surveyStation2.Azimuth = 3.05;
surveyStation2.Inclination = 0.056;
surveyStationList.Add(surveyStation2);
SurveyStation surveyStation3 = PseudoConstructors.ConstructSurveyStation();
surveyStation3.Abscissa = 109;
surveyStation3.Azimuth = 2.92;
surveyStation3.Inclination = 0.111;
surveyStationList.Add(surveyStation3);
//Trajectory trajectory = PseudoConstructors.ConstructTrajectory();
Trajectory trajectory = new()
{
    MetaInfo = new() { ID = Guid.NewGuid() },
    Name = "Default Name",
    Description = "Default Description",
    CreationDate = DateTimeOffset.UtcNow,
    LastModificationDate = DateTimeOffset.UtcNow,
    WellBoreID = new Guid(),
    SurveyStationList = surveyStationList,
    InterpolatedTrajectory = [],
    MDStep = 10.0,
};
//trajectory.SurveyStationList = ConstructSurveyStationList();
//Extract metainfo
try
{
    await nSwagClient.PostTrajectoryAsync(trajectory);
}
catch (ApiException ex)
{
    System.Console.WriteLine("Impossible to POST given Trajectory\n" + ex.Message);
}
#endregion

static List<SurveyStation> ConstructSurveyStationList()
{
    string jsonString =
                    "[{ \"Abscissa\": 0.0, \"Azimuth\": 0.000000, \"Inclination\": 0.000000, \"X\": 0.0, \"Y\": 0.0, \"Z\":0.0 }, " + // first survey station is complete
                    "{ \"Abscissa\": 90.0, \"Azimuth\": 3.051883, \"Inclination\": 0.056374 }, " +
                    "{ \"Abscissa\": 109.0, \"Azimuth\": 2.923426, \"Inclination\": 0.111701 }, " + // at least 3 survey stations are needed to complete the survey
                    "{ \"Abscissa\": 160.0, \"Azimuth\": 2.894806, \"Inclination\": 0.237486 }, " +
                    "{ \"Abscissa\": 216.0, \"Azimuth\": 3.078990, \"Inclination\": 0.314985 }, " +
                    "{ \"Abscissa\": 243.0, \"Azimuth\": 2.971742, \"Inclination\": 0.315332 }, " +
                    "{ \"Abscissa\": 316.0, \"Azimuth\": 3.054304, \"Inclination\": 0.381440 }, " +
                    "{ \"Abscissa\": 347.0, \"Azimuth\": 3.063636, \"Inclination\": 0.388621 }, " +
                    "{ \"Abscissa\": 404.0, \"Azimuth\": 3.002276, \"Inclination\": 0.570171 }, " +
                    "{ \"Abscissa\": 470.0, \"Azimuth\": 2.938753, \"Inclination\": 0.732573 }, " +
                    "{ \"Abscissa\": 519.2, \"Azimuth\": 2.997911, \"Inclination\": 0.776672 }, " +
                    "{ \"Abscissa\": 546.8, \"Azimuth\": 3.007962, \"Inclination\": 0.783520 }, " +
                    "{ \"Abscissa\": 602.7, \"Azimuth\": 2.979085, \"Inclination\": 0.830482 }, " +
                    "{ \"Abscissa\": 659.4, \"Azimuth\": 2.962783, \"Inclination\": 0.932835 }, " +
                    "{ \"Abscissa\": 716.7, \"Azimuth\": 3.002276, \"Inclination\": 1.056256 }, " +
                    "{ \"Abscissa\": 745.2, \"Azimuth\": 3.024437, \"Inclination\": 1.125621 }, " +
                    "{ \"Abscissa\": 805.4, \"Azimuth\": 3.064820, \"Inclination\": 1.284859 }, " +
                    "{ \"Abscissa\": 859.2, \"Azimuth\": 3.103693, \"Inclination\": 1.331733 }, " +
                    "{ \"Abscissa\": 910.7, \"Azimuth\": 3.146106, \"Inclination\": 1.411521 }, " +
                    "{ \"Abscissa\": 958.4, \"Azimuth\": 3.157970, \"Inclination\": 1.415886 }, " +
                    "{ \"Abscissa\": 1006.7, \"Azimuth\": 3.159725, \"Inclination\": 1.402615 }, " +
                    "{ \"Abscissa\": 1016.3, \"Azimuth\": 3.163219, \"Inclination\": 1.402615 }]";
    List<SurveyStation> surveyStationList = [];
    SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
    surveyStation.Abscissa = 0;
    surveyStation.Azimuth = 0;
    surveyStation.Inclination = 0;
    surveyStation.X = 0;
    surveyStation.Y = 0;
    surveyStation.Z = 0;
    surveyStationList.Add(surveyStation);
    SurveyStation surveyStation2 = PseudoConstructors.ConstructSurveyStation();
    surveyStation2.Abscissa = 90;
    surveyStation2.Azimuth = 3.05;
    surveyStation2.Inclination = 0.056;
    surveyStationList.Add(surveyStation2);
    SurveyStation surveyStation3 = PseudoConstructors.ConstructSurveyStation();
    surveyStation3.Abscissa = 109;
    surveyStation3.Azimuth = 2.92;
    surveyStation3.Inclination = 0.111;
    surveyStationList.Add(surveyStation3);
    //var list = JsonSerializer.Deserialize<List<SurveyStation>>(jsonString, JsonSettings.Options);
    //if (list is { })
    //{
    //    foreach (var station in list)
    //    {
    //        SurveyStation surveyStation = PseudoConstructors.ConstructSurveyStation();
    //        surveyStation.Abscissa = station.Abscissa;
    //        surveyStation.Azimuth = station.Azimuth;
    //        surveyStation.Inclination = station.Inclination;
    //        surveyStation.X = station.X;
    //        surveyStation.Y = station.Y;
    //        surveyStation.Z = station.Z;
    //        surveyStationList.Add(surveyStation);
    //    }
    //}
    return surveyStationList;
}

static List<SurveyStation> ConstructCorruptedSurveyStationList()
{
    string jsonString =
                    "{ \"Abscissa\": 0.0, \"Azimuth\": 0.000000, \"Inclination\": 0.000000 }, " + // first survey station should be complete
                    "{ \"Abscissa\": 90.0, \"Azimuth\": 3.051883, \"Inclination\": 0.056374 }, " +
                    "{ \"Abscissa\": 109.0, \"Azimuth\": 2.923426, \"Inclination\": 0.111701 }, " +
                    "{ \"Abscissa\": 160.0, \"Azimuth\": 2.894806, \"Inclination\": 0.237486 }, " +
                    "{ \"Abscissa\": 216.0, \"Azimuth\": 3.078990, \"Inclination\": 0.314985 }, " +
                    "{ \"Abscissa\": 243.0, \"Azimuth\": 2.971742, \"Inclination\": 0.315332 }, " +
                    "{ \"Abscissa\": 316.0, \"Azimuth\": 3.054304, \"Inclination\": 0.381440 }, " +
                    "{ \"Abscissa\": 347.0, \"Azimuth\": 3.063636, \"Inclination\": 0.388621 }, " +
                    "{ \"Abscissa\": 404.0, \"Azimuth\": 3.002276, \"Inclination\": 0.570171 }, " +
                    "{ \"Abscissa\": 470.0, \"Azimuth\": 2.938753, \"Inclination\": 0.732573 }, " +
                    "{ \"Abscissa\": 519.2, \"Azimuth\": 2.997911, \"Inclination\": 0.776672 }, " +
                    "{ \"Abscissa\": 546.8, \"Azimuth\": 3.007962, \"Inclination\": 0.783520 }, " +
                    "{ \"Abscissa\": 602.7, \"Azimuth\": 2.979085, \"Inclination\": 0.830482 }, " +
                    "{ \"Abscissa\": 659.4, \"Azimuth\": 2.962783, \"Inclination\": 0.932835 }, " +
                    "{ \"Abscissa\": 716.7, \"Azimuth\": 3.002276, \"Inclination\": 1.056256 }, " +
                    "{ \"Abscissa\": 745.2, \"Azimuth\": 3.024437, \"Inclination\": 1.125621 }, " +
                    "{ \"Abscissa\": 805.4, \"Azimuth\": 3.064820, \"Inclination\": 1.284859 }, " +
                    "{ \"Abscissa\": 859.2, \"Azimuth\": 3.103693, \"Inclination\": 1.331733 }, " +
                    "{ \"Abscissa\": 910.7, \"Azimuth\": 3.146106, \"Inclination\": 1.411521 }, " +
                    "{ \"Abscissa\": 958.4, \"Azimuth\": 3.157970, \"Inclination\": 1.415886 }, " +
                    "{ \"Abscissa\": 1006.7, \"Azimuth\": 3.159725, \"Inclination\": 1.402615 }, " +
                    "{ \"Abscissa\": 1016.3, \"Azimuth\": 3.163219, \"Inclination\": 1.402615 }";
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
            surveyStation.X = station.X;
            surveyStation.Y = station.Y;
            surveyStation.Z = station.Z;
            surveyStationList.Add(surveyStation);
        }
    }
    return surveyStationList;
}