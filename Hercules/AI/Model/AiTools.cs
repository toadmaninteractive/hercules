using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Search;
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

        [AiTool("Gets Hercules document for the given id.", ReadOnly = true)]
        public string GetDocument(string id)
        {
            if (core.Project.Database.Documents.TryGetValue(id, out var doc))
            {
                return doc.Json.ToString();
            }
            return $"Document {id} not found";
        }

        [AiTool("Gets Hercules documents for the given list of ids. Prefer GetPropertyValuesForMultipleDocuments instead if you are only interested in the subset of document properties.", ReadOnly = true)]
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

        [AiTool("Gets the list of Hercules document categories.", ReadOnly = true)]
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

        [AiTool("Gets the list of all Hercules document IDs.", ReadOnly = true)]
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

        [AiTool("Gets the list of Hercules document IDs belonging to the category.", ReadOnly = true)]
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

        [AiTool("Search inside Hercules documents", ReadOnly = true)]
        public string SearchInDocuments(string text)
        {
            var search = new CustomSearchVisitor
            {
                Text = text,
                SearchText = true,
                SearchEnums = true,
                SearchKeys = true,
                SearchNumbers = false,
                SearchFields = false,
                MatchCase = false,
                WholeWord = false,
            };
            search.Search(core.Project.SchemafulDatabase.Schema, core.Project.SchemafulDatabase.SchemafulDocuments);
            if (search.Results.Documents.Count == 0)
                return "Nothing found";
            var json = new JsonArray();
            foreach (var result in search.Results.Documents)
            {
                var refArray = new JsonArray();
                foreach (var reference in result.Value.References)
                {
                    refArray.Add(new JsonObject
                    {
                        ["path"] = reference.Entry,
                        ["text"] = reference.Text,
                    });
                }
                var jsonObject = new JsonObject
                {
                    ["id"] = result.Key,
                    ["matches"] = refArray,
                };
                json.Add(jsonObject);
            }
            return json.ToString();
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
                        var path = LooseParseJsonPath(jsonPropertyPath);
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
                bool hasErrors = false;
                bool hasSuccess = false;
                var sb = new StringBuilder();
                Dictionary<string, ImmutableJsonObject> updatedDocs = new();
                foreach (var update in updates)
                {
                    var id = update.id;
                    var path = LooseParseJsonPath(update.path);
                    ImmutableJson json;
                    try
                    {
                        json = JsonParser.Parse(update.value);
                    }
                    catch
                    {
                        json = ImmutableJson.Create(update.value);
                    }
                    if (!updatedDocs.TryGetValue(id, out var updatedJson))
                    {
                        if (core.Project.Database.Documents.TryGetValue(id, out var doc))
                        {
                            updatedJson = doc.Json;
                        }
                        else
                        {
                            sb.AppendLine($"Error: Document {id} not found.");
                            hasErrors = true;
                            continue;
                        }
                    }
                    updatedJson = updatedJson.ForceUpdate(path, json).AsObject;
                    updatedDocs[id] = updatedJson;
                    hasSuccess = true;
                }
                core.Workspace.Scheduler.ScheduleForegroundJob(() => core.GetModule<DocumentsModule>().EditDocuments(updatedDocs));
                if (hasSuccess)
                {
                    if (hasErrors)
                    {
                        sb.AppendLine("Some of updates failed.");
                        return sb.ToString();
                    }
                    else
                    {
                        return "Batch document update succeeded. User must manually save modified documents.";
                    }
                }
                else
                {
                    sb.AppendLine("Failed to perform batch update. Try another approach.");
                    return sb.ToString();
                }
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

        private JsonPath LooseParseJsonPath(string pathString)
        {
            pathString = pathString.RemovePrefix("$.");
            var path = JsonPath.Empty;
            foreach (var node in JsonPath.Parse(pathString).Nodes)
            {
                switch (node)
                {
                    case JsonObjectPathNode objectNode:
                        if (int.TryParse(objectNode.Key, out var index))
                        {
                            path = path.AppendArray(index);
                        }
                        else
                        {
                            path = path.AppendObject(objectNode.Key);
                        }
                        break;

                    case JsonArrayPathNode arrayNode:
                        path = path.AppendArray(arrayNode.Index);
                        break;
                }
            }
            return path;
        }
    }
}
