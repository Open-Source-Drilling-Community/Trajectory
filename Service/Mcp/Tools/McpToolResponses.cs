using System.Text.Json.Nodes;
namespace OSDC.Drilling.Trajectory.Service.Mcp.Tools;
internal static class McpToolResponses
{
    public static JsonNode Validation(string message) => new JsonObject { ["status"] = 400, ["error"] = message };
}
