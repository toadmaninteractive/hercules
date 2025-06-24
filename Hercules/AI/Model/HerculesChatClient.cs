using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Hercules.AI
{
    public class HerculesChatClient
    {
        private readonly List<Message> messages = new();
        private readonly IAiChatLog chatLog;
        private AnthropicClient? chatClient;
        private List<Anthropic.SDK.Common.Tool>? tools;
        private readonly McpServer mcpServer;
        private readonly Setting<string> apiKey;
        private readonly Setting<string> aiModel;
        private readonly ObservableValue<bool> isGenerating;

        public bool IsConnected => chatClient != null;

        public HerculesChatClient(IAiChatLog chatLog, McpServer mcpServer, Setting<string> apiKey, Setting<string> aiModel, ObservableValue<bool> isGenerating)
        {
            this.chatLog = chatLog;
            this.mcpServer = mcpServer;
            this.apiKey = apiKey;
            this.aiModel = aiModel;
            this.isGenerating = isGenerating;
        }

        public void Init()
        {
            chatClient = new AnthropicClient(new APIAuthentication(apiKey.Value));

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
            isGenerating.Value = true;
            try
            {
                chatClient.Auth.ApiKey = apiKey.Value;
                var parameters = new MessageParameters()
                {
                    Messages = messages,
                    MaxTokens = 2048,
                    Model = aiModel.Value,
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

                    chatLog.AddAiMessage(result.Message.ToString());

                    if (result.ToolCalls.Count > 0)
                    {
                        foreach (var toolCall in result.ToolCalls)
                        {
                            var response = toolCall.Invoke<string>();
                            chatLog.AddHerculesMessage(response);                            
                            messages.Add(new Message(toolCall, response));
                        }
                        continue;
                    }
                    if (result.StopReason == "end_turn")
                    {
                        break;
                    }
                    if (!string.IsNullOrEmpty(result.StopReason))
                    {
                        chatLog.AddSpecialMessage(result.StopReason);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogException("AI Chat error", exception);
                chatLog.AddException(exception);
            }
            finally
            {
                isGenerating.Value = false;
            }
        }

        public void Ask(string userPrompt)
        {
            userPrompt = userPrompt.Trim();
            messages.Add(new Message(RoleType.User, userPrompt));
            chatLog.AddUserMessage(userPrompt);
            WaitForAnswer().Track();
        }

        public void Reset()
        {
            messages.Clear();
        }
    }
}
