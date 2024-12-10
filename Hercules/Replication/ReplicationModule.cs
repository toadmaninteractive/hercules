using Hercules.Connections;
using Hercules.Documents;
using Hercules.Shell;

namespace Hercules.Replication
{
    public class ReplicationModule : CoreModule
    {
        public ReplicationModule(Core core)
            : base(core)
        {
            var synchronizeDatabaseOption = new UiCommandOption("Synchronize With Database", Fugue.Icons.DatabasesRelation, Commands.Execute(CompareDatabases).If(() => Core.Project != null));
            Workspace.OptionManager.AddMenuOption(synchronizeDatabaseOption, "Data#0");
        }

        private void CompareDatabases()
        {
            Workspace.WindowService.OpenPage(new SynchronizeDatabasePage(Core.Project!, Core.GetModule<DocumentsModule>(), Core.GetModule<ConnectionsModule>()));
        }
    }
}
