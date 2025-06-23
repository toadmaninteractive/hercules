using Anthropic.SDK.Constants;
using Hercules.Shell;
using System;
using System.Reactive.Linq;

namespace Hercules.AI
{
    public class AiModule : CoreModule
    {
        public AiModule(Core core)
            : base(core)
        {
            aiChatTool = new AiChatTool(core);
            Workspace.WindowService.AddTool(aiChatTool);
            var aiChatCommand = Commands.Execute(() => aiChatTool.Show());
            var searchOption = new UiCommandOption("AI Chat", Fugue.Icons.Robot, aiChatCommand);
            Workspace.OptionManager.AddMenuOption(searchOption, "Tools", showInToolbar: true);
            settingsPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<SettingsPage>().Subscribe(p => p.Tabs.Add(new AiSettingsTab(AnthropicApiKey, AiModel)));
            Core.SettingsService.AddSetting(AnthropicApiKey);
            Core.SettingsService.AddSetting(AiModel);
        }

        private McpServer? mcpServer;
        private readonly AiChatTool aiChatTool;
        private readonly IDisposable settingsPageSubscription;

        public Setting<string> AnthropicApiKey { get; } = new Setting<string>(nameof(AnthropicApiKey), "");
        public Setting<string> AiModel { get; } = new Setting<string>(nameof(AiModel), AnthropicModels.Claude35Sonnet);

        public override void OnLoad(Uri? startUri)
        {
            if (Core.HasCliArgument("-mcp"))
            {
                mcpServer = new McpServer(Core);
                mcpServer.RunMcpAsync().Track();
            }
        }

        public override void OnShutdown()
        {
            settingsPageSubscription.Dispose();
        }
    }
}
