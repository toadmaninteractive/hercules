using Json;
using System;
using System.Collections.Generic;

namespace Hercules.Documents
{
    public sealed class DocumentRevision
    {
        public ImmutableJsonObject Json { get; }
        public string Rev { get; }
        public int RevNumber { get; }
        public IReadOnlyList<AttachmentRevision> Attachments { get; }
        public HerculesMetadata Metadata { get; }

        public DocumentRevision(ImmutableJsonObject json, IReadOnlyList<AttachmentRevision> attachments)
        {
            ArgumentNullException.ThrowIfNull(nameof(json));
            ArgumentNullException.ThrowIfNull(nameof(attachments));
            Json = json;
            Attachments = attachments;
            Rev = CouchUtils.GetRevision(Json);
            RevNumber = CouchUtils.GetRevisionNumber(Rev);
            Metadata = MetadataHelper.GetMetadata(json, RevNumber);
        }
    }

    public class DocumentRevisionEqualityComparer : EqualityComparer<DocumentRevision>
    {
        private static readonly string[] IgnoredKeys = { "_id", "_rev", "hercules_metadata", "_attachments" };

        private bool CompareAttachments(IReadOnlyCollection<AttachmentRevision> attachments1, IReadOnlyCollection<AttachmentRevision> attachments2)
        {
            if (attachments1.Count == 0 && attachments2.Count == 0)
                return true;
            foreach (var (a1, a2) in attachments1.ZipByKey(attachments2, a => a.Name))
            {
                if (a1 == null || a2 == null)
                    return false;
                if (a1.Length != a2.Length)
                    return false;
                // FIXME: equal digests mean that attachments are same, but not equal digest may still happen for the same attachments. So we may need to compare file content.
                if (a1.Digest != a2.Digest)
                    return false;
            }
            return true;
        }

        public override bool Equals(DocumentRevision? doc1, DocumentRevision? doc2)
        {
            if (doc1 is null && doc2 is null)
                return true;
            if (doc1 is null || doc2 is null)
                return false;
            var data1 = new JsonObject(doc1.Json);
            var data2 = new JsonObject(doc2.Json);
            foreach (var key in IgnoredKeys)
            {
                data1.Remove(key);
                data2.Remove(key);
            }
            var equalJson = data1.ToImmutable().Equals(data2.ToImmutable());
            var equalAttachments = CompareAttachments(doc1.Attachments, doc2.Attachments);
            return equalJson && equalAttachments;
        }

        public override int GetHashCode(DocumentRevision obj)
        {
            throw new NotImplementedException();
        }
    }
}
