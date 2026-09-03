using ModelShared = OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.WebPages;

public sealed record TrajectoryReferenceDatumValues(
    double? MeanSeaLevelDepthReference,
    double? GridConvergence,
    double? MagneticDeclination);

public static class TrajectoryReferenceDatumUtils
{
    public static async Task<TrajectoryReferenceDatumValues> ResolveForTrajectoryAsync(
        ITrajectoryAPIUtils api,
        Guid? trajectoryId,
        IEnumerable<ModelShared.TrajectoryLight>? trajectories,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.TrajectoryLight? trajectory = trajectories?.FirstOrDefault(item => item?.MetaInfo?.ID == trajectoryId);
        return await ResolveForWellBoreAsync(api, trajectory?.WellBoreID, wellBores, wells, clusters);
    }

    public static async Task<TrajectoryReferenceDatumValues> ResolveForSurveyRunAsync(
        ITrajectoryAPIUtils api,
        Guid? surveyRunId,
        IEnumerable<ModelShared.SurveyRunLight>? surveyRuns,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.SurveyRunLight? surveyRun = surveyRuns?.FirstOrDefault(item => item?.MetaInfo?.ID == surveyRunId);
        return await ResolveForWellBoreAsync(api, surveyRun?.WellBoreID, wellBores, wells, clusters);
    }

    public static async Task<TrajectoryReferenceDatumValues> ResolveForWellBoreAsync(
        ITrajectoryAPIUtils api,
        Guid? wellBoreId,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ReferenceLocation? location = ResolveReferenceLocation(wellBoreId, wellBores, wells, clusters);
        if (location == null)
        {
            return new TrajectoryReferenceDatumValues(null, null, null);
        }

        Task<double?> mslTask = MslDepthReferenceUtils.ResolveMeanSeaLevelDepthReferenceForWellBoreAsync(api, wellBoreId, wellBores, wells, clusters);
        Task<double?> gridTask = ResolveGridConvergenceAsync(api, location);
        Task<double?> magneticTask = ResolveMagneticDeclinationAsync(api, location);
        await Task.WhenAll(mslTask, gridTask, magneticTask);
        return new TrajectoryReferenceDatumValues(await mslTask, await gridTask, await magneticTask);
    }

    public static void Apply(TrajectoryReferenceDatumValues values)
    {
        DataUtils.MeanSeaLevelDepthReferenceSource.MeanSeaLevelDepthReference = values.MeanSeaLevelDepthReference;
        DataUtils.GridConvergenceSource.GridConvergence = values.GridConvergence;
        DataUtils.MagneticDeclinationSource.MagneticDeclination = values.MagneticDeclination;
    }

    private static ReferenceLocation? ResolveReferenceLocation(
        Guid? wellBoreId,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.WellBore? wellBore = wellBores?.FirstOrDefault(item => item?.MetaInfo?.ID == wellBoreId);
        ModelShared.WellBore? rootWellBore = ResolveRootWellBore(wellBore, wellBores);
        ModelShared.Well? well = wells?.FirstOrDefault(item => item?.MetaInfo?.ID == rootWellBore?.WellID);
        ModelShared.Cluster? cluster = clusters?.FirstOrDefault(item => item?.MetaInfo?.ID == well?.ClusterID);
        ModelShared.Slot? slot = ResolveSlot(well, cluster, clusters);

        double? latitude = slot?.Latitude?.GaussianValue?.Mean ?? cluster?.ReferenceLatitude?.GaussianValue?.Mean;
        double? longitude = slot?.Longitude?.GaussianValue?.Mean ?? cluster?.ReferenceLongitude?.GaussianValue?.Mean;
        if (latitude == null || longitude == null)
        {
            return null;
        }

        return new ReferenceLocation(
            latitude.Value,
            longitude.Value,
            cluster?.ReferenceDepth?.GaussianValue?.Mean ?? 0.0,
            cluster?.FieldID);
    }

    private static ModelShared.WellBore? ResolveRootWellBore(ModelShared.WellBore? wellBore, IEnumerable<ModelShared.WellBore>? wellBores)
    {
        ModelShared.WellBore? current = wellBore;
        HashSet<Guid> visitedIds = new();
        while (current?.IsSidetrack == true &&
            current.ParentWellBoreID is Guid parentId &&
            parentId != Guid.Empty &&
            visitedIds.Add(parentId))
        {
            ModelShared.WellBore? parent = wellBores?.FirstOrDefault(item => item?.MetaInfo?.ID == parentId);
            if (parent == null)
            {
                break;
            }

            current = parent;
        }

        return current;
    }

    private static ModelShared.Slot? ResolveSlot(ModelShared.Well? well, ModelShared.Cluster? selectedCluster, IEnumerable<ModelShared.Cluster>? clusters)
    {
        if (well?.SlotID is not Guid slotId)
        {
            return null;
        }

        ModelShared.Cluster? cluster = selectedCluster;
        cluster ??= clusters?.FirstOrDefault(item => item?.MetaInfo?.ID == well.ClusterID);
        cluster ??= clusters?.FirstOrDefault(item => item?.Slots?.Values.Any(slot => slot?.ID == slotId) == true);
        return cluster?.Slots?.Values.FirstOrDefault(slot => slot?.ID == slotId);
    }

    private static async Task<double?> ResolveMagneticDeclinationAsync(ITrajectoryAPIUtils api, ReferenceLocation location)
    {
        ModelShared.EvaluateEarthMagneticFieldResponse response = await api.ClientEarthMagneticField.EvaluateEarthMagneticFieldAsync(
            new ModelShared.EvaluateEarthMagneticFieldRequest
            {
                Model = ModelShared.EarthMagneticFieldModel.WMM2025,
                Samples =
                [
                    new ModelShared.EarthMagneticFieldEvaluationPoint
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Depth = location.DepthWgs84,
                        DateTimeUtc = DateTimeOffset.UtcNow
                    }
                ]
            });

        return response.Samples?.FirstOrDefault()?.Declination;
    }

    private static async Task<double?> ResolveGridConvergenceAsync(ITrajectoryAPIUtils api, ReferenceLocation location)
    {
        if (location.FieldId is not Guid fieldId || fieldId == Guid.Empty)
        {
            return null;
        }

        try
        {
            ModelShared.FieldCoordinateConversionResponse response = await api.ClientField.ForwardFieldCoordinatesAsync(
                new ModelShared.FieldForwardConversionRequest
                {
                    FieldID = fieldId,
                    SourceGeographicReference = ModelShared.FieldGeographicReference.Wgs84,
                    ProjectionApplicabilityPolicy = ModelShared.FieldApplicabilityPolicy.RequireApplicable,
                    Positions =
                    [
                        new ModelShared.FieldForwardConversionPosition
                        {
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            VerticalDepth = location.DepthWgs84
                        }
                    ]
                });

            return response.Positions?.FirstOrDefault()?.GridConvergence;
        }
        catch (ModelShared.ApiException exception) when (exception.StatusCode is 404 or 422 or 502)
        {
            // Grid convergence is optional reference data. A missing field projection,
            // an out-of-area position, or an unavailable dependency must not prevent
            // the survey run or trajectory itself from opening.
            return null;
        }
    }

    private sealed record ReferenceLocation(double Latitude, double Longitude, double DepthWgs84, Guid? FieldId);
}
