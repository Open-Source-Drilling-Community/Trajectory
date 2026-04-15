namespace NORCE.Drilling.Trajectory.ModelShared
{
    internal sealed class AnnotatedAbscissaJsonConverter : System.Text.Json.Serialization.JsonConverter<AnnotatedAbscissa>
    {
        public override AnnotatedAbscissa? Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
            {
                return new AnnotatedAbscissa { Abscissa = reader.GetDouble() };
            }

            if (reader.TokenType != System.Text.Json.JsonTokenType.StartObject)
            {
                throw new System.Text.Json.JsonException("AnnotatedAbscissa must be a number or an object.");
            }

            AnnotatedAbscissa result = new();
            while (reader.Read())
            {
                if (reader.TokenType == System.Text.Json.JsonTokenType.EndObject)
                {
                    return result;
                }

                if (reader.TokenType != System.Text.Json.JsonTokenType.PropertyName)
                {
                    throw new System.Text.Json.JsonException("Unexpected token while reading AnnotatedAbscissa.");
                }

                string? propertyName = reader.GetString();
                reader.Read();

                if (string.Equals(propertyName, nameof(AnnotatedAbscissa.Abscissa), System.StringComparison.OrdinalIgnoreCase))
                {
                    result.Abscissa = reader.TokenType == System.Text.Json.JsonTokenType.Number ? reader.GetDouble() : 0.0;
                }
                else if (string.Equals(propertyName, nameof(AnnotatedAbscissa.Annotation), System.StringComparison.OrdinalIgnoreCase))
                {
                    result.Annotation = reader.TokenType == System.Text.Json.JsonTokenType.Null ? null : reader.GetString();
                }
                else
                {
                    using System.Text.Json.JsonDocument _ = System.Text.Json.JsonDocument.ParseValue(ref reader);
                }
            }

            throw new System.Text.Json.JsonException("Unexpected end of AnnotatedAbscissa payload.");
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, AnnotatedAbscissa value, System.Text.Json.JsonSerializerOptions options)
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
