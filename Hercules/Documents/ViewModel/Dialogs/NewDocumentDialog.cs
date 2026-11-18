using Hercules.Shell;
using Json;
using System;
using System.Text.RegularExpressions;

namespace Hercules.Documents.Dialogs
{
    public class NewDocumentDialog : ValidatedDialog
    {
        private readonly Action<string, DocumentDraft> createDocumentAction;
        public SchemafulDatabase SchemafulDatabase { get; }
        public NewDocumentDialog(SchemafulDatabase schemafulDatabase, Action<string, DocumentDraft> createDocumentAction, Category? category)
        {
            this.createDocumentAction = createDocumentAction;
            this.SchemafulDatabase = schemafulDatabase;
            this.createDocumentAction = createDocumentAction;
            this.Category = category;
            Title = "New Document";
        }

        Category? category;

        public Category? Category
        {
            get => category;
            set => SetField(ref category, value);
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

        public string? CategoryError
        {
            get;
            set => SetField(ref field, value);
        }

        [PropertyValidator(nameof(Category))]
        public string? ValidateCategory()
        {
            CategoryError = Category == null ? "Category is required" : null;
            return CategoryError;
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
                var json = new JsonObject
                {
                    [SchemafulDatabase.Schema.Variant!.Tag] = Category!.Name
                };
                createDocumentAction(DocumentName, new DocumentDraft(json));
            }
        }
    }
}
