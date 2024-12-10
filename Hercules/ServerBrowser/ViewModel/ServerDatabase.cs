using Hercules.Connections;

namespace Hercules.ServerBrowser
{
    public class ServerDatabase : NotifyPropertyChanged
    {
        public string Name { get; private set; }

        private DbConnection? connection;

        public DbConnection? Connection
        {
            get => connection;
            set => SetField(ref connection, value);
        }

        public ServerDatabase(string name, DbConnection? connection)
        {
            this.Name = name;
            this.connection = connection;
        }
    }
}
