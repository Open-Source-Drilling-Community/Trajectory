using System.Text.Json.Nodes;
namespace OSDC.Drilling.Trajectory.Service.Mcp.Tools;
public sealed class PingMcpTool : IMcpTool
{
    public string Name => "ping";
    public string Description => "Returns a pong response so clients can verify MCP connectivity.";
    public McpToolBehavior Behavior { get; } = new("Ping", true, false, true);
    public JsonNode InputSchema { get; } = JsonNode.Parse("""{"type":"object","properties":{},"additionalProperties":false}""")!;
    public JsonNode OutputSchema { get; } = JsonNode.Parse("""{"type":"object","properties":{"message":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["message","timestamp"],"additionalProperties":false}""")!;
    public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) =>
        Task.FromResult<JsonNode?>(new JsonObject { ["message"] = "pong", ["timestamp"] = DateTimeOffset.UtcNow.ToString("O") });
}
