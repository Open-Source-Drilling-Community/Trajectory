using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Selects all trajectory data or an explicitly ordered selection.</summary>
public enum TrajectoryBatchExportScope
{
    Unspecified = 0,
    All = 1,
    Selected = 2
}

/// <summary>
/// Requests a logical backup. Selected trajectories automatically pull in every
/// referenced survey run; selected survey runs automatically pull in parent runs.
/// </summary>
public sealed class TrajectoryBatchExportRequest
{
    public TrajectoryBatchExportScope Scope { get; set; }
    public List<Guid>? SurveyRunIDs { get; set; }
    public List<Guid>? TrajectoryIDs { get; set; }
}

/// <summary>A portable, versioned backup of survey runs, trajectories, and local catalogs.</summary>
public sealed class TrajectoryBatchExportDocument
{
    public const string CurrentFormatIdentifier = "OSDC.Drilling.Trajectory.BatchExport";
    public const int CurrentSchemaVersion = 1;

    public string FormatIdentifier { get; set; } = CurrentFormatIdentifier;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTimeOffset ExportedAtUtc { get; set; }
    public TrajectoryBatchCatalogDependencies CatalogDependencies { get; set; } = new();
    public List<SurveyRun> SurveyRuns { get; set; } = [];
    public List<Trajectory> Trajectories { get; set; } = [];
}

public sealed class TrajectoryBatchCatalogDependencies
{
    public List<TrajectoryIdentity> Identities { get; set; } = [];
    public List<TrajectoryFeatureCategory> FeatureCategories { get; set; } = [];
}

public enum TrajectoryBatchRestoreConflictPolicy
{
    Unspecified = 0,
    FailIfExists = 1,
    ReplaceExisting = 2
}

public enum TrajectoryBatchCatalogRestorePolicy
{
    Unspecified = 0,
    MapExisting = 1,
    MapOrCreateMissing = 2
}

public sealed class TrajectoryBatchRestoreRequest
{
    public TrajectoryBatchRestoreConflictPolicy ConflictPolicy { get; set; }
    public TrajectoryBatchCatalogRestorePolicy CatalogPolicy { get; set; }
    /// <summary>
    /// Explicitly permits compatible catalog definitions and feature options with different UUIDs
    /// to be mapped by normalized name. The safe default is exact UUID matching only.
    /// </summary>
    public bool AllowNormalizedNameMapping { get; set; }
    public TrajectoryBatchExportDocument? Document { get; set; }
}

public sealed class TrajectoryBatchRestoreResponse
{
    public DateTimeOffset RestoredAtUtc { get; set; }
    public int CreatedSurveyRunCount { get; set; }
    public int ReplacedSurveyRunCount { get; set; }
    public int CreatedTrajectoryCount { get; set; }
    public int ReplacedTrajectoryCount { get; set; }
    public int CreatedCatalogDefinitionCount { get; set; }
    public List<TrajectoryBatchCatalogMapping> CatalogMappings { get; set; } = [];
    public List<Guid> SurveyRunIDs { get; set; } = [];
    public List<Guid> TrajectoryIDs { get; set; } = [];
}

public sealed class TrajectoryBatchCatalogMapping
{
    public string Catalog { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid SourceID { get; set; }
    public Guid LocalID { get; set; }
    public string Resolution { get; set; } = string.Empty;
}

public sealed class TrajectoryBatchErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<TrajectoryBatchError> Errors { get; set; } = [];
}

public sealed class TrajectoryBatchError
{
    public int? PositionIndex { get; set; }
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
