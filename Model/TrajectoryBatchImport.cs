using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// Persisted definition of a trajectory batch import configuration.
    /// </summary>
    public class TrajectoryBatchImport : TrajectoryBatchImportLight
    {
        public Guid? SelectedFieldId { get; set; }
        public Guid? SelectedClusterId { get; set; }
        public Guid? SelectedWellId { get; set; }
        public string? CommonDepthReference { get; set; }
        public bool ReplaceExistingTrajectories { get; set; }
        public bool ReplaceTrajectoriesWithSameName { get; set; }
        public SurveyImportSettings? Settings { get; set; }
        public List<TrajectoryBatchImportRow>? Rows { get; set; }

        public TrajectoryBatchImport() : base()
        {
        }
    }

    public class TrajectoryBatchImportRow
    {
        public Guid RowId { get; set; } = Guid.NewGuid();
        public Guid? WellBoreId { get; set; }
        public string? DepthReferenceName { get; set; }
        public string? FileName { get; set; }
        public string? FileContentBase64 { get; set; }
    }

    public class SurveyImportSettings
    {
        public string? SelectedSurveyImportFormat { get; set; }
        public string? SelectedSurveyImportSeparator { get; set; }
        public string? SelectedSurveyImportDecimalMarker { get; set; }
        public string? SelectedSurveyImportMDUnit { get; set; }
        public string? SelectedSurveyImportInclinationUnit { get; set; }
        public string? SelectedSurveyImportAzimuthUnit { get; set; }
        public int SurveyImportMDColumn { get; set; }
        public int SurveyImportInclinationColumn { get; set; }
        public int SurveyImportAzimuthColumn { get; set; }
        public int SurveyImportMDStart { get; set; }
        public int SurveyImportMDWidth { get; set; }
        public int SurveyImportInclinationStart { get; set; }
        public int SurveyImportInclinationWidth { get; set; }
        public int SurveyImportAzimuthStart { get; set; }
        public int SurveyImportAzimuthWidth { get; set; }
    }
}
