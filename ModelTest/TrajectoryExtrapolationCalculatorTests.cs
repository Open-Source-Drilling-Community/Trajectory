using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System.Text.Json;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class TrajectoryExtrapolationCalculatorTests
{
    [Test]
    public void FixedStraightExtensionStartsAtLastStationAndIncludesExactEnd()
    {
        TrajectoryModel source = SourceTrajectory(
            Station(0.0, 0.0, 0.0, 1000.0, 0.0, 0.0),
            Station(100.0, 0.0, 0.0, 1100.0, 0.0, 0.0));
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 65.0,
                ExtensionType = FixedLengthExtrapolationType.Straight
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(calculation, source, _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.CalculationState, Is.EqualTo(CalculationState.Completed));
        Assert.That(calculation.SurveyStationList, Has.Count.EqualTo(4));
        Assert.That(calculation.SurveyStationList![0].MD, Is.EqualTo(100.0));
        Assert.That(calculation.SurveyStationList[^1].MD, Is.EqualTo(165.0).Within(1e-9));
        Assert.That(calculation.SurveyStationList[^1].TVD, Is.EqualTo(1165.0).Within(1e-9));
        Assert.That(calculation.SurveyStationList[^1].Covariance, Is.Null);
    }

    [Test]
    public void ContinueBuildAndTurnReconstructsTheLastInterval()
    {
        double tenDegrees = System.Math.PI / 18.0;
        SurveyStation first = Station(0.0, 0.0, 0.0, 1000.0, 0.0, 0.0);
        SurveyStation second = new()
        {
            MD = 100.0,
            Inclination = tenDegrees,
            Azimuth = 0.0
        };
        Assert.That(first.CompleteFromSIA(second, TrajectoryCalculationType.ConstantBuildAndTurnMethod), Is.True);
        TrajectoryModel source = SourceTrajectory(first, second);
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 30.0,
                ExtensionType = FixedLengthExtrapolationType.ContinueConstantBuildAndTurn
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(calculation, source, _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SurveyStationList![^1].MD, Is.EqualTo(130.0).Within(1e-8));
        Assert.That(calculation.SurveyStationList[^1].Inclination, Is.EqualTo(1.3 * tenDegrees).Within(1e-8));
        Assert.That(calculation.SolvedSectionList![0].ConstantBuildRate, Is.EqualTo(tenDegrees / 100.0).Within(1e-10));
    }

    [Test]
    public void WellPathRejectsCrossPairAndWrongConstraintCountBeforeSolving()
    {
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.WellPath,
            new WellPathExtrapolationSpecification
            {
                SectionList =
                [
                    new CircularArcWellPathSectionSpecification
                    {
                        Length = 100.0,
                        Curvature = 0.01
                    }
                ]
            });

        List<string> errors = TrajectoryExtrapolationValidation.Validate(calculation);

        Assert.That(errors, Does.Contain("well_path_requires_exactly_three_constraints_per_section"));
    }

    [Test]
    public void WellPathWithLengthCurvatureAndToolfaceProducesSamplesAndSolution()
    {
        TrajectoryModel source = SourceTrajectory(Station(100.0, 0.0, 0.0, 1100.0, 0.0, 0.0));
        Guid sectionId = Guid.NewGuid();
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.WellPath,
            new WellPathExtrapolationSpecification
            {
                SectionList =
                [
                    new ConstantCurvatureAndToolfaceWellPathSectionSpecification
                    {
                        SectionID = sectionId,
                        Length = 60.0,
                        Curvature = 0.005,
                        Toolface = 0.0
                    }
                ]
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(calculation, source, _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SurveyStationList![^1].MD, Is.EqualTo(160.0).Within(1e-9));
        Assert.That(calculation.SolvedSectionList, Has.Count.EqualTo(1));
        Assert.That(calculation.SolvedSectionList![0].SectionID, Is.EqualTo(sectionId));
        Assert.That(calculation.SolvedSectionList[0].ConstantCurvature, Is.EqualTo(0.005).Within(1e-12));
    }

    [Test]
    public void ReconnectFindsClosestReferencePointAdvancesAndReachesTargetTangent()
    {
        TrajectoryModel source = SourceTrajectory(
            Station(0, 0, 0, 1000, -50, 0),
            Station(100, 0, 0, 1100, -50, 0));
        TrajectoryModel reference = SourceTrajectory(
            Station(0, 0, 0, 1000, 0, 0),
            Station(300, 0, 0, 1300, 0, 0));
        Guid referenceId = reference.MetaInfo!.ID;
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.ReconnectToTrajectory,
            new ReconnectTrajectoryExtrapolationSpecification
            {
                ReferenceTrajectoryID = referenceId,
                ReferenceMDAdvance = 100,
                CurveType = ExtrapolationCurveType.CircularArc
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(
            calculation, source, id => id == referenceId ? reference : null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.ClosestReferenceMD, Is.EqualTo(100).Within(1e-5));
        Assert.That(calculation.TargetReferenceMD, Is.EqualTo(200).Within(1e-5));
        Assert.That(calculation.SolvedSectionList, Has.Count.EqualTo(2));
        Assert.That(calculation.SurveyStationList![^1].RiemannianNorth, Is.EqualTo(0).Within(1e-6));
        Assert.That(calculation.SurveyStationList[^1].TVD, Is.EqualTo(1200).Within(1e-6));
        Assert.That(calculation.SurveyStationList[^1].Inclination, Is.EqualTo(0).Within(1e-6));
    }

    [Test]
    public void ReconnectLeadInMovesTheEffectiveClosestPointAndIsReportedSeparately()
    {
        TrajectoryModel source = SourceTrajectory(
            Station(0, 0, 0, 1000, -50, 0),
            Station(100, 0, 0, 1100, -50, 0));
        TrajectoryModel reference = SourceTrajectory(
            Station(0, 0, 0, 1000, 0, 0),
            Station(400, 0, 0, 1400, 0, 0));
        Guid referenceId = reference.MetaInfo!.ID;
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.ReconnectToTrajectory,
            new ReconnectTrajectoryExtrapolationSpecification
            {
                ReferenceTrajectoryID = referenceId,
                ReferenceMDAdvance = 100,
                CurveType = ExtrapolationCurveType.CircularArc,
                LeadInLength = 20
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(
            calculation, source, id => id == referenceId ? reference : null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.ClosestReferenceMD, Is.EqualTo(120).Within(1e-5));
        Assert.That(calculation.TargetReferenceMD, Is.EqualTo(220).Within(1e-5));
        Assert.That(calculation.SolvedSectionList, Has.Count.EqualTo(3));
        Assert.That(calculation.SolvedSectionList![0].Role,
            Is.EqualTo(TrajectoryExtrapolationSectionRole.LeadInContinuation));
        Assert.That(calculation.SolvedSectionList[1].Role,
            Is.EqualTo(TrajectoryExtrapolationSectionRole.UpstreamSteeringSection));
    }

    [Test]
    public void GeosteeringDrilledLengthUsesOverallLengthAndSteeringRatio()
    {
        SurveyStation sourceStation = Station(100, 1.1, 0.7, 1400, 100, -25);
        TrajectoryPoint3D start = new()
        {
            Abscissa = sourceStation.MD,
            X = sourceStation.RiemannianNorth,
            Y = sourceStation.RiemannianEast,
            Z = sourceStation.TVD,
            Inclination = sourceStation.Inclination,
            Azimuth = sourceStation.Azimuth
        };
        ConstantCurvatureAndToolfaceArcSection first = new(start, new TrajectoryPoint3D());
        first.CTCCurve.Length = 60; first.CTCCurve.Curvature = 0.005; first.CTCCurve.Toolface = 0.8;
        Assert.That(first.CalculateLDT(), Is.True);
        ConstantCurvatureAndToolfaceArcSection second = new(first.End, new TrajectoryPoint3D());
        second.CTCCurve.Length = 40; second.CTCCurve.Curvature = 0.005; second.CTCCurve.Toolface = -0.35;
        Assert.That(second.CalculateLDT(), Is.True);

        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.Geosteering,
            new GeosteeringTrajectoryExtrapolationSpecification
            {
                TargetVerticalDepth = second.End.Z!.Value,
                EndInclination = second.End.Inclination!.Value,
                EndAzimuth = second.End.Azimuth!.Value,
                CurveType = ExtrapolationCurveType.ConstantCurvatureAndToolface,
                Extent = new DrilledLengthGeosteeringExtentConstraint
                {
                    OverallDrilledLength = 100,
                    SteeringLengthRatio = 1.5
                }
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(
            calculation, SourceTrajectory(sourceStation), _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SurveyStationList![^1].MD, Is.EqualTo(200).Within(1e-6));
        Assert.That(calculation.SurveyStationList[^1].TVD, Is.EqualTo(second.End.Z).Within(1e-5));
        Assert.That(calculation.SolvedSectionList, Has.Count.EqualTo(2));
        Assert.That(calculation.SolvedSectionList![0].Length, Is.EqualTo(60).Within(1e-5));
        Assert.That(calculation.SolvedSectionList[1].Length, Is.EqualTo(40).Within(1e-5));
    }

    [Test]
    public void GeosteeringRequiresOverallLengthToExceedLeadIn()
    {
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.Geosteering,
            new GeosteeringTrajectoryExtrapolationSpecification
            {
                LeadInLength = 30,
                TargetVerticalDepth = 1100,
                EndInclination = 0.5,
                EndAzimuth = 0.2,
                CurveType = ExtrapolationCurveType.CircularArc,
                Extent = new DrilledLengthGeosteeringExtentConstraint
                {
                    OverallDrilledLength = 30,
                    SteeringLengthRatio = 1
                }
            });

        Assert.That(TrajectoryExtrapolationValidation.Validate(calculation),
            Does.Contain("overall_drilled_length_must_exceed_lead_in_length"));
    }

    [Test]
    public void ModeAndSpecificationMustAgree()
    {
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.ReconnectToTrajectory,
            new FixedLengthExtrapolationSpecification { Length = 10.0 });

        Assert.That(TrajectoryExtrapolationValidation.Validate(calculation), Does.Contain("specification_does_not_match_mode"));
    }

    [Test]
    public void PolymorphicWellPathRoundTripsMixedSectionTypes()
    {
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.WellPath,
            new WellPathExtrapolationSpecification
            {
                SectionList =
                [
                    new CircularArcWellPathSectionSpecification { Length = 30, Curvature = 0.01, StartToolface = 0 },
                    new ConstantBuildAndTurnWellPathSectionSpecification { Length = 30, BuildRate = 0.001, TurnRate = 0.002 },
                    new ConstantCurvatureAndToolfaceWellPathSectionSpecification { Length = 30, Curvature = 0.01, Toolface = 1 }
                ]
            });

        string json = JsonSerializer.Serialize(calculation);
        TrajectoryExtrapolationCase? roundTrip = JsonSerializer.Deserialize<TrajectoryExtrapolationCase>(json);

        Assert.That(roundTrip?.Specification, Is.TypeOf<WellPathExtrapolationSpecification>());
        Assert.That(((WellPathExtrapolationSpecification)roundTrip!.Specification!).SectionList.Select(section => section.GetType()),
            Is.EqualTo(new[]
            {
                typeof(CircularArcWellPathSectionSpecification),
                typeof(ConstantBuildAndTurnWellPathSectionSpecification),
                typeof(ConstantCurvatureAndToolfaceWellPathSectionSpecification)
            }));
    }

    [Test]
    public void PolymorphicSectionRejectsAFieldFromAnotherCurveVariant()
    {
        const string json = """
            {"CurveType":"CircularArc","SectionID":"00000000-0000-0000-0000-000000000001","Length":30,"Curvature":0.01,"StartToolface":0,"BuildRate":0.001}
            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WellPathSectionSpecification>(json));
    }

    [Test]
    public void PolymorphicGeosteeringExtentRoundTripsAndRejectsCrossVariantFields()
    {
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.Geosteering,
            new GeosteeringTrajectoryExtrapolationSpecification
            {
                TargetVerticalDepth = 1500,
                EndInclination = 1.2,
                EndAzimuth = 0.8,
                CurveType = ExtrapolationCurveType.CircularArc,
                Extent = new DepartureGeosteeringExtentConstraint
                {
                    DepartureDistance = 120,
                    DepartureBearing = 0.4
                }
            });

        string json = JsonSerializer.Serialize(calculation);
        TrajectoryExtrapolationCase? roundTrip = JsonSerializer.Deserialize<TrajectoryExtrapolationCase>(json);

        Assert.That(roundTrip?.Specification, Is.TypeOf<GeosteeringTrajectoryExtrapolationSpecification>());
        Assert.That(((GeosteeringTrajectoryExtrapolationSpecification)roundTrip!.Specification!).Extent,
            Is.TypeOf<DepartureGeosteeringExtentConstraint>());
        const string invalid = """
            {"ExtentType":"Departure","DepartureDistance":120,"DepartureBearing":0.4,"OverallDrilledLength":150}
            """;
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GeosteeringExtentConstraint>(invalid));
    }

    private static TrajectoryExtrapolationCase Case(TrajectoryExtrapolationMode mode, TrajectoryExtrapolationSpecification specification) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        Name = "Test",
        SourceTrajectoryID = Guid.NewGuid(),
        Mode = mode,
        InterpolationInterval = 30.0,
        Specification = specification
    };

    private static TrajectoryModel SourceTrajectory(params SurveyStation[] stations) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
        LastModificationDate = DateTimeOffset.UtcNow,
        CalculationType = TrajectoryCalculationType.MinimumCurvatureMethod,
        SurveyStationList = stations.ToList()
    };

    private static SurveyStation Station(double md, double inclination, double azimuth, double tvd, double north, double east) => new()
    {
        MD = md,
        Inclination = inclination,
        Azimuth = azimuth,
        TVD = tvd,
        RiemannianNorth = north,
        RiemannianEast = east
    };
}
