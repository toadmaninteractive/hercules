using Hercules.Documents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.DB
{
    public class DatabaseHistoryLoadedData
    {
        public DocumentCommit Revision { get; }
        public DocumentCommit? NextRevision { get; }
        public DocumentRevision? Snapshot { get; set; }

        public void Apply()
        {
            Revision.Snapshot = Snapshot;
            if (NextRevision != null)
                NextRevision.PreviousSnapshot = Snapshot;
        }

        public DatabaseHistoryLoadedData(DocumentCommit revision, DocumentCommit? nextRevision)
        {
            Revision = revision;
            NextRevision = nextRevision;
        }
    }

    public class DatabaseHistory
    {
        private Database Database { get; }

        public DatabaseHistory(Database database)
        {
            Database = database;
        }

        public async Task<IReadOnlyList<string>> GetDeletedDocumentsAsync(CancellationToken cancellationToken)
        {
            var changes = await Database.Backend.GetChangesAsync(null, false, cancellationToken).ConfigureAwait(false);
            return changes.Where(c => c.Deleted).Select(c => c.Id).ToList();
        }

        public Task<DocumentDraft> GetDeletedDocumentAsync(string documentId, CancellationToken cancellationToken)
        {
            return Database.Backend.GetDeletedDocumentAsync(documentId, cancellationToken);
        }

        public async Task<DocumentRevision> LoadRevisionAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            var json = await Task.Run(() => Database.Cache.TryReadDocument(documentId, rev), cancellationToken);
            if (json == null)
            {
                json = await Database.Backend.GetDocumentAsync(documentId, rev, cancellationToken);
                Database.Cache.WriteDocument(json);
            }
            return Database.ParseDocumentRevision(documentId, json);
        }

        public async Task<IReadOnlyList<DocumentCommit>> GetDocumentRevisionsAsync(string documentId, CancellationToken cancellationToken)
        {
            var revInfos = await Database.Backend.GetDocumentRevisionsAsync(documentId, cancellationToken).ConfigureAwait(false);
            return revInfos.Select(revInfo => new DocumentCommit(documentId, revInfo.Rev, revInfo.Status)).ToList();
        }

        public Task LoadDocumentRevisionsAsync(IReadOnlyList<DocumentCommit> revisions, IProgress<DatabaseHistoryLoadedData> progress, CancellationToken cancellationToken)
        {
            var nextRevs = new DocumentCommit?[] { null }.Concat(revisions);
            var pairs = revisions.Zip(nextRevs, (rev1, rev2) => new DatabaseHistoryLoadedData(rev1, rev2));
            return Task.WhenAll(pairs.Select(pair => LoadDocumentRevision(pair, progress, cancellationToken)));
        }

        async Task LoadDocumentRevision(DatabaseHistoryLoadedData data, IProgress<DatabaseHistoryLoadedData> progress, CancellationToken cancellationToken)
        {
            var rev = data.Revision;
            if (rev.IsLoaded || rev.Status != RevisionStatus.Available)
                return;
            data.Snapshot = await LoadRevisionAsync(rev.DocumentId, rev.Revision, cancellationToken);
            progress.Report(data);
        }

        public IObservable<DocumentCommit> GetHistory(DateTime since)
        {
            return Observable.Create<DocumentCommit>((observer, cancellationToken) => GetHistoryAsync(since, observer.OnNext, cancellationToken));
        }

        public async Task<IReadOnlyList<DocumentCommit>> GetHistoryAsync(DateTime since, CancellationToken ct)
        {
            ConcurrentQueue<DocumentCommit> result = new();
            await GetHistoryAsync(since, c => result.Enqueue(c), ct);
            return result.OrderByDescending(r => r.Time).ToArray();
        }

        Task GetHistoryAsync(DateTime since, Action<DocumentCommit> callback, CancellationToken cancellationToken)
        {
            return Task.WhenAll(Database.Documents.Values.Select(doc => GetDocumentHistoryAsync(doc, since, callback, cancellationToken)));
        }

        static DocumentCommit? GetCurrentRevision(IDocument document)
        {
            if (document.IsExisting)
                return new DocumentCommit(document.DocumentId, document.CurrentRevision.Rev, RevisionStatus.Available, document.CurrentRevision);
            else
                return null;
        }

        async Task<DocumentCommit?> GetPreviousRevision(DocumentCommit revision, CancellationToken cancellationToken)
        {
            if (revision.PreviousRevision == null)
                return null;
            try
            {
                var prevData = await LoadRevisionAsync(revision.DocumentId, revision.PreviousRevision, cancellationToken);
                return new DocumentCommit(revision.DocumentId, revision.PreviousRevision, RevisionStatus.Available, prevData);
            }
            catch (Exception)
            {
                return null;
            }
        }

        async Task GetDocumentHistoryAsync(IDocument document, DateTime since, Action<DocumentCommit> callback, CancellationToken cancellationToken)
        {
            DocumentCommit? revision = GetCurrentRevision(document);
            while (revision != null && revision.Time > since)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prevRevision = await GetPreviousRevision(revision, cancellationToken);
                if (prevRevision != null)
                    revision.PreviousSnapshot = prevRevision.Snapshot;
                callback(revision);
                revision = prevRevision;
            }
        }
    }
}
