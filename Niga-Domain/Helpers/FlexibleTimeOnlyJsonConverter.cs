using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Niga_Domain.Helpers
{
    /// <summary>
    /// Mobile clients send HH:mm; System.Text.Json TimeOnly only accepts HH:mm:ss by default.
    /// </summary>
    public sealed class FlexibleTimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        private static readonly string[] Formats =
        {
            "HH:mm:ss", "HH:mm", "H:mm:ss", "H:mm", "hh:mm tt", "h:mm tt", "hh:mm:ss tt", "h:mm:ss tt"
        };

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Time must be a string such as HH:mm or HH:mm:ss.");

            var text = reader.GetString();
            if (TryParse(text, out var parsed))
                return parsed;

            throw new JsonException("Time must be HH:mm or HH:mm:ss.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }

        internal static bool TryParse(string? text, out TimeOnly parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            text = text.Trim();
            if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;
            return TimeOnly.TryParseExact(text, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }
    }

    public sealed class FlexibleNullableTimeOnlyJsonConverter : JsonConverter<TimeOnly?>
    {
        public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Time must be a string such as HH:mm or HH:mm:ss.");
            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (FlexibleTimeOnlyJsonConverter.TryParse(text, out var parsed))
                return parsed;
            throw new JsonException("Time must be HH:mm or HH:mm:ss.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }
    }
}
