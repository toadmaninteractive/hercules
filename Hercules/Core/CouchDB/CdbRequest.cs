using Json;
using Json.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CouchDB
{
    internal static class CdbRequest
    {
        public static async Task<ImmutableJson> GetJsonResponseAsync(this HttpContent content, CancellationToken cancellationToken)
        {
            if (content.Headers.ContentType.MediaType.StartsWith("multipart/mixed", StringComparison.OrdinalIgnoreCase))
            {
                var text = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var start = text.IndexOf("{", StringComparison.Ordinal);
                var end = text.LastIndexOf("}", StringComparison.Ordinal);
                return JsonParser.Parse(text.Substring(start, end - start + 1));
            }
            else
            {
                using var memoryStream = new MemoryStream((int)(content.Headers.ContentLength ?? 4096));
                await content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                return BytesToJson(memoryStream);
            }
        }

        public static async Task<T> GetJsonResponseAsync<T>(this HttpContent content, IJsonSerializer<T> serializer, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(nameof(serializer));

            return serializer.Deserialize(await content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false));
        }

        private static ImmutableJson BytesToJson(MemoryStream memoryStream)
        {
            var reader = new Utf8JsonReader(memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length));
            return Utf8Json.Parse(ref reader);
        }
    }
}
