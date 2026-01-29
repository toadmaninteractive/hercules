using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Documents
{
    public sealed class SchemafulDatabase
    {
        private static readonly Version RecommendedSchemaVersion = new Version(1, 1);

        public FormSchema? Schema { get; }
        public DiagramSchemaDocument? DiagramSchema { get; }
        public IReadOnlyList<Category> Categories { get; }
        public ObservableCollection<IDocument> SchemafulDocuments { get; } = new();
        public Category SchemalessDocuments { get; } = new Category(CategoryType.Schemaless, "_schemaless", null);
        public Category DesignDocuments { get; } = new Category(CategoryType.Design, "_design", null);
        public Category SchemaDocuments { get; } = new Category(CategoryType.Schema, "_schema", null);
        public Category ScriptDocuments { get; } = new Category(CategoryType.Script, "_script", null);
        public IReadOnlyDictionary<string, IDocument> AllDocuments => Database.Documents;
        public IReadOnlyDatabase Database { get; }
        public IReadOnlyList<string> CategoryGroupNames { get; }

        private readonly IReadOnlyDictionary<string, Category> categoriesByName;
        private readonly Dictionary<string, CategoryInterface> interfacesByName = new();
        private readonly Dictionary<string, Category> categoriesByDocument = new Dictionary<string, Category>();
        private readonly IDisposable databaseSubscription;
        private readonly IMetaSchemaProvider metaSchemaProvider;
        public bool SyncPreview { get; set; }

        public SchemafulDatabase(IReadOnlyDatabase database, IFormSchemaFactory formSchemaFactory, IMetaSchemaProvider metaSchemaProvider, bool syncMetadata)
        {
            GarbageMonitor.Register(this);
            Database = database;
            this.metaSchemaProvider = metaSchemaProvider;
            SyncPreview = syncMetadata;
            databaseSubscription = database.Changes.Subscribe(DatabaseChanged);

            if (AllDocuments.TryGetValue(CouchUtils.SchemaDocumentId, out var schemaDocument))
            {
                try
                {
                    Schema = formSchemaFactory.CreateFormSchema(schemaDocument.Json, this);
                }
                catch (Exception exception)
                {
                    Logger.LogException("Failed to load schema", exception);
                }
            }
            else
            {
                Logger.LogWarning("Schema document is missing");
            }

            if (Schema != null)
            {
                if (AllDocuments.TryGetValue(CouchUtils.DiagramSchemaDocumentId, out var diagramDocument))
                {
                    try
                    {
                        this.DiagramSchema = new DiagramSchemaDocument(Schema, diagramDocument.Json);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogException("Failed to load diagram schema", exception);
                    }
                }

                /*
                var dialogRecord = Schema.Variant.GetChild("dialog");
                if (dialogRecord != null)
                    DialogShchemaInit(dialogRecord);*/

                CategoryGroupNames = Schema.Groups.Select(x => x.Key).ToList();

                Categories = Schema.Variant?.Children.OrderBy(child => child.TagValue!).Select(child => new Category(CategoryType.Schemaful, child.TagValue!, child.Group, child.AiHint)).ToList() ?? new List<Category>();
                categoriesByName = Categories.ToDictionary(cat => cat.Name, cat => cat);
            }
            else
            {
                CategoryGroupNames = Array.Empty<string>();
                categoriesByName = ImmutableDictionary<string, Category>.Empty;
                Categories = Array.Empty<Category>();
            }

            foreach (var document in AllDocuments.Values.Order())
            {
                AddDocument(document, false);
            }
        }

        public Category? GetCategory(string categoryName)
        {
            return categoriesByName.GetValueOrDefault(categoryName);
        }

        public Category GetDocumentCategory(IDocument document) => categoriesByDocument[document.DocumentId];

        public IEnumerable<IDocument> GetDocumentsByInterface(string interfaceName)
        {
            if (interfacesByName.TryGetValue(interfaceName, out var result))
            {
                return result.Documents;
            }
            var categories = Schema.Variant.Children.Where(child => child.Interfaces != null && child.Interfaces.Contains(interfaceName))
                .Select(child => categoriesByName[child.TagValue!]).ToList();
            if (categories.Any())
            {
                var intf = new CategoryInterface(interfaceName, categories);
                interfacesByName[interfaceName] = intf;
            }
            return SchemafulDocuments;
        }

        public void GetDocumentSchema(IDocument document, ImmutableJsonObject json, out Category category, out SchemaRecord schema)
        {
            category = FindCategory(document, json);
            if (metaSchemaProvider.TryGetSchema(document, json, out var metaSchema))
                schema = metaSchema;
            else if (category.IsSchemaful)
                schema = Schema.Variant.GetChild(category.Name) ?? SchemaRecord.Default;
            else
                schema = SchemaRecord.Default;
        }

        private void DatabaseChanged(DatabaseChange change)
        {
            switch (change.Kind)
            {
                case DatabaseChangeKind.Add:
                    AddDocument(change.Document, true);
                    break;
                case DatabaseChangeKind.Remove:
                    RemoveDocument(change.Document);
                    break;
                case DatabaseChangeKind.Update:
                    UpdateDocument(change.Document);
                    break;
            }
        }

        private void AddDocument(IDocument document, bool insertSorted)
        {
            var category = FindCategory(document);
            category.AddDocument(document, insertSorted);
            categoriesByDocument[document.DocumentId] = category;
            if (category.IsSchemaful)
            {
                if (insertSorted)
                    SchemafulDocuments.InsertSorted(document);
                else
                    SchemafulDocuments.Add(document);
                if (SyncPreview)
                    UpdatePreview(document);
            }
        }

        private void RemoveDocument(IDocument document)
        {
            var category = categoriesByDocument[document.DocumentId];
            if (category.IsSchemaful)
                SchemafulDocuments.Remove(document);
            category.RemoveDocument(document);
        }

        private void UpdateDocument(IDocument document)
        {
            var newCategory = FindCategory(document);
            var oldCategory = categoriesByDocument[document.DocumentId];

            if (oldCategory != newCategory)
            {
                if (oldCategory.IsSchemaful && !newCategory.IsSchemaful)
                    SchemafulDocuments.Remove(document);
                oldCategory.RemoveDocument(document);
                categoriesByDocument[document.DocumentId] = newCategory;
                newCategory.AddDocument(document, true);
                if (newCategory.IsSchemaful && !oldCategory.IsSchemaful)
                    SchemafulDocuments.InsertSorted(document);
            }

            if (SyncPreview)
            {
                UpdatePreview(document);
            }
        }

        private void UpdatePreview(IDocument document)
        {
            GetDocumentSchema(document, document.Json, out var category, out var schemaRecord);
            if (schemaRecord.CaptionPath != null && document.Json.TryFetch(schemaRecord.CaptionPath, out var caption))
                document.Preview.Caption = caption.AsStringOrNull()?.ReplaceLineEndings(" ");
            else
                document.Preview.Caption = null;
        }

        private Category FindCategory(IDocument document, ImmutableJsonObject? json = null)
        {
            if (document.IsDesign)
                return DesignDocuments;
            if (IsSchemaDocument(document.DocumentId, json ?? document.Json))
                return SchemaDocuments;
            if (IsScriptDocument(json ?? document.Json))
                return ScriptDocuments;
            if (Schema?.Variant == null)
                return SchemalessDocuments;
            var name = CouchUtils.GetCategory(json ?? document.Json, Schema.Variant.Tag);
            if (name == null)
                return SchemalessDocuments;
            else
                return categoriesByName.GetValueOrDefault(name, SchemalessDocuments)!;
        }

        private static bool IsSchemaDocument(string documentId, ImmutableJsonObject json)
        {
            if (documentId == CouchUtils.SchemaDocumentId || documentId == CouchUtils.DiagramSchemaDocumentId || documentId == CouchUtils.JsonSchemaDocumentId)
                return true;
            var scope = CouchUtils.GetScope(json);
            return scope == "editor";
        }

        private static bool IsScriptDocument(ImmutableJsonObject json)
        {
            return CouchUtils.GetScope(json) == "script";
        }
    }
}
