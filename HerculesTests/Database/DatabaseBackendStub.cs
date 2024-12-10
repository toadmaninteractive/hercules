using Hercules.Documents;
using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.DB
{
    public class DatabaseBackendStub : IDatabaseBackend
    {
        private readonly struct SaveResult
        {
            public string Id { get; }
            public string Rev { get; }

            public SaveResult(string id, string rev)
            {
                Id = id;
                Rev = rev;
            }
        }

        readonly IScheduler scheduler;
        readonly Subject<BackendChange> listenChanges;
        readonly Subject<SaveResult> saves;
        int listenSeq;
        long lastTick;

        public DatabaseBackendStub(IScheduler scheduler)
        {
            this.scheduler = scheduler;
            listenChanges = new Subject<BackendChange>();
            saves = new Subject<SaveResult>();
        }

        public Task<IReadOnlyList<BackendChange>> GetChangesAsync(string? sinceSeq, bool includeDocs, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableJsonObject> GetDocumentAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDocumentAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<string> SaveDocumentAsync(string documentId, ImmutableJsonObject json, IReadOnlyList<Stream> attachments, CancellationToken cancellationToken)
        {
            var result = await saves.FirstAsync(save => save.Id == documentId);
            return result.Rev;
        }

        public Task<Stream> LoadAttachmentAsync(string documentId, string attachmentName, string rev, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<RevisionInfo>> GetDocumentRevisionsAsync(string documentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DocumentDraft> GetDeletedDocumentAsync(string documentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IObservable<BackendChange> ListenForChanges(string sinceSeq)
        {
            this.listenSeq = int.Parse(sinceSeq, CultureInfo.InvariantCulture);
            return listenChanges;
        }

        public void PushUpdate(ImmutableJsonObject data, long tick = 0)
        {
            listenSeq++;
            PushChange(new BackendChange(listenSeq.ToString(CultureInfo.InvariantCulture), CouchUtils.GetId(data), false, data, CouchUtils.GetRevision(data)));
        }

        public void PushDelete(string id)
        {
            listenSeq++;
            PushChange(new BackendChange(listenSeq.ToString(CultureInfo.InvariantCulture), id, true, null, null));
        }

        public void PushChange(BackendChange change)
        {
            Push(() => listenChanges.OnNext(change));
        }

        public void PushSaved(string id, string revision)
        {
            Push(() => saves.OnNext(new SaveResult(id, revision)));
        }

        public void Push(Action action)
        {
            lastTick += 10;
            scheduler.Schedule(TimeSpan.FromTicks(lastTick), action);
        }
    }
}