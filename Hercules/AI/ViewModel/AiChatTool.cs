using Hercules.Shell;
using System;
using System.Collections.Specialized;
using System.Threading;
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
        public ICommand StopCommand { get; }
        private CancellationTokenSource? cts;

        public AiChatTool(AiModule aiModule, AiTools aiTools, ICommand settingsCommand)
        {
            ChatLog = new();
            IsGenerating = new(false);
            herculesChatClient = new(ChatLog, aiTools, aiModule.Settings, IsGenerating);
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
            ResetChatCommand = Commands.Execute(ResetChat);
            StopCommand = Commands.Execute(Stop).If(() => cts != null);
            SettingsCommand = settingsCommand;
        }

        private void Stop()
        {
            cts?.Cancel();
            cts = null;
        }

        private void Submit()
        {
            if (!herculesChatClient.IsConnected)
            {
                herculesChatClient.Init();
            }

            Stop();
            cts = new CancellationTokenSource();
            herculesChatClient.Ask(userPrompt, cts.Token);
        }

        private void ResetChat()
        {
            ChatLog.Clear();
            herculesChatClient.Reset();
        }
    }
}
