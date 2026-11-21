using System.Text.Json;
using OSDC.Drilling.Trajectory.Service.Managers;
using Surveying = OSDC.DotnetLibraries.Drilling.Surveying;
using Generated = OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class IscwsaGeneratedDtoRegressionTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Generated_survey_instrument_dto_calculates_u7_after_leading_stations_are_removed(int stationsToSkip)
    {
        Surveying.SurveyInstrument source = CreateIscwsaInstrument();
        Generated.SurveyInstrument generated = JsonSerializer.Deserialize<Generated.SurveyInstrument>(
            JsonSerializer.Serialize(source, JsonSettings.Options), JsonSettings.Options)!;

        Surveying.SurveyInstrument runtime = SurveyRunManager.ConvertSurveyInstrument(generated);
        Assert.That(runtime.ErrorSourceList, Is.All.Matches<Surveying.ErrorSource>(errorSource =>
            errorSource.WeightingFunctionMD == null && errorSource.WeightingFunctionIncl == null &&
            errorSource.WeightingFunctionAzim == null));

        List<Surveying.SurveyStation> stations = CreateU7Stations(runtime).Skip(stationsToSkip).ToList();
        Assert.That(Surveying.CovarianceCalculatorISCWSA.Calculate(stations), Is.True);
        Assert.That(stations, Is.All.Matches<Surveying.SurveyStation>(station => station.Covariance != null));
    }

    private static Surveying.SurveyInstrument CreateIscwsaInstrument() => new()
    {
        Name = "U7 ISCWSA MWD Rev5 DTO regression tool",
        ModelType = Surveying.SurveyInstrumentModelType.MWD_ISCWSA,
        BField = 50_000e-9,
        Dip = 72.0 * Math.PI / 180.0,
        Declination = -4.0 * Math.PI / 180.0,
        Gravity = 9.80665,
        ErrorSourceList =
        [
            Surveying.ErrorSourceFactory.Create_DRFR(magnitude: 0.35),
            Surveying.ErrorSourceFactory.Create_DSFS(magnitude: 0.00056),
            Surveying.ErrorSourceFactory.Create_DSTG(magnitude: 2.5e-7),
            Surveying.ErrorSourceFactory.Create_ABXY_TI1S(magnitude: 0.004),
            Surveying.ErrorSourceFactory.Create_MBXY_TI1(magnitude: 70e-9),
            Surveying.ErrorSourceFactory.Create_DECR(magnitude: 0.1 * Math.PI / 180.0)
        ]
    };

    private static List<Surveying.SurveyStation> CreateU7Stations(Surveying.SurveyInstrument tool) =>
    [
        new()
        {
            MD = -90.72, Abscissa = -90.72, Inclination = 0, Azimuth = 0,
            X = 6535270.882093326, Y = 328667.90173262125, Z = -90.72,
            RiemannianNorth = 6535270.882093326, RiemannianEast = 328667.90173262125,
            Latitude = 1.0285851086006512, Longitude = 0.09961349831157526,
            TVD = -90.72, VerticalSection = 0, SurveyTool = tool
        },
        new()
        {
            MD = -49.22, Abscissa = -49.22, Inclination = 0.029845130209103034, Azimuth = 2.966187063764363,
            X = 6535270.272354566, Y = 328668.0097947415, Z = -49.22616062055119,
            RiemannianNorth = 6535270.272354566, RiemannianEast = 328668.0097947415,
            TVD = -49.22616062055119, VerticalSection = 0.6192404848274852, SurveyTool = tool
        },
        new()
        {
            MD = 0.7800000000000011, Abscissa = 0.7800000000000011,
            Inclination = 0.07696902001294993, Azimuth = 2.9571113516539924,
            X = 6535267.647591092, Y = 328668.49269486626, Z = 0.6979295697585925,
            RiemannianNorth = 6535267.647591092, RiemannianEast = 328668.49269486626,
            TVD = 0.6979295697585925, VerticalSection = 3.288055919478057, SurveyTool = tool
        },
        new()
        {
            MD = 50.78, Abscissa = 50.78, Inclination = 0.12479104151759457, Azimuth = 2.8963738936845895,
            X = 6535262.738337163, Y = 328669.6009536944, Z = 50.43913203361417,
            RiemannianNorth = 6535262.738337163, RiemannianEast = 328669.6009536944,
            TVD = 50.43913203361417, VerticalSection = 8.32084955399468, SurveyTool = tool
        }
    ];
}
