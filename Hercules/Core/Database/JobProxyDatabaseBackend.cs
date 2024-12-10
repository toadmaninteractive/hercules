using Hercules.Documents;
using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.DB
{
    public class JobProxyDatabaseBackend : IDatabaseBackend
    {
        public IDatabaseBackend BaseBackend { get; }
        public BackgroundJobScheduler Jobs { get; }

        public JobProxyDatabaseBackend(IDatabaseBackend baseBackend, BackgroundJobScheduler jobs)
        {
            this.BaseBackend = baseBackend;
            this.Jobs = jobs;
        }

        public Task<IReadOnlyList<BackendChange>> GetChangesAsync(string? sinceSeq, bool includeDocs, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.GetChangesAsync(sinceSeq, includeDocs, cancellationToken), "Load recent database changes", cancellationToken);
        }

        public Task<ImmutableJsonObject> GetDocumentAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.GetDocumentAsync(documentId, rev, cancellationToken), $"Load '{documentId}' document revision #{rev}", cancellationToken);
        }

        public Task DeleteDocumentAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.DeleteDocumentAsync(documentId, rev, cancellationToken), $"Delete {documentId}", cancellationToken);
        }

        public Task<string> SaveDocumentAsync(string documentId, ImmutableJsonObject json, IReadOnlyList<Stream> attachments, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.SaveDocumentAsync(documentId, json, attachments, cancellationToken), $"Save {documentId}", cancellationToken);
        }

        public Task<Stream> LoadAttachmentAsync(string documentId, string attachmentName, string rev, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.LoadAttachmentAsync(documentId, attachmentName, rev, cancellationToken), $"Load attachment {documentId}.{attachmentName}", cancellationToken);
        }

        public Task<IReadOnlyList<RevisionInfo>> GetDocumentRevisionsAsync(string documentId, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.GetDocumentRevisionsAsync(documentId, cancellationToken), $"Load '{documentId}' revision list", cancellationToken);
        }

        public Task<DocumentDraft> GetDeletedDocumentAsync(string documentId, CancellationToken cancellationToken)
        {
            return Jobs.Schedule(() => BaseBackend.GetDeletedDocumentAsync(documentId, cancellationToken), $"Load deleted document '{documentId}'", cancellationToken);
        }

        public IObservable<BackendChange> ListenForChanges(string sinceSeq)
        {
            return Jobs.Schedule(BaseBackend.ListenForChanges(sinceSeq), "Listen for database changes");
        }
    }
}
