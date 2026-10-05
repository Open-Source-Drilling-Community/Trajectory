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
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string path = Path.Combine(repositoryRoot, "WebPages", "SurveyRunMain.razor");
        string source = File.ReadAllText(path);
        string options = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryDialogOptions.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("<NavigationLock"));
            Assert.That(source, Does.Contain("OnBeforeInternalNavigation=\"OnBeforeInternalNavigationAsync\""));
            Assert.That(source, Does.Contain("options: TrajectoryDialogOptions.Compact"));
            Assert.That(options, Does.Contain("MaxWidth = MudBlazor.MaxWidth.ExtraSmall"));
            Assert.That(options, Does.Contain("FullWidth = false"));
            Assert.That(source, Does.Contain("BitExtrapolation = currentSurveyRun_?.BitExtrapolation"));
        });
    }

    [Test]
    public void All_trajectory_webpage_dialogs_use_the_shared_compact_options()
    {
        string webPagesDirectory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages"));
        List<string> violations = [];

        foreach (string path in Directory.EnumerateFiles(webPagesDirectory, "*.razor", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            if ((source.Contains("ShowMessageBoxAsync", StringComparison.Ordinal) ||
                 source.Contains("ShowAsync<", StringComparison.Ordinal)) &&
                !source.Contains("TrajectoryDialogOptions.Compact", StringComparison.Ordinal))
            {
                violations.Add(Path.GetFileName(path));
            }
        }

        Assert.That(violations, Is.Empty,
            "Every WebPages dialog must use the shared compact responsive options.");
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
            Assert.That(loadData, Does.Contain("referenceData.SurveyRuns")
                .And.Not.Contain("GetAllSurveyRunLightAsync"));
            Assert.That(loadData, Does.Not.Contain("GetAllSurveyRunAsync("),
                "The list page must defer calculated-station data until an editor needs parent candidates.");
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
            Assert.That(loadData, Does.Contain("ReferenceData.GetSnapshotAsync()")
                .And.Contain("referenceData.Trajectories")
                .And.Not.Contain("GetAllTrajectoryLightAsync"));
            Assert.That(loadData, Does.Contain("await EditTrajectory(queuedTrajectoryId)"));
            Assert.That(Regex.Matches(loadData, "_gridKey\\+\\+").Count, Is.EqualTo(1),
                "Completing reference loading must not remount the grid and discard its selection.");
            Assert.That(source, Does.Contain("Loading shared trajectories, survey runs, fields, clusters, wells, wellbores, rigs, survey instruments, and wellbore architectures"));
            Assert.That(source, Does.Contain("Disabled=\"@(!referenceDataAvailable)\">Add</MudButton>"));
            Assert.That(source, Does.Contain("isReferenceDataLoading ? \"Loading…\" : \"Unknown wellbore\""));
        });
    }

    [Test]
    public void Trajectory_pages_use_a_host_cache_or_the_reusable_direct_fallback()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string program = File.ReadAllText(Path.Combine(repositoryRoot, "WebApp", "Program.cs"));
        string cache = File.ReadAllText(Path.Combine(repositoryRoot, "WebApp", "TrajectoryReferenceDataCache.cs"));
        string cacheContract = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "ITrajectoryReferenceDataCache.cs"));
        string directProvider = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "DirectTrajectoryReferenceDataProvider.cs"));
        string registration = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryWebPagesServiceCollectionExtensions.cs"));
        string[] pagesUsingReferenceData =
        [
            "AntiCollisionPolicies.razor",
            "AntiCollisionScan.razor",
            "SurveyRunBatchImport.razor",
            "SurveyRunMain.razor",
            "SurveyRunMinimumDistanceCalculationMain.razor",
            "TargetLandingEdit.razor",
            "TargetLandingMain.razor",
            "TrajectoryAggregationEdit.razor",
            "TrajectoryAggregationMain.razor",
            "TrajectoryBackupRestore.razor",
            "TrajectoryEdit.razor",
            "TrajectoryExtrapolationEdit.razor",
            "TrajectoryExtrapolationMain.razor",
            "TrajectoryInterpolationEdit.razor",
            "TrajectoryInterpolatedMain.razor",
            "TrajectoryMain.razor",
            "TrajectoryMinimumDistanceCalculationMain.razor",
            "TrajectoryRealizationEdit.razor",
            "TrajectoryRealizationMain.razor"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddSingleton<ITrajectoryReferenceDataCache>"));
            Assert.That(program, Does.Contain("AddHostedService"));
            Assert.That(program, Does.Contain("AddTrajectoryWebPages()"));
            Assert.That(cache, Does.Contain("TimeSpan.FromMinutes(1)"));
            Assert.That(cache, Does.Contain("retaining the last successful snapshot"));
            Assert.That(cache, Does.Contain("LoadTrajectoriesAsync")
                .And.Contain("LoadSurveyRunsAsync")
                .And.Contain("GetAllWellBoreArchitectureLightAsync")
                .And.Contain("GetAllSurveyInstrumentLightAsync")
                .And.Contain("GetAllRigReferencesAsync"));
            Assert.That(cacheContract, Does.Contain("IReadOnlyList<TrajectoryLight> Trajectories")
                .And.Contain("IReadOnlyList<SurveyRunLight> SurveyRuns")
                .And.Contain("RefreshTrajectoriesAsync")
                .And.Contain("RefreshSurveyRunsAsync"));
            Assert.That(registration, Does.Contain("TryAddSingleton")
                .And.Contain("TryAddScoped")
                .And.Contain("DirectTrajectoryReferenceDataProvider"));
            Assert.That(directProvider, Does.Contain("await RefreshAsync(cancellationToken)")
                .And.Contain("GetAllTrajectoryLightAsync")
                .And.Contain("GetAllSurveyRunLightAsync")
                .And.Contain("GetAllWellBoreArchitectureLightAsync")
                .And.Contain("GetAllSurveyInstrumentLightAsync")
                .And.Contain("GetAllRigReferencesAsync"));
            Assert.That(program.IndexOf("AddSingleton<ITrajectoryReferenceDataCache>", StringComparison.Ordinal),
                Is.LessThan(program.IndexOf("AddTrajectoryWebPages()", StringComparison.Ordinal)),
                "The standalone host cache must be registered before the reusable fallback is considered.");

            foreach (string pageName in pagesUsingReferenceData)
            {
                string source = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", pageName));
                Assert.That(source, Does.Contain("@inject ITrajectoryReferenceDataCache ReferenceData"), pageName);
                Assert.That(source, pageName == "TargetLandingMain.razor"
                    ? Does.Contain("ReferenceData.GetTrajectoriesAsync()")
                    : Does.Contain("ReferenceData.GetSnapshotAsync()"), pageName);
            }

            string allPages = string.Join('\n', Directory.GetFiles(Path.Combine(repositoryRoot, "WebPages"), "*.razor")
                .Select(File.ReadAllText));
            Assert.That(allPages, Does.Not.Contain("GetAllTrajectoryLightAsync")
                .And.Not.Contain("GetAllSurveyRunLightAsync"),
                "Web pages must use the shared snapshots for trajectory and survey-run light lists.");
        });
    }

    [Test]
    public void Target_landing_list_does_not_wait_for_the_complete_reference_snapshot()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string source = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TargetLandingMain.razor"));
        string loadCases = GetMethodSource(source, "private async Task LoadAsync()", "private async Task LoadTrajectoryNamesAsync()");
        string loadNames = GetMethodSource(source, "private async Task LoadTrajectoryNamesAsync()", "private void Add()");
        string cacheContract = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "ITrajectoryReferenceDataCache.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(loadCases, Does.Contain("GetAllTargetLandingCaseLightAsync")
                .And.Not.Contain("GetSnapshotAsync")
                .And.Not.Contain("GetTrajectoriesAsync"));
            Assert.That(loadCases, Does.Contain("StartPollingIfNeeded()"));
            Assert.That(source, Does.Contain("_ = LoadTrajectoryNamesAsync()"),
                "Source names should enrich the already-visible case list without extending page initialization.");
            Assert.That(loadNames, Does.Contain("ReferenceData.GetTrajectoriesAsync()")
                .And.Contain("InvokeAsync(StateHasChanged)"));
            Assert.That(cacheContract, Does.Contain("GetTrajectoriesAsync")
                .And.Contain("RefreshTrajectoriesAsync")
                .And.Contain("return Current.Trajectories"));
        });
    }

    [Test]
    public void Trajectory_and_survey_run_hierarchy_filters_support_partial_name_search()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string component = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryResourceFilter.razor"));
        string surveyRunSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunMain.razor"));
        string trajectorySource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryMain.razor"));
        string trajectoryEditSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryEdit.razor"));
        string extrapolationSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string antiCollisionSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "AntiCollisionScan.razor"));
        string aggregationSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryAggregationEdit.razor"));
        string interpolationSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryInterpolationEdit.razor"));
        string realizationSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryRealizationEdit.razor"));
        string trajectoryMinimumDistanceSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryMinimumDistanceCalculationMain.razor"));
        string surveyRunMinimumDistanceSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunMinimumDistanceCalculationMain.razor"));
        string batchImportSource = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunBatchImport.razor"));

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(component, "<MudAutocomplete").Count, Is.EqualTo(5));
            Assert.That(component, Does.Contain("SearchFunc=\"SearchFieldsAsync\""));
            Assert.That(component, Does.Contain("SearchFunc=\"SearchClustersAsync\""));
            Assert.That(component, Does.Contain("SearchFunc=\"SearchWellsAsync\""));
            Assert.That(component, Does.Contain("SearchFunc=\"SearchWellBoresAsync\""));
            Assert.That(component, Does.Contain("SearchFunc=\"SearchResourcesAsync\""));
            Assert.That(component, Does.Contain("Contains(term, StringComparison.OrdinalIgnoreCase)"));
            Assert.That(component, Does.Contain("MinCharacters=\"0\""));
            Assert.That(surveyRunSource, Does.Contain("<TrajectoryResourceFilter").And.Not.Contain("SearchListFieldsAsync"));
            Assert.That(trajectorySource, Does.Contain("<TrajectoryResourceFilter").And.Not.Contain("SearchFieldsAsync"));
            Assert.That(trajectoryEditSource, Does.Contain("<TrajectoryResourceFilter").And.Not.Contain("Label=\"Rig\""));
            Assert.That(extrapolationSource, Does.Contain("<TrajectoryResourceFilter").And.Not.Contain("SearchSourceFieldsAsync"));
            Assert.That(antiCollisionSource, Does.Contain("<TrajectoryResourceFilter").And.Not.Contain("SearchReferenceTrajectoriesAsync"));
            Assert.That(aggregationSource, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(interpolationSource, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(realizationSource, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(trajectoryMinimumDistanceSource, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(surveyRunMinimumDistanceSource, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(batchImportSource, Does.Contain("<TrajectoryResourceFilter").And.Contain("ShowWellBore=\"false\""));
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
            Assert.That(source, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(source, Does.Contain("ResourceKind=\"TrajectoryResourceFilterKind.Trajectory\""));
            Assert.That(source, Does.Contain("ResourceIdChanged=\"OnSourceTrajectoryChanged\""));
            Assert.That(source, Does.Contain("<RigJobSelector"));
            Assert.That(source, Does.Contain("ReferenceDate=\"@sourceReferenceDate\""));
            Assert.That(source, Does.Contain("value.SourceTrajectoryID == Guid.Empty ? null"));
            Assert.That(source, Does.Not.Contain("@bind-Value=\"value.SourceTrajectoryID\""));
        });
    }

    [Test]
    public void Reconnect_extrapolation_uses_a_searchable_reference_hierarchy_and_one_vertical_section_curtain()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string source = File.ReadAllText(path);
        int leadIn = source.IndexOf("QuantityLabel=\"Lead-in continuation\"", StringComparison.Ordinal);
        int advance = source.IndexOf("QuantityLabel=\"Advance from closest reference point\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(source, "<TrajectoryResourceFilter").Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(source, Does.Contain("ResourceLabelOverride=\"Reference trajectory\" ResourceRequired=\"true\""));
            Assert.That(source, Does.Contain("CompletedTrajectoriesOnly=\"true\""));
            Assert.That(source, Does.Contain("ResourceId=\"@SelectedReconnectTrajectoryId\" ResourceIdChanged=\"OnReconnectReferenceTrajectoryChanged\""));
            Assert.That(source, Does.Not.Contain("<MudSelect T=\"Guid\" Value=\"@reconnect.ReferenceTrajectoryID\""));
            Assert.That(leadIn, Is.GreaterThan(0));
            Assert.That(advance, Is.GreaterThan(leadIn));
            Assert.That(source, Does.Not.Contain("Label=\"Az branch\""));
            Assert.That(source, Does.Contain("reconnect.AzimuthBranch = 0;"));
            Assert.That(source, Does.Contain("The advance is then added to the measured depth of the closest point"));
            Assert.That(source, Does.Contain("ResolveVerticalSectionCurtain()"));
            Assert.That(source, Does.Contain("ProjectOntoVerticalSectionCurtain(reconnectReferenceStations, curtain)"));
            Assert.That(source, Does.Contain("curtain.Value.Project(north.Value, east.Value)"));
        });
    }

    [Test]
    public void Rig_job_selection_is_separate_date_aware_and_controls_rotary_table_depth()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string selector = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "RigJobSelector.razor"));
        string selection = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "RigJobSelectionUtils.cs"));
        string dataUtils = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "DataUtils.cs"));
        string surveyRun = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunMain.razor"));
        string trajectory = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryMain.razor"));
        string trajectoryEdit = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryEdit.razor"));
        string extrapolation = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryExtrapolationEdit.razor"));
        string antiCollision = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "AntiCollisionScan.razor"));
        string aggregation = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryAggregationEdit.razor"));
        string interpolation = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryInterpolationEdit.razor"));
        string realization = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryRealizationEdit.razor"));
        string trajectoryMinimumDistance = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryMinimumDistanceCalculationMain.razor"));
        string surveyRunMinimumDistance = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunMinimumDistanceCalculationMain.razor"));
        string batchImport = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "SurveyRunBatchImport.razor"));

        Assert.Multiple(() =>
        {
            Assert.That(selector, Does.Contain("Label=\"Rig job\""));
            Assert.That(selector, Does.Contain("GetDisplayName(job.RigJobID).Contains(term, StringComparison.OrdinalIgnoreCase)"));
            Assert.That(selection, Does.Contain("job.StartDate <= instant && (job.EndDate == null || job.EndDate >= instant)"));
            Assert.That(selection, Does.Contain("AcquisitionEndUtc ?? surveyRun?.AcquisitionStartUtc"));
            Assert.That(dataUtils, Does.Contain("Guid? selectedRigJobId = null, DateTimeOffset? referenceDate = null"));
            Assert.That(dataUtils, Does.Contain("ResolveRigJob(wellBore, selectedRigJobId, referenceDate)"));
            Assert.That(surveyRun, Does.Contain("<RigJobSelector").And.Contain("CurrentSurveyRunReferenceDate"));
            Assert.That(trajectoryEdit, Does.Contain("<RigJobSelector").And.Contain("CurrentTrajectoryReferenceDate"));
            Assert.That(trajectory, Does.Not.Contain("Label=\"Rig\""));
            Assert.That(extrapolation, Does.Contain("<RigJobSelector").And.Contain("sourceReferenceDate"));
            Assert.That(antiCollision, Does.Contain("<RigJobSelector").And.Contain("referenceTrajectoryDate_"));
            Assert.That(aggregation, Does.Contain("<RigJobSelector").And.Contain("trajectoryReferenceDate"));
            Assert.That(interpolation, Does.Contain("<RigJobSelector").And.Contain("trajectoryReferenceDate"));
            Assert.That(realization, Does.Contain("<RigJobSelector").And.Contain("trajectoryReferenceDate"));
            Assert.That(trajectoryMinimumDistance, Does.Contain("<RigJobSelector").And.Contain("referenceTrajectoryDate_"));
            Assert.That(surveyRunMinimumDistance, Does.Contain("<RigJobSelector").And.Contain("referenceSurveyRunDate_"));
            Assert.That(batchImport, Does.Contain("<RigJobSelector").And.Contain("row.SelectedRigJobId"));
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
        string scatterPlot = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "ScatterPlot.razor"));
        string scatter3DPlot = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "Scatter3DPlot.razor"));
        string dataUtils = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "DataUtils.cs"));
        string extrapolationModel = File.ReadAllText(Path.Combine(repositoryRoot, "Model", "TrajectoryExtrapolationCase.cs"));
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
            Assert.That(editor, Does.Contain("Text=\"Plot extrapolated trajectory\""));
            Assert.That(editor, Does.Contain("<Scatter3DPlot"));
            Assert.That(editor, Does.Contain("ZAxisUsesDepthReference=\"@true\""));
            Assert.That(editor, Does.Contain("XAxisUsesPositionReference=\"@true\""));
            Assert.That(editor, Does.Contain("EllipseResultsChanged=\"OnExtrapolationEllipseResultsChanged\""));
            Assert.That(editor, Does.Contain("ExternalCalculation=\"@sourceEllipseCalculation\""));
            Assert.That(editor, Does.Contain("ExternalCalculation=\"@reconnectEllipseCalculation\""));
            Assert.That(editor, Does.Contain("SourceTrajectoryID=\"@SelectedSourceTrajectoryId\""));
            Assert.That(editor, Does.Contain("SourceTrajectoryID=\"@reconnectEllipses.ReferenceTrajectoryID\""));
            Assert.That(editor, Does.Contain("CalculateRelatedTrajectoryEllipsesAsync(calculation.ConfidenceFactor)"));
            Assert.That(editor, Does.Contain("PostTrajectorySurveyStationEllipseCalculationAsync(trajectoryId, request)"));
            Assert.That(editor, Does.Contain("await Task.WhenAll(sourceTask, reconnectTask)"));
            Assert.That(editor, Does.Contain("DataUtils.UpdatePlots("));
            Assert.That(editor, Does.Contain("DataUtils.UpdateEllipsePlots("));
            Assert.That(editor, Does.Contain("restrictExtremePathsToStationInterval: true"));
            Assert.That(editor, Does.Contain("DataUtils.AddExtremeTvdPathPlots(calculation, plotData, minimumAbscissa, maximumAbscissa)"));
            Assert.That(editor, Does.Contain("? \"blue\"").And.Contain("? \"red\"").And.Contain(": \"green\";"));
            Assert.That(editor, Does.Contain("Label=\"Focus on extrapolation\""));
            Assert.That(editor, Does.Contain("private bool focusPlotsOnExtrapolation;"));
            Assert.That(editor, Does.Contain("focusPlotsOnExtrapolation ? northPlotBounds.Minimum : null"));
            Assert.That(editor, Does.Contain("focusPlotsOnExtrapolation ? eastPlotBounds.Minimum : null"));
            Assert.That(editor, Does.Contain("focusPlotsOnExtrapolation ? verticalSectionPlotBounds.Minimum : null"));
            Assert.That(editor, Does.Contain("KeepPanelsAlive=\"true\""));
            Assert.That(editor, Does.Contain("InterpolationInterval = 10.0"));
            Assert.That(extrapolationModel, Does.Contain("DefaultInterpolationInterval = 10.0"));
            Assert.That(editor, Does.Contain("UpdatePlotBounds();"));
            Assert.That(editor, Does.Contain("LoadReconnectReferenceTrajectoryAsync"));
            Assert.That(scatterPlot, Does.Contain("public double? XAxisMinimum").And.Contain("ConvertXAxisBound(XAxisMinimum)"));
            Assert.That(scatter3DPlot, Does.Contain("public double? ZAxisMinimum").And.Contain("ApplyZAxisRange"));
            Assert.That(scatterPlot, Does.Contain("double.IsFinite(converted)").And.Contain("FiniteOrNull"));
            Assert.That(scatter3DPlot, Does.Contain("double.IsFinite(converted)").And.Contain("FiniteOrNull"));
            Assert.That(dataUtils, Does.Contain("SurveyStationList is { Count: > 1 } traj"));
            Assert.That(dataUtils, Does.Contain("double? minimumAbscissa = null").And.Contain("pathAbscissa!.Value < minimumAbscissa.Value - abscissaTolerance"));
            Assert.That(editor, Does.Contain("Items=\"@(new[] { startingStation })\""));
            Assert.That(editor, Does.Contain("GetTrajectorySurveyStationChunkCountAsync(sourceTrajectoryId)"));
            Assert.That(editor, Does.Contain("for (int chunkIndex = chunkCount - 1; chunkIndex >= 0; chunkIndex--)"));
            Assert.That(editor, Does.Contain("sourceLastSurveyStation = finalStation"));
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
        string promptDialog = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TextPromptDialog.razor"));

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
            Assert.That(editor, Does.Not.Contain("PromptAsync(\"Export interpolated extrapolation\", \"File name\""));
            Assert.That(editor, Does.Contain("bool saved = await JSRuntime.InvokeAsync<bool>("));
            Assert.That(editor, Does.Contain("SurveyStationAsciiExport.EnsureTsvFileName"));
            Assert.That(editor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(surveyRunEditor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(trajectoryEditor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(exportUtility, Does.Contain("Measured Depth"));
            Assert.That(exportUtility, Does.Contain("Vertical Section"));
            Assert.That(exportUtility, Does.Contain("DLS"));
            Assert.That(exportUtility, Does.Contain("BUR"));
            Assert.That(exportUtility, Does.Contain("TR"));
            Assert.That(promptDialog, Does.Contain("Immediate=\"@true\" Autofocus=\"@true\""));
            Assert.That(promptDialog, Does.Not.Contain("Autofocus=\"true\""),
                "MudBlazor must receive a Boolean Autofocus parameter rather than a string attribute.");
        });
    }

    [Test]
    public void Trajectory_aggregation_exposes_interpolated_stations_and_shared_exports()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string editor = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TrajectoryAggregationEdit.razor"));
        string calculator = File.ReadAllText(Path.Combine(repositoryRoot, "Model", "TrajectoryAggregationCalculator.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(editor, Does.Contain("QuantityLabel=\"Interpolation Step\""));
            Assert.That(editor, Does.Contain("Text=\"Interpolated aggregated trajectory\""));
            Assert.That(editor, Does.Contain("Items=\"@interpolatedAggregationStations\""));
            Assert.That(editor, Does.Contain("@CurvatureHeader(\"DLS\")"));
            Assert.That(editor, Does.Contain("OnClick=\"SaveAsSurveyRunAsync\">Save as survey run"));
            Assert.That(editor, Does.Contain("OnClick=\"ExportInterpolatedAggregationAsync\">Export ASCII"));
            Assert.That(editor, Does.Contain("SurveyStationAsciiExport.BuildTabSeparated"));
            Assert.That(editor, Does.Contain("SurveyRunType = SurveyRunType.Planned"));
            Assert.That(editor, Does.Contain("WellBoreID = source.WellBoreID"));
            Assert.That(editor, Does.Contain("GroupBy(item => item.SurveyInstrumentID)"));
            Assert.That(editor, Does.Contain("group.Sum(item => item.CoveredLength)"));
            Assert.That(editor, Does.Contain("ParentSurveyRunID = selection.ParentSurveyRunID"));
            Assert.That(editor, Does.Contain("await Api.ClientTrajectory.PostSurveyRunAsync(surveyRun)"));
            Assert.That(calculator, Does.Contain("PopulateDerivedSurveyValues(aggregation.AggregatedSurveyPointList, sourcePoints)"));
        });
    }

    [Test]
    public void Trajectory_host_loads_the_generated_scoped_style_bundle()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebApp", "Pages", "_Layout.cshtml"));
        string source = File.ReadAllText(path);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("<link href=\"WebApp.styles.css\" rel=\"stylesheet\" />"));
            Assert.That(source, Does.Contain("window.showSaveFilePicker"));
            Assert.That(source, Does.Contain("id: \"trajectory-export\""));
            Assert.That(source, Does.Contain("startIn: \"downloads\""));
            Assert.That(source, Does.Contain("suggestedName: fileName"));
        });
    }

    [Test]
    public void Anti_collision_pages_have_their_own_top_level_navigation_group()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebApp", "Shared", "NavMenu.razor"));
        string source = File.ReadAllText(path);
        int antiCollisionGroup = source.IndexOf("Title=\"Anti-collision Management\"", StringComparison.Ordinal);
        int surveyManagementGroup = source.IndexOf("Title=\"Survey Management\"", StringComparison.Ordinal);
        int surveyCalculationsGroup = source.IndexOf("Title=\"Survey Calculations\"", StringComparison.Ordinal);
        int importExportGroup = source.IndexOf("Title=\"Import/Export\"", StringComparison.Ordinal);
        string antiCollisionSection = source[antiCollisionGroup..importExportGroup];
        string surveyCalculationSection = source[surveyCalculationsGroup..antiCollisionGroup];

        Assert.Multiple(() =>
        {
            Assert.That(antiCollisionGroup, Is.GreaterThanOrEqualTo(0));
            Assert.That(surveyManagementGroup, Is.LessThan(surveyCalculationsGroup));
            Assert.That(surveyCalculationsGroup, Is.LessThan(antiCollisionGroup));
            Assert.That(antiCollisionGroup, Is.LessThan(importExportGroup));
            Assert.That(antiCollisionSection, Does.Contain("/Trajectory/webapp/AntiCollisionScan"));
            Assert.That(antiCollisionSection, Does.Contain("/Trajectory/webapp/AntiCollisionPolicies"));
            Assert.That(antiCollisionSection.IndexOf("/Trajectory/webapp/AntiCollisionPolicies", StringComparison.Ordinal),
                Is.LessThan(antiCollisionSection.IndexOf("/Trajectory/webapp/AntiCollisionScan", StringComparison.Ordinal)));
            Assert.That(antiCollisionSection, Does.Contain("/Trajectory/webapp/SurveyRunMinimumDistanceCalculation"));
            Assert.That(antiCollisionSection, Does.Contain("/Trajectory/webapp/TrajectoryMinimumDistanceCalculation"));
            Assert.That(surveyCalculationSection, Does.Not.Contain("AntiCollision"));
            Assert.That(surveyCalculationSection, Does.Not.Contain("MinimumDistanceCalculation"));
        });
    }

    [Test]
    public void Confidence_factor_editors_use_proportion_units_and_extrapolation_reuses_the_ellipse_component()
    {
        string webPages = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages"));
        string ellipse = File.ReadAllText(Path.Combine(webPages, "SurveyStationEllipseTable.razor"));
        string policies = File.ReadAllText(Path.Combine(webPages, "AntiCollisionPolicies.razor"));
        string scan = File.ReadAllText(Path.Combine(webPages, "AntiCollisionScan.razor"));
        string extrapolation = File.ReadAllText(Path.Combine(webPages, "TrajectoryExtrapolationEdit.razor"));

        Assert.Multiple(() =>
        {
            Assert.That(ellipse, Does.Contain("QuantityName=\"ProportionStandard\" QuantityLabel=\"Confidence factor\""));
            Assert.That(ellipse, Does.Contain("Azimuth [@Parent?.GetAzimuthUnitLabel(\"PlaneAngleDrilling\")]"));
            Assert.That(ellipse, Does.Contain("DrillingSignalReference=\"DrillingSignalReferenceType.Azimuth\" SIValue=\"@orientationAngle\""));
            Assert.That(policies, Does.Contain("QuantityName=\"ProportionStandard\" QuantityLabel=\"Confidence factor\""));
            Assert.That(policies, Does.Contain("<MudUnitAndReferenceChoiceTag").And.Contain("</MudUnitAndReferenceChoiceTag>"));
            Assert.That(policies.IndexOf("<MudUnitAndReferenceChoiceTag", StringComparison.Ordinal),
                Is.LessThan(policies.IndexOf("<MudInputWithUnitAdornment", StringComparison.Ordinal)));
            Assert.That(policies.IndexOf("</MudUnitAndReferenceChoiceTag>", StringComparison.Ordinal),
                Is.GreaterThan(policies.LastIndexOf("<MudInputWithUnit", StringComparison.Ordinal)));
            Assert.That(scan, Does.Contain("QuantityName=\"ProportionStandard\" QuantityLabel=\"Confidence factor\""));
            Assert.That(scan, Does.Contain("Label=\"Field anti-collision policy assignment\""));
            Assert.That(scan, Does.Contain("SearchFunc=\"SearchPolicyAssignmentsAsync\""));
            Assert.That(scan, Does.Contain("ReadOnly=\"@(selectedPolicyAssignmentId_ != null)\""));
            Assert.That(scan, Does.Contain("RequestedPolicyAssignmentID = selectedPolicyAssignmentId_"));
            Assert.That(scan, Does.Contain("color:#ed6c02").And.Contain("color:#d32f2f"));
            Assert.That(scan, Does.Contain("LineDashList=\"@PlotLineDashes\"").And.Contain("LineWidthList=\"@PlotLineWidths\""));
            Assert.That(scan, Does.Contain("ProfileClassification.Alarm ? 5m : 2m"));
            Assert.That(scan, Does.Contain("InterpolateThresholdCrossings(left, right, evaluation)"));
            Assert.That(scan, Does.Contain("left.ReferenceMD + fraction * (right.ReferenceMD - left.ReferenceMD)"));
            Assert.That(scan, Does.Contain("Value=\"@AllCandidatesSelected\" ValueChanged=\"SetAllCandidatesSelected\""));
            Assert.That(scan, Does.Not.Contain("OnClick=\"@SelectAllCandidates\"").And.Not.Contain("OnClick=\"@ClearCandidateSelection\""));
            Assert.That(ellipse, Does.Contain("confidenceFactor_ > 0.0 && confidenceFactor_ <= MaximumConfidenceFactor"));
            Assert.That(ellipse, Does.Contain("public bool ReadOnlyCalculation").And.Contain("public SurveyStationEllipseCalculation? ExternalCalculation"));
            Assert.That(ellipse, Does.Contain("confidenceFactor_ = ExternalCalculation.ConfidenceFactor"));
            Assert.That(policies, Does.Contain("draft_.ConfidenceFactor > 0.0"));
            Assert.That(scan, Does.Contain("confidenceFactor_ > 0.0 && confidenceFactor_ <= MaximumConfidenceFactor"));
            Assert.That(extrapolation, Does.Contain("<SurveyStationEllipseTable Title=\"Extrapolated trajectory uncertainty ellipses\""));
            Assert.That(extrapolation, Does.Contain("SurveyStationList=\"@interpolatedExtrapolationStations\""));
            Assert.That(extrapolation, Does.Contain("UncertaintyPropagationHistory=\"@WolffDeWardtSourceHistory\""));
            Assert.That(extrapolation, Does.Contain("sourceLastDefinedSurveyTool"));
            Assert.That(extrapolation, Does.Contain("station.SurveyTool ??= sourceLastDefinedSurveyTool"));
            Assert.That(extrapolation, Does.Contain("first.Covariance = sourceLastSurveyStation.Covariance"));
        });
    }

    [Test]
    public void Anti_collision_field_assignment_selectors_use_nullable_empty_values()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "WebPages", "AntiCollisionPolicies.razor"));
        string source = File.ReadAllText(path);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("MudAutocomplete T=\"Guid?\" Value=\"@assignmentFieldId_\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchAssignmentFieldsAsync\""));
            Assert.That(source, Does.Contain("ToStringFunc=\"GetAssignmentFieldName\""));
            Assert.That(source, Does.Contain("Contains(term, StringComparison.OrdinalIgnoreCase)"));
            Assert.That(source, Does.Contain("MudAutocomplete T=\"Guid?\" Value=\"@assignmentPolicyId_\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchAssignmentPoliciesAsync\""));
            Assert.That(source, Does.Contain("ToStringFunc=\"GetAssignmentPolicyName\""));
            Assert.That(source, Does.Contain("MudAutocomplete T=\"Guid?\" Value=\"@assignmentPolicyRevisionId_\""));
            Assert.That(source, Does.Contain("SearchFunc=\"SearchAssignmentRevisionsAsync\""));
            Assert.That(source, Does.Contain("ToStringFunc=\"GetAssignmentRevisionName\""));
            Assert.That(source, Does.Not.Contain("ToStringFunc=\"GetAssignmentFieldName\" Required=\"true\""));
            Assert.That(source, Does.Not.Contain("ToStringFunc=\"GetAssignmentPolicyName\" Required=\"true\""));
            Assert.That(source, Does.Not.Contain("ToStringFunc=\"GetAssignmentRevisionName\" Required=\"true\""));
            Assert.That(source, Does.Contain("Disabled=\"@(assignmentPolicyId_ == null)\""));
            Assert.That(source, Does.Contain("revision.PolicyID == assignmentPolicyId_"));
            Assert.That(source, Does.Contain("assignmentPolicyRevisionId_ = null;"));
            Assert.That(source, Does.Contain("Placeholder=\"Select a Field\""));
            Assert.That(source, Does.Contain("Placeholder=\"Select a policy\""));
            Assert.That(source, Does.Contain("Placeholder=\"Select a policy revision\""));
            Assert.That(source, Does.Contain("assignment_.FieldID = fieldId;"));
            Assert.That(source, Does.Contain("assignment_.PolicyRevisionID = policyRevisionId;"));
            Assert.That(source, Does.Not.Contain("@bind-Value=\"assignment_.FieldID\""));
            Assert.That(source, Does.Not.Contain("@bind-Value=\"assignment_.PolicyRevisionID\""));
            Assert.That(source, Does.Contain("Policies and revisions"));
            Assert.That(source, Does.Contain("PolicyGroups").And.Contain("policy.Revisions"));
            Assert.That(source, Does.Contain("OnRowClick=\"@OnRevisionRowClicked\""));
            Assert.That(source, Does.Contain("Edit to create new revision"));
            Assert.That(source, Does.Contain("Historical revisions are read-only"));
            Assert.That(source, Does.Contain("!IsLatestRevision(selectedRevision_)"));
            Assert.That(source, Does.Contain("Label=\"Priority\" Min=\"1\""));
            Assert.That(source, Does.Not.Contain(">New revision</MudButton>"));
            Assert.That(source, Does.Contain("Fields: @(policy.FieldNames.Count == 0 ? \"None\""));
            Assert.That(source, Does.Contain("DeletePolicyAsync(policy)"));
            Assert.That(source, Does.Contain("policy.FieldNames.Count == 0"));
            Assert.That(source, Does.Not.Contain("policy.Description"));
            Assert.That(source, Does.Contain("MudTextField T=\"double\" @bind-Value=\"rule.AlertThreshold\""));
            Assert.That(source, Does.Contain("MudTextField T=\"double\" @bind-Value=\"rule.AlarmThreshold\""));
            Assert.That(source, Does.Not.Contain("MudNumericField T=\"double\" @bind-Value=\"rule.AlertThreshold\""));
            Assert.That(source, Does.Not.Contain("MudNumericField T=\"double\" @bind-Value=\"rule.AlarmThreshold\""));
            Assert.That(source, Does.Not.Contain("@revision.PolicyID"));
            Assert.That(source, Does.Contain("ex.StatusCode == 404"));
            Assert.That(source, Does.Contain("ex.StatusCode == 409"));
            Assert.That(source, Does.Contain("Deleted Field policy assignment"));
        });
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

    [Test]
    public void Target_landing_uses_shared_resource_filter_references_and_three_zone_results()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        string editor = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TargetLandingEdit.razor"));
        string scatter3D = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "Scatter3DPlot.razor"));
        string generatedClient = File.ReadAllText(Path.Combine(repositoryRoot, "ModelSharedOut", "TrajectoryMergedModel.cs"));
        string main = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "TargetLandingMain.razor"));
        string stableUnitInput = File.ReadAllText(Path.Combine(repositoryRoot, "WebPages", "StableMudInputWithUnitAdornment.razor"));
        string navigation = File.ReadAllText(Path.Combine(repositoryRoot, "WebApp", "Shared", "NavMenu.razor"));
        string savePayload = GetMethodSource(editor,
            "private static TargetLandingCase CreateSavePayload", "private List<PlaneContour> PlaneContours");
        string saveAs = GetMethodSource(editor,
            "private async Task SaveAsAsync()", "private async Task<string?> PromptAsync");
        string listLoad = GetMethodSource(main,
            "private async Task LoadAsync()", "private async Task LoadTrajectoryNamesAsync()");
        string closeEditor = GetMethodSource(main,
            "private Task CloseEditorAsync(TargetLandingCaseLight? persisted)", "private void StartPollingIfNeeded()");
        int referenceSelectorEnd = main.IndexOf("</MudUnitAndReferenceChoiceTag>", StringComparison.Ordinal);
        int listGridStart = main.IndexOf("<MudDataGrid T=\"TargetLandingCaseLight\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(editor, Does.Contain("<TrajectoryResourceFilter"));
            Assert.That(editor, Does.Contain("CompletedTrajectoriesOnly=\"true\""));
            Assert.That(editor, Does.Contain("<RigJobSelector").And.Contain("sourceReferenceDate"));
            Assert.That(main, Does.Contain("GroundMudLineDepthReferenceSource=\"@DataUtils.GroundMudLineDepthReferenceSource\""));
            Assert.That(main, Does.Contain("WellHeadPositionReferenceSource=\"@DataUtils.WellHeadPositionReferenceSource\""));
            Assert.That(main, Does.Contain("GridConvergenceSource=\"@DataUtils.GridConvergenceSource\""));
            Assert.That(main, Does.Contain("GeodeticReferenceName=\"@DataUtils.UnitAndReferenceParameters.GeodeticReferenceName\""));
            Assert.That(main, Does.Contain("CartographicProjectionDatumGeodeticReferenceSource=\"@DataUtils.CartographicProjectionDatumGeodeticReferenceSource\""));
            Assert.That(referenceSelectorEnd, Is.GreaterThanOrEqualTo(0).And.LessThan(listGridStart),
                "The list must render without waiting for the editor-only unit and reference selector.");
            Assert.That(listLoad, Does.Contain("GetAllTargetLandingCaseLightAsync")
                .And.Contain("StartPollingIfNeeded()")
                .And.Not.Contain("GetSnapshotAsync")
                .And.Not.Contain("GetTrajectoriesAsync"),
                "Target landing cases and active calculation polling must not wait for reference-data loading.");
            Assert.That(main, Does.Contain("_ = LoadTrajectoryNamesAsync()")
                .And.Contain("ReferenceData.GetTrajectoriesAsync()"),
                "Source names should enrich the already-visible target-landing list asynchronously.");
            Assert.That(main, Does.Contain("<TargetLandingEdit CaseId=\"@caseId\" ValueChanged=\"CloseEditorAsync\" />"));
            Assert.That(closeEditor, Does.Contain("cases.FindIndex")
                .And.Contain("cases[index] = persisted")
                .And.Contain("StartPollingIfNeeded()")
                .And.Not.Contain("LoadAsync")
                .And.Not.Contain("GetAllTargetLandingCaseLightAsync")
                .And.Not.Contain("GetAllTrajectoryLightAsync"),
                "Closing the editor must merge its persisted light record locally instead of repeating the case and trajectory list requests.");
            Assert.That(editor, Does.Contain("EventCallback<TargetLandingCaseLight?> ValueChanged")
                .And.Contain("ValueChanged.InvokeAsync(persistedLight)")
                .And.Contain("persistedLight = status")
                .And.Contain("persistedLight = ToLight(value)"),
                "The editor must return only its latest persisted light state so unsaved draft fields are not copied into the list.");
            Assert.That(editor, Does.Contain("QuantityLabel=\"North\"").And.Contain("QuantityLabel=\"East\""));
            Assert.That(editor, Does.Contain("QuantityLabel=\"Latitude\"").And.Contain("QuantityLabel=\"Longitude\""));
            Assert.That(editor, Does.Contain("DrillingSignalReferenceType.Geodetic").And.Contain("PlaneAngleGeodesic"));
            Assert.That(editor, Does.Not.Contain("OriginCoordinateMode").And.Not.Contain("PolygonCoordinateMode").And.Not.Contain("Riemannian North"));
            Assert.That(editor, Does.Contain("<MudTh>Plane X [@LengthUnitLabel]</MudTh>")
                .And.Contain("<MudTh>Plane Y [@LengthUnitLabel]</MudTh>")
                .And.Contain("<MudTh>Radial distance [@LengthUnitLabel]</MudTh>")
                .And.Contain("<MudTh>Angle [@AngleUnitLabel]</MudTh>"));
            Assert.That(editor, Does.Contain("The angle follows the toolface convention"));
            Assert.That(editor, Does.Contain("SetX(context, x)").And.Contain("SetY(context, x)").And.Contain("SetRadius(context, x)").And.Contain("SetAngle(context, x)"));
            Assert.That(editor, Does.Not.Contain("CoordinateEditorKey").And.Not.Contain("RefreshPolygonRow"));
            Assert.That(editor, Does.Contain("QuantityName=\"LengthStandard\"").And.Not.Contain("LengthDrilling"),
                "Target-plane coordinates and landing lengths must use the supported LengthStandard physical quantity.");
            Assert.That(editor, Does.Contain("private static bool Finite(double value)").And.Contain("CylinderPathSegments(TargetLandingSample sample)"));
            Assert.That(editor, Does.Contain("@if (referenceValuesReady)")
                .And.Contain("referenceValuesReady = true;")
                .And.Contain("referenceSourceVersion++;")
                .And.Contain("ReferenceSourceVersion=\"@referenceSourceVersion\""),
                "Reference-aware inputs and plots must initialize only after source reference values are resolved, and must reformat when their source context changes.");
            Assert.That(stableUnitInput, Does.Contain("ReferenceSourceVersion != lastReferenceSourceVersion")
                .And.Contain("lastReferenceSourceVersion = ReferenceSourceVersion"),
                "A changed reference source must refresh the displayed value even when its canonical SI value is unchanged.");
            Assert.That(editor, Does.Contain("DrillingSignalReferenceType.Depth"));
            Assert.That(editor, Does.Contain("<StableMudInputWithUnitAdornment QuantityLabel=\"Target TVD\"")
                .And.Contain("SIValueNullableChanged=\"SetTargetTvd\"")
                .And.Contain("private void SetTargetTvd(double? x) { Plane.TVD=x; Plane.Z=x; }"));
            Assert.That(editor, Does.Not.Contain("<MudInputWithUnitAdornment"),
                "Every unit-bearing target-landing editor must use the page-local input that synchronizes displayed and canonical values.");
            Assert.That(stableUnitInput, Does.Contain("Immediate=\"true\"")
                .And.Contain("displayValue = value;")
                .And.Contain("lastCanonicalValue = canonicalValue;")
                .And.Contain("DrillingSignalReferenceType.Depth => Parent!.ToWGS84DepthSI")
                .And.Contain("DrillingSignalReferenceType.Position => Parent!.ToWGS84PositionSI")
                .And.Contain("DrillingSignalReferenceType.Geodetic => Parent!.ToWGS84GeodeticSI")
                .And.Contain("DrillingSignalReferenceType.Azimuth => Parent!.ToTrueNorthAzimuthSI"),
                "Target-landing engineering inputs must commit every valid edit without reformatting raw text from stale parent values.");
            Assert.That(editor, Does.Contain("PutWithSafeCalculationRetryAsync(value.MetaInfo.ID, savePayload)")
                .And.Contain("catch (ApiException ex) when (ex.StatusCode == 409)")
                .And.Contain("latest.CalculationState is CalculationState.Completed or CalculationState.Failed")
                .And.Contain("string.Equals(EditableFingerprint(latest), loadedEditableFingerprint, StringComparison.Ordinal)")
                .And.Contain("ConcurrencyToken.Require(latest.LastModificationDate), savePayload"),
                "A terminal background-calculation write may advance the revision, but the editor must retry only when the editable configuration is unchanged.");
            Assert.That(editor, Does.Contain("await RefreshAfterCalculationAsync(value.MetaInfo.ID, calculationPolling.Token)")
                .And.Contain("GetTargetLandingCaseStatusAsync(caseId, cancellationToken)")
                .And.Contain("GetTargetLandingCaseEditDataAsync(caseId, cancellationToken)")
                .And.Contain("GetTargetLandingCaseDisplayDataAsync(caseId, cancellationToken)"),
                "Saving must keep the detailed editor open, report progress, and reload only the edit and display projections in place.");
            Assert.That(editor, Does.Not.Contain("GetTargetLandingCaseByIdAsync"),
                "The editor must never download the unrestricted calculation aggregate.");
            Assert.That(editor, Does.Contain("% complete — calculation continues on the server")
                .And.Contain("MonitorExistingCalculationAsync")
                .And.Contain("Disabled=\"@(saving || IsCalculationActive)\""));
            Assert.That(main, Does.Contain("GetTargetLandingCaseStatusAsync(item.MetaInfo.ID, token)")
                .And.Contain("cases[index] = status"),
                "The list must poll and replace the complete lightweight status row, including its latest concurrency token, rather than repeatedly downloading every heavy case.");
            Assert.That(GetMethodSource(editor, "private async Task SaveAsync()", "private async Task RefreshAfterCalculationAsync"),
                Does.Not.Contain("ValueChanged.InvokeAsync"),
                "Saving must not invoke the close callback.");
            Assert.That(editor, Does.Contain("OnClick=\"SaveAsAsync\"")
                .And.Contain(">Save as</MudButton>")
                .And.Contain("ShowAsync<TextPromptDialog>"),
                "An existing target-landing case must expose a named Save-as workflow.");
            Assert.That(saveAs, Does.Contain("TargetLandingCase copy = CreateSavePayload(value);")
                .And.Contain("copy.MetaInfo = new MetaInfo { ID = Guid.NewGuid() };")
                .And.Contain("copy.Name = copyName;")
                .And.Contain("PostTargetLandingCaseAsync(copy, calculationPolling.Token)")
                .And.Contain("RefreshAfterCalculationAsync(value.MetaInfo.ID, calculationPolling.Token)")
                .And.Not.Contain("PutTargetLandingCaseByIdAsync")
                .And.Not.Contain("SampleList")
                .And.Not.Contain("CalculationFingerprint"),
                "Save as must create a new resource from editable inputs and must not overwrite or copy calculated results.");
            Assert.That(savePayload, Does.Contain("Target = source.Target")
                .And.Contain("MaximumLandingCurvature = source.MaximumLandingCurvature")
                .And.Not.Contain("SampleList")
                .And.Not.Contain("MeshTriangleList")
                .And.Not.Contain("SourceEndStation")
                .And.Not.Contain("LeadSurveyStationList")
                .And.Not.Contain("SteeringStartStation")
                .And.Not.Contain("CalculationFingerprint"),
                "Saving an edited case must not resend the large server-derived calculation result through nginx.");
            Assert.That(editor, Does.Contain("DrillingSignalReferenceType.Azimuth"));
            Assert.That(editor, Does.Contain("QuantityName=\"ProportionStandard\""));
            Assert.That(editor, Does.Contain("Math.Clamp(x, 0.000001, 0.999)"));
            Assert.That(editor, Does.Contain("Maximum Landing Curvature"));
            Assert.That(editor, Does.Contain("DrillerTargetContourList").And.Contain("ReachableTargetContourList"));
            Assert.That(editor, Does.Contain("AspectRatio=\"1\""));
            Assert.That(editor, Does.Contain("FillToSelfList=\"@PlanePlotFill\"")
                .And.Contain("rgba(46,125,50,0.24)")
                .And.Contain("Curvature-excluded island")
                .And.Contain("rgba(230,81,0,0.20)")
                .And.Not.Contain("Uncertainty excluded")
                .And.Not.Contain("Curvature/geometry excluded"),
                "The plane plot must fill reachable contours without rendering adaptive excluded samples.");
            Assert.That(editor, Does.Contain("ShowUnitCylinder=\"true\"")
                .And.Contain("point.Radius*Math.Sin(point.Toolface)")
                .And.Contain("point.Radius*Math.Cos(point.Toolface)")
                .And.Contain("CylinderPlotData cylinderPlot = BuildCylinderPlot()")
                .And.Contain("LineWidthList=\"@cylinderPlot.LineWidths\"")
                .And.Contain("CentralCurvatureMarker=\"@(value.SourceEndStation?.Curvature)\"")
                .And.Contain("ZUnit=\"CurvatureDrilling\""),
                "CA, CTC and BT boundary paths must be drawn as marker-free lines on a normalized-length toolface/curvature cylinder.");
            Assert.That(editor, Does.Contain("@if (cylinderPlot.Names.Count > 0)")
                .And.Contain("saved result predates inclination-aware normalized curve controls")
                .And.Contain("Run Save and calculate against the current Trajectory service"),
                "Legacy results must explain why exact-control graphs cannot be drawn instead of silently showing empty plot areas.");
            Assert.That(editor, Does.Contain("SplitControlPath(sample,false)")
                .And.Contain("VerticalControlDisplayInclination=3.0*Math.PI/180.0")
                .And.Contain("point.Inclination.HasValue&&ControlPointIsFinite(point)")
                .And.Contain("ControlPointIsNearVertical")
                .And.Contain("Paths contain a gap within 3° of vertical")
                .And.Not.Contain("StationControl(")
                .And.Not.Contain("StationBuildTurn(")
                .And.Not.Contain("XAxisTitle=\"sin(toolface)")
                .And.Not.Contain("YAxisTitle=\"cos(toolface)"),
                "The paths must use authoritative model-level curve controls without reconstructing them from sparse survey stations.");
            Assert.That(generatedClient, Does.Contain("class TargetLandingControlPoint")
                .And.Contain("public System.Collections.Generic.List<TargetLandingControlPoint> ControlPointList")
                .And.Contain("public double NormalizedLength")
                .And.Contain("public double? Inclination")
                .And.Contain("public double BuildRate")
                .And.Contain("public double TurnRate"),
                "The exact normalized controls must be part of the generated REST/client result contract.");
            Assert.That(scatter3D, Does.Contain("Math.Max(0.0, convertedCurvatures.Max()) * 1.2")
                .And.Contain("return (0.0, Math.Max(maximum, 1e-6))")
                .And.Contain("ShowUnitCylinder ? UnitCylinderCurvatureRange() : null")
                .And.Contain("Name = \"Curvature axis\"")
                .And.Contain("Name = \"Source terminal curvature\"")
                .And.Contain("scene.XAxis.Title.Text = ShowUnitCylinder ? \"\"")
                .And.Contain("scene.YAxis.Title.Text = ShowUnitCylinder ? \"\""),
                "The cylinder and curvature axis must span zero to 120 percent of the maximum boundary curvature.");
            Assert.That(editor, Does.Contain("Build and turn landing controls")
                .And.Contain("XAxisTitle=\"Turn rate\"")
                .And.Contain("YAxisTitle=\"Normalized length\"")
                .And.Contain("ZAxisTitle=\"Build rate\"")
                .And.Contain("XUnit=\"CurvatureDrilling\" YUnit=\"Dimensionless\" ZUnit=\"CurvatureDrilling\"")
                .And.Contain("YAxisMinimum=\"0\" YAxisMaximum=\"1\"")
                .And.Contain("UseCubeAspect=\"true\"")
                .And.Contain("BuildTurnPathSegments(sample)")
                .And.Contain("point.TurnRate,point.NormalizedLength,point.BuildRate")
                .And.Contain("with { NormalizedLength=1.0 }")
                .And.Contain("family.IsClosed ? CloseTrace(terminalPoints) : terminalPoints.ToList()")
                .And.Contain("0.5*Math.PI/(180.0*30.0)")
                .And.Contain("0.2*(maximum-minimum)"),
                "The build/turn view must show sampled control paths from normalized length zero to the reachable boundary at one, using unit-aware axes with the requested padding.");
            Assert.That(editor, Does.Contain("family.IsClosed ? CloseTrace(terminalRows) : terminalRows.ToList()")
                .And.Contain("private static List<T> CloseTrace<T>(IEnumerable<T> points)")
                .And.Contain("if (result.Count>1) result.Add(result[0]);"),
                "The terminal build/turn and cylindrical boundary traces must close only for a continuous solution family.");
            Assert.That(editor, Does.Contain("SplitSolutionFamilies(contour)")
                .And.Contain("private static bool IsSolutionDiscontinuity")
                .And.Contain("midpointSeparation>Math.Max(25.0,8.0*Math.Max(planeStep,PositionComparisonTolerance))")
                .And.Contain("Color identifies a continuous solution family")
                .And.Contain("<MudTh>Family</MudTh>"),
                "Distinct inverse-solution branches must be split, colored, and identified instead of being joined by false contour segments.");
            Assert.That(scatter3D, Does.Contain("ShowUnitCylinder || UseCubeAspect"),
                "The control-space graph must remain legible despite the different numerical scales of curvature and normalized length.");
            Assert.That(editor, Does.Contain("Reachable target boundary commands")
                .And.Contain("Items=\"@BoundarySolutions\"")
                .And.Contain("SectionCurvature(section)")
                .And.Contain("SectionToolface(section)")
                .And.Contain("section.ConstantBuildRate")
                .And.Contain("section.ConstantTurnRate"),
                "The end-user table must report boundary points and their unit-aware section commands.");
            Assert.That(editor, Does.Contain("Cartesian landing geometry")
                .And.Contain("GetTrajectorySurveyStationChunkCountAsync")
                .And.Contain("GetTrajectorySurveyStationChunkAsync")
                .And.Contain("value.LeadSurveyStationList")
                .And.Contain("LeadStationsForPlot()")
                .And.Contain("value.SourceEndStation??sourceTrajectoryStations.LastOrDefault")
                .And.Contain("LineWidthList=\"@cartesianPlot.LineWidths\"")
                .And.Contain("\"Lead path\",\"#ff6f00\",8")
                .And.Contain("foreach (BoundarySolutionFamily family in BoundarySolutionFamilies)")
                .And.Contain("style.Translucent,3,firstPath")
                .And.Contain("sample.SurveyStationList")
                .And.Contain("TargetBoundaryPoints")
                .And.Contain("XAxisTitle=\"North\"")
                .And.Contain("YAxisTitle=\"East\"")
                .And.Contain("ZAxisTitle=\"TVD\"")
                .And.Contain("ZAxisReversed=\"true\""),
                "The Cartesian view must combine the chunked source trajectory, target boundaries, lead path, and landing paths in unit-aware N/E/TVD coordinates.");
            Assert.That(editor, Does.Contain("Reference perpendicular ellipses")
                .And.Contain("Lead perpendicular ellipses")
                .And.Contain("Landing ellipses in target plane")
                .And.Contain("GetTargetLandingCaseUncertaintyDisplayDataAsync")
                .And.Contain("PerpendicularEllipsePoints")
                .And.Contain("TargetPlaneEllipsePoints")
                .And.Contain("sample.LandingEllipseInTargetPlane")
                .And.Not.Contain("Extended path perpendicular ellipses"),
                "The Cartesian view must offer perpendicular uncertainty only for the reference and lead, while landing-path ellipses remain projected in the target plane.");
            Assert.That(navigation, Does.Contain("/Trajectory/webapp/TargetLanding"));
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
