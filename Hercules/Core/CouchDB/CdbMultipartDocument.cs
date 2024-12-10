using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CouchDB
{
    public class CdbMultipartDocumentAttachment
    {
        public string Name { get; private set; }
        public string ContentType { get; private set; }
        public Stream Content { get; private set; }

        public CdbMultipartDocumentAttachment(string name, string contentType, Stream content)
        {
            this.Name = name;
            this.ContentType = contentType;
            this.Content = content;
        }
    }

    public class CdbMultipartDocument
    {
        public ImmutableJson Json { get; }
        public IReadOnlyList<CdbMultipartDocumentAttachment> Attachments { get; }

        public CdbMultipartDocument(ImmutableJson json, IReadOnlyList<CdbMultipartDocumentAttachment> attachments)
        {
            Json = json;
            Attachments = attachments;
        }
    }

    internal sealed class MultipartRelatedReader
    {
        public Stream BaseStream { get; private set; }
        public bool Eof { get; private set; }

        readonly byte[] buffer;
        readonly string boundary;
        int bufferPos;
        int bufferLength;

        public MultipartRelatedReader(string boundary, Stream stream, int bufferSize = 4096)
        {
            this.boundary = "--" + boundary;
            BaseStream = stream;
            buffer = new byte[bufferSize];
        }

        public async Task<IDictionary<string, string>> ReadHeadersAsync(CancellationToken ct)
        {
            var dict = new Dictionary<string, string>();
            while (true)
            {
                var str = (await ReadLineAsync(ct).ConfigureAwait(false)).Trim();
                if (str.Length == 0)
                    break;
                var sep = str.IndexOf(":", StringComparison.Ordinal);
                dict.Add(str.Substring(0, sep).Trim(), str.Substring(sep + 1).Trim());
            }
            return dict;
        }

        public async Task ReadBoundaryAsync(CancellationToken ct)
        {
            while (true)
            {
                var str = (await ReadLineAsync(ct).ConfigureAwait(false)).Trim();
                if (str.Length == 0)
                    continue;
                if (str.StartsWith(boundary, StringComparison.Ordinal))
                {
                    Eof = str.EndsWith("--", StringComparison.Ordinal);
                    return;
                }
                throw new InvalidOperationException("Unexpected string in multipart/related content: " + str);
            }
        }

        public async Task<string> ReadTextPartAsync(CancellationToken ct)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var str = (await ReadLineAsync(ct).ConfigureAwait(false));
                if (str.StartsWith(boundary, StringComparison.Ordinal))
                {
                    Eof = str.EndsWith("--", StringComparison.Ordinal);
                    return sb.ToString();
                }
                sb.AppendLine(str);
            }
        }

        public async Task<Stream> ReadBinaryPartAsync(int size, CancellationToken ct)
        {
            var stream = new MemoryStream();
            var bytesFromBuffer = Math.Min(bufferLength - bufferPos, size);
            stream.Write(buffer, bufferPos, bytesFromBuffer);
            bufferPos += bytesFromBuffer;
            var bytesLeft = size - bytesFromBuffer;
            if (bytesLeft > 0)
            {
                var bytes = new byte[bytesLeft];
                await BaseStream.ReadExactlyAsync(bytes, 0, bytesLeft, ct).ConfigureAwait(false);
                stream.Write(bytes, 0, bytesLeft);
            }
            return stream;
        }

        private async Task<string> ReadLineAsync(CancellationToken ct)
        {
            using (var lineStream = new MemoryStream())
            {
                bool rFound = false;
                while (true)
                {
                    if (bufferPos == bufferLength)
                    {
                        bufferPos = 0;
                        bufferLength = await BaseStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                        if (bufferLength == 0)
                        {
                            var strbytes = lineStream.ToArray();
                            return Encoding.UTF8.GetString(strbytes, 0, strbytes.Length);
                        }
                    }
                    var b = buffer[bufferPos];
                    bufferPos++;
                    lineStream.WriteByte(b);
                    if (b == '\n' && rFound)
                    {
                        var strbytes = lineStream.ToArray();
                        return Encoding.UTF8.GetString(strbytes, 0, strbytes.Length - 2);
                    }
                    rFound = b == '\r';
                }
            }
        }
    }
}
