using OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.WebPages;

public interface ITrajectoryReferenceDataCache
{
    TrajectoryReferenceDataSnapshot Current { get; }

    Task<TrajectoryReferenceDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}

public sealed record TrajectoryReferenceDataSnapshot(
    IReadOnlyList<Field> Fields,
    IReadOnlyList<Cluster> Clusters,
    IReadOnlyList<Well> Wells,
    IReadOnlyList<WellBore> WellBores,
    IReadOnlyList<Rig> Rigs,
    IReadOnlyList<SurveyInstrumentLight> SurveyInstruments,
    IReadOnlyList<WellBoreArchitectureLight> WellBoreArchitectures,
    DateTimeOffset? RefreshedAtUtc)
{
    public static TrajectoryReferenceDataSnapshot Empty { get; } = new(
        [], [], [], [], [], [], [], null);
}
