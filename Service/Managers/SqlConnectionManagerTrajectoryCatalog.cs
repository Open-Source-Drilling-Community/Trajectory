using Microsoft.Extensions.Logging;

namespace OSDC.Drilling.Trajectory.Service.Managers;

/// <summary>Owns the shared identity and feature catalogs used by survey runs and trajectories.</summary>
public sealed class SqlConnectionManagerTrajectoryCatalog : SqlConnectionManager
{
    private static readonly IReadOnlyDictionary<string, string[]> Tables = new Dictionary<string, string[]>
    {
        ["TrajectoryIdentityTable"] =
        [
            "ID text primary key", "MetaInfo text", "Name text", "CreationDate text",
            "LastModificationDate text", "TrajectoryIdentity text"
        ],
        ["TrajectoryFeatureCategoryTable"] =
        [
            "ID text primary key", "MetaInfo text", "Name text", "IsExclusive integer",
            "HasValidityPeriod integer", "CreationDate text", "LastModificationDate text",
            "TrajectoryFeatureCategory text"
        ]
    };

    public SqlConnectionManagerTrajectoryCatalog(ILogger<SqlConnectionManagerTrajectoryCatalog> logger)
        : base(logger, "TrajectoryCatalog.db", Tables)
    {
    }

    public SqlConnectionManagerTrajectoryCatalog(string databasePath, ILogger<SqlConnectionManagerTrajectoryCatalog> logger)
        : base(BuildConnectionString(databasePath), logger, databasePath, "TrajectoryCatalog.db", Tables)
    {
    }
}
