using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public class HerculesChatClient
    {
        private readonly List<ChatMessage> chatHistory = new();
        private readonly TextDocument chatLog;
        private IChatClient? chatClient;

        public bool IsConnected => chatClient != null;

        public HerculesChatClient(TextDocument chatLog)
        {
            this.chatLog = chatLog;
        }

        public void InitOllama()
        {
            chatClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "phi3:mini");
        }

        public async Task WaitForAnswer()
        {
            var response = "";
            await foreach (ChatResponseUpdate item in chatClient!.GetStreamingResponseAsync(chatHistory))
            {
                Console.Write(item.Text);
                response += item.Text;
                chatLog.Insert(chatLog.TextLength, item.Text);
            }
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            chatLog.Insert(chatLog.TextLength, Environment.NewLine);
        }

        public void Ask(string userPrompt)
        {
            chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));
            chatLog.Insert(chatLog.TextLength, userPrompt);
            chatLog.Insert(chatLog.TextLength, Environment.NewLine);
            WaitForAnswer().Track();
        }
    }
}
