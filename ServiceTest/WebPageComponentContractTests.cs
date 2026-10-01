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
        int tableEnd = source.IndexOf(">", table, StringComparison.Ordinal);
        string tableDeclaration = source[table..tableEnd];

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
            Assert.That(tableDeclaration, Does.Contain("Height=\"400px\""));
            Assert.That(tableDeclaration, Does.Contain("FixedHeader=\"true\""));
            Assert.That(tableDeclaration, Does.Contain("HorizontalScrollbar=\"true\""));
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
        string loadData = GetMethodSource(pageSource, "private async Task LoadDataAsync()", "private RenderFragment CalculationStatusContent");

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
            Assert.That(loadData.IndexOf("await surveyRunTask", StringComparison.Ordinal),
                Is.LessThan(loadData.IndexOf("await referenceDataTask", StringComparison.Ordinal)),
                "Survey Run rows must render before slower reference catalogs finish loading.");
            Assert.That(loadData, Does.Contain("ReferenceData.GetSnapshotAsync()"));
            Assert.That(loadData, Does.Contain("await InvokeAsync(StateHasChanged)"));
            Assert.That(pageSource, Does.Contain("<MudProgressLinear Indeterminate=\"true\" Color=\"Color.Info\""));
            Assert.That(pageSource, Does.Contain("Disabled=\"@(!referenceDataAvailable_)\">Add</MudButton>"));
            Assert.That(pageSource, Does.Contain("isReferenceDataLoading_ ? \"Loading…\" : \"Unknown wellbore\""));
            Assert.That(pageSource, Does.Contain("isReferenceDataLoading_ ? \"Loading…\" : \"Unknown survey instrument\""));
            Assert.That(loadData, Does.Contain("await EditSurveyRunAsync(pendingSurveyRunId)"));
            Assert.That(Regex.Matches(loadData, "gridKey_\\+\\+").Count, Is.EqualTo(1),
                "Completing reference loading must not remount the grid and discard its selection.");
            Assert.That(lightQuery, Does.Contain("SELECT SurveyRun FROM SurveyRunTable"));
            Assert.That(lightQuery, Does.Not.Contain("GetAllSurveyRun("),
                "The light endpoint must not load calculated survey-station chunks through the full endpoint.");
            Assert.That(lightQuery, Does.Not.Contain("GetSurveyStationListBySurveyRunId"));
        });
    }

    [Test]
    public void Trajectory_list_loads_reference_catalogs_without_losing_an_early_selection()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "TrajectoryMain.razor"));
        string source = File.ReadAllText(path);
        string loadData = GetMethodSource(source, "private async Task LoadDataAsync()", "private List<Field> AvailableFields");

        Assert.Multiple(() =>
        {
            Assert.That(loadData.IndexOf("await trajectoryTask", StringComparison.Ordinal),
                Is.LessThan(loadData.IndexOf("await referenceDataTask", StringComparison.Ordinal)),
                "Trajectory rows must render before reference catalogs finish loading.");
            Assert.That(loadData, Does.Contain("ReferenceData.GetSnapshotAsync()"));
            Assert.That(loadData, Does.Contain("await EditTrajectory(queuedTrajectoryId)"));
            Assert.That(Regex.Matches(loadData, "_gridKey\\+\\+").Count, Is.EqualTo(1),
                "Completing reference loading must not remount the grid and discard its selection.");
            Assert.That(source, Does.Contain("Loading fields, clusters, wells, wellbores, rigs, and wellbore architectures"));
            Assert.That(source, Does.Contain("Disabled=\"@(!referenceDataAvailable)\">Add</MudButton>"));
            Assert.That(source, Does.Contain("isReferenceDataLoading ? \"Loading…\" : \"Unknown wellbore\""));
        });
    }

    [Test]
    public void Shared_reference_data_is_cached_by_the_webapp_and_reused_by_trajectory_pages()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string program = File.ReadAllText(Path.Combine(repositoryRoot, "WebApp", "Program.cs"));
        string cache = File.ReadAllText(Path.Combine(repositoryRoot, "WebApp", "TrajectoryReferenceDataCache.cs"));
        string[] pagesUsingReferenceData =
        [
            "AntiCollisionPolicies.razor",
            "AntiCollisionScan.razor",
            "SurveyRunBatchImport.razor",
            "SurveyRunMain.razor",
            "SurveyRunMinimumDistanceCalculationMain.razor",
            "TrajectoryAggregationMain.razor",
            "TrajectoryInterpolatedMain.razor",
            "TrajectoryMain.razor",
            "TrajectoryMinimumDistanceCalculationMain.razor",
            "TrajectoryRealizationMain.razor"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddSingleton<ITrajectoryReferenceDataCache>"));
            Assert.That(program, Does.Contain("AddHostedService"));
            Assert.That(cache, Does.Contain("TimeSpan.FromMinutes(1)"));
            Assert.That(cache, Does.Contain("retaining the last successful snapshot"));

            foreach (string pageName in pagesUsingReferenceData)
            {
                string source = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", pageName));
                Assert.That(source, Does.Contain("@inject ITrajectoryReferenceDataCache ReferenceData"), pageName);
                Assert.That(source, Does.Contain("ReferenceData.GetSnapshotAsync()"), pageName);
            }
        });
    }

    [Test]
    public void Trajectory_and_survey_run_hierarchy_filters_support_partial_name_search()
    {
        string surveyRunPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "SurveyRunMain.razor"));
        string surveyRunSource = File.ReadAllText(surveyRunPath);
        string trajectoryPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "TrajectoryMain.razor"));
        string trajectorySource = File.ReadAllText(trajectoryPath);

        Assert.Multiple(() =>
        {
            Assert.That(surveyRunSource, Does.Contain("SearchFunc=\"SearchListFieldsAsync\""));
            Assert.That(surveyRunSource, Does.Contain("SearchFunc=\"SearchListClustersAsync\""));
            Assert.That(surveyRunSource, Does.Contain("SearchFunc=\"SearchListWellsAsync\""));
            Assert.That(surveyRunSource, Does.Contain("SearchFunc=\"SearchListWellBoresAsync\""));
            Assert.That(trajectorySource, Does.Contain("SearchFunc=\"SearchFieldsAsync\""));
            Assert.That(trajectorySource, Does.Contain("SearchFunc=\"SearchClustersAsync\""));
            Assert.That(trajectorySource, Does.Contain("SearchFunc=\"SearchWellsAsync\""));
            Assert.That(trajectorySource, Does.Contain("SearchFunc=\"SearchWellBoresAsync\""));
            Assert.That(surveyRunSource, Does.Contain("Contains(term, StringComparison.OrdinalIgnoreCase)"));
            Assert.That(trajectorySource, Does.Contain("Contains(term, StringComparison.OrdinalIgnoreCase)"));
            Assert.That(surveyRunSource, Does.Contain("MinCharacters=\"0\""));
            Assert.That(trajectorySource, Does.Contain("MinCharacters=\"0\""));
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

    [Test]
    public void New_trajectory_extrapolation_does_not_request_results_before_it_is_persisted()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string source = File.ReadAllText(path);
        string loadSurveyStations = GetMethodSource(source,
            "private async Task LoadSurveyStationsAsync()", "private void AddSection()");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("CalculationState = CalculationState.NotCalculated"));
            Assert.That(loadSurveyStations, Does.Contain("CaseId is not Guid persistedCaseId"));
            Assert.That(loadSurveyStations, Does.Contain("GetTrajectoryExtrapolationSurveyStationChunkCountAsync(persistedCaseId)"));
            Assert.That(loadSurveyStations, Does.Not.Contain("GetTrajectoryExtrapolationSurveyStationChunkCountAsync(value.MetaInfo.ID)"));
        });
    }

    [Test]
    public void Trajectory_extrapolation_source_uses_the_searchable_resource_hierarchy()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string source = File.ReadAllText(path);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain(">Trajectory to extrapolate<"));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchSourceFieldsAsync\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchSourceClustersAsync\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchSourceWellsAsync\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchSourceWellBoresAsync\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchSourceTrajectoriesAsync\""));
            Assert.That(source, Does.Contain("Contains(term, StringComparison.OrdinalIgnoreCase)"));
            Assert.That(source, Does.Contain("value.SourceTrajectoryID == Guid.Empty ? null"));
            Assert.That(source, Does.Not.Contain("@bind-Value=\"value.SourceTrajectoryID\""));
        });
    }

    [Test]
    public void Trajectory_extrapolation_uses_shared_units_references_and_labeled_result_columns()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string editor = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string editorStyles = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryExtrapolationEdit.razor.css"));
        string main = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryExtrapolationMain.razor"));
        int sourceSelector = editor.IndexOf(">Trajectory to extrapolate<", StringComparison.Ordinal);
        int mode = editor.IndexOf("Label=\"Extrapolation mode\"", StringComparison.Ordinal);
        int interval = editor.IndexOf("QuantityLabel=\"Interpolation interval\"", StringComparison.Ordinal);
        int startingStation = editor.IndexOf(">Starting survey station<", StringComparison.Ordinal);
        int specification = editor.IndexOf("value.Specification is FixedLengthExtrapolationSpecification", StringComparison.Ordinal);
        int wellPathInput = editor.IndexOf(">Well-path constraints<", StringComparison.Ordinal);
        int wellPathResult = editor.IndexOf(">Calculated well path<", StringComparison.Ordinal);
        int calculationPanel = editor.IndexOf("Calculation: @value.CalculationState", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(mode, Is.GreaterThan(sourceSelector));
            Assert.That(interval, Is.GreaterThan(mode));
            Assert.That(startingStation, Is.GreaterThan(interval));
            Assert.That(startingStation, Is.LessThan(specification));
            Assert.That(wellPathResult, Is.GreaterThan(wellPathInput));
            Assert.That(wellPathResult, Is.LessThan(calculationPanel));
            Assert.That(main, Does.Contain("GroundMudLineDepthReferenceSource=\"@DataUtils.GroundMudLineDepthReferenceSource\""));
            Assert.That(main, Does.Contain("WellHeadPositionReferenceSource=\"@DataUtils.WellHeadPositionReferenceSource\""));
            Assert.That(main, Does.Contain("GridConvergenceSource=\"@DataUtils.GridConvergenceSource\""));
            Assert.That(main, Does.Contain("MagneticDeclinationSource=\"@DataUtils.MagneticDeclinationSource\""));
            Assert.That(editor, Does.Not.Contain("<MudInputWithUnit "),
                "The extrapolation editor must not mix title-unit and value-adornment input components.");
            Assert.That(Regex.Matches(editor, "<MudInputWithUnitAdornment(?=\\s|>)").Count, Is.GreaterThan(20));
            Assert.That(editor, Does.Contain("<MudInputWithUnitAdornment QuantityLabel=\"Target TVD\" QuantityName=\"DepthDrilling\" DrillingSignalReference=\"DrillingSignalReferenceType.Depth\""));
            Assert.That(editor, Does.Contain("<MudInputWithUnitAdornment QuantityLabel=\"End azimuth\" QuantityName=\"PlaneAngleDrilling\" DrillingSignalReference=\"DrillingSignalReferenceType.Azimuth\""));
            Assert.That(editor, Does.Contain("<MudInputWithUnitAdornment QuantityLabel=\"Departure bearing\" QuantityName=\"PlaneAngleDrilling\" DrillingSignalReference=\"DrillingSignalReferenceType.Azimuth\""));
            Assert.That(editor, Does.Not.Contain("QuantityLabel=\"Target WGS84 vertical depth\""));
            Assert.That(editor, Does.Not.Contain("QuantityLabel=\"End true-north azimuth\""));
            Assert.That(editor, Does.Not.Contain("Label=\"Azimuth branch\""));
            Assert.That(editor, Does.Contain("geosteering.AzimuthBranch = 0;"));
            Assert.That(editor, Does.Contain("Label=\"Upstream steering-section length ratio\" Min=\"0.01\" Max=\"99.99\""));
            Assert.That(editor, Does.Contain("QuantityLabel=\"Steering length\" QuantityName=\"DepthDrilling\""));
            Assert.That(editor, Does.Contain("@drilled.SteeringLength"));
            Assert.That(editor, Does.Not.Contain("Overall drilled length"));
            Assert.That(editor, Does.Contain("AdornmentText=\"%\""));
            Assert.That(editor, Does.Contain("boundedPercentage / (100.0 - boundedPercentage)"));
            Assert.That(editor, Does.Contain("@DepthHeader(\"End MD\")"));
            Assert.That(editor, Does.Contain("@PositionHeader(NorthCoordinateLabel)"));
            Assert.That(editor, Does.Contain("@AzimuthHeader(\"Az\")"));
            Assert.That(editor, Does.Contain("QuantityName=\"CurvatureDrilling\" SIValue=\"@context.CircularArcCurvature\""));
            Assert.That(editor, Does.Contain("QuantityName=\"PlaneAngleDrilling\" SIValue=\"@context.ConstantToolface\""));
            Assert.That(editor, Does.Contain(">Interpolated extrapolated trajectory<"));
            Assert.That(editor, Does.Contain("Items=\"@interpolatedExtrapolationStations\""));
            Assert.That(editor, Does.Contain("Height=\"400px\" FixedHeader=\"true\""));
            Assert.That(editor, Does.Contain("@CurvatureHeader(\"DLS\")"));
            Assert.That(editor, Does.Contain("SIValue=\"@context.Curvature\""));
            Assert.That(editor, Does.Contain("SIValue=\"@context.BUR\""));
            Assert.That(editor, Does.Contain("SIValue=\"@context.TUR\""));
            Assert.That(editor, Does.Contain("SIValue=\"@context.VerticalSection\""));
            Assert.That(editor, Does.Contain("interpolatedExtrapolationStations.AddRange(chunk.SurveyStationList)"));
            Assert.That(editor, Does.Contain("Items=\"@(new[] { startingStation })\""));
            Assert.That(editor, Does.Contain("GetTrajectorySurveyStationChunkCountAsync(sourceTrajectoryId)"));
            Assert.That(editor, Does.Contain("for (int chunkIndex = chunkCount - 1; chunkIndex >= 0; chunkIndex--)"));
            Assert.That(editor, Does.Contain("sourceLastSurveyStation = lastStation"));
            Assert.That(editor, Does.Contain("Items=\"@WellPathSolvedSections\""));
            Assert.That(editor, Does.Contain("section.Role == TrajectoryExtrapolationSectionRole.WellPathSection"));
            Assert.That(editor, Does.Contain("SIValue=\"@context.End?.Inclination\""));
            Assert.That(editor, Does.Contain("SIValue=\"@context.End?.RiemannianNorth\""));
            Assert.That(editor, Does.Contain("class=\"@WellPathInputClass(context.SectionID, WellPathResultValue.Length)\""));
            Assert.That(editor, Does.Contain("class=\"@WellPathInputClass(context.SectionID, WellPathResultValue.EndInclination)\""));
            Assert.That(editor, Does.Contain("class=\"@WellPathInputClass(context.SectionID, WellPathResultValue.CircularArcCurvature)\""));
            Assert.That(editor, Does.Contain("section.SectionID == sectionId"));
            Assert.That(editor, Does.Contain("well-path-result-value well-path-input-value"));
            Assert.That(editorStyles, Does.Contain(".well-path-input-value ::deep *"));
            Assert.That(editorStyles, Does.Contain("font-weight: 700 !important;"));
            Assert.That(editor, Does.Contain("value.Mode != TrajectoryExtrapolationMode.WellPath"));
        });
    }

    [Test]
    public void Trajectory_extrapolation_can_be_exported_or_saved_as_a_planned_survey_run()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string editor = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string surveyRunEditor = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunMain.razor"));
        string trajectoryEditor = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryEdit.razor"));
        string exportUtility = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyStationAsciiExport.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(editor, Does.Contain("OnClick=\"SaveAsSurveyRunAsync\">Save as survey run"));
            Assert.That(editor, Does.Contain("OnClick=\"ExportInterpolatedExtrapolationAsync\">Export ASCII"));
            Assert.That(editor, Does.Contain("SurveyRunType = SurveyRunType.Planned"));
            Assert.That(editor, Does.Contain("WellBoreID = source.WellBoreID"));
            Assert.That(editor, Does.Contain("SurveyInstrumentID = parentSurveyRun.SurveyInstrumentID"));
            Assert.That(editor, Does.Contain("source.SurveyRunSectionList?.AsEnumerable().Reverse()"));
            Assert.That(editor, Does.Contain("ParentSurveyRunID = parentSurveyRunId"));
            Assert.That(editor, Does.Contain("SurveyMeasurementList = measurements"));
            Assert.That(editor, Does.Contain("await Api.ClientTrajectory.PostSurveyRunAsync(surveyRun)"));
            Assert.That(editor, Does.Not.Contain("CommitSurveyRunSurveyMeasurementChunksAsync(surveyRunId)"),
                "Creating the planned run should use the service's atomic inline-measurement transaction.");
            Assert.That(editor, Does.Contain("PromptAsync(\"Export interpolated extrapolation\", \"File name\""));
            Assert.That(editor, Does.Contain("SurveyStationAsciiExport.EnsureTsvFileName"));
            Assert.That(editor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(surveyRunEditor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(trajectoryEditor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(exportUtility, Does.Contain("Measured Depth"));
            Assert.That(exportUtility, Does.Contain("Vertical Section"));
            Assert.That(exportUtility, Does.Contain("DLS"));
            Assert.That(exportUtility, Does.Contain("BUR"));
            Assert.That(exportUtility, Does.Contain("TR"));
        });
    }

    [Test]
    public void Trajectory_host_loads_the_generated_scoped_style_bundle()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebApp", "Pages", "_Layout.cshtml"));
        string source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("<link href=\"WebApp.styles.css\" rel=\"stylesheet\" />"));
    }

    [Test]
    public void Trajectory_reference_datum_lookup_resolves_a_ranked_applicable_grid_transformation()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "TrajectoryReferenceDatumUtils.cs"));
        string source = File.ReadAllText(path);
        string method = GetMethodSource(source, "private static async Task<double?> ResolveGridConvergenceAsync", "private sealed record ReferenceLocation");

        Assert.Multiple(() =>
        {
            Assert.That(method, Does.Contain("SelectionPolicy = ModelShared.FieldTransformationSelectionPolicy.FirstAvailable"));
            Assert.That(method, Does.Contain("ApplicabilityPolicy = ModelShared.FieldApplicabilityPolicy.RequireApplicable"));
            Assert.That(method, Does.Contain("DepthPolicy = ModelShared.FieldDepthTransformationPolicy.AllowUntransformedDepthFor2D"));
        });
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
