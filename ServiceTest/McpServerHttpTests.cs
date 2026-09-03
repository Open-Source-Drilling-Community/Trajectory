using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ServiceTest;

[TestFixture]
public sealed class McpServerHttpTests
{
    private HttpClientTransport _transport = null!;
    private McpClient _client = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:8080/trajectory/api/mcp"),
            TransportMode = HttpTransportMode.AutoDetect
        }, NullLoggerFactory.Instance);
        _client = await McpClient.CreateAsync(_transport, new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "TrajectoryServiceTest", Version = "1.0.0" }
        }, NullLoggerFactory.Instance, CancellationToken.None);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (_client is not null) await _client.DisposeAsync();
        if (_transport is not null) await _transport.DisposeAsync();
    }

    [Test]
    public async Task Http_endpoint_publishes_all_104_non_statistics_tools_and_ping()
    {
        string[] remote = (await _client.ListToolsAsync(cancellationToken: CancellationToken.None)).Select(tool => tool.Name).ToArray();
        Assert.That(remote, Has.Length.EqualTo(105));
        Assert.That(remote, Is.Unique);
        Assert.That(remote, Has.None.Contains("usage_statistics"));
    }

    [Test]
    public async Task Ping_can_be_invoked_over_http()
    {
        var result = await _client.CallToolAsync("ping", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        Assert.That(((JsonObject)result.StructuredContent!)["message"]?.GetValue<string>(), Is.EqualTo("pong"));
    }

    [Test]
    public async Task Required_identifier_is_validated_before_controller_invocation()
    {
        var result = await _client.CallToolAsync("trajectory_get_trajectory_by_id", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        var payload = (JsonObject)result.StructuredContent!;
        Assert.That(payload["status"]?.GetValue<int>(), Is.EqualTo(400));
        Assert.That(payload["error"]?.GetValue<string>(), Does.Contain("id"));
    }

    [Test]
    public async Task Required_string_identifier_is_validated_before_controller_invocation()
    {
        var result = await _client.CallToolAsync("global_anti_collisions_get_by_id", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        var payload = (JsonObject)result.StructuredContent!;
        Assert.That(payload["status"]?.GetValue<int>(), Is.EqualTo(400));
        Assert.That(payload["error"]?.GetValue<string>(), Does.Contain("id"));
    }

    [Test]
    public async Task Required_body_is_validated_before_controller_invocation()
    {
        var result = await _client.CallToolAsync("trajectory_post_trajectory", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        var payload = (JsonObject)result.StructuredContent!;
        Assert.That(payload["status"]?.GetValue<int>(), Is.EqualTo(400));
        Assert.That(payload["error"]?.GetValue<string>(), Does.Contain("data"));
    }

    [Test]
    public async Task Controller_backed_read_tool_can_be_invoked_over_http()
    {
        var result = await _client.CallToolAsync("trajectory_get_all_trajectory_id", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        var payload = (JsonObject)result.StructuredContent!;
        Assert.That(payload["status"]?.GetValue<int>(), Is.EqualTo(200));
        Assert.That(payload["data"], Is.InstanceOf<JsonArray>());
    }
}
