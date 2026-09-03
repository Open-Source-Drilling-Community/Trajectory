namespace OSDC.Drilling.Trajectory.Service.Managers;

public sealed class TrajectoryAssignmentValidator
{
    private readonly TrajectoryIdentityManager identities;
    private readonly TrajectoryFeatureCategoryManager categories;

    public TrajectoryAssignmentValidator(TrajectoryIdentityManager identities, TrajectoryFeatureCategoryManager categories)
    {
        this.identities = identities;
        this.categories = categories;
    }

    public bool Validate(Model.SurveyRun value)
    {
        value.SurveyRunIdentityAssignments ??= [];
        value.SurveyRunFeatureAssignments ??= [];
        return Validate(value.SurveyRunIdentityAssignments, value.SurveyRunFeatureAssignments);
    }

    public bool Validate(Model.Trajectory value)
    {
        value.TrajectoryIdentityAssignments ??= [];
        value.TrajectoryFeatureAssignments ??= [];
        return Validate(value.TrajectoryIdentityAssignments, value.TrajectoryFeatureAssignments);
    }

    private bool Validate(List<Model.TrajectoryIdentityAssignment> identityAssignments, List<Model.TrajectoryFeatureAssignment> featureAssignments)
    {
        if (identityAssignments.Any(value => value.ID == Guid.Empty) || identityAssignments.GroupBy(value => value.ID).Any(group => group.Count() > 1) ||
            featureAssignments.Any(value => value.ID == Guid.Empty) || featureAssignments.GroupBy(value => value.ID).Any(group => group.Count() > 1))
            return false;

        HashSet<Guid> identityIds = identities.GetAll().Select(value => value.MetaInfo!.ID).ToHashSet();
        if (identityAssignments.Any(value => value.IdentityID is not Guid id || !identityIds.Contains(id))) return false;

        Dictionary<Guid, Model.TrajectoryFeatureCategory> catalog = categories.GetAll().ToDictionary(value => value.MetaInfo!.ID);
        foreach (Model.TrajectoryFeatureAssignment assignment in featureAssignments)
        {
            if (assignment.FeatureCategoryID is not Guid categoryId || !catalog.TryGetValue(categoryId, out Model.TrajectoryFeatureCategory? category) ||
                assignment.FeatureOptionID is not Guid optionId || category.Options?.Any(option => option.ID == optionId) != true ||
                (!category.HasValidityPeriod && (assignment.FromDate != null || assignment.ToDate != null)) ||
                assignment.FromDate > assignment.ToDate)
                return false;
        }

        foreach (Model.TrajectoryFeatureCategory category in catalog.Values.Where(value => value.IsExclusive))
        {
            List<Model.TrajectoryFeatureAssignment> assigned = featureAssignments.Where(value => value.FeatureCategoryID == category.MetaInfo!.ID).ToList();
            for (int i = 0; i < assigned.Count; i++)
                for (int j = i + 1; j < assigned.Count; j++)
                    if (Overlaps(assigned[i], assigned[j])) return false;
        }
        return true;
    }

    private static bool Overlaps(Model.TrajectoryFeatureAssignment left, Model.TrajectoryFeatureAssignment right)
    {
        DateTimeOffset leftStart = left.FromDate ?? DateTimeOffset.MinValue;
        DateTimeOffset leftEnd = left.ToDate ?? DateTimeOffset.MaxValue;
        DateTimeOffset rightStart = right.FromDate ?? DateTimeOffset.MinValue;
        DateTimeOffset rightEnd = right.ToDate ?? DateTimeOffset.MaxValue;
        return leftStart <= rightEnd && rightStart <= leftEnd;
    }
}
