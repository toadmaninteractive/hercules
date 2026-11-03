using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Scripting;
using Hercules.Scripting.JavaScript;
using Hercules.Search;
using Json;
using ModelContextProtocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Hercules.AI
{
    public class AiToolAttribute : Attribute
    {
        public string Description { get; }
        public string? DescriptionProperty { get; init; }
        public bool ReadOnly { get; init; }
        public bool Destructive { get; init; }
        public bool OpenWorld { get; init; }
        public AiToolAttribute(string description)
        {
            Description = description;
        }

        public string GetDescription(object instance)
        {
            if (DescriptionProperty != null)
            {
                var property = instance.GetType().GetProperty(DescriptionProperty);
                if (property == null)
                    throw new InvalidOperationException($"{instance.GetType()} has no property {DescriptionProperty}");
                return property.GetValue(instance)!.ToString()!;
            }
            return Description;
        }
    }

    /// <summary>An error the tool wants the AI agent to see (reported as a failed tool call, not as a normal result).</summary>
    public class AiToolException : McpException
    {
        public AiToolException(string message)
            : base(message)
        {
        }
    }

    public class AiTools
    {
        private readonly Core core;

        public AiTools(Core core)
        {
            this.core = core;
        }

        /// <summary>
        /// Runs the action on the UI thread with the active project. Documents and their editors are owned by the UI thread,
        /// while MCP requests arrive on the thread pool.
        /// </summary>
        private T OnUi<T>(Func<Project, T> action)
        {
            return core.Workspace.Scheduler.ScheduleForegroundJob(() => action(core.Project ?? throw new AiToolException("No project is open.")));
        }

        [AiTool("Gets Hercules design database schema as JSON.", ReadOnly = true)]
        public string GetSchema()
        {
            return OnUi(project =>
            {
                if (project.Database.Documents.TryGetValue(CouchUtils.JsonSchemaDocumentId, out var doc) ||
                    project.Database.Documents.TryGetValue(CouchUtils.SchemaDocumentId, out doc))
                    return doc.Json.ToString();
                throw new AiToolException("Schema document not found.");
            });
        }

        [AiTool("Gets Hercules document for the given id.", ReadOnly = true)]
        public string GetDocument([Description("Document _id.")] string id)
        {
            return OnUi(project =>
            {
                if (project.Database.Documents.TryGetValue(id, out var doc))
                    return doc.DraftJson.ToString();
                throw new AiToolException($"Document {id} not found.");
            });
        }

        [AiTool("Gets Hercules documents for the given list of ids. Fails if any of the documents doesn't exist. Prefer GetPropertyValuesForMultipleDocuments instead if you are only interested in the subset of document properties.", ReadOnly = true)]
        public string GetDocuments([Description("Document _id values.")] string[] ids)
        {
            return OnUi(project =>
            {
                var result = new List<string>();
                var missing = new List<string>();
                foreach (var id in ids)
                {
                    if (project.Database.Documents.TryGetValue(id, out var doc))
                        result.Add(doc.DraftJson.ToString());
                    else
                        missing.Add(id);
                }
                if (missing.Count > 0)
                    throw new AiToolException($"Documents not found: {string.Join(", ", missing)}.");
                return string.Join(Environment.NewLine, result);
            });
        }

        [AiTool("Gets the list of Hercules document categories.", ReadOnly = true)]
        public string GetCategories()
        {
            return OnUi(project =>
            {
                var sb = new StringBuilder();
                foreach (var cat in project.SchemafulDatabase.Categories)
                {
                    sb.Append(cat.Name);
                    if (!string.IsNullOrEmpty(cat.AiHint))
                    {
                        sb.Append($" (comment: {cat.AiHint})");
                    }
                    sb.AppendLine();
                }
                return sb.ToString();
            });
        }

        [AiTool("Gets the list of all Hercules document IDs.", ReadOnly = true)]
        public string GetAllDocumentIds()
        {
            return OnUi(project => string.Join(Environment.NewLine, project.Database.Documents.Select(d => d.Key)));
        }

        [AiTool("Gets the list of opened Hercules document IDs.", ReadOnly = true)]
        public string GetOpenedDocumentIds()
        {
            return OnUi(_ =>
            {
                var documentIds = core.Workspace.WindowService.Pages.OfType<DocumentEditorPage>().Select(d => d.Document.DocumentId).ToArray();
                if (documentIds.Length > 0)
                    return string.Join(Environment.NewLine, documentIds);
                else
                    return "There're no opened documents";
            });
        }

        [AiTool("Gets the list of Hercules document IDs belonging to the category. Fails if there's no such category.", ReadOnly = true)]
        public string GetDocumentIdsByCategory([Description("Category name, as returned by GetCategories. Case insensitive.")] string category)
        {
            return OnUi(project =>
            {
                var categories = project.SchemafulDatabase.Categories;
                var categoryObject = categories.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.CurrentCultureIgnoreCase));
                if (categoryObject == null)
                    throw new AiToolException($"Category {category} not found. Available categories: {string.Join(", ", categories.Select(c => c.Name))}.");
                return string.Join(Environment.NewLine, categoryObject.Documents.Select(d => d.DocumentId));
            });
        }

        [AiTool("Search inside Hercules documents. Matches text values, enum values and property names, ignoring case. Returns the list of matching document ids with the paths of matches.", ReadOnly = true)]
        public string SearchInDocuments([Description("Text to search for.")] string text)
        {
            return OnUi(project =>
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
                    SearchDraft = true,
                };
                search.Search(project.SchemafulDatabase.Schema, project.SchemafulDatabase.SchemafulDocuments);
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
            });
        }

        [AiTool("Gets values for the specified property paths for multiple Hercules documents. Returns the list of objects with document ID (_id) and property values as JSON, keyed by the requested paths. A property that is null in the document is returned as null. Paths that don't exist in a document are not returned as properties, but listed in the _missing array of that object. Fails if any of the documents doesn't exist.", ReadOnly = true)]
        public string GetPropertyValuesForMultipleDocuments(
            [Description("Document _id values.")] string[] ids,
            [Description("Dot separated property paths, e.g. 'name' or 'items.0.cost'. Numeric segments are array indexes.")] string[] jsonPropertyPaths)
        {
            return OnUi(project =>
            {
                JsonArray result = new();
                var missing = new List<string>();
                foreach (var id in ids)
                {
                    var obj = new JsonObject();
                    obj["_id"] = id;
                    if (project.Database.Documents.TryGetValue(id, out var doc))
                    {
                        var missingPaths = new JsonArray();
                        foreach (var jsonPropertyPath in jsonPropertyPaths)
                        {
                            var path = LooseParseJsonPath(jsonPropertyPath);
                            if (doc.DraftJson.TryFetch(path, out var value))
                                obj[jsonPropertyPath] = value ?? ImmutableJson.Null;
                            else
                                missingPaths.Add(jsonPropertyPath);
                        }
                        if (missingPaths.Count > 0)
                            obj["_missing"] = missingPaths;
                    }
                    else
                    {
                        missing.Add(id);
                    }
                    result.Add(obj);
                }
                if (missing.Count > 0)
                    throw new AiToolException($"Documents not found: {string.Join(", ", missing)}.");
                return result.ToImmutable().ToString();
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Protocol usage")]
        public record BatchDocumentUpdateEntry(
            [property: Description("Document _id.")] string id,
            [property: Description("Dot separated path to the property, e.g. 'name' or 'items.0.cost'. Numeric segments are array indexes. Missing parts of the path are created.")] string path,
            [property: Description("New value of the property, encoded as JSON: 5, true, null, \"text\" (strings must be quoted), {\"a\": 1}, [1, 2].")] string value);

        [AiTool("Updates multiple values in Hercules documents. Accepts the list of JSON objects as input. Each object has three properties: id is document ID, path is the dot separated path to the property, and value is new JSON value of updated property. Changes are applied to the document editors (including their unsaved changes) and are not saved to the database.", Destructive = true)]
        public string BatchUpdateDocuments([Description("The list of updates.")] List<BatchDocumentUpdateEntry> updates)
        {
            try
            {
                return OnUi(project =>
                {
                    var errors = new StringBuilder();
                    Dictionary<string, EditDocumentRequest> updatedDocs = new();
                    foreach (var update in updates)
                    {
                        var id = update.id;
                        var path = LooseParseJsonPath(update.path);
                        ImmutableJson json;
                        try
                        {
                            json = JsonParser.Parse(update.value);
                        }
                        catch (Exception exception)
                        {
                            errors.AppendLine($"Error: Value for {id} {update.path} is not valid JSON ({exception.Message}). Strings must be quoted, e.g. \"text\".");
                            continue;
                        }
                        if (!updatedDocs.TryGetValue(id, out var updateRequest))
                        {
                            if (!project.Database.Documents.TryGetValue(id, out var doc))
                            {
                                errors.AppendLine($"Error: Document {id} not found.");
                                continue;
                            }
                            // Start from the draft, so unsaved changes made in the editor are preserved.
                            updateRequest = new EditDocumentRequest(doc, Json: doc.DraftJson, Path: path);
                        }
                        updateRequest = updateRequest with { Json = updateRequest.Json!.ForceUpdate(path, json).AsObject };
                        updatedDocs[id] = updateRequest;
                    }

                    if (updatedDocs.Count == 0)
                        throw new AiToolException($"Failed to perform batch update. {errors}".TrimEnd());
                    core.GetModule<DocumentsModule>().EditDocuments(updatedDocs.Values.ToList());
                    if (errors.Length > 0)
                        return $"Some of updates failed. Other updates succeeded, user must manually save modified documents.{Environment.NewLine}{errors}".TrimEnd();
                    return "Batch document update succeeded. User must manually save modified documents.";
                });
            }
            catch (AiToolException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Logger.LogException(exception);
                throw new AiToolException($"Failed to perform batch update: {exception.Message}");
            }
        }

        [AiTool("Create new Hercules document with the given id and JSON content. Fails if the document already exists, unless overwrite is true (then the document content is replaced entirely; prefer BatchUpdateDocuments to change individual properties). When using this function, make sure either to clone existing document or check schema document to ensure that properties are correct.", Destructive = true)]
        public string CreateDocument(
            [Description("Id of the new document.")] string id,
            [Description("Document content, a JSON object.")] string jsonString,
            [Description("Replace the content of the document if it already exists.")] bool overwrite = false)
        {
            ImmutableJsonObject json;
            try
            {
                json = JsonParser.Parse(jsonString).AsObject;
            }
            catch (Exception exception)
            {
                throw new AiToolException($"jsonString must be a valid JSON object: {exception.Message}");
            }

            return OnUi(project =>
            {
                var sb = new StringBuilder();
                DocumentEditorPage editor;
                var exists = project.Database.Documents.ContainsKey(id);
                if (exists)
                {
                    if (!overwrite)
                        throw new AiToolException($"Document {id} already exists. Pass overwrite=true to replace its content, or use BatchUpdateDocuments to change individual properties.");
                    editor = core.GetModule<DocumentsModule>().EditDocument(id, json);
                }
                else
                {
                    editor = core.GetModule<DocumentsModule>().CreateDocument(id, new DocumentDraft(json));
                }
                var root = editor.FormTab.Form.Root;
                if (!root.IsValid)
                {
                    foreach (var issue in editor.FormTab.GetIssues())
                    {
                        sb.Append($"[{issue.Severity}] {issue.Path}: {issue.Message}.");
                        if (issue.SupportedValues != null)
                        {
                            sb.Append($" Supported values: {string.Join(", ", issue.SupportedValues)}.");
                        }
                        sb.AppendLine();
                    }
                }
                sb.AppendLine(exists ? $"Document {id} updated." : $"Document {id} created.");
                return sb.ToString();
            });
        }

        [AiTool("Expose/open the CSV table to the user.")]
        public string ShowCsvTable([Description("Table page title.")] string title, [Description("Table content as CSV.")] string csv)
        {
            core.Workspace.Scheduler.ScheduleForegroundJob(() =>
                core.Workspace.WindowService.OpenPage(new TablePage(title, CustomTable.LoadFromCsv(csv))));
            return "Csv table opened";
        }

        [AiTool("Get the content of exposed (currently opened) table as CSV. Fails if there's no opened table.", ReadOnly = true)]
        public string GetOpenedTable()
        {
            return core.Workspace.Scheduler.ScheduleForegroundJob(() =>
            {
                var table = core.Workspace.WindowService.Pages.OfType<TablePage>().FirstOrDefault();
                if (table == null)
                    throw new AiToolException("There's no currently opened table.");
                return table.Table.AsCsv();
            });
        }

        public string RunJavaScriptDescription
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(@"Run JavaScript (ECMAScript 2023) code in the context of Hercules. 
Use it for batch document api.
Prefer this tool when you need to update more than 10 documents at once.
DO NOT use console API.
Note that documents are JSON objects and have their id in ""_id"" property.
Useful API:");
                GetApiDescriptionForAI<HerculesDbApi>(sb);
                GetApiDescriptionForAI<HerculesJsApi>(sb);
                return sb.ToString();
            }
        }

        private static void GetApiDescriptionForAI<T>(StringBuilder sb)
        {
            foreach (var member in typeof(T).GetMembers())
            {
                var attr = member.GetCustomAttribute<ScriptingApiAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.AiHint))
                {
                    sb.AppendLine(attr.AiHint);
                }
            }
        }

        private static readonly TimeSpan ScriptTimeout = TimeSpan.FromSeconds(30);

        [AiTool("", DescriptionProperty = nameof(RunJavaScriptDescription), Destructive = true)]
        public string RunJavaScript([Description("JavaScript code to run.")] string script)
        {
            return core.Workspace.Scheduler.ScheduleForegroundJob(() =>
            {
                bool result;
                string? error = null;
                var context = core.GetModule<ScriptingModule>().CreateScriptContext();
                var errors = JsHost.SyntaxCheck(script);
                if (errors == null)
                {
                    var host = new JsHost(context.ScriptingModuleProvider, ScriptTimeout);
                    host.SetValue("hercules", new HerculesJsApi(context, host).Api);
                    try
                    {
                        host.Execute(script);
                        result = true;
                    }
                    catch (Exception e)
                    {
                        error = e.Message;
                        result = false;
                    }
                }
                else
                {
                    error = errors[0].Message;
                    result = false;
                }

                if (result)
                {
                    context.ActiveDatabaseContext.Flush();
                    core.GetModule<DocumentsModule>().EditDocuments(context.ActiveDatabaseContext.DocumentCache);
                }

                if (result)
                {
                    var sb = new StringBuilder();
                    foreach (var message in context.LogMessages)
                    {
                        sb.AppendLine(message);
                    }
                    sb.AppendLine("JavaScript code executed successfully.");
                    return sb.ToString();
                }
                else
                    throw new AiToolException($"Script execution error: {error}");
            });
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
