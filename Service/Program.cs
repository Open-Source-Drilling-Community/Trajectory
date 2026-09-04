using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using System;
using ModelContextProtocol.Protocol;
using OSDC.Drilling.Trajectory.Service.Mcp;
using OSDC.Drilling.Trajectory.Service.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

// registering the managers of SQLite connections through dependency injection
builder.Services.AddSingleton<SqlConnectionManagerTrajectory>();
builder.Services.AddSingleton<SqlConnectionManager>(sp => sp.GetRequiredService<SqlConnectionManagerTrajectory>());
builder.Services.AddSingleton<TrajectoryIdentityManager>();
builder.Services.AddSingleton<TrajectoryFeatureCategoryManager>();
builder.Services.AddSingleton<TrajectoryAssignmentValidator>();
builder.Services.AddSingleton<TrajectoryBatchService>();
builder.Services.AddHttpClient(nameof(TrajectoryExternalReferenceValidator), client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<ITrajectoryExternalReferenceValidator, TrajectoryExternalReferenceValidator>();
builder.Services.AddSingleton<SqlConnectionManagerSeparationFactorResults>();
builder.Services.AddSingleton<SqlConnectionManagerOctree>();
builder.Services.AddSingleton<OctreeManager>();
builder.Services.AddHostedService<OctreeReconciliationService>();

// serialization settings (using System.Json)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        JsonSettings.ApplyTo(options.JsonSerializerOptions);
    });

// serialize using short name rather than full names
builder.Services.AddSwaggerGen(config =>
{
    config.CustomSchemaIds(type => type.FullName);
});

builder.Services.Configure<McpHubOptions>(builder.Configuration.GetSection(McpHubOptions.SectionName));
builder.Services.AddHttpClient(nameof(McpHubRegistrationService));
builder.Services.AddHostedService<McpHubRegistrationService>();
var serverVersion = typeof(SqlConnectionManager).Assembly.GetName().Version?.ToString() ?? "1.0.0";
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation { Name = "TrajectoryService", Version = serverVersion };
    options.Capabilities = new ServerCapabilities { Tools = new ToolsCapability() };
}).WithHttpTransport();
builder.Services.AddLegacyMcpTool<PingMcpTool>();
builder.Services.AddTrajectoryRestMcpTools();

var app = builder.Build();

var basePath = "/trajectory/api";

app.UsePathBase(basePath);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    string path = context.Request.Path.Value ?? string.Empty;
    if (path.Contains("/.well-known/oauth-protected-resource", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/.well-known/oauth-authorization-server", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        const string body = "{\"error\":\"oauth_not_configured\",\"error_description\":\"This MCP server does not require OAuth. Connect directly to the MCP endpoint.\",\"authentication\":\"none\"}";
        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(body));
        return;
    }
    await next();
});

if (!String.IsNullOrEmpty(builder.Configuration["FieldHostURL"]))
    ServiceConfiguration.FieldHostURL = builder.Configuration["FieldHostURL"];
if (!String.IsNullOrEmpty(builder.Configuration["ClusterHostURL"]))
    ServiceConfiguration.ClusterHostURL = builder.Configuration["ClusterHostURL"];
if (!String.IsNullOrEmpty(builder.Configuration["WellBoreHostURL"]))
    ServiceConfiguration.WellBoreHostURL = builder.Configuration["WellBoreHostURL"];
if (!String.IsNullOrEmpty(builder.Configuration["WellBoreArchitectureHostURL"]))
    ServiceConfiguration.WellBoreArchitectureHostURL = builder.Configuration["WellBoreArchitectureHostURL"];
if (!String.IsNullOrEmpty(builder.Configuration["WellHostURL"]))
    ServiceConfiguration.WellHostURL = builder.Configuration["WellHostURL"];
if (!String.IsNullOrEmpty(builder.Configuration["SurveyInstrumentHostURL"]))
    ServiceConfiguration.SurveyInstrumentHostURL = builder.Configuration["SurveyInstrumentHostURL"];

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

string relativeSwaggerPath = "/swagger/merged/swagger.json";
string fullSwaggerPath = $"{basePath}{relativeSwaggerPath}";
string customVersion = "Merged API Version 1";
string exposedModel = "wwwroot/json-schema/TrajectoryMergedModel.json";
if (File.Exists(exposedModel))
{
    var mergedDoc = SwaggerMiddlewareExtensions.ReadOpenApiDocument(exposedModel);
    app.UseCustomSwagger(mergedDoc, relativeSwaggerPath);
    app.UseSwaggerUI(c =>
    {
        //c.SwaggerEndpoint("v1/swagger.json", "API Version 1");
        c.SwaggerEndpoint(fullSwaggerPath, customVersion);
    });
}

app.UseCors(cors => cors
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(origin => true)
                        .AllowCredentials()
           );

app.MapMcp("/mcp");
app.MapMcpWebSocket("/mcp/ws");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

