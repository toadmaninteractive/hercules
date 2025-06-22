using Json;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiModule : CoreModule
    {
        public AiModule(Core core)
            : base(core)
        {
        }

        public override void OnLoad(Uri? startUri)
        {
            if (Core.HasCliArgument("-mcp"))
            {
                RunMcpAsync().Track();
            }
        }

        public async Task RunMcpAsync(CancellationToken ct = default)
        {
            var serverOptions = new McpServerOptions();
            serverOptions.ServerInfo = new ModelContextProtocol.Protocol.Implementation { Name = "Hercules", Version = Core.GetVersion().ToString(), Title = "Hercules design data database" };
            serverOptions.Capabilities = new ModelContextProtocol.Protocol.ServerCapabilities();
            // Add tools directly
            serverOptions.Capabilities.Tools = new()
            {
                ListChanged = true,
                ToolCollection = [
                    McpServerTool.Create(GetSchema, new() { 
                        Name = nameof(GetSchema), 
                        Description = "Gets Hercules design database schema as JSON.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                    McpServerTool.Create(GetDocument, new()
                    {
                        Name = nameof(GetDocument),
                        Description = "Gets Hercules design document for the given id.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                    McpServerTool.Create(GetDocuments, new()
                    {
                        Name = nameof(GetDocuments),
                        Description = "Gets Hercules design documents for the given list of ids.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                    McpServerTool.Create(GetCategoryList, new()
                    {
                        Name = nameof(GetCategoryList),
                        Description = "Gets the list of Hercules design document categories.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                    McpServerTool.Create(GetAllDocumentIds, new()
                    {
                        Name = nameof(GetAllDocumentIds),
                        Description = "Gets the list of all Hercules design document IDs.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                    McpServerTool.Create(GetDocumentIdsByCategory, new()
                    {
                        Name = nameof(GetDocumentIdsByCategory),
                        Description = "Gets the list of Hercules design documents belonging to the category.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                    McpServerTool.Create(GetPropertyValuesForMultipleDocuments, new()
                    {
                        Name = nameof(GetPropertyValuesForMultipleDocuments),
                        Description = "Gets values for the specified property path for multiple Hercules documents. Returns the list of objects with document ID and property values.",
                        ReadOnly = true,
                        Destructive = false,
                    }),
                ]
            };

            // Create and run server with stdio transport
            await using var stdioTransport = new StdioServerTransport(serverOptions);
            var server = McpServerFactory.Create(stdioTransport, serverOptions);
            await server.RunAsync(ct);
        }

        public string GetSchema()
        {
            return Core.Project.Database.Documents["schema"].Json.ToString();
        }

        public string GetDocument(string id)
        {
            if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
            {
                return doc.Json.ToString();
            }
            return "null";
        }

        public List<string> GetDocuments(string[] ids)
        {
            var result = new List<string>();
            foreach (var id in ids)
            {
                if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
                {
                    result.Add(doc.Json.ToString());
                }
            }
            return result;
        }

        public string[] GetCategoryList(string category)
        {
            return Core.Project.SchemafulDatabase.Categories.Select(c => c.Name).ToArray();
        }

        public string[] GetAllDocumentIds()
        {
            return Core.Project.Database.Documents.Select(d => d.Key).ToArray();
        }

        public string[] GetDocumentIdsByCategory(string category)
        {
            var categoryObject = Core.Project.SchemafulDatabase.Categories.FirstOrDefault(c => c.Name == category);
            if (categoryObject == null)
                return Array.Empty<string>();
            else
                return categoryObject.Documents.Select(d => d.DocumentId).ToArray();
        }

        public List<Dictionary<string, string>> GetPropertyValuesForMultipleDocuments(string[] ids, string[] jsonPropertyPaths)
        {
            List<Dictionary<string, string>> result = new();
            foreach (var id in ids)
            {
                var obj = new Dictionary<string, string>();
                obj["_id"] = id;
                if (Core.Project.Database.Documents.TryGetValue(id, out var doc))
                {
                    foreach (var jsonPropertyPath in jsonPropertyPaths)
                    {
                        var path = JsonPath.Parse(jsonPropertyPath);
                        if (doc.Json.TryFetch(path, out var value))
                            obj[jsonPropertyPath] = value?.ToString() ?? "null";
                    }
                }
                result.Add(obj);
            }
            return result;
        }
    }
}
