using Hercules.Documents;
using Hercules.Shell;
using System;

namespace Hercules
{
    public interface IDatabaseSingletonService
    {
        IDisposable CreateIpcListener(string name);
    }

    public class DatabaseSingletonService : IDatabaseSingletonService
    {
        private readonly Workspace workspace;

        public DatabaseSingletonService(Workspace workspace)
        {
            this.workspace = workspace;
        }

        public IDisposable CreateIpcListener(string name)
        {
            return new NamedPipeServer($@"Global\{name}", Callback);
        }

        private void Callback(string uri)
        {
            Logger.LogDebug($"IPC request to open {uri}");
            if (workspace.ShortcutService.TryParseUri(new Uri(uri), out var shortcut))
                workspace.ShortcutService.Open(shortcut);
        }
    }
}
