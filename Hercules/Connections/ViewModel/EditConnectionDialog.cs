using CouchDB;
using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.Connections
{
#pragma warning disable CA1055 // Uri return values should not be strings
#pragma warning disable CA1056 // Uri properties should not be strings
    public class EditConnectionDialog : ValidatedDialog
    {
        const string DefaultUrl = @"https://";

        public DbConnectionCollection Connections { get; }
        public IReadOnlyList<Uri> KnownUrls { get; }

        public EditConnectionDialog(DbConnectionCollection connections, DbConnection? connection)
        {
            Connections = connections;
            Connection = connection;
            KnownUrls = connections.Items.GroupBy(c => c.Url).Select(g => g.Key).ToList();
            Title = connection == null ? "New Connection" : "Edit Connection";

            if (Connection != null)
            {
                ConnectionTitle = Connection.Title;
                ConnectionUrl = Connection.Url.ToString();
                Database = Connection.Database;
                UserName = Connection.Username;
                Password = Connection.Password;
            }
            else
            {
                ConnectionUrl = DefaultUrl;
            }

            FetchDatabasesCommand = Commands.Execute(() => FetchDatabases(false));
        }

        public DbConnection? Connection { get; private set; }

        public ICommand FetchDatabasesCommand { get; }

        string connectionTitle = string.Empty;

        public string ConnectionTitle
        {
            get => connectionTitle;
            set => SetField(ref connectionTitle, value);
        }

        string connectionUrl = string.Empty;

        private string? titleError;

        public string? TitleError
        {
            get => titleError;
            set => SetField(ref titleError, value);
        }

        [PropertyValidator(nameof(ConnectionTitle))]
        public string? ValidateTitle()
        {
            if (string.IsNullOrWhiteSpace(ConnectionTitle))
            {
                TitleError = "Title cannot be empty";
                return TitleError;
            }

            var existing = Connections.Items.FirstOrDefault(c => string.Compare(c.Title, ConnectionTitle, StringComparison.OrdinalIgnoreCase) == 0);
            TitleError = existing != null && existing != Connection ? "This title is already used for another connection" : null;
            return TitleError;
        }

        public string ConnectionUrl
        {
            get => connectionUrl;
            set
            {
                if (SetField(ref connectionUrl, value) && Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    var known = Connections.Items.FirstOrDefault(c => c.Url == uri);
                    if (known != null)
                    {
                        UserName = known.Username;
                        Password = known.Password;
                    }
                }
            }
        }

        private string? urlError;

        public string? UrlError
        {
            get => urlError;
            set => SetField(ref urlError, value);
        }

        string database = string.Empty;

        [Required(ErrorMessage = "Database cannot be empty")]
        public string Database
        {
            get => database;
            set
            {
                if (database != value)
                {
                    if (string.IsNullOrEmpty(ConnectionTitle) || ConnectionTitle == database)
                        ConnectionTitle = value;
                    SetField(ref database, value);
                }
            }
        }

        string userName = string.Empty;

        public string UserName
        {
            get => userName;
            set => SetField(ref userName, value);
        }

        private string? userNameError;

        public string? UserNameError
        {
            get => userNameError;
            set => SetField(ref userNameError, value);
        }

        [PropertyValidator(nameof(UserName))]
        public string? ValidateUserName()
        {
            UserNameError = string.IsNullOrWhiteSpace(UserName) ? "UserName cannot be empty" : null;
            return UserNameError;
        }

        string password = string.Empty;

        public string Password
        {
            get => password;
            set => SetField(ref password, value);
        }

        private string? passwordError;

        public string? PasswordError
        {
            get => passwordError;
            set => SetField(ref passwordError, value);
        }

        [PropertyValidator(nameof(Password))]
        public string? ValidatePasswordError()
        {
            PasswordError = string.IsNullOrWhiteSpace(Password) ? "Password cannot be empty" : null;
            return PasswordError;
        }

        public ObservableCollection<string> FetchedDatabases { get; private set; } = new ObservableCollection<string>();

        [PropertyValidator(nameof(ConnectionUrl))]
        public string? ValidateConnectionUrl()
        {
            UrlError = !Uri.IsWellFormedUriString(ConnectionUrl, UriKind.Absolute) ? "Connection URL should be a well-formed URL" : null;
            return UrlError;
        }

        string fetchDatabasesError = string.Empty;

        public string FetchDatabasesError
        {
            get => fetchDatabasesError;
            set => SetField(ref fetchDatabasesError, value);
        }

        CancellationTokenSource? fetchDatabasesCancellation;
        string? fetchDatabasesUrl;
        string? fetchDatabasesUserName;
        string? fetchDatabasesPassword;

        void FetchDatabases(bool silent)
        {
            CancelFetchDatabases();
            if (!silent)
                FetchDatabasesError = string.Empty;
            fetchDatabasesCancellation = new CancellationTokenSource();
            fetchDatabasesUrl = ConnectionUrl;
            fetchDatabasesUserName = UserName;
            fetchDatabasesPassword = Password;
            FetchDatabasesAsync(silent, ConnectionUrl, UserName, Password, fetchDatabasesCancellation.Token).Track();
        }

        async Task FetchDatabasesAsync(bool silent, string url, string username, string pass, CancellationToken token)
        {
            try
            {
                var databases = await new CdbServer(new Uri(url.EnsureTrailingSlash()), username, pass).GetDatabaseNamesAsync(token).ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                FetchedDatabases.Clear();
                FetchedDatabases.AddRange(databases.Where(db => !db.StartsWith("_", StringComparison.Ordinal)));
                FetchDatabasesError = string.Empty;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                FetchedDatabases.Clear();
                if (!silent || string.IsNullOrEmpty(FetchDatabasesError))
                {
                    FetchDatabasesError = "Failed to fetch database list:\n" + TaskUtils.ExceptionMessage(ex);
                    Logger.LogException("Failed to fetch database list", ex.GetInnerException());
                }
            }
        }

        public void MaybeRefetchDatabases()
        {
            if (fetchDatabasesUrl != ConnectionUrl || fetchDatabasesUserName != UserName || fetchDatabasesPassword != Password)
                FetchDatabases(true);
        }

        void CancelFetchDatabases()
        {
            if (fetchDatabasesCancellation != null)
            {
                fetchDatabasesCancellation.Cancel();
                fetchDatabasesCancellation.Dispose();
                fetchDatabasesCancellation = null;
                fetchDatabasesUrl = null;
                fetchDatabasesUserName = null;
                fetchDatabasesPassword = null;
            }
        }

        protected override void OnClose(bool result)
        {
            CancelFetchDatabases();
            if (result)
            {
                if (Connection == null)
                {
                    Connection = new DbConnection(ConnectionTitle, new Uri(this.ConnectionUrl.EnsureTrailingSlash()), UserName, Password,
                        Database, PathUtils.GenerateUniqueFolderName());

                    Connections.Items.Add(Connection);
                    Connections.SaveConfiguration();

                    Logger.Log("Added new connection: " + Connection.Title);
                }
                else
                {
                    Connection.Title = this.ConnectionTitle;
                    Connection.Url = new Uri(this.ConnectionUrl.EnsureTrailingSlash());
                    Connection.Database = this.Database;
                    Connection.Username = this.UserName;
                    Connection.Password = this.Password;
                    Connections.SaveConfiguration();
                }
            }
        }
    }
#pragma warning restore CA1056 // Uri properties should not be strings
#pragma warning restore CA1055 // Uri return values should not be strings
}
