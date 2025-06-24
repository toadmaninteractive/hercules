using Hercules.Shell;
using System;
using System.Collections.Specialized;
using System.Windows.Documents;
using System.Windows.Input;

namespace Hercules.AI
{
    public class AiChatTool : Tool
    {
        public AiChatLog ChatLog { get; }
        public ObservableValue<bool> IsGenerating { get; }

        private string userPrompt;
        public string UserPrompt
        {
            get => userPrompt;
            set => SetField(ref userPrompt, value);
        }

        private readonly HerculesChatClient herculesChatClient;

        public ICommand SubmitCommand { get; }
        public ICommand ResetChatCommand { get; }
        public ICommand SettingsCommand { get; }

        public AiChatTool(AiModule aiModule, McpServer mcpServer, ICommand settingsCommand)
        {
            ChatLog = new();
            IsGenerating = new(false);
            herculesChatClient = new(ChatLog, mcpServer, aiModule.AnthropicApiKey, aiModule.AiModel, IsGenerating);
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
            ResetChatCommand = Commands.Execute(ResetChat);
            SettingsCommand = settingsCommand;
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
            ChatLog.Clear();
            herculesChatClient.Reset();
        }
    }
}
