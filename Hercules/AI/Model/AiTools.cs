using Hercules.Documents;
using Hercules.Documents.Editor;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.AI
{
    public class AiTools
    {
        private readonly Core core;

        public AiTools(Core core)
        {
            this.core = core;
        }

        public string GetSchema()
        {
            return core.Project.Database.Documents["schema"].Json.ToString();
        }

        public string? GetDocument(string id)
        {
            if (core.Project.Database.Documents.TryGetValue(id, out var doc))
            {
                return doc.Json.ToString();
            }
            return null;
        }

        public string GetDocuments(string[] ids)
        {
            var result = new List<string>();
            foreach (var id in ids)
            {
                if (core.Project.Database.Documents.TryGetValue(id, out var doc))
                {
                    result.Add(doc.Json.ToString());
                }
            }
            return string.Join(Environment.NewLine, result);
        }

        public string GetCategoryList(string category)
        {
            var strings = core.Project.SchemafulDatabase.Categories.Select(c => c.Name).ToArray();
            return string.Join(Environment.NewLine, strings);
        }

        public string GetAllDocumentIds()
        {
            var strings = core.Project.Database.Documents.Select(d => d.Key).ToArray();
            return string.Join(Environment.NewLine, strings);
        }

        public string GetOpenedDocumentIds()
        {
            var documentIds = core.Workspace.WindowService.Pages.OfType<DocumentEditorPage>().Select(d => d.Document.DocumentId).ToArray();
            if (documentIds.Length > 0)
                return string.Join(Environment.NewLine, documentIds);
            else
                return "There're no opened documents";
        }

        public string GetDocumentIdsByCategory(string category)
        {
            var categoryObject = core.Project.SchemafulDatabase.Categories.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.CurrentCultureIgnoreCase));
            if (categoryObject == null)
                return "No documents found";
            else
            {
                var strings = categoryObject.Documents.Select(d => d.DocumentId).ToArray();
                return string.Join(Environment.NewLine, strings);
            }
        }

        public string GetPropertyValuesForMultipleDocuments(string[] ids, string[] jsonPropertyPaths)
        {
            JsonArray result = new();
            foreach (var id in ids)
            {
                var obj = new JsonObject();
                obj["_id"] = id;
                if (core.Project.Database.Documents.TryGetValue(id, out var doc))
                {
                    foreach (var jsonPropertyPath in jsonPropertyPaths)
                    {
                        var path = JsonPath.Parse(jsonPropertyPath.RemovePrefix("$."));
                        if (doc.Json.TryFetch(path, out var value))
                            obj[jsonPropertyPath] = value?.ToString() ?? "null";
                    }
                }
                result.Add(obj);
            }
            return result.ToImmutable().ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Protocol usage")]
        public record BatchDocumentUpdateEntry(string id, string path, string value);

        public string BatchUpdateDocuments(List<BatchDocumentUpdateEntry> updates)
        {
            try
            {
                Dictionary<string, ImmutableJsonObject> updatedDocs = new();
                foreach (var update in updates)
                {
                    var id = update.id;
                    var path = JsonPath.Parse(update.path.RemovePrefix("$."));
                    ImmutableJson json = JsonParser.Parse(update.value);
                    if (!updatedDocs.TryGetValue(id, out var updatedJson))
                    {
                        if (core.Project.Database.Documents.TryGetValue(id, out var doc))
                        {
                            updatedJson = doc.Json;
                        }
                        else
                        {
                            continue;
                        }
                        updatedJson = updatedJson.ForceUpdate(path, json).AsObject;
                        updatedDocs[id] = updatedJson;
                    }
                }
                core.Workspace.Scheduler.ScheduleForegroundJob(() => core.GetModule<DocumentsModule>().EditDocuments(updatedDocs));
                return "Batch document update succeeded.";
            }
            catch (Exception exception)
            {
                Logger.LogException(exception);
                throw;
            }
        }

        public string CreateDocument(string id, string jsonString)
        {
            var json = JsonParser.Parse(jsonString);
            var draft = new DocumentDraft(json.AsObject);
            core.Workspace.Scheduler.ScheduleForegroundJob(() => core.GetModule<DocumentsModule>().CreateDocument(id, draft));
            return $"Document {id} created.";
        }
    }
}
