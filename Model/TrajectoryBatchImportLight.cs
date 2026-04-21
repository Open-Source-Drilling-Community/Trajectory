using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// Light-weight version of a persisted trajectory batch import.
    /// </summary>
    public class TrajectoryBatchImportLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }

        public TrajectoryBatchImportLight() : base()
        {
        }

        public TrajectoryBatchImportLight(MetaInfo? metaInfo, string? name, string? description, DateTimeOffset? creationDate, DateTimeOffset? lastModificationDate)
        {
            MetaInfo = metaInfo;
            Name = name;
            Description = description;
            CreationDate = creationDate;
            LastModificationDate = lastModificationDate;
        }
    }
}
