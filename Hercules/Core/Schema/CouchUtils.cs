using Json;
using System;
using System.Globalization;

namespace Hercules
{
    public static class CouchUtils
    {
        public const string SchemaDocumentId = "schema";
        public const string DiagramSchemaDocumentId = "diagram_schema";
        public const string HerculesBase = "hercules_base";
        public static readonly string[] BaseJsonExcludedKeys = { "_rev", "_attachments", HerculesBase, "hercules_metadata" };

        public static int GetRevisionNumber(string? revision)
        {
            if (string.IsNullOrEmpty(revision))
                return -1;
            var span = revision.AsSpan();
            return int.Parse(span.Slice(0, span.IndexOf('-')), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static string GetRevision(ImmutableJsonObject json) => json["_rev"].AsString;

        public static string GetId(ImmutableJsonObject json) => json["_id"].AsString;

        public static void SetId(JsonObject json, string id) => json["_id"] = id;

        public static string? GetCategory(ImmutableJsonObject json, string categoryTag)
        {
            if (json.TryGetValue(categoryTag, out var result))
            {
                if (!result.IsString)
                    return null;
                return result.AsString;
            }
            else
                return null;
        }

        public static string? GetScope(ImmutableJsonObject json)
        {
            if (json.TryGetValue("scope", out var result))
            {
                if (!result.IsString)
                    return null;
                return result.AsString;
            }
            else
                return null;
        }

        public static void SetRevision(JsonObject json, string revision)
        {
            json["_rev"] = revision;
        }

        public static ImmutableJsonObject? GetBase(ImmutableJsonObject json)
        {
            if (json.TryGetValue(HerculesBase, out var result) && result.IsObject)
                return result.AsObject;
            else
                return null;
        }
    }
}
