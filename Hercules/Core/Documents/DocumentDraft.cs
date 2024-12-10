using CouchDB.Api;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Documents
{
    public sealed class DocumentDraft
    {
        public ImmutableJsonObject Json { get; }
        public IReadOnlyList<Attachment> Attachments { get; }

        public DocumentDraft(ImmutableJsonObject json, IReadOnlyList<Attachment>? attachments = null)
        {
            Json = json;
            Attachments = attachments ?? Array.Empty<Attachment>();
        }

        public ImmutableJsonObject GetFullDocumentJson(string documentId, string? revision, HerculesMetadata metadata)
        {
            var jsonObject = new JsonObject(Json)
            {
                ["_id"] = documentId
            };
            MetadataHelper.SetMetadata(jsonObject, metadata);
            if (revision != null)
                jsonObject["_rev"] = revision;
            else
                jsonObject.Remove("_rev");
            if (Attachments.Count == 0)
                jsonObject.Remove("_attachments");
            else
            {
                var jsonAttachments = new JsonObject(Attachments.Count);
                foreach (var attachment in Attachments)
                {
                    var cdb = new CdbAttachment();
                    if (attachment is AttachmentRevision existingAttachment)
                    {
                        cdb.Stub = true;
                        cdb.Revpos = existingAttachment.RevPos;
                        cdb.Digest = existingAttachment.Digest;
                        cdb.Length = (int)existingAttachment.Length;
                        cdb.ContentType = existingAttachment.ContentType;
                    }
                    else
                    {
                        var newAttachment = (AttachmentDraft)attachment;
                        cdb.ContentType = newAttachment.ContentType;
                        cdb.Length = (int)newAttachment.Length;
                        cdb.Follows = true;
                    }
                    jsonAttachments.Add(attachment.Name, CdbAttachmentJsonSerializer.Instance.Serialize(cdb));
                }
                jsonObject["_attachments"] = jsonAttachments;
            }
            return jsonObject.ToImmutable();
        }

        public static DocumentDraft CopyFrom(DocumentRevision revision, TempStorage tempStorage, DocumentRevision? currentRevision = null)
        {
            var attachments = revision.Attachments.Cast<Attachment>().ToList();
            for (int i = 0; i < attachments.Count; i++)
            {
                if (currentRevision == null || !currentRevision.Attachments.Contains(attachments[i]))
                    attachments[i] = AttachmentDraft.CopyFrom(attachments[i], tempStorage);
            }
            return new DocumentDraft(revision.Json, attachments);
        }
    }
}
