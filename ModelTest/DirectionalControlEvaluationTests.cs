using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

[TestFixture]
public sealed class DirectionalControlEvaluationTests
{
    [Test]
    public void Evaluates_forward_actual_intervals_against_first_reconnect_section()
    {
        Guid wellBoreId = Guid.NewGuid();
        TrajectoryModel reference = CreateTrajectory(wellBoreId,
            Station(0, 0, 0, 1000, 0, 0),
            Station(400, 0, 0, 1400, 0, 0));
        TrajectoryModel actual = CreateTrajectory(wellBoreId,
            Station(0, 0, 0, 1000, -50, 0),
            Station(100, 0, 0, 1100, -50, 0),
            Station(200, 0, 0, 1200, -50, 0),
            Station(300, 0, 0, 1300, -50, 0));
        DirectionalControlEvaluationCase evaluation = Case(reference, actual,
            ExtrapolationCurveType.CircularArc);
        evaluation.StartActualMD = 100;
        evaluation.EndActualMD = 200;
        evaluation.EvaluationInterval = 10;
        evaluation.ReferenceMDAdvance = 100;
        evaluation.MinimumBundleLength = 20;
        evaluation.MinimumBundleSampleCount = 2;

        bool success = DirectionalControlEvaluationCalculator.Calculate(evaluation, reference, actual);

        Assert.That(success, Is.True, evaluation.CalculationMessage);
        Assert.That(evaluation.SampleList, Has.Count.EqualTo(10));
        Assert.That(evaluation.SampleList!.All(sample => sample.ActualEndMD - sample.ActualMD == 10), Is.True);
        Assert.That(evaluation.SampleList.Any(sample => sample.IsValid), Is.True);
        DirectionalControlEvaluationSample first = evaluation.SampleList.First(sample => sample.IsValid);
        Assert.Multiple(() =>
        {
            Assert.That(first.ActualCurvature, Is.Zero.Within(1e-12));
            Assert.That(first.ExpectedCurvature, Is.GreaterThan(0.0));
            Assert.That(first.CurvatureResidual, Is.LessThan(0.0));
            Assert.That(first.ClosestReferenceMD, Is.EqualTo(first.ActualMD).Within(1e-4));
            Assert.That(first.TargetReferenceMD - first.ClosestReferenceMD, Is.EqualTo(100).Within(1e-8));
            Assert.That(evaluation.BundleList, Is.Not.Empty);
        });
    }

    [TestCase(ExtrapolationCurveType.CircularArc)]
    [TestCase(ExtrapolationCurveType.ConstantBuildAndTurn)]
    [TestCase(ExtrapolationCurveType.ConstantCurvatureAndToolface)]
    public void Fits_actual_interval_with_selected_exact_curve_family(ExtrapolationCurveType curveType)
    {
        SurveyStation start = Station(100, 0.7, 0.4, 1000, 20, -10);
        ArcSection source = CreateSection(start, curveType);
        Assert.That(Calculate(source, curveType), Is.True);
        SurveyStation end = TrajectoryExtrapolationCalculator.FromPoint(source.End);

        bool fitted = TrajectoryExtrapolationCalculator.TryFitIntervalControls(start, end, curveType,
            out TrajectoryExtrapolationSolvedSection? result);

        Assert.That(fitted, Is.True);
        Assert.That(result, Is.Not.Null);
        switch (curveType)
        {
            case ExtrapolationCurveType.CircularArc:
                Assert.That(result!.CircularArcCurvature, Is.EqualTo(0.004).Within(1e-8));
                Assert.That(DirectionalControlEvaluationCalculator.AngularDifference(
                    result.CircularArcStartToolface, 0.6), Is.Zero.Within(1e-8));
                break;
            case ExtrapolationCurveType.ConstantBuildAndTurn:
                Assert.That(result!.ConstantBuildRate, Is.EqualTo(0.002).Within(1e-8));
                Assert.That(result.ConstantTurnRate, Is.EqualTo(-0.001).Within(1e-8));
                break;
            case ExtrapolationCurveType.ConstantCurvatureAndToolface:
                Assert.That(result!.ConstantCurvature, Is.EqualTo(0.004).Within(1e-8));
                Assert.That(DirectionalControlEvaluationCalculator.AngularDifference(
                    result.ConstantToolface, 0.6), Is.Zero.Within(1e-8));
                break;
        }
    }

