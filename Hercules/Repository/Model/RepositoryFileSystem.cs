using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hercules.Repository
{
    public class PathStyle
    {
        protected PathStyle(char delimiter)
        {
            Delimiter = delimiter;
        }

        public char Delimiter { get; }
        public string NormalizeDelimiters(string? path)
        {
            if (path == null)
                return "";
            if (Delimiter == '/')
                return path.Replace('\\', '/');
            else
                return path.Replace('/', '\\');
        }

        public string[] PathParts(string path)
        {
            if (path == "")
                return Array.Empty<string>();
            return path.Split(Delimiter);
        }

        public string Join(params string?[] parts)
        {
            var sb = new StringBuilder();
            bool needDelimiter = false;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                if (needDelimiter)
                    sb.Append(Delimiter);
                sb.Append(NormalizeDelimiters(part));
                needDelimiter = !part.EndsWith(Delimiter);
            }
            return sb.ToString();
        }

        public string GetRelativePath(string relativeTo, string path)
        {
            return Path.GetRelativePath(relativeTo, path);
        }

        public string GetDirectory(string filename)
        {
            var lastIndex = filename.LastIndexOf(Delimiter);
            if (lastIndex >= 0)
                return filename[..lastIndex];
            else
                return "";
        }

        public string GetFileName(string filename)
        {
            var lastIndex = filename.LastIndexOf(Delimiter);
            if (lastIndex >= 0)
                return filename.Substring(lastIndex + 1);
            else
                return filename;
        }

        public static readonly PathStyle Slash = new PathStyle('/');
        public static readonly PathStyle Backslash = new PathStyle('\\');
    }

    public class RepositoryFile : NotifyPropertyChanged
    {
        private readonly IRepository repository;
        public string Name { get; }
        public string Path { get; }

        private BitmapSource? previewImage;

        public BitmapSource? PreviewImage
        {
            get => previewImage;
            private set => SetField(ref previewImage, value);
        }

        private bool isLoading;
        private bool isReady;

        public RepositoryFile(IRepository repository, string name, string path)
        {
            this.repository = repository;
            Name = name;
            Path = path;
        }

        public bool IsReady
        {
            get => isReady;
            private set => SetField(ref isReady, value);
        }

        public bool IsLoading
        {
            get => isLoading;
            private set => SetField(ref isLoading, value);
        }

        public async Task LoadAsync()
        {
            if (!IsLoading && !IsReady)
            {
                IsLoading = true;
                try
                {
                    PreviewImage = await repository.DownloadImageAsync(Path, default);
                }
                catch
                {
                    PreviewImage = FileUtils.NoImage;
                }

                IsLoading = false;
                IsReady = true;
            }
        }
    }

    public class RepositoryFolder : NotifyPropertyChanged
    {
        public string Name { get; }
        public string Path { get; }

        private readonly IRepository repository;
        private bool isExpanded;
        private bool isReady;

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetField(ref isExpanded, value);
        }

        public bool IsReady
        {
            get => isReady;
            private set => SetField(ref isReady, value);
        }

        public Task LoadAsync()
        {
            loadTask ??= LoadAsyncImpl();
            return loadTask;
        }

        private async Task LoadAsyncImpl()
        {
            await repository.RefreshFolderAsync(this, default);
            IsReady = true;
        }

        private Task? loadTask;

        public ObservableCollection<RepositoryFolder> Folders { get; } = new();
        public ObservableCollection<RepositoryFile> Files { get; } = new();

        public RepositoryFolder(IRepository repository, string name, string path)
        {
            this.repository = repository;
            Name = name;
            Path = path;
        }
    }
}
