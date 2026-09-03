using Microsoft.Data.Sqlite;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Text.Json;

namespace OSDC.Drilling.Trajectory.Service.Managers;

public sealed class TrajectoryFeatureCategoryManager
{
    private static readonly (string Name, bool Exclusive, bool Validity, string[] Options)[] Defaults =
    [
        ("SurveyContext", false, false, ["InitialSurvey", "RoutineSurvey", "Resurvey", "CheckSurvey", "TieInSurvey", "GyroTieIn", "FinalSurvey", "DefinitiveSurvey", "PostRunSurvey", "MemorySurvey", "AntiCollisionSurvey", "ReliefWellSurvey", "SidetrackSurvey", "CasingExitSurvey", "MultilateralJunctionSurvey", "PlugbackSurvey", "AbandonmentSurvey", "Unknown"]),
        ("BoreholeSectionContext", false, false, ["ConductorSection", "SurfaceSection", "IntermediateSection", "ProductionSection", "ReservoirSection", "LateralSection", "BuildSection", "TangentSection", "LandingSection", "HorizontalSection", "OpenHoleSection", "CasedHoleSection", "RiserlessSection", "TopHoleSection", "Unknown"]),
        ("SurveyPurpose", false, false, ["DirectionalControl", "PositionUpdate", "InclinationCheck", "AzimuthCheck", "ToolfaceReference", "AntiCollision", "WellPlacement", "Geosteering", "CasingShoePosition", "SidetrackOrientation", "CasingExitOrientation", "ReliefIntercept", "VerticalityControl", "DefinitivePosition", "QualityCheck", "CalibrationCheck", "Unknown"]),
        ("TrajectoryPurpose", false, false, ["PlannedTrajectory", "ActualTrajectory", "DefinitiveTrajectory", "CorrectedTrajectory", "ProposedTrajectory", "AlternativeTrajectory", "CollisionAvoidanceTrajectory", "ReliefInterceptTrajectory", "GeosteeringReferenceTrajectory", "TargetLineTrajectory", "AsBuiltTrajectory", "SyntheticTrajectory", "ImportedTrajectory", "Unknown"]),
        ("SurveyReferenceStatus", true, true, ["Draft", "Proposed", "Planned", "Active", "Preliminary", "Accepted", "Rejected", "Approved", "Superseded", "Definitive", "Archived", "Cancelled", "Unknown"]),
        ("AcquisitionMode", false, false, ["SingleShot", "MultiShot", "StationarySurvey", "StaticSurvey", "ContinuousSurvey", "RotatingSurvey", "SlidingSurvey", "WhileDrillingSurvey", "WhileTrippingSurvey", "MemorySurvey", "RealTimeSurvey", "PostRunSurvey", "ManualSurvey", "Unknown"]),
        ("MeasurementCondition", false, false, ["WhileDrilling", "WhileSliding", "WhileRotating", "WhileTrippingIn", "WhileTrippingOut", "WhileReaming", "WhileBackreaming", "DuringCirculation", "PumpsOn", "PumpsOff", "AtPumpStartup", "AtPumpStop", "AtConnection", "OnBottom", "OffBottom", "Stationary", "Moving", "Rotating", "NonRotating", "Continuous", "OnDemand", "Scheduled", "Unknown"]),
        ("RunningMode", false, false, ["MWD", "LWD", "GyroWhileDrilling", "RSSIntegrated", "Wireline", "Slickline", "CoiledTubing", "DropGyro", "PumpDown", "PumpedInDrillPipe", "PumpedThroughDrillString", "MemoryTool", "SurfaceReadout", "RigFloorMeasurement", "Unknown"]),
        ("DataProcessingState", false, true, ["Raw", "Decoded", "Filtered", "Corrected", "Interpolated", "Smoothed", "Resampled", "Merged", "TieInAdjusted", "Validated", "ManuallyEdited", "PostProcessed", "DefinitiveProcessed", "Imported", "Unknown"]),
        ("CorrectionApplied", false, true, ["MagneticDeclinationCorrection", "GridConvergenceCorrection", "TotalCorrection", "MagneticDipCorrection", "TotalMagneticFieldCorrection", "DrillstringMagneticInterferenceCorrection", "AxialInterferenceCorrection", "CrossAxialInterferenceCorrection", "SagCorrection", "MisalignmentCorrection", "BiasCorrection", "ScaleFactorCorrection", "TemperatureCorrection", "VibrationCorrection", "MultiStationCorrection", "TieInCorrection", "DepthCorrection", "None", "Unknown"]),
        ("QualityStatus", true, true, ["Unchecked", "Passed", "PassedWithWarnings", "Failed", "Suspect", "Rejected", "Accepted", "Approved", "Superseded", "Unknown"]),
        ("QualityIssue", false, true, ["MagneticInterference", "ExcessiveVibration", "NonStationaryTool", "PoorToolfaceStability", "PoorInclinationRepeatability", "PoorAzimuthRepeatability", "GravityOutOfRange", "MagneticFieldOutOfRange", "DipAngleOutOfRange", "TemperatureOutOfRange", "ShockLimitExceeded", "DepthMismatch", "TimeMismatch", "TelemetryDropout", "MissingStations", "DuplicateStations", "ManualEntrySuspected", "Unknown"]),
        ("SurveyStationDensity", true, false, ["Sparse", "Standard", "Dense", "Continuous", "Irregular", "Unknown"])
    ];

