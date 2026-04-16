using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NORCE.Drilling.GlobalAntiCollision
{
    /// <summary>
    /// Serializes the separation factor profile while remaining backward compatible
    /// with the historical three-value JSON shape.
    /// </summary>
    public sealed class SeparationFactorProfileJsonConverter : JsonConverter<List<SeparationFactorPoint>>
    {
        public override List<SeparationFactorPoint> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected the separation factor profile to be an array.");
            }

            List<SeparationFactorPoint> result = [];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return result;
                }

                result.Add(ReadPoint(ref reader));
            }

            throw new JsonException("Unexpected end of JSON while reading the separation factor profile.");
        }

        public override void Write(Utf8JsonWriter writer, List<SeparationFactorPoint> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (SeparationFactorPoint point in value)
            {
                writer.WriteStartObject();
                writer.WriteNumber("ReferenceMD", point.ReferenceMD);
                writer.WriteNumber("ComparisonMD", point.ComparisonMD);
                writer.WriteNumber("SeparationFactor", point.SeparationFactor);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static SeparationFactorPoint ReadPoint(ref Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.StartObject => ReadPointFromObject(ref reader),
                JsonTokenType.StartArray => ReadPointFromArray(ref reader),
                _ => throw new JsonException("Expected each separation factor entry to be an object or array.")
            };
        }

        private static SeparationFactorPoint ReadPointFromObject(ref Utf8JsonReader reader)
        {
            double? referenceMD = null;
            double? comparisonMD = null;
            double? separationFactor = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (referenceMD.HasValue && comparisonMD.HasValue && separationFactor.HasValue)
                    {
                        return new SeparationFactorPoint(referenceMD.Value, comparisonMD.Value, separationFactor.Value);
                    }

                    throw new JsonException("Each separation factor object must define all three values.");
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected a property name in the separation factor object.");
                }

                string? propertyName = reader.GetString();
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading a separation factor object.");
                }

                double numericValue = reader.GetDouble();
                switch (propertyName)
                {
                    case "Left":
                    case "ReferenceMD":
                    case "Item1":
                        referenceMD = numericValue;
                        break;
                    case "Middle":
                    case "ComparisonMD":
                    case "Item2":
                        comparisonMD = numericValue;
                        break;
                    case "Right":
                    case "SeparationFactor":
                    case "Item3":
                        separationFactor = numericValue;
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON while reading a separation factor object.");
        }

        private static SeparationFactorPoint ReadPointFromArray(ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading a separation factor array.");
            }
            double referenceMD = reader.GetDouble();

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading a separation factor array.");
            }
            double comparisonMD = reader.GetDouble();

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading a separation factor array.");
            }
            double separationFactor = reader.GetDouble();

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException("Each separation factor array must contain exactly three values.");
            }

            return new SeparationFactorPoint(referenceMD, comparisonMD, separationFactor);
        }
    }
}
