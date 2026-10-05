using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Globalization;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

[TestFixture]
public sealed class DirectionalControlUllriggCalibrationTests
{
    [Test]
    public void Aggregated_U3_reference_can_drive_a_directional_control_evaluation()
    {
        Guid wellBoreId = Guid.NewGuid();
        TrajectoryModel actual = ReadActualTrajectory("U3-MD-Incl-Az.txt", wellBoreId);
        TrajectoryAggregation aggregation = new() { TrajectoryID = actual.MetaInfo!.ID };
        TrajectoryAggregationCase aggregationCase = new() { TrajectoryAggregationList = [aggregation] };
        Assert.That(aggregationCase.Calculate(id => id == actual.MetaInfo.ID ? actual : null), Is.True,
            aggregation.CalculationMessage);

        TrajectoryModel reference = new()
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
            WellBoreID = wellBoreId,
            LastModificationDate = DateTimeOffset.UtcNow,
            CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
            SurveyStationList = aggregation.AggregatedSurveyPointList!.Select(point => new SurveyStation
            {
                MD = point.MD ?? point.Abscissa,
                Abscissa = point.MD ?? point.Abscissa,
                Inclination = point.Inclination,
                Azimuth = point.Azimuth,
                TVD = point.TVD,
                RiemannianNorth = point.RiemannianNorth,
                RiemannianEast = point.RiemannianEast,
                Curvature = point.Curvature,
                BUR = point.BUR,
                TUR = point.TUR,
                Toolface = point.Toolface
            }).ToList()
        };
        DirectionalControlEvaluationCase evaluation = new()
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
            ReferenceTrajectoryID = reference.MetaInfo.ID,
            ActualTrajectoryID = actual.MetaInfo.ID,
            CurveType = ExtrapolationCurveType.CircularArc,
            StartActualMD = 100,
            EndActualMD = 900,
            EvaluationInterval = 30,
            ReferenceMDAdvance = 30,
            MinimumBundleLength = 60,
            MinimumBundleSampleCount = 2
        };

        bool calculated = DirectionalControlEvaluationCalculator.Calculate(evaluation, reference, actual);

        Assert.Multiple(() =>
        {
            Assert.That(calculated, Is.True, evaluation.CalculationMessage);
            Assert.That(evaluation.SampleList, Is.Not.Empty);
            Assert.That(evaluation.SampleList!.Count(sample => sample.IsValid), Is.GreaterThan(10));
            Assert.That(evaluation.BundleList, Is.Not.Empty);
        });
    }

    private static TrajectoryModel ReadActualTrajectory(string fileName, Guid wellBoreId)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Ullrigg", fileName);
        List<SurveyStation> stations = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split('\t'))
            .Select(parts => new SurveyStation
            {
                MD = Parse(parts[0]),
                Abscissa = Parse(parts[0]),
                Inclination = Parse(parts[1]) * Math.PI / 180.0,
                Azimuth = Parse(parts[2]) * Math.PI / 180.0
            }).ToList();
        stations[0].TVD = 0.0;
        stations[0].RiemannianNorth = 0.0;
        stations[0].RiemannianEast = 0.0;
        stations[0].VerticalSection = 0.0;
        TrajectoryModel result = new()
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
            WellBoreID = wellBoreId,
            LastModificationDate = DateTimeOffset.UtcNow,
            CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
            MDStep = 10,
            SurveyStationList = stations
        };
        Assert.That(result.Calculate(), Is.True);
        return result;
    }

    private static double Parse(string value) =>
        double.Parse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
}
