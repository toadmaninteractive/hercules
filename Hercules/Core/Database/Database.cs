using Hercules.Documents;
using Hercules.Shortcuts;
using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.DB
{
    public class DatabaseException : Exception
    {
        public DatabaseException()
        {
        }

        public DatabaseException(string? message)
            : base(message)
        {
        }

        public DatabaseException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }

    public class Database : IReadOnlyDatabase
    {
        readonly record struct AttachmentCacheId(string DocumentId, string Name, int RevPos);

        public IDatabaseCache Cache { get; }

        public IDatabaseBackend Backend { get; }

        public TempStorage TempStorage { get; }

        public IReadOnlyDictionary<string, IDocument> Documents => documents;

        public IObservable<DatabaseChange> Changes => changesSubject.AsObservable();

        public bool IsOnline { get; private set; }
        public bool IsLoaded { get; private set; }

        private readonly Dictionary<string, IDocument> documents = new();
        private readonly CancellationTokenSource cancellationTokenSource;
        private IDisposable? listener;
        private readonly Subject<DatabaseChange> changesSubject;
        private readonly Dictionary<AttachmentCacheId, AttachmentRevision> attachments = new();

        public Database(IDatabaseBackend backend, IDatabaseCache cache, TempStorage tempStorage)
        {
            GarbageMonitor.Register(this);
            TempStorage = tempStorage;
            this.IsOnline = false;
            this.IsLoaded = false;
            this.Backend = backend;
            this.Cache = cache;
            this.changesSubject = new Subject<DatabaseChange>();
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void LoadCache(CancellationToken cancellationToken)
        {
            foreach (var entry in Cache.LoadCache(cancellationToken))
            {
                documents.Add(entry.DocumentId, new DatabaseDocument(entry.DocumentId, ParseDocumentRevision(entry.DocumentId, entry.Data)));
            }
            IsLoaded = true;
        }

        public async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            var changes = await Backend.GetChangesAsync(Cache.ReadLastSequence(), true, cancellationToken).ConfigureAwait(false);
            if (changes.Any())
            {
                await Task.Run(() => Synchronize(changes), cancellationToken).ConfigureAwait(false);
            }
            IsOnline = true;
        }

        public void Synchronize(IReadOnlyList<BackendChange> changes)
        {
            if (changes.Any())
            {
                var removed = new List<string>();
                var updated = new List<BackendChange>();
                foreach (var change in changes)
                {
                    LogBackendChange(change);
                    if (change.Deleted)
                    {
                        if (Documents.ContainsKey(change.Id))
                        {
                            documents.Remove(change.Id);
                            removed.Add(change.Id);
                        }
                    }
                    else
                    {
                        var rev = change.Rev!;
                        var documentRevision = ParseDocumentRevision(change.Id, change.Doc!);
                        if (documents.TryGetValue(change.Id, out var document))
                        {
                            if (document.CurrentRevision?.Rev != rev)
                            {
                                if (CouchUtils.GetRevisionNumber(document.CurrentRevision?.Rev!) >= CouchUtils.GetRevisionNumber(rev))
                                {
                                    Logger.LogWarning($"Received smaller revision {rev} for document {document.DocumentId}. Known revision is {document.CurrentRevision!.Rev}. The document could be deleted and created again.", new DocumentShortcut(document.DocumentId));
                                    removed.Add(document.DocumentId);
                                }

                                ((DatabaseDocument)document).RemoteUpdated(documentRevision);
                            }
                        }
                        else
                        {
                            documents[change.Id] = new DatabaseDocument(change.Id, documentRevision);
                        }
                        updated.Add(change);
                    }
                }

                Parallel.ForEach(removed, docId => Cache.DeleteDocument(docId));
                Parallel.ForEach(updated, change =>
                {
                    Cache.WriteDocument(change.Doc!);
                    Cache.WriteRevision(change.Id, change.Rev!);
                });
                Cache.WriteLastSequence(changes[^1].Seq);
            }

            IsOnline = true;
        }

        public IDocument CreateDocument(string newDocumentId, DocumentDraft draft)
        {
            if (Documents.ContainsKey(newDocumentId))
                throw new ArgumentException($"Document {newDocumentId} already exists");
            var document = new DatabaseDocument(newDocumentId, draft);
            documents[newDocumentId] = document;
            RaiseCollectionChanged(DatabaseChangeKind.Add, document);
            return document;
        }

        private void DocumentDeleted(string documentId)
        {
            if (Documents.TryGetValue(documentId, out var document))
            {
                if (document.Editor == null)
                {
                    documents.Remove(documentId);
                    RaiseCollectionChanged(DatabaseChangeKind.Remove, document);
                }
                ((DatabaseDocument)document).Deleted(a => AttachmentDraft.CopyFrom(a, TempStorage));
                Cache.DeleteDocument(documentId);
            }
        }

        public void EditDocument(IDocument document, IDocumentEditor editor)
        {
            ((DatabaseDocument)document).Editor = editor;
        }

        public void CloseDocument(IDocument document)
        {
            ((DatabaseDocument)document).Editor = null;
            if (!document.IsExisting)
            {
                documents.Remove(document.DocumentId);
                RaiseCollectionChanged(DatabaseChangeKind.Remove, document);
            }
        }

        public async Task DeleteDocumentAsync(IDocument document)
        {
            ArgumentNullException.ThrowIfNull(nameof(document));
            if (!(document is DatabaseDocument dbDocument))
                throw new ArgumentException($"Document is not a {nameof(DatabaseDocument)}", nameof(document));
            if (!document.IsExisting)
                throw new InvalidOperationException($"Trying to delete non-existing document <{document.DocumentId}>.");

            var rev = document.CurrentRevision!.Rev;
            var shortcut = new DocumentShortcut(document.DocumentId);
            Logger.Log($"Deleting document <{document.DocumentId}>");
            dbDocument.Deleting();
            try
            {
                await Backend.DeleteDocumentAsync(document.DocumentId, rev, cancellationTokenSource.Token).ConfigureAwait(true);
                Logger.Log($"Document deleted: <{document.DocumentId}>");
                DocumentDeleted(document.DocumentId);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogException($"Failed to delete <{document.DocumentId}>", ex, shortcut);
                dbDocument.DeleteFailed();
            }
        }

        public async Task SaveDocumentAsync(IDocument document, DocumentDraft draft, MetadataDraft metadataDraft)
        {
            ArgumentNullException.ThrowIfNull(nameof(document));
            if (!(document is DatabaseDocument dbDocument))
                throw new ArgumentException($"Document is not a {nameof(DatabaseDocument)}", nameof(document));
            ArgumentNullException.ThrowIfNull(nameof(draft));
            ArgumentNullException.ThrowIfNull(nameof(metadataDraft));

            var shortcut = new DocumentShortcut(document.DocumentId);
            Logger.Log($"Saving document <{document.DocumentId}>", shortcut);
            try
            {
                var metadata = MetadataHelper.MakeMetadata(metadataDraft, document.CurrentRevision?.Rev);
                var fullJsonDraft = draft.GetFullDocumentJson(document.DocumentId, document.CurrentRevision?.Rev, metadata);
                var attachmentStreams = draft.Attachments.OfType<AttachmentDraft>().Select(f => File.OpenRead(f.File.FileName)).ToList();
                string? newRevision;
                try
                {
                    newRevision = await Backend.SaveDocumentAsync(document.DocumentId, fullJsonDraft, attachmentStreams, cancellationTokenSource.Token).ConfigureAwait(true);
                }
                finally
                {
                    foreach (var stream in attachmentStreams)
                        await stream.DisposeAsync();
                }
                foreach (var attachment in draft.Attachments.OfType<AttachmentDraft>())
                {
                    using var stream = File.OpenRead(attachment.File.FileName);
                    Cache.SaveAttachment(document.DocumentId, attachment.Name, CouchUtils.GetRevisionNumber(newRevision), stream);
                }
                // We need to fetch attachment digests for new attachments, if there're any
                ImmutableJsonObject cacheDocument = attachmentStreams.Count == 0 ?
                    draft.GetFullDocumentJson(document.DocumentId, newRevision, metadata) :
                    await Backend.GetDocumentAsync(document.DocumentId, newRevision, cancellationTokenSource.Token).ConfigureAwait(true);
                DocumentUpdated(dbDocument, newRevision, cacheDocument);
                Logger.Log($"Document saved: <{document.DocumentId}>", shortcut);
            }
            catch (Exception exception)
            {
                Logger.LogException($"Failed to save <{document.DocumentId}>", exception, shortcut);
                throw new DatabaseException($"Failed to save <{document.DocumentId}>", exception);
            }
        }

        private void DocumentCreated(string docId, string rev, ImmutableJsonObject json)
        {
            Logger.Log($"Document created: <{docId}>", new DocumentShortcut(docId));
            Cache.WriteRevision(docId, rev);
            Cache.WriteDocument(json);
            var document = new DatabaseDocument(docId, ParseDocumentRevision(docId, json));
            documents[docId] = document;
            RaiseCollectionChanged(DatabaseChangeKind.Add, document);
        }

        private void DocumentUpdated(DatabaseDocument document, string rev, ImmutableJsonObject json)
        {
            if (document.CurrentRevision?.Rev == rev)
                return;
            if (CouchUtils.GetRevisionNumber(document.CurrentRevision?.Rev) > CouchUtils.GetRevisionNumber(rev))
            {
                Logger.LogWarning($"Received smaller revision {rev} for document {document.DocumentId}. Known revision is {document.CurrentRevision.Rev}. The document could be deleted and created again.", new DocumentShortcut(document.DocumentId));
                Cache.DeleteDocument(document.DocumentId);
            }
            Cache.WriteDocument(json);
            Cache.WriteRevision(document.DocumentId, rev);
            document.RemoteUpdated(ParseDocumentRevision(document.DocumentId, json));
            RaiseCollectionChanged(DatabaseChangeKind.Update, document);
        }

        public void Close()
        {
            documents.Clear();
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
            cancellationTokenSource.Cancel();
        }

        private static void LogBackendChange(BackendChange change)
        {
            var intSeq = int.Parse(change.Seq.Split('-').First(), CultureInfo.InvariantCulture);
            if (change.Deleted)
            {
                Logger.Log($"CouchDB change #{intSeq}: {change.Id} deleted");
            }
            else
            {
                Logger.Log($"CouchDB change #{intSeq}: {change.Id} updated to revision {change.Rev}", new DocumentShortcut(change.Id));
            }
        }

        void UpdateNotificationCallback(BackendChange change)
        {
            LogBackendChange(change);
            if (change.Deleted)
            {
                DocumentDeleted(change.Id);
            }
            else
            {
                var rev = change.Rev;
                if (Documents.TryGetValue(change.Id, out var document))
                    DocumentUpdated((DatabaseDocument)document, rev!, change.Doc!);
                else
                    DocumentCreated(change.Id, rev!, change.Doc!);
            }

            Cache.WriteLastSequence(change.Seq);
        }

        public void ListenForChanges(IScheduler scheduler)
        {
            async void OnFailure(Exception x)
            {
                Logger.LogException("Listen for changes stopped with error", x);
                if (cancellationTokenSource.IsCancellationRequested)
                    return;
                await Task.Delay(TimeSpan.FromSeconds(10f)).ConfigureAwait(true);
                if (cancellationTokenSource.IsCancellationRequested)
                    return;
                ListenForChanges(scheduler);
            }

            static void OnSuccess()
            {
                Logger.Log("Listen for changes stopped without error");
            }

            listener = Backend.ListenForChanges(Cache.ReadLastSequence()).ObserveOn(scheduler).Subscribe(UpdateNotificationCallback, OnFailure, OnSuccess);
        }

        void RaiseCollectionChanged(DatabaseChangeKind kind, IDocument document)
        {
            changesSubject.OnNext(new DatabaseChange(kind, document));
        }

        public DocumentRevision ParseDocumentRevision(string documentId, ImmutableJsonObject json)
        {
            return new DocumentRevision(json, ParseAttachments(documentId, json));
        }

        IReadOnlyList<AttachmentRevision> ParseAttachments(string documentId, ImmutableJsonObject documentJson)
        {
            if (!documentJson.TryGetValue("_attachments", out var attachmentsJson))
                return Array.Empty<AttachmentRevision>();
            var result = new List<AttachmentRevision>();
            foreach (var pair in attachmentsJson.AsObject)
            {
                var name = pair.Key;
                var cdbAttachment = CouchDB.Api.CdbAttachmentJsonSerializer.Instance.Deserialize(pair.Value);
                var revPos = cdbAttachment.Revpos.Value;
                var id = new AttachmentCacheId(documentId, name, revPos);
                if (!attachments.TryGetValue(id, out var attachment))
                {
                    AttachmentFile file;
                    if (Cache.TryGetAttachmentFile(documentId, name, revPos, out var fileName))
                        file = new AttachmentFile(fileName);
                    else
                        file = new AttachmentFile(LoadAttachmentAsync(id, CouchUtils.GetRevision(documentJson)));
                    file.LoadAsync().Track();
                    attachment = new AttachmentRevision(documentId, name, cdbAttachment.Length.Value, revPos, cdbAttachment.ContentType, cdbAttachment.Digest, file);
                    attachments.Add(id, attachment);
                }
                result.Add(attachment);
            }
            return result;
        }

        async Task<string> LoadAttachmentAsync(AttachmentCacheId id, string rev)
        {
            Stream stream;
            try
            {
                stream = await Backend.LoadAttachmentAsync(id.DocumentId, id.Name, rev, cancellationTokenSource.Token).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                throw new DatabaseException($"Failed to load attachment {id.DocumentId}.{id.Name}#{id.RevPos}: {exception.Message}", exception);
            }
            if (Documents.TryGetValue(id.DocumentId, out var document) && document.IsExisting)
            {
                return Cache.SaveAttachment(id.DocumentId, id.Name, id.RevPos, stream);
            }
            throw new InvalidOperationException($"Document {id.DocumentId} has been deleted while fetching attachment {id.Name}");
        }
    }
}
