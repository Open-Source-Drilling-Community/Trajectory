using OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.WebPages;

/// <summary>Loads only the selected ownership chain when slots, tie-in context, or rig history are needed.</summary>
internal static class ReferenceDetailsLoader
{
    private sealed class LoadedMarker { }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<WellBore, LoadedMarker> Loaded = new();
    public static async Task EnsureAsync(ITrajectoryAPIUtils api, Guid? wellBoreId,
        IEnumerable<WellBore>? wellBores, IEnumerable<Well>? wells, IEnumerable<Cluster>? clusters)
    {
        var visited = new HashSet<Guid>();
        Guid? next = wellBoreId;
        while (next is Guid id && id != Guid.Empty && visited.Add(id))
        {
            var reference = wellBores?.FirstOrDefault(item => item.MetaInfo?.ID == id);
            var full = reference;
            if (reference == null || !Loaded.TryGetValue(reference, out _))
            {
                full = await api.ClientWellBore.GetWellBoreByIdAsync(id);
                if (reference != null) { Copy(full, reference); Loaded.GetValue(reference, _ => new LoadedMarker()); }
            }
            var well = wells?.FirstOrDefault(item => item.MetaInfo?.ID == full.WellID);
            if (well?.ClusterID is Guid clusterId)
            {
                var cluster = clusters?.FirstOrDefault(item => item.MetaInfo?.ID == clusterId);
                if (cluster != null) await api.ClientCluster.LoadClusterSlotReferencesAsync(cluster);
            }
            next = full.ParentWellBoreID;
        }
    }

    private static void Copy<T>(T source, T target)
    {
        foreach (var property in typeof(T).GetProperties().Where(property => property.CanWrite))
            property.SetValue(target, property.GetValue(source));
    }
}
