using Json;
using Json.Serialization;
using System;

namespace CouchDB.Api
{
    internal class UriJsonSerializer : IJsonSerializer<Uri>
    {
        public Uri Deserialize(ImmutableJson json) => new(json.AsString);

        public ImmutableJson Serialize(Uri value) => value.ToString();

        public static readonly UriJsonSerializer Instance = new();
    }
}
