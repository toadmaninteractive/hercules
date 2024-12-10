using Hercules.Shell;
using System;
using System.Collections.ObjectModel;

namespace Hercules.Documents.Dialogs
{
    public class OpenDocumentDialog : ValidatedDialog
    {
        private readonly Action<string> openDocument;
        public SchemafulDatabase SchemafulDatabase { get; }

        public OpenDocumentDialog(SchemafulDatabase schemafulDatabase, Action<string> openDocument)
        {
            this.openDocument = openDocument;
            this.SchemafulDatabase = schemafulDatabase;
            this.Title = "Open document...";
        }

        IDocument? document;

        public IDocument? Document
        {
            get => document;
            set => SetField(ref document, value);
        }

        public ObservableCollection<IDocument> Documents => SchemafulDatabase.SchemafulDocuments;

        protected override bool IsOkEnabled()
        {
            return Document != null;
        }

        protected override void OnClose(bool result)
        {
            if (result)
                openDocument(Document!.DocumentId);
        }
    }
}
