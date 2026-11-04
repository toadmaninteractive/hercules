using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
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
using System.Text.Json;

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

    public static class AiToolErrors
    {
        public static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
        }

        /// <summary>A normal tool failure: the tool reported it on purpose, or the agent passed wrong arguments.</summary>
        public static bool IsExpected(Exception exception)
        {
            return Unwrap(exception) is McpException or ArgumentException or JsonException;
        }

        /// <summary>Logs the failed tool call and returns the error text for the agent.</summary>
        public static string Report(string source, string toolName, Exception exception)
        {
            exception = Unwrap(exception);
            if (IsExpected(exception))
            {
                Logger.LogError($"{source} tool call {toolName} failed: {exception.Message}");
                return exception.Message;
            }
            Logger.LogException($"{source} tool call {toolName} failed", exception);
            return $"An error occurred invoking '{toolName}'.";
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

        [AiTool("Gets Hercules design database schema as JSON. It is big: prefer GetCategorySchema if you only work with some categories.", ReadOnly = true)]
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

        [AiTool("Gets the JSON schema of a single Hercules document category: the definition of the category and all definitions it references, as a self-contained JSON schema. Much smaller than GetSchema.", ReadOnly = true)]
        public string GetCategorySchema([Description("Category name, as returned by GetCategories. Case insensitive.")] string category)
        {
            return OnUi(project =>
            {
                if (!project.Database.Documents.TryGetValue(CouchUtils.JsonSchemaDocumentId, out var doc))
                    throw new AiToolException("The project has no JSON schema document. Use GetSchema instead.");
                if (!doc.Json.TryGetValue("$defs", out var defsJson) || !defsJson.IsObject)
                    throw new AiToolException("The schema document has no definitions. Use GetSchema instead.");
                var defs = defsJson.AsObject;

                var main = defs.FirstOrDefault(def => string.Equals(GetSchemaCategory(def.Value), category, StringComparison.CurrentCultureIgnoreCase));
                if (main.Key == null)
                    throw new AiToolException($"Category {category} not found. Available categories: {string.Join(", ", defs.Select(def => GetSchemaCategory(def.Value)).Where(name => name != null))}.");

                // The category definition plus everything it references, directly or not.
                var needed = new List<string> { main.Key };
                for (var i = 0; i < needed.Count; i++)
                {
                    foreach (var reference in FindSchemaReferences(defs[needed[i]]))
                    {
                        if (defs.ContainsKey(reference) && !needed.Contains(reference))
                            needed.Add(reference);
                    }
                }

                var neededDefs = new JsonObject();
                foreach (var name in needed)
                    neededDefs[name] = defs[name];
                var result = new JsonObject
                {
                    ["$ref"] = "#/$defs/" + main.Key,
                    ["$defs"] = neededDefs,
                };
                return result.ToImmutable().ToString();
            });
        }

        private static string? GetSchemaCategory(ImmutableJson definition)
        {
            return definition.TryFetch(new JsonPath("properties", "category", "const"), out var value) && value != null && value.IsString ? value.AsString : null;
        }

        private static IEnumerable<string> FindSchemaReferences(ImmutableJson node)
        {
            if (node.IsObject)
            {
                foreach (var property in node.AsObject)
                {
                    if (property.Key == "$ref" && property.Value.IsString)
                        yield return property.Value.AsString.RemovePrefix("#/$defs/");
                    else
                        foreach (var nested in FindSchemaReferences(property.Value))
                            yield return nested;
                }
            }
            else if (node.IsArray)
            {
                foreach (var item in node.AsArray)
                    foreach (var nested in FindSchemaReferences(item))
                        yield return nested;
            }
        }

        [AiTool("Gets Hercules document for the given id.", ReadOnly = true)]
        public string GetDocument([Description("Document _id.")] string id)
        {
            return OnUi(project =>
            {
                if (project.Database.Documents.TryGetValue(id, out var doc))
                    return GetJsonWithId(id, doc.DraftJson);
                throw new AiToolException($"Document {id} not found.");
            });
        }

        [AiTool("Gets Hercules documents for the given list of ids. Fails if any of the documents doesn't exist. At most 50 documents per call. Prefer QueryDocuments instead if you are only interested in the subset of document properties.", ReadOnly = true)]
        public string GetDocuments([Description("Document _id values.")] string[] ids)
        {
            if (ids.Length > MaxDocumentsPerCall)
                throw new AiToolException($"Too many documents requested ({ids.Length}). Request at most {MaxDocumentsPerCall} per call, or use QueryDocuments to get only the properties you need.");
            return OnUi(project =>
            {
                var result = new List<string>();
                var missing = new List<string>();
                foreach (var id in ids)
                {
                    if (project.Database.Documents.TryGetValue(id, out var doc))
                        result.Add(GetJsonWithId(id, doc.DraftJson));
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

        [AiTool("Gets the list of all Hercules document IDs, sorted. Long lists are paged: if the result is cut, its last line tells the offset to continue from.", ReadOnly = true)]
        public string GetAllDocumentIds(
            [Description("Maximum number of IDs to return.")] int limit = DefaultListLimit,
            [Description("Number of IDs to skip.")] int offset = 0)
        {
            return OnUi(project => FormatPage(project.Database.Documents.Select(d => d.Key), limit, offset));
        }

        private const int DefaultListLimit = 500;
        private const int MaxListLimit = 2000;
        private const int DefaultSearchLimit = 50;
        private const int MaxDocumentsPerCall = 50;

        private static string FormatPage(IEnumerable<string> items, int limit, int offset)
        {
            var all = items.OrderBy(item => item, StringComparer.Ordinal).ToList();
            (limit, offset) = ClampPaging(limit, offset);
            var page = all.Skip(offset).Take(limit).ToList();
            var text = string.Join(Environment.NewLine, page);
            if (page.Count == 0 && all.Count > 0)
                return $"[Nothing at offset {offset}: there are {all.Count} items in total.]";
            if (offset + page.Count < all.Count)
                text += $"{Environment.NewLine}[Showing {offset + 1}-{offset + page.Count} of {all.Count}. Call again with offset={offset + page.Count} for the rest.]";
            return text;
        }

        private static (int Limit, int Offset) ClampPaging(int limit, int offset)
        {
            return (Math.Clamp(limit, 1, MaxListLimit), Math.Max(offset, 0));
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

        [AiTool("Gets the list of Hercules document IDs belonging to the category, sorted. Fails if there's no such category. Long lists are paged: if the result is cut, its last line tells the offset to continue from.", ReadOnly = true)]
        public string GetDocumentIdsByCategory(
            [Description("Category name, as returned by GetCategories. Case insensitive.")] string category,
            [Description("Maximum number of IDs to return.")] int limit = DefaultListLimit,
            [Description("Number of IDs to skip.")] int offset = 0)
        {
            return OnUi(project => FormatPage(GetCategoryDocumentIds(project, category), limit, offset));
        }

        private static IEnumerable<string> GetCategoryDocumentIds(Project project, string category)
        {
            var categories = project.SchemafulDatabase.Categories;
            var categoryObject = categories.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.CurrentCultureIgnoreCase));
            if (categoryObject == null)
                throw new AiToolException($"Category {category} not found. Available categories: {string.Join(", ", categories.Select(c => c.Name))}.");
            return categoryObject.Documents.Select(d => d.DocumentId);
        }

        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);
        private const long QueryMaxMemoryBytes = 512L * 1024 * 1024;

        [AiTool("""
            Queries Hercules documents with a jq program (jq 1.7 syntax, gojq-compatible) and returns the results, one compact JSON value per line.
            The input (.) is an array of documents, including unsaved changes; every document has _id. Restrict it with category and/or ids.
            Use it for filtering, projecting, counting and for searching nested or recursive structures, e.g.:
              .[] | select(.category == "resource" and .mass > 10) | {_id, name, mass}
              .[] | ._id as $id | .. | objects | select(.type? == "target_impact") | {doc: $id, target, inner: .impact.type}
              group_by(.category) | map({category: .[0].category, count: length})
            Some properties of given documents (ids = ["a", "b"]): .[] | {_id, name, mass}. A missing property is null; has("name") tells it from a real null.
            path(..) gives the location of nested values, e.g. [paths] or path(.. | select(type == "number")).
            Not available: env, $ENV, input, modules, debug, stderr. The query is stopped after 10 s or 512 MB, and the output is capped: use limit(n; ...), select and projections to keep it small.
            """, ReadOnly = true)]
        public string QueryDocuments(
            [Description("The jq program.")] string query,
            [Description("Only include documents of this category, as returned by GetCategories. Case insensitive.")] string? category = null,
            [Description("Only include documents with these _id values.")] string[]? ids = null)
        {
            // Only references are collected on the UI thread (the JSON is immutable); the query runs on the caller's thread.
            var documents = OnUi(project => SelectQueryDocuments(project, category, ids));
            var inputJson = "[" + string.Join(",", documents.Select(document => GetJsonWithId(document.Id, document.Json))) + "]";
            return JqRunner.Run(query, inputJson, QueryTimeout, QueryMaxMemoryBytes);
        }

        private static List<(string Id, ImmutableJsonObject Json)> SelectQueryDocuments(Project project, string? category, string[]? ids)
        {
            var documentIds = category == null ? project.Database.Documents.Select(d => d.Key) : GetCategoryDocumentIds(project, category);
            if (ids != null)
            {
                var missing = ids.Where(id => !project.Database.Documents.ContainsKey(id)).ToList();
                if (missing.Count > 0)
                    throw new AiToolException($"Documents not found: {string.Join(", ", missing)}.");
                documentIds = documentIds.Intersect(ids);
            }
            return documentIds
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => (id, project.Database.Documents[id].DraftJson))
                .ToList();
        }

        // The draft JSON of a document that was never saved has no _id.
        private static string GetJsonWithId(string id, ImmutableJsonObject json)
        {
            return json.ContainsKey("_id") ? json.ToString() : new JsonObject(json) { ["_id"] = id }.ToImmutable().ToString();
        }

        [AiTool("Search inside Hercules documents. Matches text values, enum values and property names, ignoring case. Returns a JSON object: total number of matching documents, offset, the documents (id and the paths of matches), and next_offset if there are more documents to get.", ReadOnly = true)]
        public string SearchInDocuments(
            [Description("Text to search for.")] string text,
            [Description("Maximum number of matching documents to return.")] int limit = DefaultSearchLimit,
            [Description("Number of matching documents to skip.")] int offset = 0)
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
                var total = search.Results.Documents.Count;
                if (total == 0)
                    return "Nothing found";
                (limit, offset) = ClampPaging(limit, offset);
                var json = new JsonArray();
                var page = search.Results.Documents.OrderBy(result => result.Key, StringComparer.Ordinal).Skip(offset).Take(limit).ToList();
                foreach (var result in page)
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
                var response = new JsonObject
                {
                    ["total"] = total,
                    ["offset"] = offset,
                    ["documents"] = json,
                };
                if (offset + page.Count < total)
                    response["next_offset"] = offset + page.Count;
                return response.ToImmutable().ToString();
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Protocol usage")]
        public record BatchDocumentUpdateEntry(
            [property: Description("Document _id.")] string id,
            [property: Description("Dot separated path to the property, e.g. 'name' or 'items.0.cost'. Numeric segments are array indexes. Missing parts of the path are created; an array index past the end pads the array with nulls.")] string path,
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
            var json = ParseJsonObject(jsonString);

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
                    sb.Append(FormatIssues(editor.FormTab.GetIssues()));
                sb.AppendLine(exists ? $"Document {id} updated." : $"Document {id} created.");
                return sb.ToString();
            });
        }

        [AiTool("Checks a Hercules document against the schema, without changing anything and without opening an editor. Pass id to check the current content of an existing document (including unsaved changes), or jsonString to check a document before creating it. Returns the issues, or says that there are none.", ReadOnly = true)]
        public string ValidateDocument(
            [Description("Id of an existing document.")] string? id = null,
            [Description("Content of a document that does not exist yet: a JSON object that has its category.")] string? jsonString = null)
        {
            if ((id == null) == (jsonString == null))
                throw new AiToolException("Pass either id or jsonString.");
            var proposedJson = jsonString == null ? null : ParseJsonObject(jsonString);

            return OnUi(project =>
            {
                ImmutableJsonObject json;
                SchemaRecord schema;
                if (id != null)
                {
                    if (!project.Database.Documents.TryGetValue(id, out var doc))
                        throw new AiToolException($"Document {id} not found.");
                    json = doc.DraftJson;
                    project.SchemafulDatabase.GetDocumentSchema(doc, json, out _, out schema);
                }
                else
                {
                    json = proposedJson!;
                    schema = project.SchemafulDatabase.Schema.Variant.GetChildForJson(json)
                        ?? throw new AiToolException($"The document has no known category. Available categories: {string.Join(", ", project.SchemafulDatabase.Categories.Select(c => c.Name))}.");
                }
                // Documents of a schemaless category have nothing to be validated against.
                if (schema == SchemaRecord.Default)
                    throw new AiToolException("The document has no schema, so there is nothing to validate.");

                var issues = core.GetModule<DocumentsModule>().Validate(json, schema);
                return issues.Count == 0 ? "No issues found." : FormatIssues(issues);
            });
        }

        private static ImmutableJsonObject ParseJsonObject(string jsonString)
        {
            try
            {
                return JsonParser.Parse(jsonString).AsObject;
            }
            catch (Exception exception)
            {
                throw new AiToolException($"jsonString must be a valid JSON object: {exception.Message}");
            }
        }

        private const int MaxSupportedValuesShown = 15;

        private static string FormatIssues(IEnumerable<FormIssue> issues)
        {
            var sb = new StringBuilder();
            foreach (var issue in issues)
            {
                sb.Append($"[{issue.Severity}] {issue.Path}: {issue.Message}.");
                if (issue.SupportedValues != null)
                {
                    // A reference to a big category would list hundreds of document ids.
                    var values = issue.SupportedValues.ToList();
                    var more = values.Count > MaxSupportedValuesShown ? $" ... and {values.Count - MaxSupportedValuesShown} more" : "";
                    sb.Append($" Supported values: {string.Join(", ", values.Take(MaxSupportedValuesShown))}{more}.");
                }
                sb.AppendLine();
            }
            return sb.ToString();
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
