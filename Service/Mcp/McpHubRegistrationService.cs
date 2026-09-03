using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSDC.Drilling.Trajectory.Service.Managers;
namespace OSDC.Drilling.Trajectory.Service.Mcp;
public sealed class McpHubRegistrationService : BackgroundService
{
    public static readonly Guid ServiceTypeId = Guid.Parse("5dd4d00d-b7f5-45a5-9f86-2390c1bcf07a");
    private const string InstanceIdFileName = "trajectory-mcp-hub-instance-id.txt";
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<McpHubRegistrationService> _logger;
    private readonly IOptionsMonitor<McpHubOptions> _options;
    private Guid? _registeredInstanceId;
    public McpHubRegistrationService(IHttpClientFactory clients, ILogger<McpHubRegistrationService> logger, IOptionsMonitor<McpHubOptions> options)
    { _clients = clients; _logger = logger; _options = options; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        McpHubOptions options = _options.CurrentValue;
        if (!options.Enabled) return;
        if (!Complete(options)) { _logger.LogWarning("MCP hub registration skipped because its URLs are not configured."); return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            options = _options.CurrentValue;
            if (!options.Enabled || !Complete(options)) return;
            try
            {
                Guid id = ResolveInstanceId(options);
                Uri collection = CollectionUri(options);
                using HttpClient client = _clients.CreateClient(nameof(McpHubRegistrationService));
                using HttpResponseMessage response = await PutOrPost(client, collection, id, RegistrationFor(options, id), stoppingToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) _registeredInstanceId = id;
                else _logger.LogWarning("MCP hub registration failed with status {StatusCode}.", response.StatusCode);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _logger.LogWarning(ex, "MCP hub registration attempt failed."); }
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.RetryIntervalSeconds)), stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        McpHubOptions options = _options.CurrentValue;
        if (options.UnregisterOnShutdown && _registeredInstanceId.HasValue && !string.IsNullOrWhiteSpace(options.HubBaseUrl))
        {
            try
            {
                using HttpClient client = _clients.CreateClient(nameof(McpHubRegistrationService));
                using HttpResponseMessage response = await client.DeleteAsync(new Uri(CollectionUri(options), _registeredInstanceId.Value.ToString()), cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound) _logger.LogWarning("MCP hub unregister failed with status {StatusCode}.", response.StatusCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "MCP hub unregister failed."); }
        }
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
    private static async Task<HttpResponseMessage> PutOrPost(HttpClient client, Uri collection, Guid id, Registration value, CancellationToken ct)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(new Uri(collection, id.ToString()), value, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.NotFound) return response;
        response.Dispose();
        return await client.PostAsJsonAsync(collection, value, ct).ConfigureAwait(false);
    }
    private static bool Complete(McpHubOptions o) => !string.IsNullOrWhiteSpace(o.HubBaseUrl) && !string.IsNullOrWhiteSpace(o.PublicBaseUrl);
    private static Uri CollectionUri(McpHubOptions o) => new(new Uri(o.HubBaseUrl!.TrimEnd('/') + "/"), (string.IsNullOrWhiteSpace(o.RegistrationEndpoint) ? "McpMicroservice" : o.RegistrationEndpoint.Trim('/')) + "/");
    private static Registration RegistrationFor(McpHubOptions o, Guid id)
    {
        string url = o.PublicBaseUrl!.TrimEnd('/');
        return new Registration(ServiceTypeId, id, string.IsNullOrWhiteSpace(o.ServiceName) ? "Trajectory" : o.ServiceName,
            $"{url}/Trajectory/api/mcp", ToWebSocket($"{url}/Trajectory/api/mcp/ws"), DateTimeOffset.UtcNow);
    }
    private static Guid ResolveInstanceId(McpHubOptions o)
    {
        if (Guid.TryParse(o.InstanceId, out Guid configured) && configured != Guid.Empty) return configured;
        Directory.CreateDirectory(SqlConnectionManager.HOME_DIRECTORY);
        string file = Path.Combine(SqlConnectionManager.HOME_DIRECTORY, InstanceIdFileName);
        if (File.Exists(file) && Guid.TryParse(File.ReadAllText(file), out Guid persisted) && persisted != Guid.Empty) return persisted;
        Guid generated = Guid.NewGuid(); File.WriteAllText(file, generated.ToString()); return generated;
    }
    private static string ToWebSocket(string url) => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "wss://" + url[8..] : url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "ws://" + url[7..] : url;
    private sealed record Registration(Guid ServiceTypeId, Guid InstanceId, string Name, string McpHttpUrl, string McpWebSocketUrl, DateTimeOffset LastSeenUtc);
}
