using Hercules.Shell;

namespace Hercules.DatabaseExplorer
{
    public abstract class DatabaseExplorerItem : NotifyPropertyChanged
    {
        public abstract WorkspaceContextMenu? ContextMenu { get; }
    }
}
