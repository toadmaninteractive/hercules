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
            Settings = new();
            var settingsCommand = Commands.Execute(() => core.Workspace.ShowSettings("AI"));
            aiChatTool = new AiChatTool(this, new McpServer(core), settingsCommand);
            Workspace.WindowService.AddTool(aiChatTool);
            var aiChatCommand = Commands.Execute(() => aiChatTool.Show());
            var searchOption = new UiCommandOption("AI Chat", Fugue.Icons.Robot, aiChatCommand);
            Workspace.OptionManager.AddMenuOption(searchOption, "Tools", showInToolbar: true);
            settingsPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<SettingsPage>().Subscribe(p => p.Tabs.Add(new AiSettingsTab(Settings)));
            Core.SettingsService.AddSettingGroup(Settings);
        }

        private McpServer? mcpServer;
        private readonly AiChatTool aiChatTool;
        private readonly IDisposable settingsPageSubscription;

        public AiSettings Settings { get; }

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
