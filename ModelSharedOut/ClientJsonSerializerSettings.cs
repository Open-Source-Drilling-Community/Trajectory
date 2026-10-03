namespace OSDC.Drilling.Trajectory.ModelShared;

public partial class Client
{
    static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        // Full replacement requests deserialize omitted nullable properties to the same null value.
        // Omitting them avoids transmitting result-shaped contracts as rows of null members when an
        // editor intentionally submits only its compact input projection.
        settings.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // NSwag cannot currently attach an item converter to collections of enums.
        // Rig payloads contain string-valued StationKeepingMode collections, so apply
        // the same string-enum convention globally to generated client payloads.
        settings.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }
}
