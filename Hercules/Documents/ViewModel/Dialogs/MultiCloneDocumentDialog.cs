using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hercules.Documents.Dialogs
{
    public class MultiCloneDocumentDialog : ValidatedDialog
    {
        private readonly Action<IDocument, string> cloneDocumentAction;
        public SchemafulDatabase SchemafulDatabase { get; }
        public IDocument Source { get; }

        public MultiCloneDocumentDialog(SchemafulDatabase schemafulDatabase, Action<IDocument, string> cloneDocumentAction, IDocument source)
        {
            this.SchemafulDatabase = schemafulDatabase;
            this.cloneDocumentAction = cloneDocumentAction;
            Title = "Multi Clone Document: " + source.DocumentId;
            Source = source;
        }

        string documentNames = string.Empty;

        public string DocumentNames
        {
            get => documentNames;
            set => SetField(ref documentNames, value);
        }

        private string? documentNamesError;

        public string? DocumentNamesError
        {
            get => documentNamesError;
            set => SetField(ref documentNamesError, value);
        }

        public List<string> SplitDocumentNames()
        {
            return DocumentNames.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
        }

        string? ValidateDocumentName(string name)
        {
            Match match = Regex.Match(name, @"^[a-zA-Z]{1}[a-zA-Z0-9_]*$");
            if (!match.Success)
                return "Invalid document name: " + name;

            var exists = SchemafulDatabase.AllDocuments.ContainsKey(name);
            if (exists)
                return "Name already exists: " + name;

            return null;
        }

        [PropertyValidator(nameof(DocumentNames))]
        public string? ValidateDocumentNames()
        {
            if (string.IsNullOrWhiteSpace(DocumentNames))
            {
                DocumentNamesError = "Document Name is required";
                return DocumentNamesError;
            }

            var names = SplitDocumentNames();
            if (names.Count == 0)
            {
                DocumentNamesError = "Enter at least one document name";
                return DocumentNamesError;
            }

            var duplicate = names.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (duplicate != null)
            {
                DocumentNamesError = "Duplicated name: " + duplicate;
                return DocumentNamesError;
            }

            foreach (var name in names)
            {
                var error = ValidateDocumentName(name);
                if (error != null)
                {
                    DocumentNamesError = error;
                    return DocumentNamesError;
                }
            }

            DocumentNamesError = null;
            return null;
        }

        protected override void OnClose(bool result)
        {
            if (result)
            {
                foreach (var name in SplitDocumentNames())
                {
                    cloneDocumentAction(Source, name);
                }
            }
        }
    }
}
