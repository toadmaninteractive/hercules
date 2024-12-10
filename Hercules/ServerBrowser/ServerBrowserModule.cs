using Hercules.Connections;
using Hercules.Shell;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.ServerBrowser
{
    public class ServerBrowserModule : CoreModule
    {
        public ServerBrowserModule(Core core)
            : base(core)
        {
            var serverBrowserOption = new UiCommandOption("Server Browser", Fugue.Icons.ServersNetwork, ShowServerBrowser);
            Workspace.OptionManager.AddMenuOption(serverBrowserOption, "Tools#0");
        }

        private void ShowServerBrowser()
        {
            var dialog = new ServerConnectionDialog(GetKnownConnections());
            if (Core.Project != null)
                dialog.Url = Core.Project.Connection.Url.ToString();
            if (Workspace.DialogService.ShowDialog(dialog))
                Workspace.WindowService.OpenPage(new ServerBrowserPage(Workspace, Core.GetModule<ConnectionsModule>(), dialog.Connection!));
        }

        private List<ServerConnectionParams> GetKnownConnections()
        {
            var connectionsModule = Core.GetModule<ConnectionsModule>();
            return connectionsModule.Connections.Items
                .GroupBy(c => c.Url)
                .Select(g => new ServerConnectionParams(g.Key, g.First().Username, g.First().Password))
                .ToList();
        }
    }
}
