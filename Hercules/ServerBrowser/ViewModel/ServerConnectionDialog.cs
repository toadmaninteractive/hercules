using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.ServerBrowser
{
    public class ServerConnectionDialog : Dialog
    {
        public ServerConnectionParams? Connection { get; private set; }
        public IReadOnlyList<ServerConnectionParams> KnownConnections { get; private set; }

        public ServerConnectionDialog(IReadOnlyList<ServerConnectionParams> knownConnections)
        {
            KnownConnections = knownConnections;
            if (KnownConnections.Any())
                Url = KnownConnections[0].Url.ToString();
        }

        string url = string.Empty;

#pragma warning disable CA1056 // Uri properties should not be strings
        public string Url
#pragma warning restore CA1056 // Uri properties should not be strings
        {
            get
            {
                return url;
            }
            set
            {
                if (SetField(ref url, value) && Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    var known = KnownConnections.FirstOrDefault(c => c.Url == uri);
                    if (known != null)
                    {
                        UserName = known.UserName;
                        Password = known.Password;
                    }
                }
            }
        }

        string userName = string.Empty;

        public string UserName
        {
            get => userName;
            set => SetField(ref userName, value);
        }

        string password = string.Empty;

        public string Password
        {
            get => password;
            set => SetField(ref password, value);
        }

        protected override void OnClose(bool result)
        {
            if (result)
            {
                Connection = new ServerConnectionParams(new Uri(Url), UserName, Password);
            }
        }
    }
}
