﻿using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.IO;
using Json;

namespace Hercules.AI
{
    public class AnthropicAiChat : IHerculesAiChat
    {
        private readonly List<Message> messages = new();
        private readonly IAiChatLog chatLog;
        private readonly AnthropicClient chatClient;
        private readonly List<Anthropic.SDK.Common.Tool> tools;
        private readonly AiSettings settings;
        private readonly ObservableValue<bool> isGenerating;

        public AnthropicAiChat(IAiChatLog chatLog, AiTools aiTools, AiSettings settings, ObservableValue<bool> isGenerating)
        {
            this.chatLog = chatLog;
            this.settings = settings;
            this.isGenerating = isGenerating;
            chatClient = new AnthropicClient(new APIAuthentication(settings.AnthropicApiKey.Value));

            tools = new(
                from methodInfo in aiTools.GetType().GetMethods()
                let attr = methodInfo.GetCustomAttribute<AiToolAttribute>()
                where attr != null
                let description = attr.GetDescription(aiTools)
                select Anthropic.SDK.Common.Tool.GetOrCreateTool(aiTools, methodInfo.Name, description));
        }

        public async Task WaitForAnswer(CancellationToken ct)
        {
            isGenerating.Value = true;
            try
            {
                chatClient.Auth.ApiKey = settings.AnthropicApiKey.Value;
                var parameters = new MessageParameters()
                {
                    Messages = messages,
                    MaxTokens = settings.MaxTokens.Value,
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
                while (!ct.IsCancellationRequested)
                {
                    MessageResponse result;
                    try
                    {
                        result = await chatClient!.Messages.GetClaudeMessageAsync(parameters, ct);
                    }
                    catch (RateLimitsExceeded rateLimitsExceeded)
                    {
                        var retryAfter = rateLimitsExceeded.RateLimits.RetryAfter ?? TimeSpan.FromSeconds(30);
                        Logger.LogException(rateLimitsExceeded);
                        chatLog.AddSpecialMessage($"Rate limits exceeded. Retry in {retryAfter.TotalSeconds:0.##}s");
                        await Task.Delay(retryAfter, ct);
                        continue;
                    }
                    // result.Message.Content.First().CacheControl = new Anthropic.SDK.Messaging.CacheControl() { Type = CacheControlType.ephemeral };

                    messages.Add(result.Message);

                    chatLog.AddAiMessage(result.Message.ToString());

                    if (result.ToolCalls.Count > 0)
                    {
                        foreach (var toolCall in result.ToolCalls)
                        {
                            string response;
                            Exception? exception = null;
                            try
                            {
                                response = toolCall.Invoke<string>();
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                                if (exception is TargetInvocationException targetInocation && targetInocation.InnerException != null)
                                {
                                    exception = targetInocation.InnerException;
                                }
                                response = $"Tool call error: {ex.Message}";
                            }
                            var arguments = new Dictionary<string, object>();
                            foreach (var argument in (System.Text.Json.Nodes.JsonObject)toolCall.Arguments)
                            {
                                arguments.Add(argument.Key, argument.Value.ToImmutableJson());
                            }
                            chatLog.AddToolCall(toolCall.MethodInfo.Name, toolCall.Arguments.ToImmutableJson().AsObject, response);
                            if (exception != null)
                            {
                                chatLog.AddException(exception);
                                Logger.LogException($"AI tool {toolCall.MethodInfo.Name} call error", exception);
                            }
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

        public void Ask(string userPrompt, IReadOnlyCollection<string> attachments, CancellationToken ct)
        {
            userPrompt = userPrompt.Trim();
            var message = new Message(RoleType.User, userPrompt);
            foreach (var attachment in attachments)
            {
                switch (Path.GetExtension(attachment).ToLowerInvariant())
                {
                    case ".pdf":
                        message.Content.Add(new DocumentContent()
                        {
                            Source = new DocumentSource()
                            {
                                Type = SourceType.base64,
                                Data = Convert.ToBase64String(File.ReadAllBytes(attachment)),
                                MediaType = "application/pdf"
                            },
                            CacheControl = new CacheControl()
                            {
                                Type = CacheControlType.ephemeral
                            }
                        });
                        break;

                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".gif":
                    case ".webp":
                        message.Content.Add(new ImageContent()
                        {
                            Source = new ImageSource
                            {
                                Data = Convert.ToBase64String(File.ReadAllBytes(attachment)),
                                MediaType = MimeMapping.MimeUtility.GetMimeMapping(attachment)
                            }
                        });
                        break;

                    default:
                        message.Content.Add(new TextContent { Text = File.ReadAllText(attachment) });
                        break;
                }
            }
            messages.Add(message);
            chatLog.AddUserMessage(userPrompt);
            WaitForAnswer(ct).Track();
        }

        public void Reset()
        {
            messages.Clear();
        }
    }
}