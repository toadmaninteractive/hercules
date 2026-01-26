using Hercules.Connections;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Documents
{
    public static class HerculesUrl
    {
        static string MakeHerculesUrl(Uri url)
        {
            var urlString = url.ToString();
            var i = urlString.IndexOf("://", StringComparison.Ordinal);
            if (i < 0)
                return "hercules://" + urlString;
            else
                return "hercules://" + urlString.Substring(i + 3);
        }

        public static string GetUrl(DbConnection connection, IDocument document)
        {
            ArgumentNullException.ThrowIfNull(connection);
            var url = MakeHerculesUrl(connection.Url);
            if (!url.EndsWith(@"/", StringComparison.Ordinal))
                url += @"/";
            return url + connection.Database + "/" + document.DocumentId;
        }

        public static string GetDatabaseUrl(DbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            var url = MakeHerculesUrl(connection.Url);
            if (!url.EndsWith(@"/", StringComparison.Ordinal))
                url += @"/";
            return url + connection.Database;
        }

        public static bool IsSuitableConnection(DbConnection connection, Uri herculesUri)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(herculesUri);
            var connectionUri = connection.Url;
            if (string.Compare(herculesUri.Host, connectionUri.Host, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            var herculesPort = herculesUri.Port <= 0 ? (int?)null : herculesUri.Port;
            var connectionPort = connectionUri.Port <= 0 ? 80 : connectionUri.Port;
            if (herculesPort.HasValue && herculesPort != connectionPort)
                return false;
            if (herculesUri.Segments.Length < 2)
                return false;
            var db = herculesUri.Segments[1].TrimEnd('/');
            if (string.Compare(db, connection.Database, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            return true;
        }

        public static string? DocumentId(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            if (uri.Segments.Length > 2)
                return uri.Segments[2].TrimEnd('/');
            else
                return null;
        }

        public static string? Database(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            if (uri.Segments.Length > 1)
                return uri.Segments[1].TrimEnd('/');
            else
                return null;
        }

        public static bool TryGetDatabaseHerculesUrl(Uri source, [MaybeNullWhen(returnValue: false)] out Uri dbUrl)
        {
            dbUrl = null;
            if (source.Scheme != "hercules" || string.IsNullOrEmpty(source.Host) || source.Segments.Length < 2)
                return false;
            var builder = new UriBuilder();
            builder.Scheme = source.Scheme;
            builder.Host = source.Host;
            builder.Port = source.Port;
            builder.Path = source.Segments[1].TrimEnd('/');
            dbUrl = builder.Uri;
            return true;
        }
    }
}
