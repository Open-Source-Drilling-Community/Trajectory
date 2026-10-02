using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class TrajectoryAggregationCalculatorTests
{
    [Test]
    public void U3TrajectoryWithOverlappingConstantPeriodsCalculatesSuccessfully()
    {
        TrajectoryModel source = new()
        {
            SurveyStationList = U3Stations()
        };
        TrajectoryAggregation aggregation = new()
        {
            TrajectoryID = Guid.NewGuid()
        };
        TrajectoryAggregationCase calculation = new()
        {
            TrajectoryAggregationList = [aggregation]
        };

        bool success = calculation.Calculate(id => id == aggregation.TrajectoryID ? source : null);
        SurveyPoint? aggregateEnd = aggregation.AggregatedSurveyPointList?.LastOrDefault();
        SurveyPoint? aggregateSecond = aggregation.AggregatedSurveyPointList?.Skip(1).FirstOrDefault();
        SurveyStation sourceStart = source.SurveyStationList![0];
        SurveyStation sourceEnd = source.SurveyStationList![^1];
        double sourceNorthDisplacement = sourceEnd.RiemannianNorth!.Value - sourceStart.RiemannianNorth!.Value;
        double sourceEastDisplacement = sourceEnd.RiemannianEast!.Value - sourceStart.RiemannianEast!.Value;
        double aggregateNorthDisplacement = aggregateEnd!.RiemannianNorth!.Value - sourceStart.RiemannianNorth.Value;
        double aggregateEastDisplacement = aggregateEnd.RiemannianEast!.Value - sourceStart.RiemannianEast.Value;
        double horizontalDirectionDotProduct =
            sourceNorthDisplacement * aggregateNorthDisplacement + sourceEastDisplacement * aggregateEastDisplacement;
        double endpointDistance = Math.Sqrt(
            Math.Pow(aggregateEnd.RiemannianNorth.Value - sourceEnd.RiemannianNorth.Value, 2.0) +
            Math.Pow(aggregateEnd.RiemannianEast.Value - sourceEnd.RiemannianEast.Value, 2.0) +
            Math.Pow(aggregateEnd.TVD!.Value - sourceEnd.TVD!.Value, 2.0));

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True, aggregation.CalculationMessage);
            Assert.That(calculation.CalculationState, Is.EqualTo(CalculationState.Completed));
            Assert.That(aggregation.CalculationState, Is.EqualTo(CalculationState.Completed));
            Assert.That(aggregation.OriginalReferenceStationCount, Is.EqualTo(23));
            Assert.That(aggregation.SectionList, Is.Not.Empty);
            Assert.That(aggregation.AggregatedSurveyPointList, Is.Not.Empty);
            Assert.That(aggregation.DistanceResultList, Has.Count.EqualTo(aggregation.CoarsenedReferencePointCount));
            Assert.That(aggregateSecond!.RiemannianNorth, Is.LessThan(sourceStart.RiemannianNorth), "The vertical start must depart towards U3's southerly heading.");
            Assert.That(aggregateSecond.RiemannianEast, Is.GreaterThan(sourceStart.RiemannianEast), "The vertical start must depart towards U3's easterly component.");
            Assert.That(horizontalDirectionDotProduct, Is.GreaterThan(0.0), "The aggregate path must not mirror the source displacement.");
            Assert.That(endpointDistance, Is.LessThan(35.0));
            Assert.That(aggregation.DistanceResultList!.Max(result => result.CenterToCenterDistance), Is.LessThan(35.0));
        });
    }

    private static List<SurveyStation> U3Stations()
    {
        double[][] values =
        [
            [-91.2, 0.0, 0.0, -91.2, 6535267.00301702, 328671.1614195671],
            [-49.22, 0.02984513466228953, 2.966116779673778, -49.22623187778105, 6535266.386233453, 328671.2707749292],
            [0.7800000000000011, 0.07696902915291015, 2.9570840628854005, 0.6978582860341547, 6535263.761488429, 328671.75377833017],
            [50.78, 0.12479116435322367, 2.8963570907648255, 50.439060344734706, 6535258.852253639, 328672.8621402485],
            [100.78, 0.14573509170645463, 2.900897331059204, 99.98145050610819, 6535252.307591311, 328674.48310008156],
            [150.78, 0.15271636641869787, 2.914162444034333, 149.425690470863, 6535245.076570689, 328676.2060369644],
            [200.78, 0.15254177274819863, 2.9359789906681466, 198.8444746123927, 6535237.652656779, 328677.8391254093],
            [250.78, 0.1864011462127813, 2.935806982997934, 248.12582286236594, 6535229.397778933, 328679.56158589246],
            [300.78, 0.2752383032270253, 2.976476909678966, 296.7841167097465, 6535218.152970874, 328681.62643981556],
            [350.78, 0.3546508244018595, 2.950822345569193, 344.31254976458507, 6535202.918810123, 328684.39083900413],
            [400.78, 0.39968040146335526, 2.9059680768357983, 390.795287828824, 6535184.931987677, 328688.3087769406],
            [450.78, 0.3872884849461403, 2.935638471824987, 436.97442536985875, 6535166.229813741, 328692.510800551],
            [500.78, 0.38309961044105983, 2.967577921741953, 483.31122450576424, 6535147.783332728, 328696.05970564764],
            [550.78, 0.38659024829150856, 2.9701959518977494, 529.6540376946334, 6535129.291812868, 328699.2853248534],
            [600.78, 0.3888591338398203, 2.9827623453109404, 575.9427003919362, 6535110.645048683, 328702.39211032866],
            [650.78, 0.40840670997784445, 3.026744939988745, 622.0227101509421, 6535091.421667926, 328705.0291640064],
            [700.78, 0.4136426334322702, 3.049085232036247, 667.8585569864238, 6535071.552489293, 328707.09521736443],
            [814.78, 0.3176494778881951, 3.0543195252535775, 774.2815131215093, 6535030.972961946, 328710.7662116121],
            [885.78, 0.3054320370260543, 3.106679184232585, 841.8647744168161, 6535009.258019892, 328712.1053294043],
            [905.78, 0.30543195539339985, 3.141585834467332, 860.9392922960917, 6535003.245693119, 328712.2103161793],
            [922.78, 0.28797869523224956, 3.124132061288359, 877.1963052739194, 6534998.275804975, 328712.2524846864],
            [951.78, 0.2844883352921184, 2.9845053863106266, 905.0199983912321, 6534990.137604863, 328712.9611460306],
            [982.78, 0.3577923117971354, 2.9146938185442055, 934.4297472299482, 6534980.547083098, 328714.8637621408]
        ];

        return values.Select(value => new SurveyStation
        {
            MD = value[0],
            Abscissa = value[0],
            Inclination = value[1],
            Azimuth = value[2],
            TVD = value[3],
            Z = value[3],
            RiemannianNorth = value[4],
            X = value[4],
            RiemannianEast = value[5],
            Y = value[5]
        }).ToList();
    }
}
