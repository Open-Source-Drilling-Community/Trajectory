using Microsoft.Data.Sqlite;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Text.Json;

namespace OSDC.Drilling.Trajectory.Service.Managers;

public sealed class TrajectoryIdentityManager
{
    private static readonly string[] Defaults =
        ["NameForPlanning", "NameForCompanyReporting", "NameForRegulatoryReporting", "Nickname", "NameForOperationReporting"];
    private readonly TrajectoryCatalogStore<Model.TrajectoryIdentity> store;
    private readonly SqlConnectionManager mainDatabase;

    public TrajectoryIdentityManager(SqlConnectionManager mainDatabase)
    {
        this.mainDatabase = mainDatabase;
        store = new(mainDatabase, "TrajectoryIdentityTable", "TrajectoryIdentity",
            value => value.MetaInfo, value => value.Name, (value, date) => value.CreationDate = date,
            (value, date) => value.LastModificationDate = date);
    }

    public List<Model.TrajectoryIdentity> GetAll() { EnsureDefaults(); return store.All(); }
    public Model.TrajectoryIdentity? Get(Guid id) => store.ById(id);
    public bool Add(Model.TrajectoryIdentity value) => store.Add(value);
    public bool Update(Guid id, Model.TrajectoryIdentity value) => store.Update(id, value);
    public bool Delete(Guid id) => !IsReferenced(id) && store.Delete(id);
    public bool IsReferenced(Guid id) => ReadAssignments().Any(ids => ids.Contains(id));

    private IEnumerable<HashSet<Guid>> ReadAssignments()
    {
        foreach (Model.SurveyRun value in ReadDocuments<Model.SurveyRun>("SurveyRunTable", "SurveyRun"))
            yield return (value.SurveyRunIdentityAssignments ?? []).Where(a => a.IdentityID.HasValue).Select(a => a.IdentityID!.Value).ToHashSet();
        foreach (Model.Trajectory value in ReadDocuments<Model.Trajectory>("TrajectoryTable", "Trajectory"))
            yield return (value.TrajectoryIdentityAssignments ?? []).Where(a => a.IdentityID.HasValue).Select(a => a.IdentityID!.Value).ToHashSet();
    }

    private IEnumerable<T> ReadDocuments<T>(string table, string column)
    {
        using SqliteConnection connection = mainDatabase.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {column} FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            T? value = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options);
            if (value != null) yield return value;
        }
    }

    private void EnsureDefaults()
    {
        if (store.All().Count != 0) return;
        foreach (string name in Defaults)
            store.Add(new() { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = name });
    }
}
