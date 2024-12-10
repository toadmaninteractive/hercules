using CouchDB;
using CouchDB.Api;
using Hercules.DB;
using Hercules.Shell;
using Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.ServerBrowser
{
    public class CopyDatabaseNotification : Notification
    {
        private readonly Action onComplete;

        public CopyDatabaseNotification(CdbDatabase sourceDatabase, IDatabaseBackend targetDatabase, Action onComplete) : base(null)
        {
            this.onComplete = onComplete;
            cancellationTokenSource = new CancellationTokenSource();
            AbortCommand = Commands.Execute(Abort);
            IsIndeterminate = true;
            StatusMessage = $"Download {sourceDatabase.Name} documents";
            CopyDatabase(sourceDatabase, targetDatabase, cancellationTokenSource.Token).Track();
        }

        void Abort()
        {
            Logger.Log("Copying database was cancelled");
            cancellationTokenSource.Cancel();
            Close();
        }

        public ICommand AbortCommand { get; }

        readonly CancellationTokenSource cancellationTokenSource;

        string? statusMessage;

        public string? StatusMessage
        {
            get => statusMessage;
            set => SetField(ref statusMessage, value);
        }

        async Task CopyDatabase(CdbDatabase sourceDatabase, IDatabaseBackend targetBackend, CancellationToken cancellationToken)
        {
            try
            {
                var allDocs = await sourceDatabase.GetAllDocumentsAsync(includeDocs: true, cancellationToken: cancellationToken).ConfigureAwait(true);
                IsIndeterminate = false;
                DocumentCount = allDocs.TotalRows;
                Progress = 0;
                var progress = new Progress<int>(d =>
                {
                    Progress += d;
                    StatusMessage = $"Documents saved: {Progress} / {DocumentCount}";
                });
                var saveTasks = allDocs.Rows.Select(row => CopyDocument(sourceDatabase, row.Id, row.Doc!.AsObject, targetBackend, progress, cancellationToken)).ToList();
                await Task.WhenAll(saveTasks);
                onComplete();
            }
            catch (OperationCanceledException)
            {
            }
            Close();
        }

        async Task CopyDocument(CdbDatabase sourceDatabase, string id, ImmutableJsonObject doc, IDatabaseBackend targetBackend, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var newDoc = doc.ToMutable();
            newDoc.Remove("_rev");
            if (doc.ContainsKey("_attachments"))
            {
                var multipartDocument = await sourceDatabase.GetMultipartDocumentAsync(id, attachments: true, cancellationToken: cancellationToken);
                var docInfo = CdbDocumentInfoJsonSerializer.Instance.Deserialize(multipartDocument.Json);
                var jsonAttachments = new JsonObject(docInfo.Attachments!.Count);
                foreach (var attachment in docInfo.Attachments)
                {
                    var newAttachment = new CdbAttachment();
                    newAttachment.ContentType = attachment.Value.ContentType;
                    newAttachment.Length = attachment.Value.Length;
                    newAttachment.Follows = true;
                    jsonAttachments.Add(attachment.Key, CdbAttachmentJsonSerializer.Instance.Serialize(newAttachment));
                }
                newDoc["_attachments"] = jsonAttachments;
                await targetBackend.SaveDocumentAsync(id, newDoc, multipartDocument.Attachments.Select(att => att.Content).ToList(), cancellationToken).ConfigureAwait(true);
            }
            else
            {
                await targetBackend.SaveDocumentAsync(id, newDoc, Array.Empty<Stream>(), cancellationToken).ConfigureAwait(true);
            }
            progress.Report(1);
        }

        private bool isIndeterminate;

        public bool IsIndeterminate { get => isIndeterminate; set => SetField(ref isIndeterminate, value); }

        private double progress;

        public double Progress { get => progress; set => SetField(ref progress, value); }

        private double documentCount;

        public double DocumentCount { get => documentCount; set => SetField(ref documentCount, value); }
    }
}
