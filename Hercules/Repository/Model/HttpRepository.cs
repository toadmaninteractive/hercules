using Hercules.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hercules.Repository
{
    public sealed class ObservableHttpImage : NotifyPropertyChanged, IReadOnlyObservableValue<BitmapSource>, IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly IReadOnlyObservableValue<string?> filename;
        private readonly BitmapSource defaultBitmap;
        private readonly CancellationTokenSource cts = new();
        private string? currentPath = null;

        public BitmapSource Value { get; private set; }

        public IDisposable Subscribe(IObserver<BitmapSource> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => Value).Subscribe(observer);
        }

        public ObservableHttpImage(HttpClient httpClient, IReadOnlyObservableValue<string?> filename, BitmapSource defaultBitmap)
        {
            this.httpClient = httpClient;
            this.filename = filename;
            this.defaultBitmap = defaultBitmap;
            Value = defaultBitmap;
            UpdateImage(filename.Value);
            filename.PropertyChanged += Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IReadOnlyObservableValue<string?>.Value))
            {
                UpdateImage(filename.Value);
            }
        }

        public override string? ToString()
        {
            return filename?.ToString();
        }

        public void Dispose()
        {
            filename.PropertyChanged -= Source_PropertyChanged;
            cts.Cancel();
        }

        private void UpdateImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                if (!ReferenceEquals(Value, defaultBitmap))
                {
                    Value = defaultBitmap;
                    RaisePropertyChanged(nameof(Value));
                }
            }
            else
            {
                currentPath = path.Replace('\\', '/');
                _ = DownloadImageAsync(currentPath, cts.Token);
            }
        }

        private async Task DownloadImageAsync(string path, CancellationToken ct)
        {
            try
            {
                var response = await httpClient.GetAsync(path, ct).ConfigureAwait(true);
                ct.ThrowIfCancellationRequested();
                if (path == currentPath)
                {
                    response.EnsureSuccessStatusCode();
                    var stream = await response.Content.ReadAsStreamAsync(ct);
                    ct.ThrowIfCancellationRequested();
                    if (path == currentPath)
                    {
                        Value = FileUtils.LoadImageFromStream(stream, Path.GetExtension(path));
                        RaisePropertyChanged(nameof(Value));
                    }
                }
            }
            catch (Exception)
            {
                if (path == currentPath)
                {
                    Value = defaultBitmap;
                    RaisePropertyChanged(nameof(Value));
                }
            }
        }
    }

#pragma warning disable IDE1006 // Naming Styles
    public record HttpFileEntry(string type, string name);
#pragma warning restore IDE1006 // Naming Styles

    public class HttpRepository : IRepository
    {
        private readonly HttpClient httpClient;
        private readonly IDialogService dialogService;

        public HttpClient HttpClient => httpClient;

        public Uri BaseUrl
        {
            get => HttpClient.BaseAddress!;
            set => HttpClient.BaseAddress = value;
        }

        public PathStyle PathStyle => PathStyle.Slash;

        public HttpRepository(Uri baseUrl, IDialogService dialogService)
        {
            httpClient = HttpClientFactory.Create(baseUrl);
            this.dialogService = dialogService;
        }

        public IReadOnlyObservableValue<BitmapSource> ObserveImage(IReadOnlyObservableValue<string?> filename, BitmapSource defaultBitmap)
        {
            return new ObservableHttpImage(HttpClient, filename, defaultBitmap);
        }

        public async Task RefreshFolderAsync(RepositoryFolder folder, CancellationToken ct)
        {
            var requestPath = folder.Path;
            var entries = await httpClient.GetFromJsonAsync<HttpFileEntry[]>(requestPath, ct);
            if (entries != null)
            {
                folder.Folders.Clear();
                folder.Files.Clear();
                foreach (var entry in entries)
                {
                    var entryPath = string.IsNullOrEmpty(folder.Path) ? entry.name : (folder.Path.EnsureTrailingSlash() + entry.name);
                    if (entry.type == "directory")
                    {
                        folder.Folders.Add(new RepositoryFolder(this, entry.name, entryPath));
                    }
                    else if (entry.type == "file")
                    {
                        folder.Files.Add(new RepositoryFile(this, entry.name, entryPath));
                    }
                }
            }
        }

        public async Task<BitmapSource> DownloadImageAsync(string filename, CancellationToken ct)
        {
            var response = await httpClient.GetAsync(filename, ct).ConfigureAwait(true);
            ct.ThrowIfCancellationRequested();
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            ct.ThrowIfCancellationRequested();
            return FileUtils.LoadImageFromStream(stream, Path.GetExtension(filename));
        }

        public bool Browse(string title, BrowseRepositoryDialogParams dialogParams, [MaybeNullWhen(false)] out string filePath)
        {
            var dialog = new BrowseRepositoryDialog(title, this, dialogParams);
            if (dialogService.ShowDialog(dialog))
            {
                filePath = dialog.SelectedPath!;
                return true;
            }
            else
            {
                filePath = null;
                return false;
            }
        }
    }
}
