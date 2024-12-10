using CouchDB;
using CouchDB.Api;
using Hercules.Documents;
using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.DB
{
    public class CouchDatabaseBackend : IDatabaseBackend
    {
        public CdbDatabase CouchDatabase { get; }

        public TempStorage TempStorage { get; }

        public CouchDatabaseBackend(CdbDatabase couchDatabase, TempStorage tempStorage)
        {
            this.CouchDatabase = couchDatabase;
            this.TempStorage = tempStorage;
        }

        public async Task<IReadOnlyList<BackendChange>> GetChangesAsync(string? sinceSeq, bool includeDocs, CancellationToken cancellationToken)
        {
            var changes = await CouchDatabase.GetChangesAsync(since: sinceSeq, includeDocs: includeDocs, cancellationToken: cancellationToken).ConfigureAwait(false);
            return changes.Results.Select(CdbToBackendChange).ToList();
        }

        public async Task<IReadOnlyList<ImmutableJsonObject>> GetAllDocumentsAsync(CancellationToken cancellationToken)
        {
            var allDocs = await CouchDatabase.GetAllDocumentsAsync(includeDocs: true).ConfigureAwait(true);
            return allDocs.Rows.Select(doc => doc.Doc!.AsObject).ToList();
        }

        public async Task<ImmutableJsonObject> GetDocumentAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            return (await CouchDatabase.GetDocumentAsync(documentId, rev: rev, cancellationToken: cancellationToken).ConfigureAwait(false)).AsObject;
        }

        public async Task DeleteDocumentAsync(string documentId, string rev, CancellationToken cancellationToken)
        {
            await CouchDatabase.DeleteDocumentAsync(documentId, rev, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> SaveDocumentAsync(string documentId, ImmutableJsonObject json, IReadOnlyList<Stream> attachments, CancellationToken cancellationToken)
        {
            var response = attachments.Count == 0 ? await CouchDatabase.PutDocumentAsync(documentId, json, cancellationToken).ConfigureAwait(false) : await CouchDatabase.PutMultipartDocumentAsync(documentId, json, attachments, cancellationToken).ConfigureAwait(false);
            return response.Rev;
        }

        public Task<Stream> LoadAttachmentAsync(string documentId, string attachmentName, string rev, CancellationToken cancellationToken)
        {
            return CouchDatabase.GetBinaryAttachmentAsync(documentId, attachmentName, rev, cancellationToken);
        }

        public async Task<IReadOnlyList<RevisionInfo>> GetDocumentRevisionsAsync(string documentId, CancellationToken cancellationToken)
        {
            var docInfo = await CouchDatabase.GetDocumentAsync(CdbDocumentInfoJsonSerializer.Instance, documentId, revsInfo: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            return docInfo.RevsInfo!.Select(CdbToBackendRevisionInfo).ToList();
        }

        public async Task<DocumentDraft> GetDeletedDocumentAsync(string documentId, CancellationToken cancellationToken)
        {
            var revs = await GetDeletedDocumentRevisionsAsync(documentId, cancellationToken).ConfigureAwait(false);
            var multipartDocument = await CouchDatabase.GetMultipartDocumentAsync(documentId, rev: revs[1], attachments: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new DocumentDraft(multipartDocument.Json.AsObject, multipartDocument.Attachments.Select(a => new AttachmentDraft(documentId, a.Name, TempStorage.CreateFile(a.Name, a.Content))).ToList());
        }

        async Task<IReadOnlyList<string>> GetDeletedDocumentRevisionsAsync(string documentId, CancellationToken cancellationToken)
        {
            var docInfo = await CouchDatabase.GetMultipartDocumentAsync(documentId, revs: true, openRevsAll: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            var revisions = CdbDocumentInfoJsonSerializer.Instance.Deserialize(docInfo.Json[0]["ok"]).Revisions!;
            var count = revisions.Ids.Count;
            var revNumbers = Enumerable.Range(revisions.Start - count + 1, count).Reverse();
            return revisions.Ids.Zip(revNumbers, (rev, num) => num + "-" + rev).ToList();
        }

        public IObservable<BackendChange> ListenForChanges(string sinceSeq)
        {
            var database = CouchDatabase;
            return Observable.Create<BackendChange>((observer, cancellationToken) => Task.Run(() => Listen(database, sinceSeq, observer.OnNext, cancellationToken), cancellationToken));
        }

        static Task Listen(CdbDatabase database, string sinceSeq, Action<BackendChange> callback, CancellationToken cancellationToken)
        {
            return database.GetChangesContinuousAsync(change => callback(CdbToBackendChange(change)), heartbeat: 15000, includeDocs: true, since: sinceSeq, cancellationToken: cancellationToken);
        }

        static BackendChange CdbToBackendChange(CdbChange change)
        {
            if (change.Deleted)
                return new BackendChange(SeqToString(change.Seq), change.Id, true, null, null);
            else
                return new BackendChange(SeqToString(change.Seq), change.Id, false, change.Doc?.AsObject, change.Changes[0].Rev);
        }

        static string SeqToString(ImmutableJson jsonSeq)
        {
            if (jsonSeq.IsString)
                return jsonSeq.AsString;
            else if (jsonSeq.IsInt)
                return jsonSeq.AsInt.ToString(CultureInfo.InvariantCulture);
            else
                return jsonSeq.ToString();
        }

        private static RevisionInfo CdbToBackendRevisionInfo(CdbRevisionInfo info)
        {
            return new RevisionInfo(info.Rev, (RevisionStatus)info.Status);
        }
    }
}
