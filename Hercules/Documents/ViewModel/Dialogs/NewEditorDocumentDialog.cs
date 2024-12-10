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

        string documentName = string.Empty;

        public string DocumentName
        {
            get => documentName;
            set => SetField(ref documentName, value);
        }

        private string? documentNameError;

        public string? DocumentNameError
        {
            get => documentNameError;
            set => SetField(ref documentNameError, value);
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

        CustomTypeSupport? customType;

        public CustomTypeSupport? CustomType
        {
            get => customType;
            set => SetField(ref customType, value);
        }

        private string? customTypeError;

        public string? CustomTypeError
        {
            get => customTypeError;
            set => SetField(ref customTypeError, value);
        }

        [PropertyValidator(nameof(CustomType))]
        public string? ValidateCustomType()
        {
            CustomTypeError = CustomType == null ? "Editor type is required" : null;
            return CustomTypeError;
        }
    }
}