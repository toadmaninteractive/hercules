using Hercules.Shell;
using System;
using System.Text.RegularExpressions;

namespace Hercules.Documents.Dialogs
{
    public class CloneDocumentDialog : ValidatedDialog
    {
        public SchemafulDatabase SchemafulDatabase { get; }
        public IDocument Source { get; }
        private readonly Action<IDocument, string> cloneDocumentAction;

        public CloneDocumentDialog(SchemafulDatabase schemafulDatabase, Action<IDocument, string> cloneDocumentAction, IDocument source)
        {
            this.SchemafulDatabase = schemafulDatabase;
            this.cloneDocumentAction = cloneDocumentAction;
            Title = "Clone Document: " + source.DocumentId;
            Source = source;
            DocumentName = source.DocumentId;
        }

        private string? documentNameError;

        public string? DocumentNameError
        {
            get => documentNameError;
            set => SetField(ref documentNameError, value);
        }

        string documentName = string.Empty;

        public string DocumentName
        {
            get => documentName;
            set => SetField(ref documentName, value);
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

            var exists = SchemafulDatabase.AllDocuments.ContainsKey(DocumentName);
            DocumentNameError = exists ? "Name already exists" : null;
            return DocumentNameError;
        }

        protected override void OnClose(bool result)
        {
            if (result)
            {
                cloneDocumentAction(Source, DocumentName);
            }
        }
    }
}
