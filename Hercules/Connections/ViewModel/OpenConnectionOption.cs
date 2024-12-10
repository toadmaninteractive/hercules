using Hercules.Shell;

namespace Hercules.Connections
{
    public class OpenConnectionOption : UiCustomOption
    {
        public DbConnectionCollection Connections { get; }
        public ICommand<DbConnection> LoadConnectionCommand { get; }

        public override string ToolbarCommandTemplateKey => "OpenConnectionOptionToolbarTemplateKey";

        public override string ItemContainerTemplateKey => "OpenConnectionOptionItemContainerTemplateKey";

        public OpenConnectionOption(DbConnectionCollection connections, ICommand<DbConnection> loadConnectionCommand)
        {
            Connections = connections;
            LoadConnectionCommand = loadConnectionCommand;
        }
    }
}
