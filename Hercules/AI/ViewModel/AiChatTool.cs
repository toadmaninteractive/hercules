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
        private readonly Core core;

        public ICommand SubmitCommand { get; }
        public ICommand ResetChatCommand { get; }

        public AiChatTool(Core core)
        {
            ChatLog = new();
            herculesChatClient = new(ChatLog, new McpServer(core));
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
            ResetChatCommand = Commands.Execute(ResetChat);
            this.core = core;
        }

        private void Submit()
        {
            if (!herculesChatClient.IsConnected)
                herculesChatClient.Init(core.GetModule<AiModule>().AnthropicApiKey.Value);

            herculesChatClient.Ask(userPrompt);
        }

        private void ResetChat()
        {
            ChatLog.Text = "";
            herculesChatClient.Reset();
        }
    }
}
