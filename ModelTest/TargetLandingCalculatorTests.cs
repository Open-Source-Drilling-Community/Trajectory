using OSDC.Drilling.Trajectory.Model;
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
        });
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
}
