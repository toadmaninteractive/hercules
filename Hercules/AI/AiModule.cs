using Hercules.Repository;
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
        }

        private ProjectSettings? projectSettings;
        private string projectTitle = "";
        private int? runningMcpPort;
        private CancellationTokenSource? mcpCancellation;
        private readonly AiChatTool aiChatTool;
        private readonly IDisposable settingsPageSubscription;
        private readonly AiTools aiTools;

        public AiSettings Settings { get; }

        public const string SystemPrompt = "You're the assistant in the tool called Hercules. This is the design data database frontend and editor for game development. Each database entry is a JSON document, and a single document describes a single game entity. Documents are identified by their _id property, and contain their type in category property. Special schema document defines which other properties are available. Answer user's questions.";

        public override void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
            projectSettings = project.Settings;
            projectTitle = project.Connection.Title;
            projectSettings.Changed += UpdateMcpServer;
            UpdateMcpServer();
        }

        public override void OnCloseProject()
        {
            if (projectSettings != null)
                projectSettings.Changed -= UpdateMcpServer;
            projectSettings = null;
            UpdateMcpServer();
        }

        // The MCP server serves the project database, so it runs only while a project is loaded
        // and the project has it enabled. The port is a project setting, so that different projects
        // can be told apart by their urls.
        private void UpdateMcpServer()
        {
            int? port = projectSettings is { McpServerEnabled: true } ? projectSettings.McpServerPort : null;
            if (port == runningMcpPort)
                return;

            StopMcpServer();
            if (port == null)
                return;
            mcpCancellation = new CancellationTokenSource();
            runningMcpPort = port;
            new McpServer(aiTools, port.Value, projectTitle).RunMcpAsync(mcpCancellation.Token).Track();
        }

        private void StopMcpServer()
        {
            mcpCancellation?.Cancel();
            mcpCancellation?.Dispose();
            mcpCancellation = null;
            runningMcpPort = null;
        }

        public override void OnShutdown()
        {
            StopMcpServer();
            settingsPageSubscription.Dispose();
        }
    }
}
