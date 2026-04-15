using NORCE.Drilling.Trajectory.ModelShared;

public static class APIUtils
{
    // API parameters
    public static readonly string HostNameField = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.FieldHostURL!;
    public static readonly string HostBasePathField = "Field/api/";
    public static readonly HttpClient HttpClientField = APIUtils.SetHttpClient(HostNameField, HostBasePathField);
    public static readonly Client ClientField = new Client(APIUtils.HttpClientField.BaseAddress!.ToString(), APIUtils.HttpClientField);

    public static readonly string HostNameCluster = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.ClusterHostURL!;
    public static readonly string HostBasePathCluster = "Cluster/api/";
    public static readonly HttpClient HttpClientCluster = APIUtils.SetHttpClient(HostNameCluster, HostBasePathCluster);
    public static readonly Client ClientCluster = new Client(APIUtils.HttpClientCluster.BaseAddress!.ToString(), APIUtils.HttpClientCluster);

    public static readonly string HostNameRig = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.RigHostURL!;
    public static readonly string HostBasePathRig = "Rig/api/";
    public static readonly HttpClient HttpClientRig = APIUtils.SetHttpClient(HostNameRig, HostBasePathRig);
    public static readonly Client ClientRig = new Client(APIUtils.HttpClientRig.BaseAddress!.ToString(), APIUtils.HttpClientRig);

    public static readonly string HostNameWell = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.WellHostURL!;
    public static readonly string HostBasePathWell = "Well/api/";
    public static readonly HttpClient HttpClientWell = APIUtils.SetHttpClient(HostNameWell, HostBasePathWell);
    public static readonly Client ClientWell = new Client(APIUtils.HttpClientWell.BaseAddress!.ToString(), APIUtils.HttpClientWell);

    public static readonly string HostNameWellBore = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.WellBoreHostURL!;
    public static readonly string HostBasePathWellBore = "WellBore/api/";
    public static readonly HttpClient HttpClientWellBore = APIUtils.SetHttpClient(HostNameWellBore, HostBasePathWellBore);
    public static readonly NORCE.Drilling.Trajectory.ModelShared.Client ClientWellBore = new(APIUtils.HttpClientWellBore.BaseAddress!.ToString(), APIUtils.HttpClientWellBore);

    public static readonly string HostNameWellBoreArchitecture = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.WellBoreArchitectureHostURL!;
    public static readonly string HostBasePathWellBoreArchitecture = "WellBoreArchitecture/api/";
    public static readonly HttpClient HttpClientWellBoreArchitecture = APIUtils.SetHttpClient(HostNameWellBoreArchitecture, HostBasePathWellBoreArchitecture);
    public static readonly NORCE.Drilling.Trajectory.ModelShared.Client ClientWellBoreArchitecture = new(APIUtils.HttpClientWellBoreArchitecture.BaseAddress!.ToString(), APIUtils.HttpClientWellBoreArchitecture);

    public static readonly string HostNameTrajectory = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.TrajectoryHostURL!;
    public static readonly string HostBasePathTrajectory = "Trajectory/api/";
    public static readonly HttpClient HttpClientTrajectory = APIUtils.SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
    public static readonly NORCE.Drilling.Trajectory.ModelShared.Client ClientTrajectory = new(APIUtils.HttpClientTrajectory.BaseAddress!.ToString(), APIUtils.HttpClientTrajectory);

    public static readonly string HostNameUnitConversion = NORCE.Drilling.Trajectory.WebApp.WebAppConfiguration.UnitConversionHostURL!;
    public static readonly string HostBasePathUnitConversion = "UnitConversion/api/";

    // API utility methods
    public static HttpClient SetHttpClient(string host, string microServiceUri)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }; // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri(host + microServiceUri)
        };
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }

    /// <summary>
    /// Retrieves the cluster and slot hosting the <paramref name="wellBore"/>
    /// <param name="wellBore">the wellbore to retrieve the hosting cluster from</param>
    /// </summary>
    public static async Task<(GaussianGeodeticPoint3D?, string)> GetReferencePointAsync(WellBore wellBore)
    {
        string msg;
        try
        {
            // We cascade up to the slot it is connected to retrieve the reference point coordinates of the tie-in Gaussian geodetic point
            if (wellBore?.WellID is Guid wellId && wellId != Guid.Empty)
            {
                Well? well = await APIUtils.ClientWell.GetWellByIdAsync(wellId);
                if (well is not null &&
                    well.ClusterID is Guid clusterId && clusterId != Guid.Empty &&
                    well.SlotID is Guid slotId && slotId != Guid.Empty)
                {
                    Cluster? cluster = await APIUtils.ClientCluster.GetClusterByIdAsync(clusterId);
                    if (cluster?.Slots is { } slots &&
                        slots.TryGetValue(slotId.ToString(), out var slot) &&
                        slot is not null)
                    {
                        if (slot.Latitude?.GaussianValue?.Mean is { } refLat &&
                            slot.Longitude?.GaussianValue?.Mean is { } refLon &&
                            cluster.ReferenceDepth?.GaussianValue?.Mean is { } refTVD)
                        {
                            msg = "cluster, slot, and wellbore successfully retrieved";
                            return (new GaussianGeodeticPoint3D()
                            {
                                GeodeticMean = new GeodeticPoint3D() { LatitudeWGS84 = refLat, LongitudeWGS84 = refLon, TvdWGS84 = refTVD },
                                CovarianceNED = new Matrix3x3(),
                                ReferencePoint = new GeodeticPoint3D() { LatitudeWGS84 = refLat, LongitudeWGS84 = refLon, TvdWGS84 = refTVD }
                            },
                                    msg);
                        }
                        else
                        {
                            msg = "coordinates of the hosting slot are not properly set";
                            return (null, msg);
                        }
                    }
                    else
                    {
                        msg = "the cluster hosting the well hosting the wellbore is null or has no slots or does not contain the expected slot ID";
                        return (null, msg);
                    }
                }
                else
                {
                    msg = "the well hosting the wellbore is null or has a corrupted cluster ID or slot ID";
                    return (null, msg);
                }
            }
            else
            {
                msg = "the wellbore has a corrupted well ID";
                return (null, msg);
            }
        }
        catch (Exception ex)
        {
            msg = ex.Message + ": an exception was raised while retrieving hosting cluster and slot from wellbore";
            return (null, msg);
        }
    }
}