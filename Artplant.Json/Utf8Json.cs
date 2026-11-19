using System.Collections.Generic;
using System.Text.Json;

namespace Json
{
    public static class Utf8Json
    {
        public static ImmutableJson Parse(ref Utf8JsonReader reader)
        {
            if (reader.Read())
                return GetValue(ref reader);
            else
                throw new JsonException("Unexpected end of JSON");
        }

        private static ImmutableJson GetValue(ref Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => ImmutableJson.Null,
                JsonTokenType.True => ImmutableJson.True,
                JsonTokenType.False => ImmutableJson.False,
                JsonTokenType.Number => GetNumber(ref reader),
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartArray => GetArray(ref reader),
                JsonTokenType.StartObject => GetObject(ref reader),
                _ => throw new JsonException($"Unexpected token {reader.TokenType}"),
            };
        }

        private static ImmutableJson GetNumber(ref Utf8JsonReader reader)
        {
            if (reader.TryGetInt32(out var intValue))
                return intValue;
            else if (reader.TryGetInt64(out var longValue))
                return longValue;
            else
                return reader.GetDouble();
        }

        private static ImmutableJson GetObject(ref Utf8JsonReader reader)
        {
            Dictionary<string, ImmutableJson>? jsonObject = null;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        {
                            var propertyName = reader.GetString();
                            var propertyValue = Parse(ref reader);
                            jsonObject ??= new Dictionary<string, ImmutableJson>();
                            jsonObject[propertyName] = propertyValue;
                        }
                        break;
                    case JsonTokenType.EndObject:
                        return jsonObject != null ? new ImmutableJsonObject(jsonObject) : ImmutableJson.EmptyObject;
                    default:
                        throw new JsonException($"Unexpected token {reader.TokenType}");
                }
            }
            throw new JsonException("Unexpected end of JSON");
        }

        private static ImmutableJson GetArray(ref Utf8JsonReader reader)
        {
            List<ImmutableJson>? jsonArray = null;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return jsonArray != null ? new ImmutableJsonArray(jsonArray) : ImmutableJson.EmptyArray;
                    default:
                        jsonArray ??= new List<ImmutableJson>();
                        jsonArray.Add(GetValue(ref reader));
                        break;
                }
            }
            throw new JsonException("Unexpected end of JSON");
        }
    }
}
