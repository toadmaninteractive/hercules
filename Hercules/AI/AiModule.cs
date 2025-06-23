using System;

namespace Hercules.AI
{
    public class AiModule : CoreModule
    {
        public AiModule(Core core)
            : base(core)
        {
        }

        private McpServer? mcpServer;

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
