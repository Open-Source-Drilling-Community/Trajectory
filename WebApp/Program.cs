using MudBlazor;
using MudBlazor.Services;
using NORCE.Drilling.Trajectory.WebApp;
using NORCE.Drilling.Trajectory.WebPages;

var builder = WebApplication.CreateBuilder(args);

WebPagesHostConfiguration webPagesConfiguration = new()
{
    TrajectoryHostURL = builder.Configuration["TrajectoryHostURL"] ?? string.Empty,
    GeodeticDatumHostURL = builder.Configuration["GeodeticDatumHostURL"] ?? string.Empty,
    CartographicProjectionHostURL = builder.Configuration["CartographicProjectionHostURL"] ?? string.Empty,
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    WellBoreHostURL = builder.Configuration["WellBoreHostURL"] ?? string.Empty,
    WellBoreArchitectureHostURL = builder.Configuration["WellBoreArchitectureHostURL"] ?? string.Empty,
    WellHostURL = builder.Configuration["WellHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty,
    SurveyInstrumentHostURL = builder.Configuration["SurveyInstrumentHostURL"] ?? string.Empty,
    EarthMagneticFieldHostURL = builder.Configuration["EarthMagneticFieldHostURL"] ?? string.Empty
};

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<ITrajectoryWebPagesConfiguration>(webPagesConfiguration);
builder.Services.AddSingleton<ITrajectoryAPIUtils, TrajectoryAPIUtils>();
builder.Services.AddExternalWebPages(webPagesConfiguration);
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

var app = builder.Build();

app.UseForwardedHeaders();
var basePath = "/trajectory/webapp";
app.UsePathBase(basePath);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

