using Hercules.Shell;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiChatTool : Tool
    {
        public TextDocument ChatLog { get; }

        private string userPrompt;
        public string UserPrompt
        {
            get => userPrompt;
            set => SetField(ref userPrompt, value);
        }

        private readonly HerculesChatClient herculesChatClient;

        public ICommand SubmitCommand { get; }
        public ICommand ResetChatCommand { get; }

        public AiChatTool(AiModule aiModule, McpServer mcpServer)
        {
            ChatLog = new();
            herculesChatClient = new(ChatLog, mcpServer, aiModule.AnthropicApiKey, aiModule.AiModel);
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
            ResetChatCommand = Commands.Execute(ResetChat);
        }

        private void Submit()
        {
            if (!herculesChatClient.IsConnected)
            {
                herculesChatClient.Init();
            }

            herculesChatClient.Ask(userPrompt);
        }

        private void ResetChat()
        {
            ChatLog.Text = "";
            herculesChatClient.Reset();
        }
    }
}
