using Hercules.Documents;
using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.DB
{
    public record BackendChange(string Seq, string Id, bool Deleted, ImmutableJsonObject? Doc, string? Rev);

    public enum RevisionStatus
    {
        Available = 1,
        Missing = 2,
        Deleted = 3,
    }

    public record RevisionInfo(string Rev, RevisionStatus Status);

    public interface IDatabaseBackend
    {
        Task<IReadOnlyList<BackendChange>> GetChangesAsync(string? sinceSeq, bool includeDocs, CancellationToken cancellationToken);

        Task<ImmutableJsonObject> GetDocumentAsync(string documentId, string rev, CancellationToken cancellationToken);

        Task DeleteDocumentAsync(string documentId, string rev, CancellationToken cancellationToken);

        Task<string> SaveDocumentAsync(string documentId, ImmutableJsonObject json, IReadOnlyList<Stream> attachments, CancellationToken cancellationToken);

        Task<Stream> LoadAttachmentAsync(string documentId, string attachmentName, string rev, CancellationToken cancellationToken);

        Task<IReadOnlyList<RevisionInfo>> GetDocumentRevisionsAsync(string documentId, CancellationToken cancellationToken);

        Task<DocumentDraft> GetDeletedDocumentAsync(string documentId, CancellationToken cancellationToken);

        IObservable<BackendChange> ListenForChanges(string sinceSeq);
    }
}
