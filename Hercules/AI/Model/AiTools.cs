using Hercules.Documents;
using Hercules.Documents.Editor;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hercules.AI
{
    public class AiToolAttribute : Attribute
    {
        public string Description { get; }
        public bool ReadOnly { get; set; }
        public bool Destructive { get; set; }
        public bool OpenWorld { get; set; }
        public AiToolAttribute(string description)
        {
            Description = description;
        }
    }

    public class AiTools
    {
        private readonly Core core;

        public AiTools(Core core)
        {
            this.core = core;
        }

        [AiTool("Gets Hercules design database schema as JSON.", ReadOnly = true)]
        public string GetSchema()
        {
            return core.Project.Database.Documents["schema"].Json.ToString();
        }

        [AiTool("Gets Hercules design document for the given id.", ReadOnly = true)]
        public string? GetDocument(string id)
        {
            if (core.Project.Database.Documents.TryGetValue(id, out var doc))
            {
                return doc.Json.ToString();
            }
            return null;
        }

        [AiTool("Gets Hercules design documents for the given list of ids. Prefer GetPropertyValuesForMultipleDocuments instead if you are only interested in the subset of document properties.", ReadOnly = true)]
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

        [AiTool("Gets the list of Hercules design document categories.", ReadOnly = true)]
        public string GetCategoryList(string category)
        {
            var sb = new StringBuilder();
            foreach (var cat in core.Project.SchemafulDatabase.Categories)
            {
                sb.Append(cat.Name);
                if (!string.IsNullOrEmpty(cat.AiHint))
                {
                    sb.Append($" (comment: {cat.AiHint})");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        [AiTool("Gets the list of all Hercules design document IDs.", ReadOnly = true)]
        public string GetAllDocumentIds()
        {
            var strings = core.Project.Database.Documents.Select(d => d.Key).ToArray();
            return string.Join(Environment.NewLine, strings);
        }

        [AiTool("Gets the list of opened Hercules document IDs.", ReadOnly = true)]
        public string GetOpenedDocumentIds()
        {
            var documentIds = core.Workspace.WindowService.Pages.OfType<DocumentEditorPage>().Select(d => d.Document.DocumentId).ToArray();
            if (documentIds.Length > 0)
                return string.Join(Environment.NewLine, documentIds);
            else
                return "There're no opened documents";
        }

        [AiTool("Gets the list of Hercules design document IDs belonging to the category.", ReadOnly = true)]
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

        [AiTool("Gets values for the specified property path for multiple Hercules documents. Returns the list of objects with document ID and property values.", ReadOnly = true)]
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

        [AiTool("Updates multiple values in Hercules documents. Accepts the list of JSON objects as input. Each object has three properties: id is document ID, path is the dot separated path to the property, and value is new JSON value of updated property.")]
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

        [AiTool("Create new Hercules document with the given id and JSON content. When using this function, make sure either to clone existing document or check schema document to ensure that properties are correct.")]
        public string CreateDocument(string id, string jsonString)
        {
            var json = JsonParser.Parse(jsonString);
            var draft = new DocumentDraft(json.AsObject);
            core.Workspace.Scheduler.ScheduleForegroundJob(() => core.GetModule<DocumentsModule>().CreateDocument(id, draft));
            return $"Document {id} created.";
        }
    }
}
