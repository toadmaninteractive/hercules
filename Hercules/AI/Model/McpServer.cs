using Hercules.Documents;
using Hercules.Documents.Editor;
using Json;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public class McpServer
    {
        public Core Core { get; }

        public McpServer(Core core)
        {
            Core = core;
        }

        public async Task RunMcpAsync(CancellationToken ct = default)
        {
            var serverOptions = new McpServerOptions();
            serverOptions.ServerInfo = new ModelContextProtocol.Protocol.Implementation { Name = "Hercules", Version = Core.GetVersion().ToString(), Title = "Hercules design data database" };
            serverOptions.ServerInstructions = "Hercules is the database of JSON design documents. Each document describes a single ingame entity. Important properties: _id property is unique string id; category property is the document type. Which other properties are available for each category is defined by a special schema document.";
            serverOptions.Capabilities = new ModelContextProtocol.Protocol.ServerCapabilities();
            serverOptions.Capabilities.Tools = new()
            {
                ListChanged = true,
                ToolCollection = [
                    McpServerTool.Create(GetSchema, new() {
                        Name = nameof(GetSchema),
                        Description = "Gets Hercules design database schema as JSON.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetDocument, new()
                    {
                        Name = nameof(GetDocument),
                        Description = "Gets Hercules design document for the given id.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetDocuments, new()
                    {
                        Name = nameof(GetDocuments),
                        Description = "Gets Hercules design documents for the given list of ids. Prefer GetPropertyValuesForMultipleDocuments instead if you are only interested in the subset of document properties.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetCategoryList, new()
                    {
                        Name = nameof(GetCategoryList),
                        Description = "Gets the list of Hercules design document categories.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetAllDocumentIds, new()
                    {
                        Name = nameof(GetAllDocumentIds),
                        Description = "Gets the list of all Hercules design document IDs.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetOpenedDocumentIds, new()
                    {
                        Name = nameof(GetOpenedDocumentIds),
                        Description = "Gets the list of opened Hercules document IDs.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetDocumentIdsByCategory, new()
                    {
                        Name = nameof(GetDocumentIdsByCategory),
                        Description = "Gets the list of Hercules design document IDs belonging to the category.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(GetPropertyValuesForMultipleDocuments, new()
                    {
                        Name = nameof(GetPropertyValuesForMultipleDocuments),
                        Description = "Gets values for the specified property path for multiple Hercules documents. Returns the list of objects with document ID and property values.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(BatchUpdateDocuments, new()
                    {
                        Name = nameof(BatchUpdateDocuments),
                        Description = "Updates multiple values in Hercules documents. Accepts the list of JSON objects as input. Each object has three properties: id is document ID, path is the dot separated path to the property, and value is new JSON value of updated property.",
                        ReadOnly = false,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(CreateDocument, new()
                    {
                        Name = nameof(CreateDocument),
                        Description = "Create new Hercules document with the given id and JSON content. When using this function, make sure either to clone existing document or check schema document to ensure that properties are correct.",
                        ReadOnly = false,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                ]
            };

            var loggerFactory = new HerculesLoggerFactory();
            // Create and run server with stdio transport
            await using var stdioTransport = new StdioServerTransport(serverOptions, loggerFactory);
            var server = McpServerFactory.Create(stdioTransport, serverOptions, loggerFactory);
            await server.RunAsync(ct);
        }

        public string GetSchema()
        {
            return Core.Project.Database.Documents["schema"].Json.ToString();
        }

        public string? GetDocument(string id)
        {
            if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
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
                if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
                {
                    result.Add(doc.Json.ToString());
                }
            }
            return string.Join(Environment.NewLine, result);
        }

        public string GetCategoryList(string category)
        {
            var strings = Core.Project.SchemafulDatabase.Categories.Select(c => c.Name).ToArray();
            return string.Join(Environment.NewLine, strings);
        }

        public string GetAllDocumentIds()
        {
            var strings = Core.Project.Database.Documents.Select(d => d.Key).ToArray();
            return string.Join(Environment.NewLine, strings);
        }

        public string GetOpenedDocumentIds()
        {
            var documentIds = Core.Workspace.WindowService.Pages.OfType<DocumentEditorPage>().Select(d => d.Document.DocumentId).ToArray();
            if (documentIds.Length > 0)
                return string.Join(Environment.NewLine, documentIds);
            else
                return "There're no opened documents";
        }

        public string GetDocumentIdsByCategory(string category)
        {
            var categoryObject = Core.Project.SchemafulDatabase.Categories.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.CurrentCultureIgnoreCase));
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
                if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
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
                        if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
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
                Core.Workspace.Scheduler.ScheduleForegroundJob(() => Core.GetModule<DocumentsModule>().EditDocuments(updatedDocs));
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
            Core.Workspace.Scheduler.ScheduleForegroundJob(() => Core.GetModule<DocumentsModule>().CreateDocument(id, draft));
            return $"Document {id} created.";
        }
    }
}
