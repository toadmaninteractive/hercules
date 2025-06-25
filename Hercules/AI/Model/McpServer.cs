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
        public AiTools Tools { get; }

        public McpServer(AiTools aiTools)
        {
            Tools = aiTools;
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
                    McpServerTool.Create(Tools.GetSchema, new() {
                        Name = nameof(AiTools.GetSchema),
                        Description = "Gets Hercules design database schema as JSON.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetDocument, new()
                    {
                        Name = nameof(AiTools.GetDocument),
                        Description = "Gets Hercules design document for the given id.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetDocuments, new()
                    {
                        Name = nameof(AiTools.GetDocuments),
                        Description = "Gets Hercules design documents for the given list of ids. Prefer GetPropertyValuesForMultipleDocuments instead if you are only interested in the subset of document properties.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetCategoryList, new()
                    {
                        Name = nameof(AiTools.GetCategoryList),
                        Description = "Gets the list of Hercules design document categories.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetAllDocumentIds, new()
                    {
                        Name = nameof(AiTools.GetAllDocumentIds),
                        Description = "Gets the list of all Hercules design document IDs.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetOpenedDocumentIds, new()
                    {
                        Name = nameof(AiTools.GetOpenedDocumentIds),
                        Description = "Gets the list of opened Hercules document IDs.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetDocumentIdsByCategory, new()
                    {
                        Name = nameof(AiTools.GetDocumentIdsByCategory),
                        Description = "Gets the list of Hercules design document IDs belonging to the category.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.GetPropertyValuesForMultipleDocuments, new()
                    {
                        Name = nameof(AiTools.GetPropertyValuesForMultipleDocuments),
                        Description = "Gets values for the specified property path for multiple Hercules documents. Returns the list of objects with document ID and property values.",
                        ReadOnly = true,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.BatchUpdateDocuments, new()
                    {
                        Name = nameof(AiTools.BatchUpdateDocuments),
                        Description = "Updates multiple values in Hercules documents. Accepts the list of JSON objects as input. Each object has three properties: id is document ID, path is the dot separated path to the property, and value is new JSON value of updated property.",
                        ReadOnly = false,
                        Destructive = false,
                        OpenWorld = false,
                    }),
                    McpServerTool.Create(Tools.CreateDocument, new()
                    {
                        Name = nameof(AiTools.CreateDocument),
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
    }
}
