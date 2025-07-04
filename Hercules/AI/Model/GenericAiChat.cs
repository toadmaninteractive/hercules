﻿using Json;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public class GenericAiChat : IHerculesAiChat
    {
        private readonly IAiChatLog chatLog;
        private readonly ObservableValue<bool> isGenerating;
        protected readonly List<ChatMessage> messages = new();
        protected readonly IChatClient chatClient;
        protected readonly ChatOptions chatOptions;
        private readonly Dictionary<string, AIFunction> functions;

        public GenericAiChat(IChatClient chatClient, IAiChatLog chatLog, AiTools aiTools, AiSettings settings, ObservableValue<bool> isGenerating)
        {
            this.chatClient = chatClient;
            this.chatLog = chatLog;
            this.isGenerating = isGenerating;
            messages.Add(new ChatMessage(ChatRole.System, AiModule.SystemPrompt));
            chatOptions = new ChatOptions() { Tools = new List<AITool>(), Temperature = (float)settings.AiTemperature.Value };
            functions = new();
            foreach (var methodInfo in aiTools.GetType().GetMethods())
            {
                var attr = methodInfo.GetCustomAttribute<AiToolAttribute>();
                if (attr == null)
                    continue;
                var description = attr.GetDescription(aiTools);
                var function = AIFunctionFactory.Create(ReflectionHelper.CreateDelegate(methodInfo, aiTools), methodInfo.Name, description);
                functions.Add(function.Name, function);
                chatOptions.Tools.Add(function);
            }
        }

        public async Task WaitForAnswer(CancellationToken ct)
        {
            isGenerating.Value = true;
            try
            {
                while (true)
                {
                    ChatResponse response = await chatClient!.GetResponseAsync(messages, chatOptions);
                    messages.AddMessages(response);
                    List<AIContent>? toolResults = null;
                    foreach (ChatMessage message in response.Messages)
                    {
                        foreach (var content in message.Contents)
                        {
                            switch (content)
                            {
                                case TextContent textContent:
                                    chatLog.AddAiMessage(textContent.Text);
                                    break;

                                case FunctionCallContent functionCallContent:
                                    toolResults ??= new();
                                    var tool = functions[functionCallContent.Name];

                                    var result = await tool.InvokeAsync(new AIFunctionArguments(functionCallContent.Arguments), ct);
                                    var resultContent = new FunctionResultContent(functionCallContent.CallId, result);
                                    toolResults.Add(resultContent);
                                    var arguments = functionCallContent.Arguments!.ToDictionary(p => p.Key, p => ((JsonElement)p.Value!).ToImmutableJson());
                                    chatLog.AddToolCall(functionCallContent.Name, arguments, result!.ToString()!);
                                    break;
                            }
                        }
                    }
                    if (toolResults != null)
                    {
                        var toolResultMessage = new ChatMessage(ChatRole.Tool, toolResults);
                        messages.Add(toolResultMessage);
                    }
                    else
                    {
                        break;
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

        public void Ask(string userPrompt, IReadOnlyCollection<string> attachments, CancellationToken ct)
        {
            messages.Add(new ChatMessage(ChatRole.User, userPrompt));
            chatLog.AddUserMessage(userPrompt);
            WaitForAnswer(ct).Track();
        }

        public void Reset()
        {
            messages.Clear();
        }
    }
}
