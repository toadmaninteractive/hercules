using Hercules.Documents;
using Hercules.Documents.Editor;
using Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public class McpServer
    {
        private readonly AiTools tools;

        public McpServer(AiTools aiTools)
        {
            tools = aiTools;
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
                ToolCollection = []
            };
            var mcpTools = 
                from methodIndo in tools.GetType().GetMethods()
                let attr = methodIndo.GetCustomAttribute<AiToolAttribute>()
                where attr != null
                select McpServerTool.Create(ReflectionHelper.CreateDelegate(methodIndo, tools), new()
                {
                    Name = methodIndo.Name,
                    Description = attr.GetDescription(tools),
                    ReadOnly = attr.ReadOnly,
                    Destructive = attr.Destructive,
                    OpenWorld = attr.OpenWorld,
                });
            foreach (var tool in mcpTools)
                serverOptions.Capabilities.Tools.ToolCollection.Add(tool);

            var loggerFactory = new HerculesLoggerFactory();
            // Create and run server with stdio transport
            await using var stdioTransport = new StdioServerTransport(serverOptions, loggerFactory);
            var server = McpServerFactory.Create(stdioTransport, serverOptions, loggerFactory);
            await server.RunAsync(ct);
        }
    }
}
