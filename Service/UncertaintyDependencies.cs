using System.Security.Cryptography;
using System.Text.Json;
namespace OSDC.Drilling.Trajectory.Service;

public sealed class UncertaintyDependencies : IDisposable
{
    private static readonly AsyncLocal<UncertaintyDependencies?> Current = new();
    public Dictionary<string, string> Hashes { get; } = [];
    public Dictionary<string, JsonElement> Snapshots { get; } = [];
    public UncertaintyDependencies() { if (Current.Value != null) throw new InvalidOperationException("An uncertainty repair is already capturing dependencies."); Current.Value = this; }
    public static void Track(string kind, Guid id, object data)
    {
        if (Current.Value is not { } capture) return; string key = kind + "/" + id, hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(data, JsonSettings.Options)));
        if (capture.Hashes.TryGetValue(key, out string? previous) && previous != hash) throw new InvalidOperationException("An uncertainty source changed during calculation: " + key); capture.Hashes[key] = hash;
        capture.Snapshots[key]=JsonSerializer.SerializeToElement(data,JsonSettings.Options);
    }
    public async Task RevalidateAsync()
    {
        foreach (string key in Hashes.Keys.ToArray())
        {
            string[] parts = key.Split('/'); Guid id = Guid.Parse(parts[1]); object value = parts[0] switch
            {
                "WellBore" => await APIUtils.ClientWellBore.GetWellBoreByIdAsync(id),
                "Well" => await APIUtils.ClientWell.GetWellByIdAsync(id),
                "Cluster" => await APIUtils.ClientCluster.GetClusterByIdAsync(id),
                "SurveyInstrument" => await APIUtils.ClientSurveyInstrument.GetSurveyInstrumentByIdAsync(id),
                _ => throw new InvalidOperationException("Unknown uncertainty source")
            };
            Track(parts[0], id, value);
        }
    }
    public void Dispose() { if (Current.Value == this) Current.Value = null; }
}
