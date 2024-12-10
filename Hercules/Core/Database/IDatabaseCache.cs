using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Hercules.DB
{
    public record CacheEntry(string DocumentId, string Revision, ImmutableJsonObject Data);

    public class CorruptedCacheEntryException : Exception
    {
        public string FileName { get; }

        public CorruptedCacheEntryException(string fileName, Exception innerException)
            : base($"Corrupted cache entry {fileName}: {innerException.Message}", innerException)
        {
            this.FileName = fileName;
        }
    }

    public interface IDatabaseCache
    {
        IEnumerable<CacheEntry> LoadCache(CancellationToken cancellationToken);

        void DeleteDocument(string docId);

        string ReadLastSequence();

        string? ReadRevision(string docId);

        void Reset();

        string SaveAttachment(string docId, string attachmentName, int revpos, Stream stream);

        bool TryGetAttachmentFile(string docId, string attachmentName, int revpos, out string fileName);

        ImmutableJsonObject? TryReadDocument(string docId, string revision);

        void WriteDocument(ImmutableJsonObject data);

        void WriteLastSequence(string seq);

        void WriteRevision(string docId, string revision);
    }
}
