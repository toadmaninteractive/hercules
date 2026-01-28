using Hercules.DB;
using System;
using System.Threading.Tasks;

namespace Hercules.Documents
{
    /// <summary>
    /// This interface abstracts out working with database for document editors.
    /// </summary>
    public interface IDocumentProxy : IDisposable
    {
        Task<DocumentRevision> SaveDocumentAsync(DocumentDraft draft);
    }

    public sealed class DocumentProxy : IDocumentProxy
    {
        private readonly Database database;
        private readonly string username;
        private readonly IDocument document;
        private readonly IDocumentEditor editor;
        private string? saveTimestamp;

        public DocumentProxy(Database database, string username, IDocument document, IDocumentEditor editor)
        {
            this.database = database;
            this.username = username;
            this.document = document;
            this.editor = editor;
            database.EditDocument(document, editor);
            document.OnChanged += Document_OnChanged;
        }

        private void Document_OnChanged(DocumentChange change)
        {
            switch (change)
            {
                case DocumentChange.UserDeleting:
                    editor.UserDeleting();
                    break;

                case DocumentChange.UserDeleted:
                    editor.UserDeleted();
                    break;

                case DocumentChange.UserDeleteFailed:
                    editor.UserDeleteFailed();
                    break;

                case DocumentChange.RemoteDeleted:
                    editor.RemoteDeleted(document.Draft!);
                    break;

                case DocumentChange.RemoteUpdated:
                    {
                        var isUpdated = true;
                        if (saveTimestamp != null)
                            isUpdated = document.CurrentRevision!.Metadata.Timestamp != saveTimestamp;
                        if (isUpdated)
                            editor.RemoteUpdated(document.CurrentRevision!);
                    }
                    break;
            }
        }

        public async Task<DocumentRevision> SaveDocumentAsync(DocumentDraft draft)
        {
            var metadata = new MetadataDraft(username);
            saveTimestamp = metadata.Timestamp;
            try
            {
                await database.SaveDocumentAsync(document, draft, metadata).ConfigureAwait(true);
            }
            finally
            {
                saveTimestamp = null;
            }
            return document.CurrentRevision!;
        }

        public void Dispose()
        {
            document.OnChanged -= Document_OnChanged;
            database.CloseDocument(document);
        }
    }
}
