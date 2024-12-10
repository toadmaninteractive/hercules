using Hercules.Shell;
using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Connections
{
    public class ConnectionsDialog : Dialog
    {
        public ConnectionsModule ConnectionsModule { get; }
        public IDialogService DialogService { get; }
        public DbConnectionCollection Connections { get; }

        public ConnectionsDialog(ConnectionsModule connectionsModule, IDialogService dialogService)
        {
            ConnectionsModule = connectionsModule;
            DialogService = dialogService;
            Connections = connectionsModule.Connections;
            Title = "Manage Connections";
            NewCommand = Commands.Execute(NewConnection);
            EditCommand = Commands.Execute<DbConnection>(EditConnection).IfNotNull();
            DeleteCommand = Commands.Execute<DbConnection>(DeleteConnection).IfNotNull();
            ConnectCommand = Commands.Execute<DbConnection>(Connect).IfNotNull().If(c => c != Connections.ActiveConnection);
            UpCommand = Commands.Execute<DbConnection>(MoveUp).IfNotNull().If(c => Connections.Items.First() != c);
            DownCommand = Commands.Execute<DbConnection>(MoveDown).IfNotNull().If(c => Connections.Items.Last() != c);
            SortCommand = Commands.Execute(Sort);
        }

        DbConnection? selectedConnection;

        public DbConnection? SelectedConnection
        {
            get => selectedConnection;
            set => SetField(ref selectedConnection, value);
        }

        public ICommand<DbConnection> LoadConnectionCommand => ConnectionsModule.LoadConnectionCommand;
        public ICommand NewCommand { get; }
        public ICommand<DbConnection> EditCommand { get; }
        public ICommand<DbConnection> DeleteCommand { get; }
        public ICommand<DbConnection> ConnectCommand { get; }
        public ICommand<DbConnection> UpCommand { get; }
        public ICommand<DbConnection> DownCommand { get; }
        public ICommand SortCommand { get; }

        void NewConnection()
        {
            var dialog = new EditConnectionDialog(Connections, null);
            if (DialogService.ShowDialog(dialog))
            {
                SelectedConnection = dialog.Connection;
            }
        }

        void EditConnection(DbConnection connection)
        {
            var dialog = new EditConnectionDialog(Connections, connection);
            DialogService.ShowDialog(dialog);
        }

        void Connect(DbConnection connection)
        {
            if (ConnectionsModule.LoadConnection(connection))
                SetDialogResult(true);
        }

        void DeleteConnection(DbConnection connection)
        {
            if (!DialogService.ShowQuestion("Are you sure you want to delete connection '" + connection.Title + "'?"))
                return;

            if (Connections.ActiveConnection == connection)
            {
                if (!ConnectionsModule.CloseConnection())
                    return;
            }

            Connections.Items.Remove(connection);
            Connections.SaveConfiguration();
            if (connection.Path.Contains("Hercules", StringComparison.OrdinalIgnoreCase))
                Directory.Delete(connection.Path, true);
        }

        void MoveDown(DbConnection connection)
        {
            Connections.Items.MoveDown(connection);
            Connections.SaveConfiguration();
        }

        void MoveUp(DbConnection connection)
        {
            Connections.Items.MoveUp(connection);
            Connections.SaveConfiguration();
        }

        void Sort()
        {
            var sorted = Connections.Items.OrderBy(c => c.Title).ToList();
            Connections.Items.Clear();
            Connections.Items.AddRange(sorted);
            Connections.SaveConfiguration();
        }

        protected override void OnClose(bool result)
        {
            base.OnClose(result);
            ConnectionsModule.SaveJumpList();
        }
    }
}
