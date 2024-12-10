using Json;
using Json.Serialization;
using System;
using System.Globalization;

namespace Hercules.Documents
{
    public class HerculesMetadata
    {
        public DateTime? Time { get; }
        public string? Timestamp { get; }
        public string? User { get; }
        public string? PrevRev { get; }

        public HerculesMetadata(DateTime? time, string? timestamp, string? user, string? prevRev)
        {
            Time = time;
            Timestamp = timestamp;
            User = user;
            PrevRev = prevRev;
        }

        public static readonly HerculesMetadata Empty = new(null, null, null, null);
    }

    public class HerculesMetadataJsonSerializer : IJsonSerializer<HerculesMetadata>
    {
        public HerculesMetadata Deserialize(ImmutableJson json)
        {
            ArgumentNullException.ThrowIfNull(nameof(json));

            if (!json.IsObject)
                return HerculesMetadata.Empty;

            string? prevRev = null;
            string? user = null;
            string? timestamp = null;
            DateTime? time = null;
            var jsonObject = json.AsObject;
            if (jsonObject.TryGetValue("timestamp", out var timestampJson) && !timestampJson.IsNull)
                timestamp = JsonSerializer.String.Deserialize(timestampJson);
            if (jsonObject.TryGetValue("user", out var userJson) && !userJson.IsNull)
                user = JsonSerializer.String.Deserialize(userJson);
            if (jsonObject.TryGetValue("prev_rev", out var prevRevJson) && !prevRevJson.IsNull)
                prevRev = JsonSerializer.String.Deserialize(prevRevJson);
            if (timestamp != null && DateTime.TryParseExact(timestamp, "yyyyMMddHHmmssffff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timeValue))
                time = timeValue;
            return new HerculesMetadata(time, timestamp, user, prevRev);
        }

        public ImmutableJson Serialize(HerculesMetadata value)
        {
            var json = new JsonObject();
            if (value.Timestamp != null)
                json["timestamp"] = JsonSerializer.String.Serialize(value.Timestamp);
            if (value.User != null)
                json["user"] = JsonSerializer.String.Serialize(value.User);
            if (!string.IsNullOrEmpty(value.PrevRev))
                json["prev_rev"] = JsonSerializer.String.Serialize(value.PrevRev);
            return json;
        }

        public static readonly HerculesMetadataJsonSerializer Instance = new HerculesMetadataJsonSerializer();
    }
}
