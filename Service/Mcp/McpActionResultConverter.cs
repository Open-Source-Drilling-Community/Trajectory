using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace OSDC.Drilling.Trajectory.Service.Mcp;

internal static class McpActionResultConverter
{
    public static JsonObject FromUnknown(object? result)
    {
        if (result is IConvertToActionResult convertible) return Build(convertible.Convert(), null);
        if (result is IActionResult actionResult) return Build(actionResult, null);
        return Build(null, result);
    }

    private static JsonObject Build(IActionResult? result, object? value)
    {
        var (status, payload) = result switch
        {
            ObjectResult objectResult => (objectResult.StatusCode ?? StatusCodes.Status200OK, objectResult.Value ?? value),
            StatusCodeResult statusResult => (statusResult.StatusCode, value),
            null => (value is null ? StatusCodes.Status204NoContent : StatusCodes.Status200OK, value),
            EmptyResult => (StatusCodes.Status204NoContent, value),
            _ => (StatusCodes.Status200OK, value)
        };
        var response = new JsonObject { ["status"] = status };
        if (payload is not null)
            response["data"] = payload is JsonNode node ? node.DeepClone() : JsonSerializer.SerializeToNode(payload, payload.GetType(), JsonSettings.Options);
        return response;
    }
}
