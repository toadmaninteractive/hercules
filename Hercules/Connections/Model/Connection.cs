using System;

namespace Hercules.Connections
{
    public class DbConnection : NotifyPropertyChanged
    {
        public string Path => PathUtils.DatabaseFolder(Folder);

        string title;

        public string Title
        {
            get => title;
            set => SetField(ref title, value);
        }

        public Uri Url { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Folder { get; set; }

        public DbConnection(string title, Uri uri, string userName, string password, string database, string folder)
        {
            this.title = title;
            Url = uri;
            Username = userName;
            Password = password;
            Database = database;
            Folder = folder;
        }
    }
}