    [Test]
    public void Hard_invalid_gap_splits_otherwise_consistent_bundles()
    {
        DirectionalControlEvaluationCase value = new()
        {
            CurveType = ExtrapolationCurveType.ConstantBuildAndTurn,
            MaximumInvalidGap = 100,
            MinimumBundleLength = 10,
            MinimumBundleSampleCount = 2,
            BundlingPenalty = 100,
            SampleList =
            [
                ValidBt(0, 10, 0.001, -0.002),
                ValidBt(10, 20, 0.001, -0.002),
                Invalid(20, 130),
                ValidBt(130, 140, 0.001, -0.002),
                ValidBt(140, 150, 0.001, -0.002)
            ]
        };

        List<DirectionalControlEvaluationBundle> bundles = DirectionalControlBundling.CreateBundles(value);

        Assert.That(bundles, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(bundles[0].StartActualMD, Is.EqualTo(0));
            Assert.That(bundles[0].EndActualMD, Is.EqualTo(20));
            Assert.That(bundles[1].StartActualMD, Is.EqualTo(130));
            Assert.That(bundles[1].EndActualMD, Is.EqualTo(150));
            Assert.That(bundles.All(bundle => bundle.BuildRateResidual!.P50 == 0.001), Is.True);
        });
    }

    [Test]
    public void Toolface_statistics_remain_centered_across_angle_wrap()
    {
        double degree = Math.PI / 180.0;
        DirectionalControlEvaluationCase value = new()
        {
            CurveType = ExtrapolationCurveType.CircularArc,
            MaximumInvalidGap = 100,
            MinimumBundleLength = 20,
            MinimumBundleSampleCount = 2,
            BundlingPenalty = 100,
            SampleList =
            [
                ValidCa(0, 10, 0.0, 179 * degree),
                ValidCa(10, 20, 0.0, -179 * degree),
                ValidCa(20, 30, 0.0, 178 * degree)
            ]
        };

        DirectionalControlDistributionSummary summary =
            DirectionalControlBundling.CreateBundles(value).Single().ToolfaceResidual!;

        Assert.That(Math.Abs(Math.Abs(summary.P50) - Math.PI), Is.LessThan(3 * degree));
        Assert.That(summary.StandardDeviation, Is.LessThan(3 * degree));
    }

    [Test]
    public void Toolface_wrap_does_not_create_a_false_change_point()
    {
        double degree = Math.PI / 180.0;
        DirectionalControlEvaluationCase value = new()
        {
            CurveType = ExtrapolationCurveType.CircularArc,
            MaximumInvalidGap = 100,
            MinimumBundleLength = 20,
            MinimumBundleSampleCount = 2,
            BundlingPenalty = 8.0,
            SampleList = Enumerable.Range(0, 8).Select(index =>
                ValidCa(index * 10, (index + 1) * 10, 0.001,
                    (index % 2 == 0 ? 179.0 : -179.0) * degree)).ToList()
        };

        List<DirectionalControlEvaluationBundle> bundles = DirectionalControlBundling.CreateBundles(value);

        Assert.That(bundles, Has.Count.EqualTo(1));
    }

    [Test]
    public void Rejects_trajectories_from_different_wellbores()
    {
        TrajectoryModel reference = CreateTrajectory(Guid.NewGuid(), Station(0, 0, 0, 0, 0, 0), Station(100, 0, 0, 100, 0, 0));
        TrajectoryModel actual = CreateTrajectory(Guid.NewGuid(), Station(0, 0, 0, 0, 0, 0), Station(100, 0, 0, 100, 0, 0));
        DirectionalControlEvaluationCase value = Case(reference, actual, ExtrapolationCurveType.CircularArc);

        Assert.That(DirectionalControlEvaluationCalculator.Calculate(value, reference, actual), Is.False);
        Assert.That(value.CalculationMessage, Does.Contain("same wellbore"));
    }

    private static DirectionalControlEvaluationCase Case(
        TrajectoryModel reference,
        TrajectoryModel actual,
        ExtrapolationCurveType curveType) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        Name = "Directional control test",
        ReferenceTrajectoryID = reference.MetaInfo!.ID,
        ActualTrajectoryID = actual.MetaInfo!.ID,
        CurveType = curveType
    };

    private static TrajectoryModel CreateTrajectory(Guid wellBoreId, params SurveyStation[] stations) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        WellBoreID = wellBoreId,
        LastModificationDate = DateTimeOffset.UtcNow,
        CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
        SurveyStationList = stations.ToList()
    };

    private static SurveyStation Station(double md, double inclination, double azimuth,
        double tvd, double north, double east) => new()
    {
        MD = md,
        Inclination = inclination,
        Azimuth = azimuth,
        TVD = tvd,
        RiemannianNorth = north,
        RiemannianEast = east
    };

    private static ArcSection CreateSection(SurveyStation start, ExtrapolationCurveType curveType)
    {
        var startPoint = TrajectoryExtrapolationCalculator.ToPoint(start);
        return curveType switch
        {
            ExtrapolationCurveType.CircularArc => new CircularArcSection(startPoint, new())
            {
                Circle = { Length = 30, Curvature = 0.004, ReferenceToolface = 0.6 }
            },
            ExtrapolationCurveType.ConstantBuildAndTurn => new BuildAndTurnArcSection(startPoint, new())
            {
                BuildAndTurn = { Length = 30, BUR = 0.002, TR = -0.001 }
            },
            _ => new ConstantCurvatureAndToolfaceArcSection(startPoint, new())
            {
                CTCCurve = { Length = 30, Curvature = 0.004, Toolface = 0.6 }
            }
        };
    }

    private static bool Calculate(ArcSection section, ExtrapolationCurveType curveType) => curveType switch
    {
        ExtrapolationCurveType.CircularArc => ((CircularArcSection)section).CalculateLDT(),
        ExtrapolationCurveType.ConstantBuildAndTurn => ((BuildAndTurnArcSection)section).CalculateLBT(),
        _ => ((ConstantCurvatureAndToolfaceArcSection)section).CalculateLDT()
    };

    private static DirectionalControlEvaluationSample ValidBt(double start, double end, double build, double turn) => new()
    {
        ActualMD = start,
        ActualEndMD = end,
        IsValid = true,
        BuildRateResidual = build,
        TurnRateResidual = turn
    };

    private static DirectionalControlEvaluationSample ValidCa(double start, double end, double curvature, double toolface) => new()
    {
        ActualMD = start,
        ActualEndMD = end,
        IsValid = true,
        CurvatureResidual = curvature,
        ToolfaceResidual = toolface
    };

    private static DirectionalControlEvaluationSample Invalid(double start, double end) => new()
    {
        ActualMD = start,
        ActualEndMD = end,
        IsValid = false,
        FailureCode = "test_gap"
    };
}
