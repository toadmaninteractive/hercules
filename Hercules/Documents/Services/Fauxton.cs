using Hercules.Connections;
using System;

namespace Hercules.Documents
{
    public static class Fauxton
    {
        public static Uri GetUrl(DbConnection connection, string documentId, bool includeAuth)
        {
            ArgumentNullException.ThrowIfNull(nameof(connection));
            var uriBuilder = new UriBuilder(connection.Url);
            uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + "/_utils/";
            uriBuilder.Fragment += $"database/{connection.Database}/{documentId}";
            if (includeAuth)
            {
                uriBuilder.UserName = connection.Username;
                uriBuilder.Password = connection.Password;
            }
            return uriBuilder.Uri;
        }
    }
}
