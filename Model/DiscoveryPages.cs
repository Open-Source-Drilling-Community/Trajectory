using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class AntiCollisionPolicyRevisionLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public System.Guid? PolicyID { get; set; }
    public int? RevisionNumber { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public double? ConfidenceFactor { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class AntiCollisionPolicyRevisionDiscoveryPage
{
    public List<AntiCollisionPolicyRevisionLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class DirectionalControlEvaluationCaseDiscoveryPage
{
    public List<DirectionalControlEvaluationCaseLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class InterpolatedTrajectoryDiscoveryPage
{
    public List<InterpolatedTrajectoryLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class SurveyRunBatchImportDiscoveryPage
{
    public List<SurveyRunBatchImportLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class SurveyRunDiscoveryPage
{
    public List<SurveyRunLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class SurveyRunMinimumDistanceCalculationDiscoveryPage
{
    public List<SurveyRunMinimumDistanceCalculationLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TargetLandingCaseDiscoveryPage
{
    public List<TargetLandingCaseLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryAggregationCaseDiscoveryPage
{
    public List<TrajectoryAggregationCaseLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryDiscoveryPage
{
    public List<TrajectoryLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryExtrapolationCaseDiscoveryPage
{
    public List<TrajectoryExtrapolationCaseLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class TrajectoryFeatureCategoryLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public bool IsExclusive { get; set; }
    public bool HasValidityPeriod { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public System.DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryFeatureCategoryDiscoveryPage
{
    public List<TrajectoryFeatureCategoryLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class TrajectoryIdentityLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public System.DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryIdentityDiscoveryPage
{
    public List<TrajectoryIdentityLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryMinimumDistanceCalculationDiscoveryPage
{
    public List<TrajectoryMinimumDistanceCalculationLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class TrajectoryRealizationCaseDiscoveryPage
{
    public List<TrajectoryRealizationCaseLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
