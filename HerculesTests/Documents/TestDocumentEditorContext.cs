using Hercules.Forms;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Hercules.Documents.Editor.Tests
{
    public static class TestDocumentEditorContext
    {
        private class TestReadOnlyDatabase : IReadOnlyDatabase
        {
            public IReadOnlyDictionary<string, IDocument> Documents { get; }

            public IObservable<DatabaseChange> Changes { get; }

            public TestReadOnlyDatabase(IReadOnlyDictionary<string, IDocument> documents, IObservable<DatabaseChange> changes)
            {
                Documents = documents;
                Changes = changes;
            }
        }

        private class TestFormSchemaFactory : IFormSchemaFactory
        {
            public TestFormSchemaFactory(FormSchema formSchema)
            {
                FormSchema = formSchema;
            }

            public FormSchema FormSchema { get; }

            public FormSchema CreateFormSchema(ImmutableJson json, SchemafulDatabase? schemafulDatabase) => FormSchema;
        }

        private class TestDialogService : IDialogService
        {
            public bool ShowDialog(Dialog dialog)
            {
                return false;
            }
        }

        private class TestMetaSchemaProvider : IMetaSchemaProvider
        {
            public bool TryGetSchema(IDocument document, ImmutableJsonObject json, [MaybeNullWhen(false)] out SchemaRecord schema)
            {
                schema = default!;
                return false;
            }
        }

        private sealed class TestDocumentProxy : IDocumentProxy
        {
            private readonly TestDocument document;
            private readonly IDocumentEditor editor;
            private bool isSaving;

            public TestDocumentProxy(TestDocument document, IDocumentEditor editor)
            {
                this.document = document;
                this.editor = editor;
                document.Editor = editor;
                document.OnChanged += Document_OnChanged;
            }

            public Task<DocumentRevision> SaveDocumentAsync(DocumentDraft draft)
            {
                isSaving = true;
                var newRevision = new DocumentRevision(draft.Json, Array.Empty<AttachmentRevision>());
                document.RemoteUpdated(newRevision);
                isSaving = false;
                return Task.FromResult(newRevision);
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
                        if (!isSaving)
                            editor.RemoteUpdated(document.CurrentRevision!);
                        break;
                }
            }

            public void Dispose()
            {
                document.OnChanged -= Document_OnChanged;
                document.Editor = null;
            }
        }

        public static DocumentEditorContext Create(IReadOnlyDictionary<string, TestDocument> documents, IObservable<DatabaseChange> databaseChanges, FormSchema formSchema)
        {
            var tempStorage = TempStorage.Create();
            var database = new TestReadOnlyDatabase(documents.ToDictionary(pair => pair.Key, pair => (IDocument)pair.Value), databaseChanges);
            var schemafulDatabase = new SchemafulDatabase(database, new TestFormSchemaFactory(formSchema), new TestMetaSchemaProvider(), false);
            var observableSchemafulDatabase = new ObservableValue<SchemafulDatabase>(schemafulDatabase);
            var customTypeRegistry = new CustomTypeRegistry();
            IElementFactory ElementFactoryCreator(ElementFactoryContext elementFactoryContext) => new ElementFactory(customTypeRegistry, elementFactoryContext);
            IDocumentProxy DocumentHandlerProvider(IDocument document, IDocumentEditor documentEditor) => new TestDocumentProxy((TestDocument)document, documentEditor);
            return new DocumentEditorContext(
                DocumentHandlerProvider,
                observableSchemafulDatabase,
                new TestDialogService(),
                ElementFactoryCreator,
                new FormSettings(),
                new ObservableValue<bool>(false),
                tempStorage,
                (cat, jsonPath) => { },
                (script, docs, progress, token) => Task.CompletedTask);
        }
    }
}
