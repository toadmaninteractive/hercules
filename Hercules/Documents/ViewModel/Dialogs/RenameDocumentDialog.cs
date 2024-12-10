using Hercules.Search;
using Hercules.Shell;
using JsonDiff;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Hercules.Documents.Dialogs
{
    public class RenameDocumentDialog : ValidatedDialog
    {
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
        public DocumentsModule DocumentsModule { get; }
        public bool HasResults => Results.Documents.Count > 0;
        public SearchResults Results
        {
            get => results;
            set => SetField(ref results, value);
        }
        public SchemafulDatabase SchemafulDatabase { get; }
        public IDocument Source { get; }
        public ICommand CheckAllCommand { get; }
        public ICommand UncheckAllCommand { get; }

        private string documentName = string.Empty;
        private string? documentNameError;
        private SearchResults results;

        public RenameDocumentDialog(SchemafulDatabase schemafulDatabase, DocumentsModule documentsModule, IDocument source)
        {
            this.SchemafulDatabase = schemafulDatabase;
            this.DocumentsModule = documentsModule;
            Title = "Rename Document: " + source.DocumentId;
            Source = source;
            DocumentName = source.DocumentId;
            results = KeySearchVisitor.Search(SchemafulDatabase.Schema, SchemafulDatabase.SchemafulDocuments, source.DocumentId);
            ExpandAll();
            CheckAll();
            CheckAllCommand = Commands.Execute(CheckAll).If(() => HasResults);
            UncheckAllCommand = Commands.Execute(UncheckAll).If(() => HasResults);
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
                DocumentsModule.CloneDocument(Source, DocumentName);
                foreach (var docRefs in Results.Documents.Values)
                {
                    if (docRefs.References.Any(r => r.IsChecked))
                    {
                        var patch = new JsonPatch();
                        foreach (var docRef in docRefs.References)
                            if (docRef.IsChecked)
                                patch.Chunks.Add(new JsonPatchChunk(docRef.Path, DocumentName));
                        var editor = DocumentsModule.EditDocument(docRefs.DocumentId);
                        editor.ApplyPatch(patch);
                    }
                }
                DocumentsModule.DeleteDocumentAsync(Source, false).Track();
            }
        }
        private void CheckAll()
        {
            foreach (var r in Results.AllReferences)
                r.IsChecked = true;
        }

        private void ExpandAll()
        {
            foreach (var r in Results.Documents.Values)
                r.IsExpanded = true;
        }

        private void UncheckAll()
        {
            foreach (var r in Results.AllReferences)
                r.IsChecked = false;
        }
    }
}
