using Microsoft.Win32;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hercules.Repository
{
    public class LocalRepository : IRepository
    {
        public required string RepositoryPath { get; set; }

        public PathStyle PathStyle => PathStyle.Backslash;

        public bool Browse(string title, BrowseRepositoryDialogParams dialogParams, [MaybeNullWhen(false)] out string filePath)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Title = title };
            var rootPath = PathStyle.Join(RepositoryPath, dialogParams.RootPath);
            var initialFileName = PathStyle.NormalizeDelimiters(dialogParams.InitialFileName);
            var defaultPath = PathStyle.NormalizeDelimiters(dialogParams.DefaultPath);
            if (initialFileName != "")
            {
                openFileDialog.InitialDirectory = PathStyle.Join(rootPath, PathStyle.GetDirectory(initialFileName));
                openFileDialog.FileName = PathStyle.GetFileName(initialFileName);
            }
            else if (defaultPath != "")
            {
                openFileDialog.InitialDirectory = PathStyle.Join(rootPath, PathStyle.GetDirectory(initialFileName));
            }
            else
            {
                openFileDialog.InitialDirectory = rootPath;
            }
            if (!string.IsNullOrEmpty(dialogParams.DefaultExtension))
            {
                openFileDialog.Filter = $"(*.{dialogParams.DefaultExtension}) | *.{dialogParams.DefaultExtension}";
                openFileDialog.DefaultExt = $".{dialogParams.DefaultExtension}";
            }
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = PathStyle.GetRelativePath(rootPath, openFileDialog.FileName).Replace('\\', '/');
                return true;
            }
            else
            {
                filePath = null;
                return false;
            }
        }

        public IReadOnlyObservableValue<BitmapSource> ObserveImage(IReadOnlyObservableValue<string?> filename, BitmapSource defaultBitmap)
        {
            return filename.Wrap(f => LoadImage(f) ?? defaultBitmap);
        }

        public Task RefreshFolderAsync(RepositoryFolder folder, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<BitmapSource> DownloadImageAsync(string filename, CancellationToken ct)
        {
            return Task.FromResult(FileUtils.LoadImageFromFile(filename));
        }

        private BitmapSource? LoadImage(string? filename) => string.IsNullOrWhiteSpace(filename) ? null : FileUtils.LoadImageFromFile(Path.Join(RepositoryPath, filename));
    }
}
