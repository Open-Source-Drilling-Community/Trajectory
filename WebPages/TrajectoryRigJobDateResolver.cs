using Microsoft.Extensions.Logging;

namespace OSDC.Drilling.Trajectory.WebPages;

public static class TrajectoryRigJobDateResolver
{
    public static async Task<DateTimeOffset?> ResolveAsync(
        ITrajectoryAPIUtils api,
        Guid? trajectoryId,
        IReadOnlyList<ModelShared.TrajectoryLight> trajectories,
        IReadOnlyList<ModelShared.SurveyRunLight> surveyRuns,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (trajectoryId is not Guid id || id == Guid.Empty) return null;
        try
        {
            ModelShared.Trajectory trajectory = await api.ClientTrajectory.GetTrajectoryByIdAsync(id, false);
            HashSet<Guid> runIds = trajectory.SurveyRunSectionList?
                .Where(section => section != null && section.SurveyRunID != Guid.Empty)
                .Select(section => section.SurveyRunID)
                .ToHashSet() ?? [];
            DateTimeOffset? surveyDate = surveyRuns
                .Where(run => run.MetaInfo != null && runIds.Contains(run.MetaInfo.ID))
                .Select(RigJobSelectionUtils.SurveyRunDate)
                .Where(date => date.HasValue)
                .Max();
            ModelShared.TrajectoryLight? light = trajectories.FirstOrDefault(item => item.MetaInfo?.ID == id);
            return surveyDate ?? light?.LastModificationDate ?? light?.CreationDate;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to resolve survey date for trajectory {TrajectoryId}", id);
            ModelShared.TrajectoryLight? light = trajectories.FirstOrDefault(item => item.MetaInfo?.ID == id);
            return light?.LastModificationDate ?? light?.CreationDate;
        }
    }
}
