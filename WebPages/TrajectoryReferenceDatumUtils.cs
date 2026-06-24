using ModelShared = NORCE.Drilling.Trajectory.ModelShared;

namespace NORCE.Drilling.Trajectory.WebPages;

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
        Guid orderId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ModelShared.EarthMagneticFieldCalculationOrder order = new()
        {
            MetaInfo = CreateMetaInfo(orderId, api.HostNameEarthMagneticField, api.HostBasePathEarthMagneticField, "EarthMagneticFieldCalculationOrder/"),
            Name = $"Magnetic declination {orderId}",
            Description = "Temporary magnetic declination calculation.",
            CreationDate = now,
            LastModificationDate = now,
            CalculationMethod = ModelShared.EarthMagneticFieldCalculationMethod.WMM2025,
            RawEarthMagneticFieldTable = CreateEarthMagneticField(orderId, api, location, "raw"),
            CompletedEarthMagneticFieldTable = CreateEarthMagneticField(orderId, api, location, "completed", false)
        };

        try
        {
            await api.ClientEarthMagneticField.PostEarthMagneticFieldCalculationOrderAsync(order);
            ModelShared.EarthMagneticFieldCalculationOrder completed = await api.ClientEarthMagneticField.GetEarthMagneticFieldCalculationOrderByIdAsync(orderId);
            return completed.CompletedEarthMagneticFieldTable?.EarthMagneticFieldData?.FirstOrDefault()?.Declination;
        }
        finally
        {
            try
            {
                await api.ClientEarthMagneticField.DeleteEarthMagneticFieldCalculationOrderByIdAsync(orderId);
            }
            catch
            {
                // Best-effort cleanup of a temporary calculation order.
            }
        }
    }

    private static async Task<double?> ResolveGridConvergenceAsync(ITrajectoryAPIUtils api, ReferenceLocation location)
    {
        if (location.FieldId is not Guid fieldId || fieldId == Guid.Empty)
        {
            return null;
        }

        Guid conversionSetId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ModelShared.FieldCartographicConversionSet conversionSet = new()
        {
            MetaInfo = CreateMetaInfo(conversionSetId, api.HostNameField, api.HostBasePathField, "FieldCartographicConversionSet/"),
            Name = $"Grid convergence {conversionSetId}",
            Description = "Temporary grid convergence calculation.",
            CreationDate = now,
            LastModificationDate = now,
            FieldID = fieldId,
            CartographicCoordinateList =
            [
                new ModelShared.CartographicCoordinate
                {
                    VerticalDepth = location.DepthWgs84,
                    GeodeticCoordinate = new ModelShared.GeodeticCoordinate
                    {
                        LatitudeWGS84 = location.Latitude,
                        LongitudeWGS84 = location.Longitude,
                        VerticalDepthWGS84 = location.DepthWgs84
                    }
                }
            ]
        };

        try
        {
            await api.ClientField.PostFieldCartographicConversionSetAsync(conversionSet);
            ModelShared.FieldCartographicConversionSet completed = await api.ClientField.GetFieldCartographicConversionSetByIdAsync(conversionSetId);
            return completed.CartographicCoordinateList?.FirstOrDefault()?.GridConvergenceDatum;
        }
        finally
        {
            try
            {
                await api.ClientField.DeleteFieldCartographicConversionSetByIdAsync(conversionSetId);
            }
            catch
            {
                // Best-effort cleanup of a temporary conversion set.
            }
        }
    }

    private static ModelShared.EarthMagneticField CreateEarthMagneticField(Guid orderId, ITrajectoryAPIUtils api, ReferenceLocation location, string suffix, bool includeRawPoint = true)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new ModelShared.EarthMagneticField
        {
            MetaInfo = CreateMetaInfo(Guid.NewGuid(), api.HostNameEarthMagneticField, api.HostBasePathEarthMagneticField, "EarthMagneticField/"),
            Name = $"Magnetic declination {suffix} {orderId}",
            Description = "Temporary magnetic declination calculation.",
            CreationDate = now,
            LastModificationDate = now,
            Type = includeRawPoint ? ModelShared.EarthMagneticFieldType.Raw : ModelShared.EarthMagneticFieldType.Completed,
            EarthMagneticFieldData = includeRawPoint
                ?
                [
                    new ModelShared.EarthMagneticData
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Depth = location.DepthWgs84,
                        Year = DateTime.UtcNow.Year + ((double)DateTime.UtcNow.DayOfYear - 1.0) / (DateTime.IsLeapYear(DateTime.UtcNow.Year) ? 366.0 : 365.0)
                    }
                ]
                : []
        };
    }

    private static ModelShared.MetaInfo CreateMetaInfo(Guid id, string hostName, string hostBasePath, string endpoint) =>
        new()
        {
            ID = id,
            HttpHostName = hostName,
            HttpHostBasePath = hostBasePath,
            HttpEndPoint = endpoint
        };

    private sealed record ReferenceLocation(double Latitude, double Longitude, double DepthWgs84, Guid? FieldId);
}
