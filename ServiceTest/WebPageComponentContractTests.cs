using System.Text.RegularExpressions;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class WebPageComponentContractTests
{
    [Test]
    public void Survey_measurement_settings_and_actions_follow_the_intended_workflow()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "SurveyRunMain.razor"));
        string source = File.ReadAllText(path);
        int measurements = source.IndexOf(">Survey measurements<", StringComparison.Ordinal);
        int settings = source.IndexOf(">Survey measurement settings<", StringComparison.Ordinal);
        int calculationMethod = source.IndexOf("Label=\"Calculation method\"", StringComparison.Ordinal);
        int inclinationReference = source.IndexOf("Label=\"Observed inclination reference\"", StringComparison.Ordinal);
        int azimuthReference = source.IndexOf("Label=\"Observed azimuth reference\"", StringComparison.Ordinal);
        int geomagneticModel = source.IndexOf("Label=\"Geomagnetic model\"", StringComparison.Ordinal);
        int bitExtrapolation = source.IndexOf("Label=\"Add terminal bit extrapolation\"", StringComparison.Ordinal);
        int bulkEditing = source.IndexOf(">Apply to all survey measurements<", StringComparison.Ordinal);
        int calculate = source.IndexOf(">Calculate</MudButton>", StringComparison.Ordinal);
        int export = source.IndexOf("OnClick=\"ExportSurveyStationsAsync\"", StringComparison.Ordinal);
        int table = source.IndexOf("<MudTable Items=\"@SurveyMeasurements\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(measurements, Is.GreaterThanOrEqualTo(0));
            Assert.That(settings, Is.GreaterThan(measurements));
            Assert.That(calculationMethod, Is.GreaterThan(settings).And.LessThan(bitExtrapolation));
            Assert.That(inclinationReference, Is.GreaterThan(calculationMethod).And.LessThan(bitExtrapolation));
            Assert.That(azimuthReference, Is.GreaterThan(inclinationReference).And.LessThan(bitExtrapolation));
            Assert.That(geomagneticModel, Is.GreaterThan(azimuthReference).And.LessThan(bitExtrapolation));
            Assert.That(bitExtrapolation, Is.LessThan(bulkEditing));
            Assert.That(calculate, Is.GreaterThan(bulkEditing).And.LessThan(table));
            Assert.That(export, Is.GreaterThan(calculate).And.LessThan(table));
        });
    }

    [Test]
    public void Survey_run_editor_guards_navigation_with_a_compact_unsaved_changes_dialog()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "SurveyRunMain.razor"));
        string source = File.ReadAllText(path);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("<NavigationLock"));
            Assert.That(source, Does.Contain("OnBeforeInternalNavigation=\"OnBeforeInternalNavigationAsync\""));
            Assert.That(source, Does.Contain("MaxWidth = MaxWidth.ExtraSmall"));
            Assert.That(source, Does.Contain("FullWidth = false"));
            Assert.That(source, Does.Contain("BitExtrapolation = currentSurveyRun_?.BitExtrapolation"));
        });
    }

    [Test]
    public void Survey_run_list_initialization_uses_only_the_lightweight_contract()
    {
        string pagePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "SurveyRunMain.razor"));
        string pageSource = File.ReadAllText(pagePath);
        string loadData = GetMethodSource(pageSource, "private async Task LoadDataAsync()", "private List<SurveyRunLight> ApplySurveyRunFilters()");

        string managerPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "Service", "Managers", "SurveyRunManager.cs"));
        string managerSource = File.ReadAllText(managerPath);
        string lightQuery = GetMethodSource(managerSource,
            "public List<SurveyRunLight>? GetAllSurveyRunLight", "public Task<bool> AddSurveyRun");

        Assert.Multiple(() =>
        {
            Assert.That(loadData, Does.Contain("GetAllSurveyRunLightAsync()"));
            Assert.That(loadData, Does.Not.Contain("GetAllSurveyRunAsync("),
                "The list page must defer calculated-station data until an editor needs parent candidates.");
            Assert.That(lightQuery, Does.Contain("SELECT SurveyRun FROM SurveyRunTable"));
            Assert.That(lightQuery, Does.Not.Contain("GetAllSurveyRun("),
                "The light endpoint must not load calculated survey-station chunks through the full endpoint.");
            Assert.That(lightQuery, Does.Not.Contain("GetSurveyStationListBySurveyRunId"));
        });
    }

    [Test]
    public void MudInputWithUnit_does_not_receive_parameters_unsupported_by_the_component()
    {
        string webPagesDirectory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages"));
        Regex component = new("<MudInputWithUnit(?=\\s|>)(.*?)/>", RegexOptions.Singleline);
        Regex unsupportedParameter = new("(?<!Quantity)\\b(?:Label|Disabled|ReadOnly|Immediate)\\s*=");
        List<string> violations = [];

        foreach (string path in Directory.EnumerateFiles(webPagesDirectory, "*.razor", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            foreach (Match match in component.Matches(source))
            {
                if (!unsupportedParameter.IsMatch(match.Value)) continue;
                int line = source.AsSpan(0, match.Index).Count('\n') + 1;
                violations.Add($"{Path.GetFileName(path)}:{line}");
            }
        }

        Assert.That(violations, Is.Empty,
            "MudInputWithUnit supports QuantityLabel but not Label, Disabled, ReadOnly, or Immediate. " +
            "Use MudInputWithUnitAdornment or render a read-only value when those behaviors are required.");
    }

    private static string GetMethodSource(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        int end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Could not find {startMarker}");
        Assert.That(end, Is.GreaterThan(start), $"Could not find {endMarker} after {startMarker}");
        return source[start..end];
    }
}
