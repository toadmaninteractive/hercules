using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace Hercules.AI
{
    public class HerculesChatClient
    {
        private readonly List<Message> messages = new();
        private readonly IAiChatLog chatLog;
        private AnthropicClient? chatClient;
        private List<Anthropic.SDK.Common.Tool>? tools;
        private readonly AiTools aiTools;
        private readonly AiSettings settings;
        private readonly ObservableValue<bool> isGenerating;

        public bool IsConnected => chatClient != null;

        public HerculesChatClient(IAiChatLog chatLog, AiTools aiTools, AiSettings settings, ObservableValue<bool> isGenerating)
        {
            this.chatLog = chatLog;
            this.aiTools = aiTools;
            this.settings = settings;
            this.isGenerating = isGenerating;
        }

        public void Init()
        {
            chatClient = new AnthropicClient(new APIAuthentication(settings.AnthropicApiKey.Value));

            tools = new(
                from methodInfo in aiTools.GetType().GetMethods()
                let attr = methodInfo.GetCustomAttribute<AiToolAttribute>()
                where attr != null
                select Anthropic.SDK.Common.Tool.GetOrCreateTool(aiTools, methodInfo.Name, attr.Description));
        }

        public async Task WaitForAnswer()
        {
            isGenerating.Value = true;
            try
            {
                chatClient.Auth.ApiKey = settings.AnthropicApiKey.Value;
                var parameters = new MessageParameters()
                {
                    Messages = messages,
                    MaxTokens = 2048,
                    Model = settings.AiModel.Value,
                    Stream = false,
                    Temperature = (decimal)settings.AiTemperature.Value,
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
                            chatLog.AddToolCall(toolCall.MethodInfo.Name, toolCall.Arguments.ToString(), response);                            
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
