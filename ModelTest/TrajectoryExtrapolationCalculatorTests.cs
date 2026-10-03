using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System.Text.Json;
using System.Text.Json.Nodes;
using TrajectoryModel = OSDC.Drilling.Trajectory.Model.Trajectory;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class TrajectoryExtrapolationCalculatorTests
{
    [Test]
    public void NewExtrapolationCaseUsesTenMetreInterpolationInterval()
    {
        TrajectoryExtrapolationCase calculation = new();

        Assert.That(calculation.InterpolationInterval, Is.EqualTo(10.0));
    }

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
        AssertDerivedSurveyValues(calculation.SurveyStationList);
        Assert.That(calculation.SurveyStationList.All(station => station.Curvature == 0.0), Is.True);
        Assert.That(calculation.SurveyStationList.All(station => station.BUR == 0.0), Is.True);
        Assert.That(calculation.SurveyStationList.All(station => station.TUR == 0.0), Is.True);
        Assert.That(calculation.SurveyStationList.All(station => station.VerticalSection == 0.0), Is.True);
    }

    [Test]
    public void StraightExtensionRetainsSourceEndpointRatesAndWritesZeroRatesOnEveryNewSample()
    {
        SurveyStation sourceEndpoint = Station(1082.0, 20.5 * System.Math.PI / 180.0,
            167.0 * System.Math.PI / 180.0, 1033.65, -286.46, 43.70);
        sourceEndpoint.Curvature = 4.24 * System.Math.PI / 180.0 / 30.0;
        sourceEndpoint.BUR = 4.10 * System.Math.PI / 180.0 / 30.0;
        sourceEndpoint.TUR = -3.10 * System.Math.PI / 180.0 / 30.0;
        sourceEndpoint.Toolface = 0.3;
        sourceEndpoint.VerticalSection = 290.301;
        TrajectoryModel source = SourceTrajectory(sourceEndpoint);
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 60.0,
                ExtensionType = FixedLengthExtrapolationType.Straight
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(calculation, source, _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SurveyStationList, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(calculation.SurveyStationList![0].Curvature, Is.EqualTo(sourceEndpoint.Curvature));
            Assert.That(calculation.SurveyStationList[0].BUR, Is.EqualTo(sourceEndpoint.BUR));
            Assert.That(calculation.SurveyStationList[0].TUR, Is.EqualTo(sourceEndpoint.TUR));
            Assert.That(calculation.SurveyStationList.Skip(1).All(station => station.Curvature == 0.0), Is.True);
            Assert.That(calculation.SurveyStationList.Skip(1).All(station => station.BUR == 0.0), Is.True);
            Assert.That(calculation.SurveyStationList.Skip(1).All(station => station.TUR == 0.0), Is.True);
            Assert.That(calculation.SurveyStationList[1].VerticalSection, Is.GreaterThan(sourceEndpoint.VerticalSection!.Value));
            Assert.That(calculation.SurveyStationList[2].VerticalSection, Is.GreaterThan(calculation.SurveyStationList[1].VerticalSection!.Value));
        });
    }

    [Test]
    public void Extrapolated_uncertainty_starts_from_source_endpoint_and_grows_with_the_source_instrument()
    {
        SymmetricMatrix3x3 sourceCovariance = new();
        sourceCovariance[0, 0] = 4.0;
        sourceCovariance[1, 1] = 9.0;
        sourceCovariance[2, 2] = 16.0;
        sourceCovariance[0, 1] = sourceCovariance[1, 0] = 0.2;
        sourceCovariance[0, 2] = sourceCovariance[2, 0] = 0.3;
        sourceCovariance[1, 2] = sourceCovariance[2, 1] = 0.4;
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            Misalignment = 0.01,
            RelDepthError = 0.001,
            TrueInclination = 0.005,
            ReferenceError = 0.01,
            DrillStringMag = 0.002
        };
        SurveyStation sourceEndpoint = Station(100.0, 0.5, 0.3, 90.0, 40.0, 12.0);
        sourceEndpoint.Covariance = sourceCovariance;
        sourceEndpoint.SurveyTool = instrument;
        sourceEndpoint.CalculateEigenProperties();
        TrajectoryModel source = SourceTrajectory(sourceEndpoint);
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 60.0,
                ExtensionType = FixedLengthExtrapolationType.Straight
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(calculation, source, _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SurveyStationList, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(calculation.SurveyStationList!.All(station => station.Covariance != null), Is.True);
            Assert.That(calculation.SurveyStationList.All(station => ReferenceEquals(station.SurveyTool, instrument)), Is.True);
            Assert.That(calculation.SurveyStationList[0].Covariance![0, 0], Is.EqualTo(4.0).Within(1e-12));
            Assert.That(CovarianceTrace(calculation.SurveyStationList[^1].Covariance!), Is.GreaterThan(29.0));
            Assert.That(sourceEndpoint.Covariance![0, 0], Is.EqualTo(4.0).Within(1e-12),
                "The source trajectory covariance must not be mutated.");
        });

        SurveyStationEllipseCalculation ellipses = new()
        {
            ConfidenceFactor = 0.95,
            SurveyStationList = calculation.SurveyStationList
        };
        Assert.That(ellipses.Calculate(), Is.True, ellipses.CalculationMessage);
        Assert.That(ellipses.SurveyStationEllipseResultList, Has.Count.EqualTo(3));
        Assert.That(ellipses.SurveyStationEllipseResultList!.All(result =>
            result.HorizontalEllipse != null && result.VerticalEllipse != null &&
            result.PerpendicularEllipse != null), Is.True);
    }

    [Test]
    public void Extrapolated_stations_inherit_the_last_defined_source_survey_instrument()
    {
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            Misalignment = 0.01,
            RelDepthError = 0.001,
            TrueInclination = 0.005,
            ReferenceError = 0.01,
            DrillStringMag = 0.002
        };
        SurveyStation previous = Station(70.0, 0.4, 0.2, 65.0, 12.0, 3.0);
        previous.SurveyTool = instrument;
        SurveyStation terminal = Station(100.0, 0.5, 0.3, 90.0, 20.0, 6.0);
        SymmetricMatrix3x3 covariance = new();
        covariance[0, 0] = covariance[1, 1] = covariance[2, 2] = 1.0;
        terminal.Covariance = covariance;
        terminal.CalculateEigenProperties();
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 60.0,
                ExtensionType = FixedLengthExtrapolationType.Straight
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(
            calculation, SourceTrajectory(previous, terminal), _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.StartStation?.SurveyTool, Is.SameAs(instrument));
        Assert.That(calculation.SurveyStationList, Is.Not.Null.And.Not.Empty);
        Assert.That(calculation.SurveyStationList!.All(station => ReferenceEquals(station.SurveyTool, instrument)), Is.True);
    }

    [Test]
    public void Wolff_de_wardt_extrapolation_replays_the_source_transfer_matrix_without_resetting_the_ellipse()
    {
        SurveyInstrument instrument = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            Misalignment = 0.01,
            RelDepthError = 0.001,
            TrueInclination = 0.005,
            ReferenceError = 0.01,
            DrillStringMag = 0.002
        };
        List<SurveyStation> sourceStations =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0)
        ];
        sourceStations.ForEach(station => station.SurveyTool = instrument);
        Assert.That(CovarianceCalculatorWolffDeWardt.Calculate(sourceStations), Is.True);
        double sourceTrace = CovarianceTrace(sourceStations[^1].Covariance!);
        Assert.That(sourceTrace, Is.GreaterThan(0.0));
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 60.0,
                ExtensionType = FixedLengthExtrapolationType.Straight
            });

        Assert.That(TrajectoryExtrapolationCalculator.Calculate(
            calculation, SourceTrajectory(sourceStations.ToArray()), _ => null), Is.True, calculation.CalculationMessage);

        Assert.That(calculation.SurveyStationList, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(CovarianceTrace(calculation.SurveyStationList![0].Covariance!),
                Is.EqualTo(sourceTrace).Within(1e-12),
                "The first extrapolated station must retain the source endpoint uncertainty.");
            Assert.That(CovarianceTrace(calculation.SurveyStationList[^1].Covariance!), Is.GreaterThan(sourceTrace));
        });
        SurveyStationEllipseCalculation ellipses = new()
        {
            ConfidenceFactor = 0.95,
            SurveyStationList = calculation.SurveyStationList
        };
        Assert.That(ellipses.Calculate(), Is.True, ellipses.CalculationMessage);
        Assert.That(ellipses.SurveyStationEllipseResultList![0].HorizontalEllipse?.SemiMajorAxis,
            Is.GreaterThan(0.0));
    }

    [Test]
    public void Iscwsa_extrapolation_replays_source_error_accumulators_without_resetting_the_ellipse()
    {
        SurveyInstrument instrument = CreateIscwsaInstrument();
        List<SurveyStation> sourceStations =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0)
        ];
        sourceStations.ForEach(station => station.SurveyTool = instrument);
        Assert.That(CovarianceCalculatorISCWSA.Calculate(sourceStations), Is.True);
        SymmetricMatrix3x3 staleEndpointCovariance = new();
        staleEndpointCovariance[0, 0] = 999.0;
        staleEndpointCovariance[1, 1] = 999.0;
        staleEndpointCovariance[2, 2] = 999.0;
        sourceStations[^1].Covariance = staleEndpointCovariance;
        sourceStations[^1].CalculateEigenProperties();
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.FixedLength,
            new FixedLengthExtrapolationSpecification
            {
                Length = 60.0,
                ExtensionType = FixedLengthExtrapolationType.Straight
            });

        Assert.That(TrajectoryExtrapolationCalculator.Calculate(
            calculation, SourceTrajectory(sourceStations.ToArray()), _ => null), Is.True, calculation.CalculationMessage);

        List<SurveyStation> expectedReplay = sourceStations.Select(station => new SurveyStation(station)
        {
            Covariance = null,
            Bias = null,
            EigenValues = null,
            EigenVectors = null,
            SurveyTool = instrument
        }).ToList();
        List<SurveyStation> extrapolatedStations = calculation.SurveyStationList!;
        expectedReplay.AddRange(extrapolatedStations.Skip(1).Select(station => new SurveyStation(station)
        {
            Covariance = null,
            Bias = null,
            EigenValues = null,
            EigenVectors = null,
            SurveyTool = instrument
        }));
        Assert.That(CovarianceCalculatorISCWSA.Calculate(expectedReplay), Is.True);

        int sourceEndIndex = sourceStations.Count - 1;
        Assert.Multiple(() =>
        {
            AssertCovarianceEqual(
                expectedReplay[sourceEndIndex].Covariance!,
                extrapolatedStations[0].Covariance!);
            AssertCovarianceEqual(
                expectedReplay[^1].Covariance!,
                extrapolatedStations[^1].Covariance!);
            Assert.That(CovarianceTrace(extrapolatedStations[0].Covariance!), Is.Not.EqualTo(2997.0),
                "The endpoint covariance must be recomputed from the ISCWSA history, not copied as sufficient state.");
        });
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
        AssertDerivedSurveyValues(calculation.SurveyStationList);
        Assert.That(calculation.SurveyStationList[^1].BUR, Is.EqualTo(tenDegrees / 100.0).Within(1e-10));
        Assert.That(calculation.SurveyStationList[^1].TUR, Is.Zero.Within(1e-12));
        Assert.That(calculation.SurveyStationList[^1].Curvature, Is.EqualTo(tenDegrees / 100.0).Within(1e-10));
        Assert.That(calculation.SurveyStationList[^1].VerticalSection, Is.GreaterThan(0.0));
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
        AssertDerivedSurveyValues(calculation.SurveyStationList);
        Assert.That(calculation.SurveyStationList![^1].Curvature, Is.EqualTo(0.005).Within(1e-10));
        Assert.That(calculation.SurveyStationList[^1].VerticalSection, Is.GreaterThan(0.0));
    }

    [Test]
    public void WellPathFailureExplainsAConstraintWithNoSensitivityToTheRemainingUnknown()
    {
        TrajectoryModel source = SourceTrajectory(Station(
            1082.0,
            20.5 * System.Math.PI / 180.0,
            167.0 * System.Math.PI / 180.0,
            1033.65,
            -286.46,
            43.70));
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.WellPath,
            new WellPathExtrapolationSpecification
            {
                SectionList =
                [
                    new ConstantBuildAndTurnWellPathSectionSpecification
                    {
                        Length = 30.0,
                        BuildRate = 4.1 * System.Math.PI / 180.0 / 30.0,
                        TurnRate = -3.1 * System.Math.PI / 180.0 / 30.0
                    },
                    new CircularArcWellPathSectionSpecification
                    {
                        Curvature = 0.0,
                        StartToolface = 0.0
                    },
                    new ConstantCurvatureAndToolfaceWellPathSectionSpecification
                    {
                        Length = 30.0,
                        EndInclination = 35.0 * System.Math.PI / 180.0,
                        Curvature = 3.0 * System.Math.PI / 180.0 / 30.0,
                        Toolface = 25.0 * System.Math.PI / 180.0
                    }
                ]
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(calculation, source, _ => null);

        Assert.That(success, Is.False);
        Assert.That(calculation.CalculationMessage, Does.Contain("run of sections ending at section 3"));
        Assert.That(calculation.CalculationMessage, Does.Contain("end inclination of section 3"));
        Assert.That(calculation.CalculationMessage, Does.Contain("has no sensitivity"));
        Assert.That(calculation.CalculationMessage, Does.Contain("length of section 2"));
        Assert.That(calculation.CalculationMessage, Does.Contain("requested 35"));
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
        AssertDerivedSurveyValues(calculation.SurveyStationList);
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
        Assert.That(calculation.TargetReferenceMD - calculation.ClosestReferenceMD,
            Is.EqualTo(100).Within(1e-5),
            "The reference advance is added after the lead-in establishes the effective closest point.");
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
                    SteeringLength = 100,
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
    public void GeosteeringRequiresPositiveSteeringLength()
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
                    SteeringLength = 0,
                    SteeringLengthRatio = 1
                }
            });

        Assert.That(TrajectoryExtrapolationValidation.Validate(calculation),
            Does.Contain("steering_length_must_be_positive"));
    }

    [Test]
    public void LegacyOverallDrilledLengthIsUpgradedWithoutChangingSteeringGeometry()
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
                    SteeringLength = 170,
                    SteeringLengthRatio = 1
                }
            });
        JsonObject persisted = JsonNode.Parse(JsonSerializer.Serialize(calculation))!.AsObject();
        JsonObject extent = persisted["Specification"]!["Extent"]!.AsObject();
        extent.Remove("SteeringLength");
        extent["OverallDrilledLength"] = 200.0;

        TrajectoryExtrapolationCase restored = persisted.Deserialize<TrajectoryExtrapolationCase>()!;
        List<string> errors = TrajectoryExtrapolationValidation.Validate(restored);
        DrilledLengthGeosteeringExtentConstraint upgraded =
            (DrilledLengthGeosteeringExtentConstraint)((GeosteeringTrajectoryExtrapolationSpecification)restored.Specification!).Extent!;

        Assert.Multiple(() =>
        {
            Assert.That(errors, Is.Empty);
            Assert.That(upgraded.SteeringLength, Is.EqualTo(170.0).Within(1e-12));
            Assert.That(JsonSerializer.Serialize(restored), Does.Not.Contain("OverallDrilledLength"));
        });
    }

    [Test]
    public void GeosteeringNearHorizontalCircularArcCaseSucceedsWithEqualSectionLengths()
    {
        SurveyStation previous = Station(597.78, 1.5809192364564637, 2.3724260522358924,
            256.0565783569098, 6534980.332099356, 328968.7600223668);
        SurveyStation last = Station(639.18, 1.579872038905267, 2.3673645974051083,
            255.65917215939598, 6534950.660957943, 328997.6290649575);
        TrajectoryExtrapolationCase calculation = Case(
            TrajectoryExtrapolationMode.Geosteering,
            new GeosteeringTrajectoryExtrapolationSpecification
            {
                LeadInLength = 30.0,
                TargetVerticalDepth = 258.78,
                EndInclination = Math.PI / 2.0,
                EndAzimuth = 140.0 * Math.PI / 180.0,
                CurveType = ExtrapolationCurveType.CircularArc,
                Extent = new DrilledLengthGeosteeringExtentConstraint
                {
                    SteeringLength = 170.0,
                    SteeringLengthRatio = 1.0
                }
            });

        bool success = TrajectoryExtrapolationCalculator.Calculate(
            calculation, SourceTrajectory(previous, last), _ => null);

        Assert.That(success, Is.True, calculation.CalculationMessage);
        Assert.That(calculation.SolvedSectionList, Has.Count.EqualTo(3));
        Assert.That(calculation.SolvedSectionList![1].Length, Is.EqualTo(85.0).Within(1.0e-6));
        Assert.That(calculation.SolvedSectionList[2].Length, Is.EqualTo(85.0).Within(1.0e-6));
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
            {"ExtentType":"Departure","DepartureDistance":120,"DepartureBearing":0.4,"SteeringLength":150}
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

    private static void AssertDerivedSurveyValues(IReadOnlyCollection<SurveyStation>? stations)
    {
        Assert.That(stations, Is.Not.Null.And.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(stations!.All(station => station.Curvature.HasValue), Is.True, "DLS must be calculated.");
            Assert.That(stations.All(station => station.BUR.HasValue), Is.True, "BUR must be calculated.");
            Assert.That(stations.All(station => station.TUR.HasValue), Is.True, "TR must be calculated.");
            Assert.That(stations.All(station => station.VerticalSection.HasValue), Is.True, "Vertical section must be calculated.");
        });
    }

    private static double CovarianceTrace(SymmetricMatrix3x3 covariance) =>
        (covariance[0, 0] ?? 0.0) + (covariance[1, 1] ?? 0.0) + (covariance[2, 2] ?? 0.0);

    private static SurveyInstrument CreateIscwsaInstrument() => new()
    {
        Name = "ISCWSA replay test tool",
        ModelType = SurveyInstrumentModelType.MWD_ISCWSA,
        BField = 50_000e-9,
        Dip = 72.0 * System.Math.PI / 180.0,
        Declination = -4.0 * System.Math.PI / 180.0,
        Gravity = 9.80665,
        ErrorSourceList =
        [
            ErrorSourceFactory.Create_DRFR(magnitude: 0.35),
            ErrorSourceFactory.Create_DSFS(magnitude: 0.00056),
            ErrorSourceFactory.Create_DSTG(magnitude: 2.5e-7),
            ErrorSourceFactory.Create_ABXY_TI1S(magnitude: 0.004),
            ErrorSourceFactory.Create_MBXY_TI1(magnitude: 70e-9),
            ErrorSourceFactory.Create_DECR(magnitude: 0.1 * System.Math.PI / 180.0)
        ]
    };

    private static void AssertCovarianceEqual(SymmetricMatrix3x3 expected, SymmetricMatrix3x3 actual)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Assert.That(actual[row, column], Is.EqualTo(expected[row, column]).Within(1e-12),
                    $"Covariance[{row},{column}]");
            }
        }
    }
}
