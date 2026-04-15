using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// An interpolation abscissa and its optional annotation.
    /// </summary>
    [JsonConverter(typeof(AnnotatedAbscissaJsonConverter))]
    public class AnnotatedAbscissa
    {
        public double Abscissa { get; set; }
        public string? Annotation { get; set; }
    }

    internal sealed class AnnotatedAbscissaJsonConverter : JsonConverter<AnnotatedAbscissa>
    {
        public override AnnotatedAbscissa? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return new AnnotatedAbscissa { Abscissa = reader.GetDouble() };
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("AnnotatedAbscissa must be a number or an object.");
            }

            AnnotatedAbscissa result = new();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return result;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Unexpected token while reading AnnotatedAbscissa.");
                }

                string? propertyName = reader.GetString();
                reader.Read();

                if (string.Equals(propertyName, nameof(AnnotatedAbscissa.Abscissa), StringComparison.OrdinalIgnoreCase))
                {
                    result.Abscissa = reader.TokenType == JsonTokenType.Number ? reader.GetDouble() : 0.0;
                }
                else if (string.Equals(propertyName, nameof(AnnotatedAbscissa.Annotation), StringComparison.OrdinalIgnoreCase))
                {
                    result.Annotation = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                }
                else
                {
                    using JsonDocument _ = JsonDocument.ParseValue(ref reader);
                }
            }

            throw new JsonException("Unexpected end of AnnotatedAbscissa payload.");
        }

        public override void Write(Utf8JsonWriter writer, AnnotatedAbscissa value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(AnnotatedAbscissa.Abscissa), value.Abscissa);
            if (!string.IsNullOrWhiteSpace(value.Annotation))
            {
                writer.WriteString(nameof(AnnotatedAbscissa.Annotation), value.Annotation);
            }
            else
            {
                writer.WriteNull(nameof(AnnotatedAbscissa.Annotation));
            }
            writer.WriteEndObject();
        }
    }
}
