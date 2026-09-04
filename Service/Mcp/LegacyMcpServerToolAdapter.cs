using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace OSDC.Drilling.Trajectory.Service.Mcp;

internal sealed class LegacyMcpServerToolAdapter : McpServerTool
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMcpTool _tool;
    private readonly ILogger _logger;
    private readonly Tool _protocolTool;

    public LegacyMcpServerToolAdapter(IMcpTool tool, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _tool = tool;
        _logger = loggerFactory.CreateLogger(tool.GetType());
        _protocolTool = new Tool
        {
            Name = tool.Name,
            Title = tool.Behavior.Title,
            Description = tool.Description,
            InputSchema = JsonSerializer.SerializeToElement(tool.InputSchema, SerializerOptions),
            OutputSchema = JsonSerializer.SerializeToElement(tool.OutputSchema, SerializerOptions),
            Annotations = new()
            {
                Title = tool.Behavior.Title,
                ReadOnlyHint = tool.Behavior.ReadOnlyHint,
                DestructiveHint = tool.Behavior.DestructiveHint,
                IdempotentHint = tool.Behavior.IdempotentHint,
                OpenWorldHint = tool.Behavior.OpenWorldHint
            }
        };
    }

    public override Tool ProtocolTool => _protocolTool;
    public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

    public override async ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            JsonNode? result = await _tool.InvokeAsync(ConvertArguments(request.Params?.Arguments), cancellationToken)
                .ConfigureAwait(false);
            if (TryGetFailure(result, out JsonNode failure))
                return Error(failure);

            string? fallback = result?.ToJsonString(SerializerOptions);
            return new CallToolResult
            {
                StructuredContent = result?.DeepClone(),
                Content = fallback is null ? [] : [new TextContentBlock { Text = fallback }]
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP tool {ToolName} failed while handling request.", _tool.Name);
            return Error(new JsonObject
            {
                ["error"] = "internal_error",
                ["message"] = "An unexpected server error occurred while executing the tool.",
                ["errors"] = new JsonArray()
            });
        }
    }

    private static bool TryGetFailure(JsonNode? result, out JsonNode failure)
    {
        failure = null!;
        if (result is not JsonObject response || response["status"]?.GetValue<int>() is not int status || status < 400)
            return false;

        JsonNode? responseError = Property(response, "error");
        JsonObject? payload = response["data"] as JsonObject;
        JsonNode? payloadError = Property(payload, "error");
        string? payloadCode = payloadError is JsonValue value && value.TryGetValue(out string? text) &&
                              !string.IsNullOrWhiteSpace(text) &&
                              text.All(character => char.IsLower(character) || char.IsDigit(character) || character == '_')
            ? text
            : null;
        failure = new JsonObject
        {
            ["error"] = payloadCode ?? ErrorCodeForStatus(status),
            ["message"] = Property(response, "message")?.DeepClone() ?? Property(payload, "message")?.DeepClone() ??
                          responseError?.DeepClone() ?? payloadError?.DeepClone() ??
                          JsonValue.Create("The tool request failed."),
            ["errors"] = ExtractErrors(response)
        };
        return true;
    }

    private static JsonArray ExtractErrors(JsonObject response)
    {
        JsonNode? errors = Property(response, "errors") ?? Property(response["data"] as JsonObject, "errors");
        return errors is JsonArray array ? array.DeepClone().AsArray() : new JsonArray();
    }

    private static JsonNode? Property(JsonObject? value, string name) => value?
        .FirstOrDefault(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static string ErrorCodeForStatus(int status) => status switch
    {
        400 => "validation_failed",
        404 => "not_found",
        409 => "conflict",
        _ => "request_failed"
    };

    private static CallToolResult Error(JsonNode problem) => new()
    {
        IsError = true,
        Content = { new TextContentBlock { Text = problem.ToJsonString(SerializerOptions) } }
    };

    private JsonObject? ConvertArguments(IReadOnlyDictionary<string, JsonElement>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
            return null;

        var result = new JsonObject();
        foreach ((string key, JsonElement element) in arguments)
        {
            try
            {
                result[key] = JsonNode.Parse(element.GetRawText());
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse argument {ArgumentKey} for MCP tool {ToolName}.", key, _tool.Name);
                result[key] = JsonValue.Create(element.GetRawText());
            }
        }
        return result;
    }
}
