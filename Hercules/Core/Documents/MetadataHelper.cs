using Json;
using System;

namespace Hercules.Documents
{
    public sealed class MetadataDraft
    {
        public string Timestamp { get; }
        public DateTime Time { get; }
        public string User { get; }

        public MetadataDraft(string user) : this(user, DateTime.UtcNow)
        {
        }

        public MetadataDraft(string user, DateTime time)
        {
            User = user;
            Time = time;
            Timestamp = MetadataHelper.FormatTimestamp(time);
        }
    }

    public static class MetadataHelper
    {
        public static string FormatTimestamp(DateTime time)
        {
            return time.ToString("yyyyMMddHHmmssffff");
        }

        public static void SetMetadata(JsonObject json, HerculesMetadata metadata)
        {
            json["hercules_metadata"] = HerculesMetadataJsonSerializer.Instance.Serialize(metadata);
        }

        public static void RemoveMetadata(JsonObject json)
        {
            json.Remove("hercules_metadata");
        }

        public static HerculesMetadata MakeMetadata(MetadataDraft metadataDraft, string? revision)
        {
            return new HerculesMetadata(metadataDraft.Time, metadataDraft.Timestamp, metadataDraft.User, revision);
        }

        static int GetExpectedRevisionNumber(HerculesMetadata metadata)
        {
            if (metadata.PrevRev == null)
                return 1;
            else
                return CouchUtils.GetRevisionNumber(metadata.PrevRev) + 1;
        }

        public static HerculesMetadata GetMetadata(ImmutableJson json, int revNumber)
        {
            if (json.IsObject && json.AsObject.TryGetValue("hercules_metadata", out var jsonMetadata))
            {
                var metadata = HerculesMetadataJsonSerializer.Instance.Deserialize(jsonMetadata);
                if (GetExpectedRevisionNumber(metadata) == revNumber)
                    return metadata;
                else
                    return new HerculesMetadata(metadata.Time, metadata.Timestamp, null, null);
            }
            return HerculesMetadata.Empty;
        }
    }
}
