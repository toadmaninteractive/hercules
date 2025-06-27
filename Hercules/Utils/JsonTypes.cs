using Hercules;
using Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Json
{
    public static class SafeJson
    {
        public static ImmutableJson? GetField(ImmutableJson? json, string key)
        {
            ImmutableJson? result = null;
            if (json != null && json.IsObject)
                json.AsObject.TryGetValue(key, out result);
            return result;
        }

        public static ImmutableJson? GetItem(ImmutableJson? json, int index)
        {
            ImmutableJson? result = null;
            if (json != null && json.IsArray && json.AsArray.Count > index)
                result = json.AsArray[index];
            return result;
        }
    }

    public static class JsonTypes
    {
        [DebuggerStepThrough]
        public static ImmutableJson? GetValueSafe(this ImmutableJson json, string key)
        {
            return json.AsObject.TryGetValue(key, out var value) ? value : null;
        }

        [DebuggerStepThrough]
        public static string ToStringSafe(this ImmutableJson? json)
        {
            if (json == null)
                return "null";
            else
                return json.ToString();
        }

        [DebuggerStepThrough]
        public static string? AsStringOrNull(this ImmutableJson? json)
        {
            if (json == null || !json.IsString)
                return null;
            else
                return json.AsString;
        }

        public static Optional<T> TryDeserialize<T>(this IJsonSerializer<T> serializer, ImmutableJson? json)
        {
            if (json == null || json.IsNull)
                return Optional<T>.None;
            try
            {
                var data = serializer.Deserialize(json);
                return Optional.Some(data);
            }
            catch
            {
                return Optional<T>.None;
            }
        }

        public static ImmutableJson? TryParse(string? jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return null;
            try
            {
                return JsonParser.Parse(jsonString);
            }
            catch
            {
                return null;
            }
        }

        public static ImmutableJsonObject WithoutKeys(this ImmutableJsonObject json, IEnumerable<string> keys)
        {
            var jsonObject = json.ToMutable();
            foreach (var key in keys)
                jsonObject.Remove(key);
            return jsonObject.ToImmutable();
        }

        public static ImmutableJsonObject WithoutKeys(this ImmutableJsonObject json, params string[] keys)
        {
            var jsonObject = json.ToMutable();
            foreach (var key in keys)
                jsonObject.Remove(key);
            return jsonObject.ToImmutable();
        }

        public static JsonObject WithoutKeys(this JsonObject jsonObject, params string[] keys)
        {
            foreach (var key in keys)
                jsonObject.Remove(key);
            return jsonObject;
        }

        public static JsonElement ToJsonElement(this ImmutableJson json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json.ToString());
        }

        public static ImmutableJson ToImmutableJson(this JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    var jsonObject = new JsonObject();
                    foreach (var item in jsonElement.EnumerateObject())
                    {
                        jsonObject.Add(item.Name, item.Value.ToImmutableJson());
                    }
                    return jsonObject.ToImmutable();
                case JsonValueKind.Array:
                    var jsonArray = new JsonArray();
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        jsonArray.Add(item.ToImmutableJson());
                    }
                    return jsonArray.ToImmutable();
                case JsonValueKind.String:
                    return ImmutableJson.Create(jsonElement.GetString()!);
                case JsonValueKind.Number:
                    return ImmutableJson.Create(jsonElement.GetDouble()!);
                case JsonValueKind.True:
                    return ImmutableJson.Null;
                case JsonValueKind.False:
                    return ImmutableJson.False;
                default:
                    return ImmutableJson.Null;
            }
        }

        public static ImmutableJson ToImmutableJson(this System.Text.Json.Nodes.JsonNode jsonNode)
        {
            if (jsonNode is System.Text.Json.Nodes.JsonObject jsonObject)
            {
                var immutableObject = new JsonObject();
                foreach (var kvp in jsonObject)
                {
                    immutableObject[kvp.Key] = kvp.Value.ToImmutableJson();
                }
                return immutableObject;
            }
            else if (jsonNode is System.Text.Json.Nodes.JsonArray jsonArray)
            {
                var immutableArray = new JsonArray();
                foreach (var item in jsonArray)
                {
                    immutableArray.Add(item.ToImmutableJson());
                }
                return immutableArray;
            }
            else if (jsonNode is System.Text.Json.Nodes.JsonValue jsonValue)
            {
                switch (jsonValue.GetValueKind())
                {
                    case JsonValueKind.String:
                        return ImmutableJson.Create(jsonValue.GetValue<string>()!);
                    case JsonValueKind.Number:
                        return ImmutableJson.Create(jsonValue.GetValue<double>());
                    case JsonValueKind.True:
                        return ImmutableJson.True;
                    case JsonValueKind.False:
                        return ImmutableJson.False;
                    default:
                        return ImmutableJson.Null;
                }
            }
            else
            {
                return ImmutableJson.Null;
            }
        }
    }

    public class JsonPathSerializer : IJsonSerializer<JsonPath>
    {
        public JsonPath Deserialize(ImmutableJson json) => JsonPath.Parse(json.AsString);

        public ImmutableJson Serialize(JsonPath value) => value.ToString();

        public static readonly JsonPathSerializer Instance = new JsonPathSerializer();
    }

    public class JsonPartialEqualityComparer : EqualityComparer<ImmutableJson>
    {
        public IReadOnlyList<string>? OnlyKeys { get; }
        public IReadOnlyList<string>? ExcludeKeys { get; }

        public JsonPartialEqualityComparer(IReadOnlyList<string>? onlyKeys = null, IReadOnlyList<string>? excludeKeys = null)
        {
            OnlyKeys = onlyKeys;
            ExcludeKeys = excludeKeys;
        }

        public override bool Equals([AllowNull] ImmutableJson x, [AllowNull] ImmutableJson y)
        {
            if (x != null && x.IsObject && y != null && y.IsObject)
            {
                var xObj = x.AsObject;
                var yObj = y.AsObject;
                if (OnlyKeys != null)
                {
                    foreach (var key in OnlyKeys)
                    {
                        if (ExcludeKeys != null && ExcludeKeys.Contains(key))
                            continue;

                        var xValue = xObj.GetValueOrDefault(key);
                        var yValue = yObj.GetValueOrDefault(key);
                        if (!ImmutableJson.Equals(xValue, yValue))
                            return false;
                    }
                    return true;
                }
                else if (ExcludeKeys != null)
                {
                    foreach (var key in xObj.Keys.Concat(yObj.Keys).Distinct())
                    {
                        if (ExcludeKeys.Contains(key))
                            continue;

                        var xValue = xObj.GetValueOrDefault(key);
                        var yValue = yObj.GetValueOrDefault(key);
                        if (!ImmutableJson.Equals(xValue, yValue))
                            return false;
                    }

                    return true;
                }
                else
                    return ImmutableJson.Equals(xObj, yObj);
            }
            else
                return ImmutableJson.Equals(x, y);
        }

        public override int GetHashCode([DisallowNull] ImmutableJson obj)
        {
            if (obj == null)
                return 0;
            if (obj.IsObject && OnlyKeys != null)
            {
                var asObject = obj.AsObject;
                var hashCode = 0;
                foreach (var key in OnlyKeys)
                {
                    var value = asObject.GetValueOrNull(key);
                    hashCode = HashCode.Combine(hashCode, value.GetHashCode());
                }
                return hashCode;
            }
            else
                return obj.GetHashCode();
        }
    }
}
