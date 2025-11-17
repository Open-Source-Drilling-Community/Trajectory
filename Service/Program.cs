using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using NORCE.Drilling.Trajectory.Service;
using NORCE.Drilling.Trajectory.Service.Managers;

var builder = WebApplication.CreateBuilder(args);

// registering the manager of SQLite connections through dependency injection
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SqlConnectionManager>>();

    var dbPath = Path.Combine(SqlConnectionManager.HOME_DIRECTORY,
                              SqlConnectionManager.DATABASE_FILENAME);

    // Ensure the directory exists before we build/use the connection string
    Directory.CreateDirectory(SqlConnectionManager.HOME_DIRECTORY);

    var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
    {
        DataSource = dbPath,
        // Optional but safe:
        Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
        Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared
    };

    return new SqlConnectionManager(csb.ToString(), logger, dbPath);
});

// registering the database cleaner service through dependency injection
builder.Services.AddHostedService(sp => new DatabaseCleanerService(
    sp.GetRequiredService<ILogger<DatabaseCleanerService>>(),
    sp.GetRequiredService<SqlConnectionManager>()));

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

var app = builder.Build();

var basePath = "/Trajectory/api";
var scheme = "http";

app.UsePathBase(basePath);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

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

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();