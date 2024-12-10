using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Hercules.DB.Tests
{
    public class DatabaseCacheStub : IDatabaseCache
    {
        private string? lastSequence;
        private readonly List<CacheEntry> entries;
        private readonly Dictionary<string, string> revisions;

        public DatabaseCacheStub(IEnumerable<CacheEntry> entries, string lastSequence)
        {
            this.lastSequence = lastSequence;
            this.entries = entries.ToList();
            this.revisions = this.entries.GroupBy(entry => entry.DocumentId).ToDictionary(g => g.Key, g => g.OrderByDescending(entry => CouchUtils.GetRevisionNumber(entry.Revision)).First().Revision);
        }

        public DatabaseCacheStub(IEnumerable<ImmutableJsonObject> entries, string lastSequence)
            : this(entries.Select(entry => new CacheEntry(CouchUtils.GetId(entry), CouchUtils.GetRevision(entry), entry)), lastSequence)
        {
        }

        public IEnumerable<CacheEntry> LoadCache(CancellationToken cancellationToken)
        {
            return entries.GroupBy(entry => entry.DocumentId).Select(g => g.OrderByDescending(entry => CouchUtils.GetRevisionNumber(entry.Revision)).First());
        }

        public void DeleteDocument(string docId)
        {
            entries.RemoveAll(entry => entry.DocumentId == docId);
            revisions.Remove(docId);
        }

        public string ReadLastSequence() => lastSequence ?? "0";

        public string? ReadRevision(string docId)
        {
            ArgumentNullException.ThrowIfNull(nameof(docId));

            return revisions.GetValueOrDefault(docId);
        }

        public void Reset()
        {
            lastSequence = null;
            entries.Clear();
            revisions.Clear();
        }

        public ImmutableJsonObject? TryReadDocument(string docId, string revision)
        {
            ArgumentNullException.ThrowIfNull(nameof(docId));
            ArgumentNullException.ThrowIfNull(nameof(revision));

            return entries.FirstOrDefault(entry => entry.DocumentId == docId && entry.Revision == revision)?.Data;
        }

        public void WriteDocument(ImmutableJsonObject data)
        {
            ArgumentNullException.ThrowIfNull(nameof(data));

            var id = CouchUtils.GetId(data);
            var revision = CouchUtils.GetRevision(data);
            entries.Add(new CacheEntry(id, revision, data));
        }

        public void WriteLastSequence(string seq) => lastSequence = seq;

        public void WriteRevision(string docId, string revision)
        {
            ArgumentNullException.ThrowIfNull(nameof(docId));
            ArgumentNullException.ThrowIfNull(nameof(revision));

            revisions[docId] = revision;
        }

        public string SaveAttachment(string docId, string attachmentName, int revpos, Stream stream)
        {
            throw new NotImplementedException();
        }

        public bool TryGetAttachmentFile(string docId, string attachmentName, int revpos, out string fileName)
        {
            throw new NotImplementedException();
        }
    }
}