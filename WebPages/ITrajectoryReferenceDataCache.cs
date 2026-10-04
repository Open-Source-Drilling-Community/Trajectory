using OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.WebPages;

public interface ITrajectoryReferenceDataCache
{
    TrajectoryReferenceDataSnapshot Current { get; }

    Task<TrajectoryReferenceDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);

    Task RefreshTrajectoriesAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);

    Task RefreshSurveyRunsAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);

    async Task<IReadOnlyList<TrajectoryLight>> GetTrajectoriesAsync(CancellationToken cancellationToken = default)
    {
        if (Current.Trajectories.Count == 0)
        {
            await RefreshTrajectoriesAsync(cancellationToken);
        }

        return Current.Trajectories;
    }

    async Task<IReadOnlyList<SurveyRunLight>> GetSurveyRunsAsync(CancellationToken cancellationToken = default)
    {
        if (Current.SurveyRuns.Count == 0)
        {
            await RefreshSurveyRunsAsync(cancellationToken);
        }

        return Current.SurveyRuns;
    }
}

public sealed record TrajectoryReferenceDataSnapshot(
    IReadOnlyList<Field> Fields,
    IReadOnlyList<Cluster> Clusters,
    IReadOnlyList<Well> Wells,
    IReadOnlyList<WellBore> WellBores,
    IReadOnlyList<Rig> Rigs,
    IReadOnlyList<SurveyInstrumentLight> SurveyInstruments,
    IReadOnlyList<WellBoreArchitectureLight> WellBoreArchitectures,
    IReadOnlyList<TrajectoryLight> Trajectories,
    IReadOnlyList<SurveyRunLight> SurveyRuns,
    DateTimeOffset? RefreshedAtUtc)
{
    public TrajectoryReferenceDataSnapshot(
        IReadOnlyList<Field> fields,
        IReadOnlyList<Cluster> clusters,
        IReadOnlyList<Well> wells,
        IReadOnlyList<WellBore> wellBores,
        IReadOnlyList<Rig> rigs,
        IReadOnlyList<SurveyInstrumentLight> surveyInstruments,
        IReadOnlyList<WellBoreArchitectureLight> wellBoreArchitectures,
        DateTimeOffset? refreshedAtUtc)
        : this(fields, clusters, wells, wellBores, rigs, surveyInstruments, wellBoreArchitectures,
            [], [], refreshedAtUtc)
    {
    }

    public static TrajectoryReferenceDataSnapshot Empty { get; } = new(
        [], [], [], [], [], [], [], [], [], null);
}
