using OSDC.Drilling.GlobalAntiCollision;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.ModelShared;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.Service;

public sealed record AntiCollisionComparisonContext(
    DateTimeOffset? OldestEvidenceUtc,
    DateTimeOffset? NewestEvidenceUtc,
    List<AntiCollisionResourceContextSnapshot> Resources);

/// <summary>Freezes comparison-side hierarchy, classifications, and measurement-age evidence.</summary>
public sealed class AntiCollisionPolicyContextResolver(
    ILogger<AntiCollisionPolicyContextResolver> logger,
    ILogger<SurveyRunManager> surveyRunLogger,
    SqlConnectionManager database)
{
    private readonly SurveyRunManager surveyRunManager_ = SurveyRunManager.GetInstance(surveyRunLogger, database);

    public async Task<AntiCollisionComparisonContext> ResolveAsync(Model.Trajectory trajectory)
    {
        (DateTimeOffset? oldest, DateTimeOffset? newest) = ResolveEvidenceRange(trajectory);
        List<AntiCollisionResourceContextSnapshot> resources = [TrajectoryContext(trajectory)];

        WellBore? wellBore = null;
        Well? well = null;
        Cluster? cluster = null;
        try
        {
            wellBore = trajectory.WellBoreID == Guid.Empty ? null : await APIUtils.ClientWellBore.GetWellBoreByIdAsync(trajectory.WellBoreID);
        }
        catch (Exception ex) { logger.LogWarning(ex, "WellBore context unavailable for anti-collision policy evaluation"); }
        resources.Add(wellBore == null ? Unavailable(AntiCollisionHierarchyLevel.WellBore, trajectory.WellBoreID, "The WellBore service or record is unavailable.") : WellBoreContext(wellBore));

        Guid wellId = wellBore?.WellID ?? trajectory.WellID ?? Guid.Empty;
        try { well = wellId == Guid.Empty ? null : await APIUtils.ClientWell.GetWellByIdAsync(wellId); }
        catch (Exception ex) { logger.LogWarning(ex, "Well context unavailable for anti-collision policy evaluation"); }
        resources.Add(well == null ? Unavailable(AntiCollisionHierarchyLevel.Well, wellId, "The Well service or record is unavailable.") : WellContext(well));

        Guid clusterId = well?.ClusterID ?? trajectory.ClusterID ?? Guid.Empty;
        try { cluster = clusterId == Guid.Empty ? null : await APIUtils.ClientCluster.GetClusterByIdAsync(clusterId); }
        catch (Exception ex) { logger.LogWarning(ex, "Cluster context unavailable for anti-collision policy evaluation"); }
        Guid slotId = well?.SlotID ?? Guid.Empty;
        Slot? slot = cluster?.Slots?.Values.FirstOrDefault(value => value.ID == slotId);
        resources.Add(slot == null ? Unavailable(AntiCollisionHierarchyLevel.Slot, slotId, "The Slot or its owning Cluster is unavailable.") : SlotContext(slot));
        resources.Add(cluster == null ? Unavailable(AntiCollisionHierarchyLevel.Cluster, clusterId, "The Cluster service or record is unavailable.") : ClusterContext(cluster));
        return new AntiCollisionComparisonContext(oldest, newest, resources);
    }

    private (DateTimeOffset? Oldest, DateTimeOffset? Newest) ResolveEvidenceRange(Model.Trajectory trajectory)
    {
        List<DateTimeOffset> oldestEvidence = [];
        List<DateTimeOffset> newestEvidence = [];
        List<TrajectorySurveyRunSection> sections = trajectory.SurveyRunSectionList?.OrderBy(section => section.StartAbscissa).ToList() ?? [];
        for (int index = 0; index < sections.Count; index++)
        {
            SurveyRun? run = surveyRunManager_.GetSurveyRunById(sections[index].SurveyRunID, includeMeasurements: true, includeCalculatedStations: false);
            if (run == null) continue;
            if (run.AcquisitionStartUtc.HasValue)
            {
                oldestEvidence.Add(run.AcquisitionStartUtc.Value);
                newestEvidence.Add(run.AcquisitionStartUtc.Value);
            }
            if (run.AcquisitionEndUtc.HasValue) newestEvidence.Add(run.AcquisitionEndUtc.Value);
            double from = sections[index].StartAbscissa;
            double? to = index + 1 < sections.Count ? sections[index + 1].StartAbscissa : null;
            List<DateTimeOffset> measurementTimes = (run.SurveyMeasurementList ?? [])
                .Where(measurement => measurement.Origin == SurveyMeasurementOrigin.Measured)
                .Where(measurement => measurement.MeasurementTimeUtc.HasValue && measurement.MD is double md && md >= from && (!to.HasValue || md < to.Value))
                .Select(measurement => measurement.MeasurementTimeUtc!.Value).ToList();
            oldestEvidence.AddRange(measurementTimes);
            newestEvidence.AddRange(measurementTimes);
        }
        return (oldestEvidence.Count == 0 ? null : oldestEvidence.Min(), newestEvidence.Count == 0 ? null : newestEvidence.Max());
    }

    private static AntiCollisionResourceContextSnapshot TrajectoryContext(Model.Trajectory value) => new()
    {
        ResourceLevel = AntiCollisionHierarchyLevel.Trajectory,
        ResourceID = value.MetaInfo?.ID ?? Guid.Empty,
        Name = value.Name,
        Identities = (value.TrajectoryIdentityAssignments ?? []).Where(item => item.IdentityID.HasValue)
            .Select(item => new AntiCollisionIdentityValueSnapshot { IdentityDefinitionID = item.IdentityID!.Value, Value = item.Value }).ToList(),
        Features = (value.TrajectoryFeatureAssignments ?? []).Where(item => item.FeatureCategoryID.HasValue && item.FeatureOptionID.HasValue)
            .Select(item => Feature(item.FeatureCategoryID!.Value, item.FeatureOptionID!.Value, item.FromDate, item.ToDate)).ToList()
    };

    private static AntiCollisionResourceContextSnapshot ClusterContext(Cluster value) => new()
    {
        ResourceLevel = AntiCollisionHierarchyLevel.Cluster, ResourceID = value.MetaInfo?.ID ?? Guid.Empty, Name = value.Name,
        Identities = (value.ClusterIdentityAssignments ?? []).Where(item => item.IdentityID.HasValue)
            .Select(item => new AntiCollisionIdentityValueSnapshot { IdentityDefinitionID = item.IdentityID!.Value, Value = item.Value }).ToList(),
        Features = (value.ClusterFeatureAssignments ?? []).Where(item => item.FeatureCategoryID.HasValue && item.FeatureOptionID.HasValue)
            .Select(item => Feature(item.FeatureCategoryID!.Value, item.FeatureOptionID!.Value, item.FromDate, item.ToDate)).ToList()
    };

    private static AntiCollisionResourceContextSnapshot SlotContext(Slot value) => new()
    {
        ResourceLevel = AntiCollisionHierarchyLevel.Slot, ResourceID = value.ID, Name = value.Name,
        IdentityCatalogAvailable = false,
        UnavailableReason = "The current Cluster contract does not expose Slot identity definitions or assignments.",
        Features = (value.SlotFeatureAssignments ?? []).Where(item => item.FeatureCategoryID.HasValue && item.FeatureOptionID.HasValue)
            .Select(item => Feature(item.FeatureCategoryID!.Value, item.FeatureOptionID!.Value, item.FromDate, item.ToDate)).ToList()
    };

    private static AntiCollisionResourceContextSnapshot WellContext(Well value) => new()
    {
        ResourceLevel = AntiCollisionHierarchyLevel.Well, ResourceID = value.MetaInfo?.ID ?? Guid.Empty, Name = value.Name,
        Identities = (value.WellIdentityAssignments ?? []).Where(item => item.IdentityID.HasValue)
            .Select(item => new AntiCollisionIdentityValueSnapshot { IdentityDefinitionID = item.IdentityID!.Value, Value = item.Value }).ToList(),
        Features = (value.WellFeatureAssignments ?? []).Where(item => item.FeatureCategoryID.HasValue && item.FeatureOptionID.HasValue)
            .Select(item => Feature(item.FeatureCategoryID!.Value, item.FeatureOptionID!.Value, item.FromDate, item.ToDate)).ToList()
    };

    private static AntiCollisionResourceContextSnapshot WellBoreContext(WellBore value) => new()
    {
        ResourceLevel = AntiCollisionHierarchyLevel.WellBore, ResourceID = value.MetaInfo?.ID ?? Guid.Empty, Name = value.Name,
        Identities = (value.WellBoreIdentityAssignments ?? []).Where(item => item.IdentityID.HasValue)
            .Select(item => new AntiCollisionIdentityValueSnapshot { IdentityDefinitionID = item.IdentityID!.Value, Value = item.Value }).ToList(),
        Features = (value.WellBoreFeatureAssignments ?? []).Where(item => item.FeatureCategoryID.HasValue && item.FeatureOptionID.HasValue)
            .Select(item => Feature(item.FeatureCategoryID!.Value, item.FeatureOptionID!.Value, item.FromDate, item.ToDate)).ToList()
    };

    private static AntiCollisionFeatureValueSnapshot Feature(Guid category, Guid option, DateTimeOffset? from, DateTimeOffset? to) =>
        new() { FeatureCategoryID = category, FeatureOptionID = option, FromUtc = from, ToUtc = to };

    private static AntiCollisionResourceContextSnapshot Unavailable(AntiCollisionHierarchyLevel level, Guid id, string reason) =>
        new() { ResourceLevel = level, ResourceID = id, IsAvailable = false, IdentityCatalogAvailable = false, UnavailableReason = reason };
}
