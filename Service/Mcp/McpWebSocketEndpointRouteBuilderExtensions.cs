using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
namespace NORCE.Drilling.Trajectory.Service.Mcp;
public static class McpWebSocketEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapMcpWebSocket(this IEndpointRouteBuilder endpoints, string pattern = "/mcp/ws") => endpoints.MapGet(pattern, HandleAsync).WithName("McpWebSocket");
    private static async Task HandleAsync(HttpContext context)
    {
        ILoggerFactory loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("MCP.WebSocket");
        if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; await context.Response.WriteAsync("Expected a WebSocket request."); return; }
        var httpOptions = context.RequestServices.GetService<IOptions<HttpServerTransportOptions>>();
        if (httpOptions?.Value.Stateless == true) { context.Response.StatusCode = 400; return; }
        McpServerOptions options = context.RequestServices.GetRequiredService<IOptionsFactory<McpServerOptions>>().Create(Options.DefaultName);
        CancellationToken ct = context.RequestAborted;
        if (httpOptions?.Value.ConfigureSessionOptions is { } configure) await configure(context, options, ct);
        McpHandshake handshake = McpHandshakeReader.FromHttpRequest(context.Request);
        if (!string.IsNullOrWhiteSpace(handshake.ClientName) && !string.IsNullOrWhiteSpace(handshake.ClientVersion))
            options.KnownClientInfo = new Implementation { Name = handshake.ClientName!, Version = handshake.ClientVersion! };
        WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
        await using var transport = new WebSocketServerTransport(socket, "websocket", loggerFactory, handshake.SessionId);
        try
        {
            await using McpServer server = McpServer.Create(transport, options, loggerFactory, context.RequestServices);
            context.Features.Set(server);
            if (httpOptions?.Value.RunSessionHandler is { } handler) await handler(context, server, ct).ConfigureAwait(false);
            else await server.RunAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Unhandled MCP WebSocket error."); }
        finally { context.Features.Set<McpServer?>(null); }
    }
}
