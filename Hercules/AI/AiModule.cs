using Hercules.Shell;
using System;
using System.Reactive.Linq;
using System.Threading;

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

            var mcpServerOption = new UiToggleOption("MCP Server", null, EnableMcpServer);
            Workspace.OptionManager.AddMenuOption(mcpServerOption, "Tools");
            core.SettingsService.AddSetting(EnableMcpServer);
            mcpServerSubscription = EnableMcpServer.Subscribe(_ => UpdateMcpServer());
        }

        private bool projectLoaded;
        private McpServer? mcpServer;
        private CancellationTokenSource? mcpCancellation;
        private readonly IDisposable mcpServerSubscription;
        private readonly AiChatTool aiChatTool;
        private readonly IDisposable settingsPageSubscription;
        private readonly AiTools aiTools;

        public AiSettings Settings { get; }

        public Setting<bool> EnableMcpServer { get; } = new Setting<bool>(nameof(EnableMcpServer), false);

        public const string SystemPrompt = "You're the assistant in the tool called Hercules. This is the design data database frontend and editor for game development. Each database entry is a JSON document, and a single document describes a single game entity. Documents are identified by their _id property, and contain their type in category property. Special schema document defines which other properties are available. Answer user's questions.";

        public override void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
            projectLoaded = true;
            UpdateMcpServer();
        }

        public override void OnCloseProject()
        {
            projectLoaded = false;
            UpdateMcpServer();
        }

        // The MCP server serves the project database, so it runs only while it's enabled and a project is loaded.
        private void UpdateMcpServer()
        {
            StopMcpServer();
            if (!EnableMcpServer.Value || !projectLoaded)
                return;
            mcpCancellation = new CancellationTokenSource();
            mcpServer = new McpServer(aiTools);
            mcpServer.RunMcpAsync(mcpCancellation.Token).Track();
        }

        private void StopMcpServer()
        {
            mcpCancellation?.Cancel();
            mcpCancellation?.Dispose();
            mcpCancellation = null;
            mcpServer = null;
        }

        public override void OnShutdown()
        {
            mcpServerSubscription.Dispose();
            StopMcpServer();
            settingsPageSubscription.Dispose();
        }
    }
}
