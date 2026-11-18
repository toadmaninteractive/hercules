using Hercules.Shell;
using System;
using System.Text.RegularExpressions;

namespace Hercules.Documents.Dialogs
{
    public class NewSchemalessDocumentDialog : ValidatedDialog
    {
        private readonly Action<string> createDocumentAction;
        public SchemafulDatabase SchemafulDatabase { get; }

        public NewSchemalessDocumentDialog(string title, SchemafulDatabase schemafulDatabase, Action<string> createDocumentAction)
        {
            this.createDocumentAction = createDocumentAction;
            this.SchemafulDatabase = schemafulDatabase;
            Title = title;
        }

        public string DocumentName
        {
            get;
            set => SetField(ref field, value);
        } = string.Empty;

        public string? DocumentNameError
        {
            get;
            set => SetField(ref field, value);
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
                createDocumentAction(DocumentName);
        }
    }
}
