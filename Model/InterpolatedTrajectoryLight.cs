using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// Light weight version of an InterpolatedTrajectory.
    /// Used to avoid loading the complete interpolation payload when only contextual data is needed.
    /// </summary>
    public class InterpolatedTrajectoryLight
    {
        /// <summary>
        /// a MetaInfo for the TrajectoryLight
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }
        /// <summary>
        /// name of the data
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// a description of the data
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// the date when the data was created
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; }
        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        public DateTimeOffset? LastModificationDate { get; set; }
        /// <summary>
        /// the ID of the wellbore associated to the trajectory
        /// </summary>
        public Guid TrajectoryID { get; set; }
        /// <summary>
        /// default constructor required for parsing the data model as a json file
        /// </summary>
        public InterpolatedTrajectoryLight() : base()
        {
        }

        /// <summary>
        /// base constructor
        /// </summary>
        public InterpolatedTrajectoryLight(MetaInfo? metaInfo, string? name, string? descr, DateTimeOffset? creationDate, DateTimeOffset? modifDate, Guid trajectoryId)
        {
            MetaInfo = metaInfo;
            Name = name;
            Description = descr;
            CreationDate = creationDate;
            LastModificationDate = modifDate;
            TrajectoryID = trajectoryId;
        }
    }
}
