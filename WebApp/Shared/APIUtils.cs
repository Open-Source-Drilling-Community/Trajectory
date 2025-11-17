public static class APIUtils
{
    // API parameters
    public static readonly string HostNameTrajectory = NORCE.Drilling.Trajectory.WebApp.Configuration.TrajectoryHostURL!;
    public static readonly string HostBasePathTrajectory = "Trajectory/api/";
    public static readonly HttpClient HttpClientTrajectory = APIUtils.SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
    public static readonly NORCE.Drilling.Trajectory.ModelShared.Client ClientTrajectory = new NORCE.Drilling.Trajectory.ModelShared.Client(APIUtils.HttpClientTrajectory.BaseAddress!.ToString(), APIUtils.HttpClientTrajectory);

    public static readonly string HostNameUnitConversion = NORCE.Drilling.Trajectory.WebApp.Configuration.UnitConversionHostURL!;
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
}