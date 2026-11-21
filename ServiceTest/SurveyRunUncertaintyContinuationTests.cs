using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Math;
using Microsoft.Extensions.Logging.Abstractions;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class SurveyRunUncertaintyContinuationTests
{
    [Test]
    public void Wolff_de_wardt_child_run_continues_parent_transfer_matrix_across_local_md_restart()
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
        List<SurveyStation> parent =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, instrument),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, instrument),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument)
        ];
        List<SurveyStation> child =
        [
            Station(0.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument),
            Station(30.0, 0.35, 0.36, 85.0, 20.0, 6.0, instrument)
        ];

        Assert.That(SurveyRunManager.ContinueWolffDeWardtFromParentHistory(parent, child, instrument), Is.True);

        double parentTieInTrace = Trace(parent[2].Covariance!);
        Assert.Multiple(() =>
        {
            Assert.That(parentTieInTrace, Is.GreaterThan(0.0));
            Assert.That(Trace(child[0].Covariance!), Is.EqualTo(parentTieInTrace).Within(1e-12));
            Assert.That(Trace(child[1].Covariance!), Is.GreaterThan(parentTieInTrace));
            Assert.That(child[0].MD, Is.EqualTo(0.0), "The child run keeps its local sidetrack MD coordinate.");
        });
    }

    [Test]
    public void Iscwsa_child_run_applies_multi_leg_correlation_across_local_md_restart()
    {
        SurveyInstrument instrument = CreateIscwsaInstrument();
        List<SurveyStation> parent =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, instrument),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, instrument),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument)
        ];
        List<SurveyStation> child =
        [
            Station(0.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument),
            Station(30.0, 0.35, 0.36, 85.0, 20.0, 6.0, instrument)
        ];

        List<SurveyStation> expectedReplay = parent.Select(CloneWithoutUncertainty).ToList();
        SurveyStation expectedChildEnd = CloneWithoutUncertainty(child[1]);
        expectedChildEnd.MD = 90.0;
        expectedChildEnd.Abscissa = 90.0;
        expectedReplay.Add(expectedChildEnd);
        Assert.That(CovarianceCalculatorISCWSA.CalculateMultiLeg(expectedReplay,
        [
            new(0, 2, instrument),
            new(3, 3, instrument)
        ]), Is.True);
        parent[^1].Covariance = DiagonalCovariance(999.0);

        Assert.That(SurveyRunManager.ContinueIscwsaFromParentHistory(parent, child, instrument), Is.True);

        double parentTieInTrace = Trace(parent[2].Covariance!);
        Assert.Multiple(() =>
        {
            Assert.That(Trace(child[0].Covariance!), Is.EqualTo(parentTieInTrace).Within(1e-12));
            Assert.That(Trace(child[1].Covariance!), Is.EqualTo(Trace(expectedReplay[^1].Covariance!)).Within(1e-12));
            Assert.That(parentTieInTrace, Is.Not.EqualTo(2997.0),
                "The endpoint covariance must be recomputed from the ISCWSA history, not copied as sufficient state.");
            Assert.That(child[0].MD, Is.EqualTo(0.0), "The child run keeps its local sidetrack MD coordinate.");
        });
    }

    [Test]
    public void Iscwsa_gyro_parent_hands_over_to_mwd_child_with_different_error_sources()
    {
        SurveyInstrument gyro = CreateIscwsaInstrument();
        gyro.ModelType = SurveyInstrumentModelType.Gyro_ISCWSA;
        gyro.ErrorSourceList = gyro.ErrorSourceList!
            .Where(source => source.ErrorCode is not ErrorCode.DSFS and not ErrorCode.MBXY_TI1)
            .ToList();
        SurveyInstrument mwd = CreateIscwsaInstrument();
        mwd.ModelType = SurveyInstrumentModelType.MWD_ISCWSA;
        List<SurveyStation> parent =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, gyro),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, gyro),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, gyro)
        ];
        List<SurveyStation> child =
        [
            Station(455.0, 0.30, 0.34, 57.0, 12.0, 4.0, mwd),
            Station(485.0, 0.35, 0.36, 85.0, 20.0, 6.0, mwd),
            Station(515.0, 0.40, 0.38, 112.0, 30.0, 8.0, mwd)
        ];

        Assert.That(SurveyRunManager.ContinueIscwsaFromParentHistory(parent, child, mwd), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(child, Is.All.Matches<SurveyStation>(station => station.Covariance != null));
            Assert.That(Trace(child[0].Covariance!), Is.EqualTo(Trace(parent[2].Covariance!)).Within(1e-12));
            Assert.That(Trace(child[^1].Covariance!), Is.GreaterThan(Trace(child[0].Covariance!)));
            Assert.That(child[0].MD, Is.EqualTo(455.0));
        });
    }

    [Test]
    public void Trajectory_sections_form_explicit_iscwsa_legs_and_initialize_a_synthetic_tie_in()
    {
        SurveyInstrument gyro = CreateIscwsaInstrument();
        gyro.ModelType = SurveyInstrumentModelType.Gyro_ISCWSA;
        gyro.ErrorSourceList = gyro.ErrorSourceList!
            .Where(source => source.ErrorCode is not ErrorCode.DSFS and not ErrorCode.MBXY_TI1)
            .ToList();
        SurveyInstrument mwd = CreateIscwsaInstrument();
        OSDC.Drilling.Trajectory.Model.Trajectory trajectory = new()
        {
            TieInPoint = Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, null),
            SurveyRunSectionList =
            [
                new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 10.0 },
                new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 60.0 }
            ],
            SurveyStationList =
            [
                Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, null),
                Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, gyro),
                Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, mwd),
                Station(90.0, 0.35, 0.36, 85.0, 20.0, 6.0, mwd),
                Station(120.0, 0.40, 0.38, 112.0, 30.0, 8.0, mwd)
            ]
        };

        Assert.That(TrajectoryManager.TryCalculateComposedTrajectoryUncertainty(
            trajectory, terminalBitExtrapolation: false, out string failure), Is.True, failure);
        Assert.Multiple(() =>
        {
            Assert.That(trajectory.SurveyStationList![0].SurveyTool, Is.SameAs(gyro));
            Assert.That(trajectory.SurveyStationList, Is.All.Matches<SurveyStation>(station => station.Covariance != null));
            Assert.That(Trace(trajectory.SurveyStationList[^1].Covariance!),
                Is.GreaterThan(Trace(trajectory.SurveyStationList[1].Covariance!)));
        });
    }

    [Test]
    public void Single_section_trajectory_preserves_authoritative_parent_lineage_covariance()
    {
        SurveyInstrument tool = CreateIscwsaInstrument();
        OSDC.Drilling.Trajectory.Model.Trajectory trajectory = new()
        {
            SurveyRunSectionList = [new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 0.0 }],
            SurveyStationList =
            [
                StationWithCovariance(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, 7.0),
                StationWithCovariance(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, 8.0),
                StationWithCovariance(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, 9.0)
            ]
        };
        trajectory.SurveyStationList[1].SurveyTool = tool;
        trajectory.SurveyStationList[2].SurveyTool = tool;

        Assert.That(TrajectoryManager.TryCalculateComposedTrajectoryUncertainty(
            trajectory, terminalBitExtrapolation: false, out string failure), Is.True, failure);
        Assert.Multiple(() =>
        {
            Assert.That(trajectory.SurveyStationList[0].SurveyTool, Is.SameAs(tool));
            Assert.That(Trace(trajectory.SurveyStationList[0].Covariance!), Is.EqualTo(21.0));
            Assert.That(Trace(trajectory.SurveyStationList[1].Covariance!), Is.EqualTo(24.0));
            Assert.That(Trace(trajectory.SurveyStationList[2].Covariance!), Is.EqualTo(27.0));
        });
    }

    [Test]
    public void Trajectory_terminal_bit_extrapolation_carries_the_last_measured_uncertainty()
    {
        SurveyInstrument tool = CreateIscwsaInstrument();
        OSDC.Drilling.Trajectory.Model.Trajectory trajectory = new()
        {
            SurveyRunSectionList = [new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 0.0 }],
            SurveyStationList =
            [
                Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, tool),
                Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, tool),
                Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, tool),
                Station(74.0, 0.30, 0.34, 70.2, 15.3, 5.1, tool)
            ]
        };

        Assert.That(TrajectoryManager.TryCalculateComposedTrajectoryUncertainty(
            trajectory, terminalBitExtrapolation: true, out string failure), Is.True, failure);
        Assert.That(Trace(trajectory.SurveyStationList![^1].Covariance!),
            Is.EqualTo(Trace(trajectory.SurveyStationList[^2].Covariance!)).Within(1e-12));
    }

    [Test]
    public void Trajectory_with_one_measurement_and_a_bit_endpoint_can_complete_uncertainty()
    {
        SurveyInstrument tool = CreateIscwsaInstrument();
        OSDC.Drilling.Trajectory.Model.Trajectory trajectory = new()
        {
            SurveyRunSectionList = [new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 0.0 }],
            SurveyStationList =
            [
                Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, tool),
                Station(14.0, 0.20, 0.30, 13.7, 2.6, 0.8, tool)
            ]
        };

        Assert.That(TrajectoryManager.TryCalculateComposedTrajectoryUncertainty(
            trajectory, terminalBitExtrapolation: true, out string failure), Is.True, failure);
        Assert.That(Trace(trajectory.SurveyStationList![^1].Covariance!),
            Is.EqualTo(Trace(trajectory.SurveyStationList[0].Covariance!)).Within(1e-12));
    }

    [Test]
    public void Trajectory_rejects_mixed_uncertainty_model_families_with_a_specific_reason()
    {
        SurveyInstrument wolffDeWardt = new()
        {
            ModelType = SurveyInstrumentModelType.MWD_WolffDeWardt,
            RelDepthError = 0.001,
            Misalignment = 0.001
        };
        SurveyInstrument iscwsa = CreateIscwsaInstrument();
        OSDC.Drilling.Trajectory.Model.Trajectory trajectory = new()
        {
            SurveyRunSectionList =
            [
                new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 0.0 },
                new() { SurveyRunID = Guid.NewGuid(), StartAbscissa = 30.0 }
            ],
            SurveyStationList =
            [
                Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, wolffDeWardt),
                Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, iscwsa),
                Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, iscwsa)
            ]
        };

        Assert.That(TrajectoryManager.TryCalculateComposedTrajectoryUncertainty(
            trajectory, terminalBitExtrapolation: false, out string failure), Is.False);
        Assert.That(failure, Does.Contain("compatible survey-instrument leg sequence"));
    }

    [Test]
    public void Derived_octree_exception_does_not_change_a_completed_trajectory()
    {
        Guid id = Guid.NewGuid();
        bool deleted = false;
        OSDC.Drilling.Trajectory.Model.Trajectory trajectory = new()
        {
            MetaInfo = new OSDC.DotnetLibraries.General.DataManagement.MetaInfo { ID = id },
            CalculationState = CalculationState.Completed,
            CalculationProgress = 1.0
        };

        bool rebuilt = TrajectoryManager.TryRefreshDerivedOctree(
            trajectory,
            id,
            _ => throw new InvalidOperationException("synthetic octree failure"),
            value => deleted = value == id,
            NullLogger.Instance);

        Assert.Multiple(() =>
        {
            Assert.That(rebuilt, Is.False);
            Assert.That(deleted, Is.True);
            Assert.That(trajectory.CalculationState, Is.EqualTo(CalculationState.Completed));
            Assert.That(trajectory.CalculationProgress, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void Ellipse_request_replaces_stale_covariance_from_authoritative_trajectory()
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
        List<SurveyStation> authoritative =
        [
            Station(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, instrument),
            Station(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, instrument),
            Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument)
        ];
        Assert.That(CovarianceCalculatorWolffDeWardt.Calculate(authoritative), Is.True);

        SurveyStation requested = Station(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, instrument);
        requested.Covariance = DiagonalCovariance(999.0);
        SurveyStationEllipseCalculation calculation = new()
        {
            SurveyStationList = [requested]
        };

        Assert.That(
            SurveyStationEllipseCalculationManager.ApplyAuthoritativeUncertainty(
                calculation,
                authoritative,
                TrajectoryCalculationType.MinimumCurvatureMethod,
                "Trajectory"),
            Is.True);
        Assert.That(
            Trace(calculation.SurveyStationList![0].Covariance!),
            Is.EqualTo(Trace(authoritative[2].Covariance!)).Within(1e-12));
    }

    [Test]
    public void Ellipse_request_interpolates_authoritative_covariance_at_display_md()
    {
        List<SurveyStation> authoritative =
        [
            StationWithCovariance(0.0, 0.20, 0.30, 0.0, 0.0, 0.0, 1.0),
            StationWithCovariance(30.0, 0.25, 0.32, 29.0, 5.0, 2.0, 2.0),
            StationWithCovariance(60.0, 0.30, 0.34, 57.0, 12.0, 4.0, 3.0)
        ];
        SurveyStationEllipseCalculation calculation = new()
        {
            SurveyStationList =
            [
                StationWithCovariance(45.0, 0.275, 0.33, 43.0, 8.5, 3.0, 999.0)
            ]
        };

        Assert.That(
            SurveyStationEllipseCalculationManager.ApplyAuthoritativeUncertainty(
                calculation,
                authoritative,
                TrajectoryCalculationType.MinimumCurvatureMethod,
                "Trajectory"),
            Is.True);
        Assert.That(Trace(calculation.SurveyStationList![0].Covariance!), Is.InRange(6.0, 9.0));
    }

    [Test]
    public void Trajectory_materialization_clone_preserves_uncertainty_components()
    {
        SurveyStation source = StationWithCovariance(455.78, 0.30, 0.34, 400.0, 100.0, 20.0, 7.0);
        source.Covariance![0, 1] = source.Covariance[1, 0] = 1.25;
        source.Bias = new Vector3D(1.0, 2.0, 3.0);
        source.EigenValues = new Vector3D(4.0, 5.0, 6.0);
        source.EigenVectors = new Matrix3x3(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);

        SurveyStation clone = TrajectoryManager.CloneSurveyStation(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Covariance, Is.Not.SameAs(source.Covariance));
            Assert.That(Trace(clone.Covariance!), Is.EqualTo(21.0));
            Assert.That(clone.Covariance![0, 1], Is.EqualTo(1.25));
            Assert.That(clone.Bias, Is.Not.SameAs(source.Bias));
            Assert.That(clone.Bias!.Z, Is.EqualTo(3.0));
            Assert.That(clone.EigenValues, Is.Not.SameAs(source.EigenValues));
            Assert.That(clone.EigenValues!.Y, Is.EqualTo(5.0));
            Assert.That(clone.EigenVectors, Is.Not.SameAs(source.EigenVectors));
            Assert.That(clone.EigenVectors![2, 2], Is.EqualTo(1.0));
        });

        clone.Covariance![0, 0] = 99.0;
        Assert.That(source.Covariance![0, 0], Is.EqualTo(7.0));
    }

    private static SurveyStation Station(
        double md,
        double inclination,
        double azimuth,
        double tvd,
        double north,
        double east,
        SurveyInstrument? instrument) => new()
        {
            MD = md,
            Abscissa = md,
            Inclination = inclination,
            Azimuth = azimuth,
            TVD = tvd,
            RiemannianNorth = north,
            RiemannianEast = east,
            SurveyTool = instrument
        };

    private static SurveyStation StationWithCovariance(
        double md,
        double inclination,
        double azimuth,
        double tvd,
        double north,
        double east,
        double diagonalCovariance) => new()
        {
            MD = md,
            Abscissa = md,
            Inclination = inclination,
            Azimuth = azimuth,
            TVD = tvd,
            RiemannianNorth = north,
            RiemannianEast = east,
            Covariance = DiagonalCovariance(diagonalCovariance)
        };

    private static SymmetricMatrix3x3 DiagonalCovariance(double value)
    {
        SymmetricMatrix3x3 covariance = new();
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                covariance[row, column] = 0.0;
            }
        }
        covariance[0, 0] = covariance[1, 1] = covariance[2, 2] = value;
        return covariance;
    }

    private static SurveyInstrument CreateIscwsaInstrument() => new()
    {
        Name = "ISCWSA parent-continuation test tool",
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

    private static SurveyStation CloneWithoutUncertainty(SurveyStation station) => new(station)
    {
        Covariance = null,
        Bias = null,
        EigenValues = null,
        EigenVectors = null
    };

    private static double Trace(SymmetricMatrix3x3 covariance) =>
        covariance[0, 0]!.Value + covariance[1, 1]!.Value + covariance[2, 2]!.Value;
}
