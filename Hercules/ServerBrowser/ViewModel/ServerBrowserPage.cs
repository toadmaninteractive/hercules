using CouchDB;
using Hercules.Connections;
using Hercules.DB;
using Hercules.Shell;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.ServerBrowser
{
    public class ServerBrowserPage : Page
    {
        public ServerConnectionParams Connection { get; }

        public ObservableCollection<ServerDatabase> Databases { get; } = new();

        public ConnectionsModule ConnectionsModule { get; }

        public Workspace Workspace { get; }

        public ICommand<DbConnection> LoadConnectionCommand => ConnectionsModule.LoadConnectionCommand;

        private readonly CdbServer cdbServer;

        public ServerBrowserPage(Workspace workspace, ConnectionsModule connectionsModule, ServerConnectionParams connection)
        {
            Workspace = workspace;
            Title = "Server Browser";
            ContentId = "{ServerBrowser}";
            Connection = connection;
            ReloadCommand = Commands.ExecuteAsync(LoadAsync);
            cdbServer = new CdbServer(connection.Url, connection.UserName, connection.Password);

            ConnectionsModule = connectionsModule;
            CloneDatabaseCommand = Commands.ExecuteAsync<ServerDatabase>(CloneDatabase);
            CreateConnectionCommand = Commands.Execute<ServerDatabase>(CreateConnection).If(db => db.Connection == null);
            DeleteDatabaseCommand = Commands.ExecuteAsync<ServerDatabase>(DeleteDatabase);

            LoadAsync().Track();
        }

        bool isLoaded;

        public bool IsLoaded
        {
            get => isLoaded;
            set => SetField(ref isLoaded, value);
        }

        public ICommand ReloadCommand { get; }
        public ICommand<ServerDatabase> CloneDatabaseCommand { get; }
        public ICommand<ServerDatabase> CreateConnectionCommand { get; }
        public ICommand<ServerDatabase> DeleteDatabaseCommand { get; }

        public async Task LoadAsync()
        {
            var dbs = await cdbServer.GetDatabaseNamesAsync().ConfigureAwait(true);
            Databases.Clear();
            Databases.AddRange(dbs.Where(db => !db.StartsWith("_", StringComparison.Ordinal)).Select(db => new ServerDatabase(db, FindConnection(db))));
            IsLoaded = true;
        }

        private DbConnection? FindConnection(string db)
        {
            var connections = ConnectionsModule.Connections;
            return connections.Items.FirstOrDefault(c => c.Url == Connection.Url && c.Database == db);
        }

        private void CreateConnection(ServerDatabase db)
        {
            ConnectionsModule.NewConnection(db.Name, Connection.Url, Connection.UserName, Connection.Password, db.Name);
        }

        private async Task DeleteDatabase(ServerDatabase db)
        {
            if (Workspace.DialogService.ShowQuestion($"Really delete database {db.Name}?\n\nThis operation is performed on remote server and cannot be reverted."))
            {
                await cdbServer.DeleteDatabaseAsync(db.Name).ConfigureAwait(true);
                Databases.Remove(db);
            }
        }

        private async Task CloneDatabase(ServerDatabase db)
        {
            var dialog = new CloneDatabaseDialog(Databases.Select(d => d.Name).ToList()) { Source = db?.Name };
            if (Workspace.DialogService.ShowDialog(dialog))
            {
                var createConnection = dialog.CreateConnection;
                void OnComplete()
                {
                    if (createConnection)
                        ConnectionsModule.NewConnection(dialog.Name, Connection.Url, Connection.UserName, Connection.Password, dialog.Name);
                }

                await cdbServer.CreateDatabaseAsync(dialog.Name).ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(dialog.Source))
                {
                    var sourceDatabase = cdbServer.GetDatabase(dialog.Source);
                    var targetDatabase = cdbServer.GetDatabase(dialog.Name);
                    var targetTempStorage = TempStorage.Create();
                    var targetBackend = new JobProxyDatabaseBackend(new CouchDatabaseBackend(targetDatabase, targetTempStorage), Workspace.Scheduler);

                    Notifications.Show(new CopyDatabaseNotification(sourceDatabase, targetBackend, OnComplete));
                }
                else
                {
                    OnComplete();
                }

                await LoadAsync().ConfigureAwait(true);
            }
        }
    }
}
