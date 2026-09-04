using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Outcome of checking references owned by other microservices.</summary>
public enum ExternalReferenceValidationStatus
{
    Valid,
    Invalid,
    Unavailable
}

public sealed class ExternalReferenceIssue
{
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class TrajectoryExternalReferenceValidation
{
    public Guid TrajectoryID { get; set; }
    public Guid? FieldID { get; set; }
    public Guid? ClusterID { get; set; }
    public Guid? WellID { get; set; }
    public Guid WellBoreID { get; set; }
    public bool? FieldExists { get; set; }
    public bool? ClusterExists { get; set; }
    public bool? WellExists { get; set; }
    public bool? WellBoreExists { get; set; }
    public ExternalReferenceValidationStatus Status { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
    public List<ExternalReferenceIssue> Issues { get; set; } = [];
}

public sealed class SurveyRunExternalReferenceValidation
{
    public Guid SurveyRunID { get; set; }
    public Guid? FieldID { get; set; }
    public Guid? ClusterID { get; set; }
    public Guid? WellID { get; set; }
    public Guid WellBoreID { get; set; }
    public Guid SurveyInstrumentID { get; set; }
    public bool? FieldExists { get; set; }
    public bool? ClusterExists { get; set; }
    public bool? WellExists { get; set; }
    public bool? WellBoreExists { get; set; }
    public bool? SurveyInstrumentExists { get; set; }
    public ExternalReferenceValidationStatus Status { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
    public List<ExternalReferenceIssue> Issues { get; set; } = [];
}

public enum ExternalReferenceAuditScope
{
    All,
    Selected
}

public sealed class TrajectoryExternalReferenceAuditRequest
{
    [JsonRequired]
    public ExternalReferenceAuditScope Scope { get; set; }
    public List<Guid>? TrajectoryIDs { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = 100;
}

public sealed class SurveyRunExternalReferenceAuditRequest
{
    [JsonRequired]
    public ExternalReferenceAuditScope Scope { get; set; }
    public List<Guid>? SurveyRunIDs { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = 100;
}

public sealed class TrajectoryExternalReferenceAuditResult
{
    public DateTimeOffset CheckedAtUtc { get; set; }
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int UnavailableCount { get; set; }
    public List<TrajectoryExternalReferenceValidation> Items { get; set; } = [];
}

public sealed class SurveyRunExternalReferenceAuditResult
{
    public DateTimeOffset CheckedAtUtc { get; set; }
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int UnavailableCount { get; set; }
    public List<SurveyRunExternalReferenceValidation> Items { get; set; } = [];
}
