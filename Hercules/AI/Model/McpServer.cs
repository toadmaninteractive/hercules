using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.AI
{
    /// <summary>
    /// Stateless MCP server over Streamable HTTP, hosted on HttpListener (no ASP.NET Core dependency).
    /// </summary>
    public class McpServer
    {
        private const string Path = "/mcp";

        private readonly AiTools tools;
        private readonly int port;
        private readonly string projectTitle;
        // The SDK logs every message at Info/Debug; only its warnings and errors are interesting.
        private readonly HerculesLoggerFactory loggerFactory = new(Microsoft.Extensions.Logging.LogLevel.Warning);

        public McpServer(AiTools aiTools, int port, string projectTitle)
        {
            tools = aiTools;
            this.port = port;
            this.projectTitle = projectTitle;
        }

        private McpServerOptions CreateOptions()
        {
            var serverOptions = new McpServerOptions();
            serverOptions.ServerInfo = new Implementation { Name = "Hercules", Version = Core.GetVersion().ToString(), Title = "Hercules design data database" };
            serverOptions.ServerInstructions = "Hercules is the database of JSON design documents. Each document describes a single ingame entity. Important properties: _id property is unique string id; category property is the document type. Which other properties are available for each category is defined by a special schema document.";
            serverOptions.Capabilities = new ServerCapabilities();
            serverOptions.Capabilities.Tools = new()
            {
                ListChanged = true,
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
            serverOptions.ToolCollection = new McpServerPrimitiveCollection<McpServerTool>();
            foreach (var tool in mcpTools)
                serverOptions.ToolCollection.Add(tool);
            serverOptions.Filters.Request.CallToolFilters.Add(LogToolCall);
            return serverOptions;
        }

        private static McpRequestHandler<CallToolRequestParams, CallToolResult> LogToolCall(McpRequestHandler<CallToolRequestParams, CallToolResult> next)
        {
            return async (request, ct) =>
            {
                var arguments = request.Params?.Arguments;
                var argumentsText = arguments is { Count: > 0 } ? " " + Truncate(JsonSerializer.Serialize(arguments), 200) : "";
                var name = request.Params?.Name;
                Logger.Log($"MCP tool call: {name}{argumentsText}");
                try
                {
                    return await next(request, ct);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Handled here, so that the SDK doesn't log the same failure again.
                    return CreateErrorResult(AiToolErrors.Report("MCP", name ?? "", exception));
                }
            };
        }

        private static CallToolResult CreateErrorResult(string message)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = message }],
            };
        }

        private static string Truncate(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text[..maxLength] + "...";
        }

        public async Task RunMcpAsync(CancellationToken ct = default)
        {
            var serverOptions = CreateOptions();
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}{Path}/");
            listener.Prefixes.Add($"http://127.0.0.1:{port}{Path}/");
            try
            {
                listener.Start();
            }
            catch (Exception exception)
            {
                Logger.LogException($"MCP server failed to start on port {port}", exception);
                return;
            }
            Logger.Log($"MCP server started for {projectTitle}: http://localhost:{port}{Path}");
            using var registration = ct.Register(listener.Stop);

            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception) when (ct.IsCancellationRequested || !listener.IsListening)
                {
                    break;
                }
                _ = Task.Run(() => HandleRequestAsync(context, serverOptions, ct), ct);
            }
            Logger.Log($"MCP server stopped (port {port})");
        }

        private async Task HandleRequestAsync(HttpListenerContext context, McpServerOptions serverOptions, CancellationToken ct)
        {
            var request = context.Request;
            var response = context.Response;
            try
            {
                // Protect against DNS rebinding and cross-site calls from browsers.
                var origin = request.Headers["Origin"];
                if (origin != null && !(Uri.TryCreate(origin, UriKind.Absolute, out var originUri) && originUri.IsLoopback))
                {
                    Logger.LogWarning($"MCP request rejected: origin {origin} is not allowed");
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }

                if (request.HttpMethod != "POST")
                {
                    // Stateless server: no standalone GET stream and no sessions to delete.
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    response.Headers["Allow"] = "POST";
                    return;
                }

                JsonRpcMessage? message;
                try
                {
                    message = await JsonSerializer.DeserializeAsync(request.InputStream, McpJsonUtilities.DefaultOptions.GetTypeInfo<JsonRpcMessage>(), ct);
                }
                catch (JsonException)
                {
                    message = null;
                }
                if (message == null)
                {
                    Logger.LogWarning("MCP request rejected: the body is not a valid JSON-RPC message");
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                await using var transport = new StreamableHttpServerTransport(loggerFactory) { Stateless = true };
                await using var server = ModelContextProtocol.Server.McpServer.Create(transport, serverOptions, loggerFactory);
                var serverTask = server.RunAsync(ct);

                response.ContentType = "text/event-stream";
                response.SendChunked = true;
                var written = await transport.HandlePostRequestAsync(message, response.OutputStream, ct);
                if (!written)
                {
                    response.ContentType = null;
                    response.StatusCode = (int)HttpStatusCode.Accepted;
                }
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogException("MCP request failed", ex);
                try
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
