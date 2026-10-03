using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

public sealed class TargetLandingCalculatorTests
{
    [Test]
    public void DefaultsMatchAgreedLandingInputs()
    {
        TargetLandingCase value = new();
        Assert.Multiple(() =>
        {
            Assert.That(value.LeadLength, Is.EqualTo(30.0));
            Assert.That(value.MaximumLandingCurvature, Is.EqualTo(Math.PI / 1800.0).Within(1e-15));
            Assert.That(value.ConfidenceFactor, Is.EqualTo(0.95));
        });
    }

    [Test]
    public void ConformingHybridContourRefinementAdvancesCalculationAlgorithmVersion()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TargetLandingCalculator.MaximumAdaptiveDepth, Is.EqualTo(4));
            Assert.That(TargetLandingCalculator.BoundaryPositionTolerance, Is.EqualTo(0.25));
            Assert.That(TargetLandingCalculator.CalculationAlgorithmVersion, Is.EqualTo(9));
        });
    }

    [Test]
    public void ExactControlSamplesUseTheDefiningCurveEquations()
    {
        TrajectoryPoint3D start = new()
        {
            X = 0.0, Y = 0.0, Z = 0.0, Abscissa = 1000.0,
            Inclination = 0.8, Azimuth = 0.3
        };
        BuildAndTurnArcSection buildTurn = new(start, new TrajectoryPoint3D());
        buildTurn.BuildAndTurn.Length = 120.0;
        buildTurn.BuildAndTurn.BUR = -0.001;
        buildTurn.BuildAndTurn.TR = 0.002;
        Assert.That(buildTurn.CalculateLBT(), Is.True);

        ConstantCurvatureAndToolfaceArcSection ctc = new(start, new TrajectoryPoint3D());
        ctc.CTCCurve.Length = 120.0;
        ctc.CTCCurve.Curvature = 0.002;
        ctc.CTCCurve.Toolface = 0.7;
        Assert.That(ctc.CalculateLDT(), Is.True);

        CircularArcSection circular = new(start, new TrajectoryPoint3D());
        circular.Circle.Length = 120.0;
        circular.Circle.Curvature = 0.002;
        circular.Circle.ReferenceToolface = 0.7;
        Assert.That(circular.CalculateLDT(), Is.True);

        List<TargetLandingControlPoint> btControls = TargetLandingCalculator.BuildControlPointList([buildTurn]);
        List<TargetLandingControlPoint> ctcControls = TargetLandingCalculator.BuildControlPointList([ctc]);
        List<TargetLandingControlPoint> caControls = TargetLandingCalculator.BuildControlPointList([circular]);

        Assert.Multiple(() =>
        {
            Assert.That(btControls, Is.Not.Empty);
            Assert.That(btControls.First().NormalizedLength, Is.Zero.Within(1e-12));
            Assert.That(btControls.Last().NormalizedLength, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(btControls.All(point => Math.Abs(point.BuildRate + 0.001) < 1e-14), Is.True);
            Assert.That(btControls.All(point => Math.Abs(point.TurnRate - 0.002) < 1e-14), Is.True);

            Assert.That(ctcControls, Is.Not.Empty);
            Assert.That(ctcControls.All(point => Math.Abs(point.Curvature - 0.002) < 1e-14), Is.True);
            Assert.That(ctcControls.All(point => AngularDifference(point.Toolface, 0.7) < 1e-12), Is.True);
            Assert.That(ctcControls.All(point => Math.Abs(point.BuildRate - 0.002 * Math.Cos(0.7)) < 1e-14), Is.True);

            Assert.That(caControls, Is.Not.Empty);
            Assert.That(AngularDifference(caControls.First().Toolface, 0.7), Is.LessThan(1e-12));
            Assert.That(AngularDifference(caControls.Last().Toolface, caControls.First().Toolface), Is.GreaterThan(1e-3),
                "A circular arc's exact local toolface must not be presented as its constant start/reference toolface.");
            Assert.That(caControls.All(point => Math.Abs(point.Curvature - 0.002) < 1e-12), Is.True);
        });
    }

    [Test]
    public void BoundaryBisectionFindsTransitionWithoutDeepeningTheMesh()
    {
        const double transition = 2.345;
        int evaluationCount = 0;
        TargetLandingSample first = SampleAt(0.0);
        TargetLandingSample second = SampleAt(10.0);

        TargetPlanePoint boundary = TargetLandingCalculator.BisectBoundary(first, second, point =>
        {
            evaluationCount++;
            return SampleAt(point.X);
        }, sample => sample.State == TargetLandingSampleState.Reachable, 0.01);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.X, Is.EqualTo(transition).Within(0.01));
            Assert.That(boundary.Y, Is.Zero);
            Assert.That(evaluationCount, Is.LessThanOrEqualTo(10));
        });

        static TargetLandingSample SampleAt(double x) => new()
        {
            PlaneX = x,
            PlaneY = 0.0,
            State = x <= transition
                ? TargetLandingSampleState.Reachable
                : TargetLandingSampleState.ExceedsMaximumLandingCurvature
        };
    }

    [Test]
    public void ValidationRejectsNonConvexPolygon()
    {
        TargetLandingCase value = Case();
        value.Target.Polygon =
        [
            new() { X = 0, Y = 0 }, new() { X = 10, Y = 0 }, new() { X = 2, Y = 2 },
            new() { X = 10, Y = 10 }, new() { X = 0, Y = 10 }
        ];

        Assert.That(TargetLandingCalculator.Validate(value), Has.Some.Contains("convex"));
    }

    [Test]
    public void FreeAttitudeDrillerTargetProducesAdaptiveReachableRegion()
    {
        TrajectoryModel source = Source();
        TargetLandingCase value = Case();
        value.AttitudeMode = TargetLandingAttitudeMode.Free;
        value.LeadLength = 0.0;

        bool success = TargetLandingCalculator.Calculate(value, source);

        Assert.That(success, Is.True, value.CalculationMessage);
        Assert.Multiple(() =>
        {
            Assert.That(value.CalculationState, Is.EqualTo(CalculationState.Completed));
            Assert.That(value.SampleList, Is.Not.Null.And.Not.Empty);
            Assert.That(value.SampleList!.Any(x => x.State == TargetLandingSampleState.Reachable), Is.True);
            Assert.That(value.MeshTriangleList, Is.Not.Null.And.Not.Empty);
            Assert.That(value.ReachableTargetBoundary, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(value.CalculationFingerprint, Is.Not.Null.And.Not.Empty);
            Assert.That(value.SourceEndStation, Is.Not.Null);
            Assert.That(value.SourceEndStation!.MD, Is.EqualTo(source.SurveyStationList!.Last().MD));
            Assert.That(value.LeadSurveyStationList, Is.Not.Null.And.Not.Empty);
            Assert.That(value.LeadSurveyStationList!.First().MD, Is.EqualTo(value.SourceEndStation.MD));
            Assert.That(value.LeadSurveyStationList!.Last().MD, Is.EqualTo(value.SteeringStartStation!.MD));
            Assert.That(value.SampleList!.Where(sample => sample.SolvedSectionList is { Count: > 0 })
                .All(sample => sample.ControlPointList is { Count: > 0 }), Is.True);
        });
    }

    [TestCase(ExtrapolationCurveType.CircularArc)]
    [TestCase(ExtrapolationCurveType.ConstantBuildAndTurn)]
    [TestCase(ExtrapolationCurveType.ConstantCurvatureAndToolface)]
    public void EveryLandingCurveFamilyProducesAuthoritativeRenderableControls(ExtrapolationCurveType curveType)
    {
        TargetLandingCase value = Case();
        value.CurveType = curveType;
        value.AttitudeMode = TargetLandingAttitudeMode.Free;
        value.LeadLength = 0.0;

        Assert.That(TargetLandingCalculator.Calculate(value, Source()), Is.True, value.CalculationMessage);
        List<TargetLandingSample> solved = value.SampleList!
            .Where(sample => sample.SolvedSectionList is { Count: > 0 })
            .ToList();

        Assert.That(solved, Is.Not.Empty);
        Assert.That(solved.All(sample => sample.ControlPointList is { Count: > 1 }), Is.True);
        foreach (TargetLandingSample sample in solved)
        {
            Assert.Multiple(() =>
            {
                Assert.That(sample.ControlPointList![0].NormalizedLength, Is.Zero.Within(1e-12));
                Assert.That(sample.ControlPointList[^1].NormalizedLength, Is.EqualTo(1.0).Within(1e-12));
                Assert.That(sample.ControlPointList.All(point =>
                    double.IsFinite(point.Curvature) && double.IsFinite(point.Toolface) &&
                    double.IsFinite(point.BuildRate) && double.IsFinite(point.TurnRate)), Is.True);
            });
        }
    }

    [Test]
    public void PerpendicularLandingUsesPlaneNormalAndRetainsShortestForwardSolution()
    {
        TrajectoryModel source = Source();
        TargetLandingCase value = Case();
        value.AttitudeMode = TargetLandingAttitudeMode.PerpendicularToTargetPlane;
        value.LeadLength = 0.0;

        Assert.That(TargetLandingCalculator.Calculate(value, source), Is.True, value.CalculationMessage);
        TargetLandingSample center = value.SampleList!.OrderBy(x => x.PolarRadius).First();
        Assert.That(center.State, Is.EqualTo(TargetLandingSampleState.Reachable), center.Message);
        Assert.Multiple(() =>
        {
            Assert.That(center.LandingStation!.Inclination, Is.EqualTo(Math.PI / 2.0).Within(1e-6));
            Assert.That(center.LandingStation.Azimuth, Is.EqualTo(0.0).Within(1e-6));
            Assert.That(center.TotalLandingLength, Is.EqualTo(100.0).Within(1e-4));
        });
    }

    [Test]
    public void MaximumLandingCurvatureExcludesSolutionWithoutChangingUncertaintySafeZone()
    {
        TargetLandingCase value = Case();
        value.LeadLength = 0.0;
        value.TargetType = TargetLandingTargetType.DrillerTarget;
        value.Target.Plane.TVD = 1010.0;
        value.MaximumLandingCurvature = 1e-9;

        Assert.That(TargetLandingCalculator.Calculate(value, Source()), Is.True, value.CalculationMessage);
        TargetLandingSample center = value.SampleList!.OrderBy(x => x.PolarRadius).First();
        Assert.Multiple(() =>
        {
            Assert.That(center.State, Is.EqualTo(TargetLandingSampleState.ExceedsMaximumLandingCurvature));
            Assert.That(center.IsUncertaintySafe, Is.True);
            Assert.That(value.DrillerTargetContourList, Is.Not.Null.And.Not.Empty);
            Assert.That(value.ReachableTargetContourList, Is.Empty);
        });
    }

    [Test]
    public void PlaneOriginMaterializesConsistentWgs84AndRiemannianCoordinates()
    {
        TargetLandingCase value = Case();
        value.LeadLength = 0.0;
        value.Target.Plane.Latitude = null;
        value.Target.Plane.Longitude = null;

        Assert.That(TargetLandingCalculator.Calculate(value, Source()), Is.True, value.CalculationMessage);
        Assert.Multiple(() =>
        {
            Assert.That(value.Target.Plane.RiemannianNorth, Is.EqualTo(100.0).Within(1e-6));
            Assert.That(value.Target.Plane.RiemannianEast, Is.EqualTo(0.0).Within(1e-6));
            Assert.That(value.Target.Plane.X, Is.EqualTo(value.Target.Plane.RiemannianNorth));
            Assert.That(value.Target.Plane.Y, Is.EqualTo(value.Target.Plane.RiemannianEast));
            Assert.That(value.Target.Plane.Z, Is.EqualTo(value.Target.Plane.TVD));
            Assert.That(value.Target.Plane.Latitude, Is.Not.Null);
            Assert.That(value.Target.Plane.Longitude, Is.Not.Null);
        });
    }

    [Test]
    public void ContourTracingIgnoresInternalBranchesInsteadOfCreatingGreenChords()
    {
        TargetPlanePoint lowerLeft = new() { X = 0.0, Y = 0.0 };
        TargetPlanePoint lowerRight = new() { X = 10.0, Y = 0.0 };
        TargetPlanePoint upperRight = new() { X = 10.0, Y = 10.0 };
        TargetPlanePoint upperLeft = new() { X = 0.0, Y = 10.0 };
        List<(TargetPlanePoint A, TargetPlanePoint B)> segments =
        [
            (upperRight, lowerLeft), // Deliberately place the internal branch first.
            (lowerLeft, lowerRight),
            (lowerRight, upperRight),
            (upperRight, upperLeft),
            (upperLeft, lowerLeft)
        ];

        List<List<TargetPlanePoint>> contours = TargetLandingCalculator.TraceBoundaryLoops(segments);

        Assert.That(contours, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(contours[0], Has.Count.EqualTo(4));
            Assert.That(contours[0].Select(point => (point.X, point.Y)).Distinct().ToList(), Has.Count.EqualTo(4));
            Assert.That(Math.Abs(PolygonArea(contours[0])), Is.EqualTo(100.0).Within(1e-9));
        });
    }

    [Test]
    public void U3GeometryProducesSimpleReachableBoundaryWithoutInternalChords()
    {
        TargetLandingCase value = U3Case(1400.78);
        TrajectoryModel source = U3Source(value.SourceTrajectoryID);

        Assert.That(TargetLandingCalculator.Calculate(value, source), Is.True, value.CalculationMessage);
        Assert.That(value.ReachableTargetContourList, Is.Not.Null.And.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(value.LeadSurveyStationList, Has.Count.GreaterThan(1));
            Assert.That(value.LeadSurveyStationList!.First().MD, Is.EqualTo(value.SourceEndStation!.MD));
            Assert.That(value.LeadSurveyStationList!.Last().MD, Is.EqualTo(value.SteeringStartStation!.MD));
        });
        foreach (List<TargetPlanePoint> contour in value.ReachableTargetContourList!)
        {
            Assert.Multiple(() =>
            {
                Assert.That(contour.Select(point => (point.X, point.Y)).Distinct().Count(), Is.EqualTo(contour.Count));
                Assert.That(HasSelfIntersection(contour), Is.False);
            });
        }
    }

    [Test]
    public void ProductionU3BuildTurnCaseRetainsDetailedReachableBoundary()
    {
        TargetLandingCase value = U3Case(1700.78);
        value.CurveType = ExtrapolationCurveType.ConstantBuildAndTurn;
        value.MaximumLandingCurvature = 8.0 * Math.PI / 180.0 / 30.0;

        Assert.That(TargetLandingCalculator.Calculate(value, U3Source(value.SourceTrajectoryID)), Is.True, value.CalculationMessage);
        Assert.That(value.ReachableTargetContourList, Is.Not.Null.And.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(value.ReachableTargetContourList![0], Has.Count.GreaterThanOrEqualTo(100),
                "The current production geometry should retain the transition samples needed to represent its curved boundary.");
            Assert.That(value.ReachableTargetContourList.All(contour => !HasSelfIntersection(contour)), Is.True);
        });
    }

    [Test]
    public void ProductionU3SteeringStartRetainsDetailedReachableBoundary()
    {
        TargetLandingCase value = U3Case(1700.78);
        value.CurveType = ExtrapolationCurveType.ConstantBuildAndTurn;
        value.MaximumLandingCurvature = 8.0 * Math.PI / 180.0 / 30.0;
        value.LeadLength = 0.0;
        TrajectoryModel source = new()
        {
            MetaInfo = new MetaInfo { ID = value.SourceTrajectoryID },
            LastModificationDate = DateTimeOffset.UtcNow,
            CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
            SurveyStationList =
            [
                new SurveyStation
                {
                    MD = 1026.78,
                    Inclination = 0.463553651284207,
                    Azimuth = 2.85256802886554,
                    RiemannianNorth = 6534963.59450052,
                    RiemannianEast = 328719.40509292,
                    TVD = 974.754488870058,
                    Curvature = 0.00246785292641435,
                    Toolface = -0.202012736161278,
                    BUR = 0.0024176685547328,
                    TUR = -0.00110740462124781
                }
            ]
        };

        Assert.That(TargetLandingCalculator.Calculate(value, source), Is.True, value.CalculationMessage);
        Assert.That(value.ReachableTargetContourList, Is.Not.Null.And.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(value.ReachableTargetContourList![0], Has.Count.GreaterThanOrEqualTo(100),
                $"The deployed steering geometry must not collapse its curved reachable boundary to a handful of mesh edges. " +
                $"Samples: {value.SampleList!.Count}; reachable: {value.SampleList.Count(sample => sample.State == TargetLandingSampleState.Reachable)}; " +
                $"contours: {string.Join(", ", value.ReachableTargetContourList.Select(contour => contour.Count))}. " +
                $"Contour: {string.Join("; ", value.ReachableTargetContourList[0].Select(point => $"({point.X:R},{point.Y:R})"))}");
            Assert.That(HasSelfIntersection(value.ReachableTargetContourList[0]), Is.False);
        });
    }

    private static TargetLandingCase Case() => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        Name = "Landing",
        SourceTrajectoryID = Guid.NewGuid(),
        TargetType = TargetLandingTargetType.DrillerTarget,
        CurveType = ExtrapolationCurveType.CircularArc,
        MaximumLandingCurvature = Math.PI / 1800.0,
        Target = new TargetPlaneDefinition
        {
            Plane = new CurvilinearPoint3D
            {
                RiemannianNorth = 100.0, RiemannianEast = 0.0, TVD = 1000.0,
                Inclination = Math.PI / 2.0, Azimuth = 0.0
            },
            Polygon =
            [
                new() { X = -5, Y = -5 }, new() { X = 5, Y = -5 },
                new() { X = 5, Y = 5 }, new() { X = -5, Y = 5 }
            ]
        }
    };

    private static TargetLandingCase U3Case(double targetTvd)
    {
        TargetLandingCase value = Case();
        value.AttitudeMode = TargetLandingAttitudeMode.Free;
        value.LeadLength = 30.0;
        value.Target.Plane = new CurvilinearPoint3D
        {
            RiemannianNorth = 6534947.00301523,
            RiemannianEast = 328721.161419567,
            TVD = targetTvd,
            Inclination = 0.191986217719376,
            Azimuth = 3.05432619099008
        };
        value.Target.Polygon =
        [
            new() { X = -50.0, Y = -50.0 }, new() { X = 50.0, Y = -50.0 },
            new() { X = 50.0, Y = 50.0 }, new() { X = -50.0, Y = 50.0 }
        ];
        return value;
    }

    private static TrajectoryModel U3Source(Guid sourceTrajectoryId) => new()
    {
        MetaInfo = new MetaInfo { ID = sourceTrajectoryId },
        LastModificationDate = DateTimeOffset.UtcNow,
        CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
        SurveyStationList =
        [
            Station(905.78, 0.30543195539339985, 3.141585834467332, 6535003.245693119, 328712.2103161793, 860.9392922960917),
            Station(922.78, 0.28797869523224956, 3.124132061288359, 6534998.275804975, 328712.2524846864, 877.1963052739194),
            Station(951.78, 0.2844883352921184, 2.9845053863106266, 6534990.137604863, 328712.9611460306, 905.0199983912321),
            Station(982.78, 0.3577923117971354, 2.9146938185442055, 6534980.547083098, 328714.8637621408, 934.4297472299482)
        ]
    };

    private static TrajectoryModel Source() => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        LastModificationDate = DateTimeOffset.UtcNow,
        CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
        SurveyStationList =
        [
            new SurveyStation { MD = 0.0, Inclination = Math.PI / 2.0, Azimuth = 0.0, RiemannianNorth = -10.0, RiemannianEast = 0.0, TVD = 1000.0 },
            new SurveyStation { MD = 10.0, Inclination = Math.PI / 2.0, Azimuth = 0.0, RiemannianNorth = 0.0, RiemannianEast = 0.0, TVD = 1000.0 }
        ]
    };

    private static SurveyStation Station(double md, double inclination, double azimuth, double north, double east, double tvd) => new()
    {
        MD = md,
        Inclination = inclination,
        Azimuth = azimuth,
        RiemannianNorth = north,
        RiemannianEast = east,
        TVD = tvd
    };

    private static bool HasSelfIntersection(IReadOnlyList<TargetPlanePoint> polygon)
    {
        for (int first = 0; first < polygon.Count; first++)
        {
            int firstNext = (first + 1) % polygon.Count;
            for (int second = first + 1; second < polygon.Count; second++)
            {
                int secondNext = (second + 1) % polygon.Count;
                if (first == second || firstNext == second || secondNext == first) continue;
                if (SegmentsIntersect(polygon[first], polygon[firstNext], polygon[second], polygon[secondNext])) return true;
            }
        }
        return false;
    }

    private static bool SegmentsIntersect(TargetPlanePoint a, TargetPlanePoint b, TargetPlanePoint c, TargetPlanePoint d)
    {
        static double Orientation(TargetPlanePoint p, TargetPlanePoint q, TargetPlanePoint r) =>
            (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
        double first = Orientation(a, b, c);
        double second = Orientation(a, b, d);
        double third = Orientation(c, d, a);
        double fourth = Orientation(c, d, b);
        return first * second < -1e-12 && third * fourth < -1e-12;
    }

    private static double PolygonArea(IReadOnlyList<TargetPlanePoint> polygon)
    {
        double twiceArea = 0.0;
        for (int index = 0; index < polygon.Count; index++)
        {
            TargetPlanePoint current = polygon[index];
            TargetPlanePoint next = polygon[(index + 1) % polygon.Count];
            twiceArea += current.X * next.Y - next.X * current.Y;
        }
        return 0.5 * twiceArea;
    }

    private static double AngularDifference(double first, double second) =>
        Math.Abs(Math.IEEERemainder(first - second, 2.0 * Math.PI));
}
