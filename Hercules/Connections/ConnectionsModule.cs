using Hercules.DB;
using Hercules.Documents;
using Hercules.Shell;
using System;
using System.Linq;

namespace Hercules.Connections
{
    public class ConnectionsModule : CoreModule
    {
        public DbConnectionCollection Connections { get; }

        public ICommand<DbConnection> LoadConnectionCommand { get; }

        public ConnectionsModule(Core core)
            : base(core)
        {
            this.Connections = new DbConnectionCollection();

            this.LoadConnectionCommand = Commands.Execute<DbConnection>(c => LoadConnection(c)).IfNotNull();

            SetupOptions(Workspace.OptionManager);
        }

        private void SetupOptions(UiOptionManager optionManager)
        {
            var newConnectionOption = new UiCommandOption("New Connection...", Fugue.Icons.DatabasePlus, NewConnection);
            optionManager.AddMenuOption(newConnectionOption, "Connection#0", showInToolbar: true);

            void EditConnections() => Workspace.DialogService.ShowDialog(new ConnectionsDialog(this, Workspace.DialogService));
            var editConnectionsOption = new UiCommandOption("Manage Connections...", Fugue.Icons.Databases, EditConnections);
            optionManager.AddMenuOption(editConnectionsOption, "Connection#0", showInToolbar: true);

            var openConnectionOption = new OpenConnectionOption(Connections, LoadConnectionCommand);
            optionManager.AddMenuOption(openConnectionOption, "Connection#0", showInToolbar: true);

            var closeConnectionCommand = Commands.Execute(() => CloseConnection()).If(() => Connections.ActiveConnection != null);
            var closeConnectionOption = new UiCommandOption("Close Connection", null, closeConnectionCommand);
            optionManager.AddMenuOption(closeConnectionOption, "Connection#0");

            var resetLocalCacheCommand = Commands.Execute(ResetLocalCache).If(() => Connections.ActiveConnection != null);
            var resetLocalCacheOption = new UiCommandOption("Reset Local Cache", Fugue.Icons.ArrowCircleDouble135, resetLocalCacheCommand);
            optionManager.AddMenuOption(resetLocalCacheOption, "Connection#20");
        }

        public override void OnLoad(Uri? startUri)
        {
            // Load configuration
            this.Connections.LoadConfiguration();
            this.Connections.SaveConfiguration();
            SaveJumpList();
            Logger.Log(this.Connections.Items.Count + " connection(s) loaded");

            if (Connections.ActiveConnection != null && !Workspace.WorkspaceSettings.LoadLastDatabaseOnStartup.Value)
                Connections.ActiveConnection = null;

            // Check if there is no connections
            if (this.Connections.Items.Count == 0)
                NewConnection();
            else if (startUri != null && !string.IsNullOrEmpty(startUri.Host))
            {
                var suitableConnection = Connections.Items.FirstOrDefault(c => HerculesUrl.IsSuitableConnection(c, startUri));
                if (suitableConnection == null)
                {
                    var dbName = HerculesUrl.Database(startUri);
                    if (dbName != null)
                    {
                        var authConnection = Connections.Items.FirstOrDefault(c => string.Compare(c.Url.Host, startUri.Host, StringComparison.OrdinalIgnoreCase) == 0);
                        var uriBuilder = new UriBuilder { Host = startUri.Host, Port = startUri.Port, Scheme = "http" };
                        var dialog = new EditConnectionDialog(Connections, null)
                        {
                            ConnectionUrl = uriBuilder.Uri.ToString(),
                            ConnectionTitle = dbName,
                            Database = dbName,
                            UserName = authConnection?.Username ?? "",
                            Password = authConnection?.Password ?? ""
                        };
                        if (Workspace.DialogService.ShowDialog(dialog))
                            suitableConnection = dialog.Connection;
                    }
                }
                if (suitableConnection != null)
                    Connections.ActiveConnection = suitableConnection;
                else
                    Logger.LogWarning("Cannot open start url: database not found");
            }

            if (Connections.ActiveConnection != null)
                OpenDatabase();
        }

        public bool LoadConnection(DbConnection connection)
        {
            if (this.Connections.ActiveConnection == connection)
                return false;

            if (!Workspace.MaybeConfirmLoseUnsavedProgress("Switch Connection"))
                return false;

            Core.CloseProject();

            this.Connections.ActiveConnection = connection;
            this.Connections.SaveConfiguration();

            OpenDatabase();

            return true;
        }

        public void SaveJumpList()
        {
            Core.Workspace.Scheduler.ScheduleForegroundIdleJob(Connections.SaveJumpList);
        }

        public Database? OpenDatabase(DbConnection connection, TempStorage tempStorage)
        {
            var vm = new OpenDatabaseDialog(Workspace.Scheduler, connection, tempStorage);
            Workspace.DialogService.ShowDialog(vm);
            var result = vm.Result ?? OpenDatabaseDialog.OpenDatabaseResult.Aborted;
            switch (result)
            {
                case OpenDatabaseDialog.OpenDatabaseResult.Aborted:
                    return null;

                case OpenDatabaseDialog.OpenDatabaseResult.LoadCacheError:
                    if (Workspace.DialogService.ShowMessageBox("Local cache is corrupted.\n\nIt is recommended to reset local cache and redownload content.", "Database cache error", DialogButtons.Reset | DialogButtons.Cancel, DialogButtons.Reset, DialogIcon.DatabaseError) == DialogButtons.Reset)
                    {
                        vm.Database.Cache.Reset();
                        return OpenDatabase(connection, tempStorage);
                    }
                    else
                        return null;

                case OpenDatabaseDialog.OpenDatabaseResult.SynchronizeError:
                    Workspace.DialogService.ShowError("Error synchronizing database");
                    return vm.Database;

                case OpenDatabaseDialog.OpenDatabaseResult.Success:
                    return vm.Database;

                default:
                    throw new InvalidOperationException();
            }
        }

        void OpenDatabase()
        {
            var db = OpenDatabase(Connections.ActiveConnection!, TempStorage.Create());
            if (db == null)
                CloseConnection();
            else
                Core.OpenProject(Connections.ActiveConnection!, db);
        }

        public bool CloseConnection()
        {
            if (!Workspace.MaybeConfirmLoseUnsavedProgress("Close Connection"))
                return false;

            Core.CloseProject();
            Connections.ActiveConnection = null;
            Connections.SaveConfiguration();
            return true;
        }

        void NewConnection()
        {
            var dialog = new EditConnectionDialog(Connections, null);
            if (Workspace.DialogService.ShowDialog(dialog))
            {
                LoadConnection(dialog.Connection!);
                SaveJumpList();
            }
        }

        public void NewConnection(string title, Uri url, string userName, string password, string database)
        {
            var dialog = new EditConnectionDialog(Connections, null)
            {
                ConnectionTitle = title,
                ConnectionUrl = url.ToString(),
                UserName = userName,
                Password = password,
                Database = database
            };
            if (Workspace.DialogService.ShowDialog(dialog))
                LoadConnection(dialog.Connection!);
        }

        void ResetLocalCache()
        {
            if (!Workspace.DialogService.ShowQuestion("Are you sure you want to reset local cache for current connection?", "Reset local cache"))
                return;
            if (!Workspace.MaybeConfirmLoseUnsavedProgress("Reset local cache"))
                return;
            var databaseCache = Core.Project!.Database.Cache;
            Core.CloseProject();
            databaseCache.Reset();
            OpenDatabase();
        }
    }
}
