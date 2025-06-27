using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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

        private readonly IHerculesAiChat herculesChat;

        public ICommand SubmitCommand { get; }
        public ICommand ResetChatCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand AttachCommand { get; }
        private CancellationTokenSource? cts;
        private readonly List<string> attachments = new();

        public AiChatTool(AiModule aiModule, AiTools aiTools, ICommand settingsCommand)
        {
            ChatLog = new();
            IsGenerating = new(false);
            herculesChat = new AnthropicChat(ChatLog, aiTools, aiModule.Settings, IsGenerating);
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
            ResetChatCommand = Commands.Execute(ResetChat);
            StopCommand = Commands.Execute(Stop).If(() => cts != null);
            AttachCommand = Commands.Execute(Attach);
            SettingsCommand = settingsCommand;
        }

        private void Attach()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Attach File"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                Attach(dlg.FileName);
            }
        }

        private void Attach(string file)
        {
            attachments.Add(file);
            ChatLog.AddAttachment(Path.GetFileName(file!));
        }

        private void Stop()
        {
            cts?.Cancel();
            cts = null;
        }

        private void Submit()
        {
            if (!herculesChat.IsConnected)
            {
                herculesChat.Init();
            }

            Stop();
            cts = new CancellationTokenSource();
            herculesChat.Ask(userPrompt, attachments, cts.Token);
            attachments.Clear();
        }

        private void ResetChat()
        {
            ChatLog.Clear();
            herculesChat.Reset();
        }
    }
}
