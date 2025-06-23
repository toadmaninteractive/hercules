using Hercules.Shell;
using System;

namespace Hercules.AI
{
    public class AiModule : CoreModule
    {
        public AiModule(Core core)
            : base(core)
        {
            aiChatTool = new AiChatTool();
            Workspace.WindowService.AddTool(aiChatTool);
            var aiChatCommand = Commands.Execute(() => aiChatTool.Show());
            var searchOption = new UiCommandOption("AI Chat", Fugue.Icons.Robot, aiChatCommand);
            Workspace.OptionManager.AddMenuOption(searchOption, "Tools", showInToolbar: true);
        }

        private McpServer? mcpServer;
        private readonly AiChatTool aiChatTool;

        public override void OnLoad(Uri? startUri)
        {
            if (Core.HasCliArgument("-mcp"))
            {
                mcpServer = new McpServer(Core);
                mcpServer.RunMcpAsync().Track();
            }
        }
    }
}
