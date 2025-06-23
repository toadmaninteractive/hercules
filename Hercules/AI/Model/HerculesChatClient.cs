using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public class HerculesChatClient
    {
        private readonly List<Message> messages = new();
        private readonly TextDocument chatLog;
        private AnthropicClient? chatClient;
        private List<Anthropic.SDK.Common.Tool>? tools;
        private readonly McpServer mcpServer;

        public bool IsConnected => chatClient != null;

        public HerculesChatClient(TextDocument chatLog, McpServer mcpServer)
        {
            this.chatLog = chatLog;
            this.mcpServer = mcpServer;
        }

        public void Init(string apiKey)
        {
            chatClient = new AnthropicClient(new APIAuthentication(apiKey));

            tools = new List<Anthropic.SDK.Common.Tool>
            {
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetSchema), "Gets Hercules design document for the given id."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetDocument), "Gets Hercules design document for the given id."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetDocuments), "Gets Hercules design documents for the given list of ids. Prefer GetPropertyValuesForMultipleDocuments instead if you are only interested in the subset of document properties."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetCategoryList), "Gets the list of Hercules design document categories."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetAllDocumentIds), "Gets the list of all Hercules design document IDs."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetDocumentIdsByCategory), "Gets the list of Hercules design document IDs belonging to the category."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.GetPropertyValuesForMultipleDocuments), "Gets values for the specified property path for multiple Hercules documents. Returns the list of objects with document ID and property values."),
                Anthropic.SDK.Common.Tool.GetOrCreateTool(mcpServer, nameof(McpServer.BatchUpdateDocuments), "Updates multiple values in Hercules documents. Accepts the list of JSON objects as input. Each object has three properties: id is document ID, path is the dot separated path to the property, and value is new JSON value of updated property."),
            };
        }

        public async Task WaitForAnswer()
        {
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 2048,
                Model = AnthropicModels.Claude37Sonnet,
                Stream = false,
                Temperature = 1.0m,
                Tools = tools,
                PromptCaching = PromptCacheType.AutomaticToolsAndSystem,
                System = new List<SystemMessage>
                {
                    new SystemMessage("You're the assistant in the tool called Hercules. This is the design data database frontend and editor for game development. Each database entry is a JSON document, and a single document describes a single game entity. Documents are identified by their _id property, and contain their type in category property. Special schema document defines which other properties are available. Answer user's questions.")
                }
            };
            while (true)
            {
                var result = await chatClient!.Messages.GetClaudeMessageAsync(parameters);
                // result.Message.Content.First().CacheControl = new Anthropic.SDK.Messaging.CacheControl() { Type = CacheControlType.ephemeral };

                messages.Add(result.Message);

                chatLog.Insert(chatLog.TextLength, result.Message.ToString());
                chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                chatLog.Insert(chatLog.TextLength, Environment.NewLine);

                if (result.ToolCalls.Count > 0)
                {
                    foreach (var toolCall in result.ToolCalls)
                    {
                        var response = toolCall.Invoke<string>();
                        Logger.LogDebug($"AI Calls {toolCall.Name}({toolCall.Arguments})");
                        chatLog.Insert(chatLog.TextLength, response);
                        chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                        chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                        messages.Add(new Message(toolCall, response));
                    }
                    continue;
                }
                if (result.StopReason == "end_turn")
                {
                    chatLog.Insert(chatLog.TextLength, "Finished.");
                    chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                    chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                    break;
                }
                if (!string.IsNullOrEmpty(result.StopReason))
                {
                    chatLog.Insert(chatLog.TextLength, result.StopReason);
                    chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                    chatLog.Insert(chatLog.TextLength, Environment.NewLine);
                }
            }
            chatLog.Insert(chatLog.TextLength, Environment.NewLine);
            chatLog.Insert(chatLog.TextLength, Environment.NewLine);
        }

        public void Ask(string userPrompt)
        {
            messages.Add(new Message(RoleType.User, userPrompt));
            chatLog.Insert(chatLog.TextLength, userPrompt);
            chatLog.Insert(chatLog.TextLength, Environment.NewLine);
            chatLog.Insert(chatLog.TextLength, Environment.NewLine);
            WaitForAnswer().Track();
        }

        public void Reset()
        {
            messages.Clear();
        }
    }
}
