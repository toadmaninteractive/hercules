using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        private IHerculesAiChat? herculesChat;

        public ICommand SubmitCommand { get; }
        public ICommand ResetChatCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand AttachCommand { get; }
        private CancellationTokenSource? cts;
        private readonly List<string> attachments = new();
        private readonly AiModule aiModule;
        private readonly AiTools aiTools;

        public AiChatTool(AiModule aiModule, AiTools aiTools, ICommand settingsCommand)
        {
            ChatLog = new();
            IsGenerating = new(false);
            Title = "AI Chat";
            userPrompt = "";
            SubmitCommand = Commands.Execute(Submit).If(() => !string.IsNullOrEmpty(UserPrompt));
            ResetChatCommand = Commands.Execute(ResetChat);
            StopCommand = Commands.Execute(Stop).If(() => cts != null);
            AttachCommand = Commands.Execute(Attach);
            this.aiModule = aiModule;
            this.aiTools = aiTools;
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
            if (herculesChat == null)
            {
                herculesChat = aiModule.Settings.AiModelProvider.Value switch
                {
                    AiModelProvider.Ollama => new OllamaAiChat(ChatLog, aiTools, aiModule.Settings, IsGenerating),
                    AiModelProvider.Anthropic => new AnthropicAiChat(ChatLog, aiTools, aiModule.Settings, IsGenerating),
                    _ => throw new NotSupportedException($"AI model provider {aiModule.Settings.AiModelProvider.Value} is not supported.")
                };
            }

            Stop();
            cts = new CancellationTokenSource();
            herculesChat.Ask(userPrompt, attachments, cts.Token);
            attachments.Clear();
        }

        private void ResetChat()
        {
            ChatLog.Clear();
            herculesChat?.Reset();
        }
    }
}
