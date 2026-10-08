using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    /// <summary>
    /// The web UI was written against the Old API (Newtonsoft), which read a JSON number or boolean into a
    /// string property (for example changedBy: 10030). Output is unchanged.
    /// </summary>
    public sealed class LenientStringJsonConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray());
                case JsonTokenType.True:
                    return bool.TrueString;
                case JsonTokenType.False:
                    return bool.FalseString;
                default:
                    throw new JsonException($"Cannot convert a JSON {reader.TokenType} to a string.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);

        public override string ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetString()!;

        public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WritePropertyName(value);
    }

    /// <summary>
    /// Reads "true"/"false"/"1"/"0" strings and 0/1 numbers into bool, as Newtonsoft did.
    /// </summary>
    public sealed class LenientBooleanJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    return reader.TryGetInt64(out var whole) ? whole != 0 : reader.GetDouble() != 0;
                case JsonTokenType.String:
                    var text = reader.GetString()?.Trim();
                    if (bool.TryParse(text, out var parsed)) return parsed;
                    if (text == "1") return true;
                    if (text == "0") return false;
                    break;
            }
            throw new JsonException("The value must be true or false.");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteBooleanValue(value);
    }

    /// <summary>
    /// Reads an empty string into null for nullable numbers, booleans and dates, as Newtonsoft did
    /// (forms post "" for a blank optional field).
    /// </summary>
    public sealed class EmptyStringAsNullJsonConverterFactory : JsonConverterFactory
    {
        private static readonly HashSet<Type> Supported = new()
        {
            typeof(int), typeof(long), typeof(short), typeof(byte), typeof(decimal), typeof(double), typeof(float),
            typeof(bool), typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly)
        };

        public override bool CanConvert(Type typeToConvert)
        {
            var underlying = Nullable.GetUnderlyingType(typeToConvert);
            return underlying != null && Supported.Contains(underlying);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var underlying = Nullable.GetUnderlyingType(typeToConvert)!;
            return (JsonConverter)Activator.CreateInstance(typeof(EmptyStringAsNullConverter<>).MakeGenericType(underlying))!;
        }

        private sealed class EmptyStringAsNullConverter<T> : JsonConverter<T?> where T : struct
        {
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;
                if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString()))
                    return null;
                return JsonSerializer.Deserialize<T>(ref reader, options);
            }

            public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                    JsonSerializer.Serialize(writer, value.Value, options);
                else
                    writer.WriteNullValue();
            }
        }
    }
}
