using System;
using System.Globalization;
using System.IO;

namespace Hercules
{
    public static class PathUtils
    {
        public static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string RootFolder => Path.Combine(AppData, @"Artplant\Hercules");

        public static string ConfigFolder => Path.Combine(RootFolder, "Config");

        public static string DataFolder => Path.Combine(RootFolder, "Data");

        public static string TempFolder => Path.Combine(Path.GetTempPath(), "Hercules");

        public static string DatabaseFolder(string database)
        {
            return Path.Combine(DataFolder, database);
        }

        public static void EnsureFoldersExist()
        {
            foreach (var path in new[] { RootFolder, ConfigFolder, DataFolder })
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
        }

        public static string GenerateUniqueFolderName()
        {
            return Guid.NewGuid().ToString("B", CultureInfo.InvariantCulture);
        }

        public static Uri GetLocalHtmlUri(string path)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            return new Uri(fullPath, UriKind.Absolute);
        }

        public static string EnsureTrailingSlash(this string path)
        {
            if (path.EndsWith('/'))
                return path;
            else
                return path + '/';
        }
    }
}
