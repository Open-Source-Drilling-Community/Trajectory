using System.Text.Json;
using System.Text.Json.Serialization;
using OSDC.DotnetLibraries.General.Math;
namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Versioned, row-major lossless transport for package-owned matrices. Legacy dimension-only matrices remain unavailable.</summary>
public sealed class MatrixJsonConverter<T> : JsonConverter<T> where T : class, IMatrix, new()
{
    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader); var root = doc.RootElement; var result = new T();
        if (!root.TryGetProperty("Values", out var values)) return result;
        if (!root.TryGetProperty("FormatVersion", out var version) || version.GetInt32() != 1 || values.ValueKind != JsonValueKind.Array || values.GetArrayLength() != 9)
            throw new JsonException("Expected matrix FormatVersion 1 with nine row-major Values.");
        int index = 0; foreach (var value in values.EnumerateArray())
        { double? number = value.ValueKind == JsonValueKind.Null ? null : value.GetDouble(); if (number is double n && !double.IsFinite(n)) throw new JsonException("Matrix values must be finite."); result[index % 3, index / 3] = number; index++; }
        if (result is SymmetricMatrix3x3) for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++) if (result[col, row] != result[row, col]) throw new JsonException("Covariance must be symmetric.");
        return result;
    }
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(); writer.WriteNumber("ColumnCount", 3); writer.WriteNumber("RowCount", 3); writer.WriteNumber("FormatVersion", 1); writer.WriteStartArray("Values");
        for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++) { if (value[col, row] is double number) { if (!double.IsFinite(number)) throw new JsonException("Matrix values must be finite."); writer.WriteNumberValue(number); } else writer.WriteNullValue(); }
        writer.WriteEndArray(); writer.WriteEndObject();
    }
}
