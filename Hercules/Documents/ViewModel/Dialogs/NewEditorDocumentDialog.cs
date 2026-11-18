using Hercules.Shell;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Hercules.Documents.Dialogs
{
    public class NewEditorDocumentDialog : ValidatedDialog
    {
        public SchemafulDatabase SchemafulDatabase { get; }

        public IReadOnlyCollection<CustomTypeSupport> CustomTypes { get; }

        public NewEditorDocumentDialog(SchemafulDatabase schemafulDatabase, IReadOnlyCollection<CustomTypeSupport> customTypes)
        {
            this.SchemafulDatabase = schemafulDatabase;
            this.CustomTypes = customTypes;
            Title = "New Editor Document";
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

        public CustomTypeSupport? CustomType
        {
            get;
            set => SetField(ref field, value);
        }

        public string? CustomTypeError
        {
            get;
            set => SetField(ref field, value);
        }

        [PropertyValidator(nameof(CustomType))]
        public string? ValidateCustomType()
        {
            CustomTypeError = CustomType == null ? "Editor type is required" : null;
            return CustomTypeError;
        }
    }
}