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

        public AiChatTool()
        {
            ChatLog = new();
            herculesChatClient = new(ChatLog);
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
        }

        private void Submit()
        {
            if (!herculesChatClient.IsConnected)
                herculesChatClient.InitOllama();

            herculesChatClient.Ask(userPrompt);
        }
    }
}
