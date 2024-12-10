using System;

namespace Hercules.ServerBrowser
{
    public record ServerConnectionParams(Uri Url, string UserName, string Password);
}
