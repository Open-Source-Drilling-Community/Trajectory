namespace OSDC.Drilling.Trajectory.WebPages;

public static class RigJobSelectionUtils
{
    public static ModelShared.RigJob? SelectDefault(
        ModelShared.WellBore? wellBore,
        DateTimeOffset? referenceDate)
    {
        List<ModelShared.RigJob> jobs = wellBore?.RigJobs?
            .Where(job => job != null && job.RigJobID != Guid.Empty && job.RigID != Guid.Empty)
            .ToList() ?? [];
        if (jobs.Count == 0) return null;
        if (referenceDate is not DateTimeOffset instant)
            return jobs.OrderBy(job => job.EndDate ?? DateTimeOffset.MaxValue).ThenBy(job => job.StartDate).Last();

        List<ModelShared.RigJob> active = jobs
            .Where(job => job.StartDate <= instant && (job.EndDate == null || job.EndDate >= instant))
            .OrderBy(job => job.StartDate)
            .ToList();
        if (active.Count > 0) return active[^1];

        return jobs
            .OrderBy(job => DistanceToInterval(job, instant))
            .ThenByDescending(job => job.StartDate)
            .First();
    }

    public static DateTimeOffset? SurveyRunDate(ModelShared.SurveyRunLight? surveyRun) =>
        surveyRun?.AcquisitionEndUtc ?? surveyRun?.AcquisitionStartUtc ?? surveyRun?.LastModificationDate ?? surveyRun?.CreationDate;

    private static TimeSpan DistanceToInterval(ModelShared.RigJob job, DateTimeOffset instant)
    {
        if (instant < job.StartDate) return job.StartDate - instant;
        if (job.EndDate is DateTimeOffset end && instant > end) return instant - end;
        return TimeSpan.Zero;
    }
}