    private readonly TrajectoryCatalogStore<Model.TrajectoryFeatureCategory> store;
    private readonly SqlConnectionManager mainDatabase;

    public TrajectoryFeatureCategoryManager(SqlConnectionManager mainDatabase)
    {
        this.mainDatabase = mainDatabase;
        store = new(mainDatabase, "TrajectoryFeatureCategoryTable", "TrajectoryFeatureCategory",
            value => value.MetaInfo, value => value.Name, (value, date) => value.CreationDate = date,
            (value, date) => value.LastModificationDate = date, value => value.IsExclusive, value => value.HasValidityPeriod);
    }

    public List<Model.TrajectoryFeatureCategory> GetAll() { EnsureDefaults(); return store.All(); }
    public Model.TrajectoryFeatureCategory? Get(Guid id) => store.ById(id);
    public bool Add(Model.TrajectoryFeatureCategory value) { Prepare(value); return store.Add(value); }
    public bool Update(Guid id, Model.TrajectoryFeatureCategory value) { Prepare(value); return !RemovesReferencedOptions(id, value) && store.Update(id, value); }
    public bool Delete(Guid id) => !IsReferenced(id) && store.Delete(id);
    public bool IsReferenced(Guid id) => ReadAssignments().Any(assignments => assignments.Any(a => a.FeatureCategoryID == id));

    private bool RemovesReferencedOptions(Guid id, Model.TrajectoryFeatureCategory value)
    {
        HashSet<Guid> retained = (value.Options ?? []).Select(option => option.ID).ToHashSet();
        return ReadAssignments().Any(assignments => assignments.Any(a => a.FeatureCategoryID == id && a.FeatureOptionID is Guid option && !retained.Contains(option)));
    }

    private IEnumerable<List<Model.TrajectoryFeatureAssignment>> ReadAssignments()
    {
        foreach (Model.SurveyRun value in ReadDocuments<Model.SurveyRun>("SurveyRunTable", "SurveyRun"))
            yield return value.SurveyRunFeatureAssignments ?? [];
        foreach (Model.Trajectory value in ReadDocuments<Model.Trajectory>("TrajectoryTable", "Trajectory"))
            yield return value.TrajectoryFeatureAssignments ?? [];
    }

    private IEnumerable<T> ReadDocuments<T>(string table, string column)
    {
        using SqliteConnection connection = mainDatabase.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {column} FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            T? value = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options);
            if (value != null) yield return value;
        }
    }

    private void EnsureDefaults()
    {
        if (store.All().Count != 0) return;
        foreach (var item in Defaults)
            Add(new() { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = item.Name, IsExclusive = item.Exclusive,
                HasValidityPeriod = item.Validity, Options = item.Options.Select(name => new Model.TrajectoryFeatureOption { ID = Guid.NewGuid(), Name = name }).ToList() });
    }

    private static void Prepare(Model.TrajectoryFeatureCategory value)
    {
        value.Options ??= [];
        foreach (Model.TrajectoryFeatureOption option in value.Options)
            if (option.ID == Guid.Empty) option.ID = Guid.NewGuid();
    }
}
