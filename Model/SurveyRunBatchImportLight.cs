using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// Light-weight version of a persisted survey run batch import.
    /// </summary>
    public class SurveyRunBatchImportLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }

        public SurveyRunBatchImportLight() : base()
        {
        }
    }
}
