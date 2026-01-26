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
            aiTools = new(core);
            var settingsCommand = Commands.Execute(() => core.Workspace.ShowSettings("AI"));
            aiChatTool = new AiChatTool(this, aiTools, settingsCommand);
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
        private readonly AiTools aiTools;

        public AiSettings Settings { get; }

        public const string SystemPrompt = "You're the assistant in the tool called Hercules. This is the design data database frontend and editor for game development. Each database entry is a JSON document, and a single document describes a single game entity. Documents are identified by their _id property, and contain their type in category property. Special schema document defines which other properties are available. Answer user's questions.";

        public override void OnLoad(Uri? startUri)
        {
            if (Core.HasCliArgument("-mcp"))
            {
                mcpServer = new McpServer(aiTools);
                mcpServer.RunMcpAsync().Track();
            }
        }

        public override void OnShutdown()
        {
            settingsPageSubscription.Dispose();
        }
    }
}
