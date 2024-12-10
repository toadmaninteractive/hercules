using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hercules.Documents.Dialogs
{
    public class InheritDocumentDialog : ValidatedDialog
    {
        private readonly Action<IDocument, string> inheritDocumentAction;

        public IDocument SelectedDocument
        {
            get => selectedDocument;
            set => SetField(ref selectedDocument, value);
        }

        public string DocumentName
        {
            get => documentName;
            set => SetField(ref documentName, value);
        }

        public string? DocumentNameError
        {
            get => documentNameError;
            set => SetField(ref documentNameError, value);
        }

        public List<IDocument> AvailableDocuments { get; }
        public SchemafulDatabase SchemafulDatabase { get; }
        public IDocument Source { get; }

        private IDocument selectedDocument;
        private string documentName = string.Empty;
        private string? documentNameError;

        public InheritDocumentDialog(SchemafulDatabase schemafulDatabase, Action<IDocument, string> inheritDocumentAction, IDocument source)
        {
            this.inheritDocumentAction = inheritDocumentAction;
            this.SchemafulDatabase = schemafulDatabase;
            Title = "Inherit Document from: " + source.DocumentId;
            Source = source;

            AvailableDocuments = SchemafulDatabase.SchemafulDocuments.Where(x => x.CurrentRevision != null).ToList();
            SelectedDocument = AvailableDocuments.SingleOrDefault(x => x.DocumentId == source.DocumentId);
        }

        [PropertyValidator(nameof(DocumentName))]
        public string? ValidateDocumentName()
        {
            if (string.IsNullOrWhiteSpace(DocumentName))
            {
                DocumentNameError = "Document Name is required";
                return DocumentNameError;
            }

            Match match = Regex.Match(DocumentName, @"^[a-zA-Z]{1}[a-zA-Z0-9_]*$");
            if (!match.Success)
            {
                DocumentNameError = "Invalid document name: " + DocumentName;
                return DocumentNameError;
            }

            DocumentNameError = SchemafulDatabase.AllDocuments.ContainsKey(DocumentName) ? "Name already exists" : null;
            return DocumentNameError;
        }

        protected override void OnClose(bool result)
        {
            if (result)
                inheritDocumentAction(Source, DocumentName);
        }

        protected override bool IsOkEnabled()
        {
            return string.IsNullOrEmpty(DocumentNameError);
        }
    }
}