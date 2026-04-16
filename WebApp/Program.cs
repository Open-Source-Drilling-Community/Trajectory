using MudBlazor;
using MudBlazor.Services;
using NORCE.Drilling.Trajectory.WebPages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
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
app.UsePathBase("/Trajectory/webapp");

if (!string.IsNullOrEmpty(builder.Configuration["TrajectoryHostURL"]))
    WebAppConfiguration.TrajectoryHostURL = builder.Configuration["TrajectoryHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["FieldHostURL"]))
    WebAppConfiguration.FieldHostURL = builder.Configuration["FieldHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["ClusterHostURL"]))
    WebAppConfiguration.ClusterHostURL = builder.Configuration["ClusterHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["RigHostURL"]))
    WebAppConfiguration.RigHostURL = builder.Configuration["RigHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["WellBoreHostURL"]))
    WebAppConfiguration.WellBoreHostURL = builder.Configuration["WellBoreHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["WellBoreArchitectureHostURL"]))
    WebAppConfiguration.WellBoreArchitectureHostURL = builder.Configuration["WellBoreArchitectureHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["WellHostURL"]))
    WebAppConfiguration.WellHostURL = builder.Configuration["WellHostURL"];
if (!string.IsNullOrEmpty(builder.Configuration["UnitConversionHostURL"]))
    WebAppConfiguration.UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"];

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
