using Hercules.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;

namespace Hercules.Repository
{
    public record GitlabSettings(Uri GitlabUrl, string Project, string AccessToken, string Branch);

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used in JSON")]
    public record GitlabEntry(string name, string path, string type);

    public sealed class ObservableGitlabImage : NotifyPropertyChanged, IReadOnlyObservableValue<BitmapSource>, IDisposable
    {
        private readonly GitlabRepository gitlabRepository;
        private readonly IReadOnlyObservableValue<string?> filename;
        private readonly BitmapSource defaultBitmap;
        private readonly CancellationTokenSource cts = new();
        private string? currentPath = null;

        public BitmapSource Value { get; private set; }

        public IDisposable Subscribe(IObserver<BitmapSource> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => Value).Subscribe(observer);
        }

        public ObservableGitlabImage(GitlabRepository gitlabRepository, IReadOnlyObservableValue<string?> filename, BitmapSource defaultBitmap)
        {
            this.gitlabRepository = gitlabRepository;
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
                var requestPath = $"/api/v4/projects/{HttpUtility.UrlEncode(gitlabRepository.Settings.Project)}/repository/files/{HttpUtility.UrlEncode(path)}/raw";
                if (!string.IsNullOrWhiteSpace(gitlabRepository.Settings.Branch))
                    requestPath += $"?ref={HttpUtility.UrlEncode(gitlabRepository.Settings.Branch.Trim())}";
                var response = await gitlabRepository.HttpClient.GetAsync(requestPath, ct).ConfigureAwait(true);
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

    public class GitlabRepository : IRepository
    {
        private GitlabSettings settings;
        private readonly IDialogService dialogService;

        public GitlabSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                httpClient.BaseAddress = settings.GitlabUrl;
                httpClient.DefaultRequestHeaders.Remove("PRIVATE-TOKEN");
                if (!string.IsNullOrWhiteSpace(settings.AccessToken))
                    httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", settings.AccessToken);
            }
        }

        private readonly HttpClient httpClient;

        public HttpClient HttpClient => httpClient;

        public PathStyle PathStyle => PathStyle.Slash;

        public GitlabRepository(GitlabSettings settings, IDialogService dialogService)
        {
            this.settings = settings;
            this.dialogService = dialogService;
            httpClient = HttpClientFactory.Create(settings.GitlabUrl);
            if (!string.IsNullOrWhiteSpace(settings.AccessToken))
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", settings.AccessToken);
        }

        public IReadOnlyObservableValue<BitmapSource> ObserveImage(IReadOnlyObservableValue<string?> filename, BitmapSource defaultBitmap)
        {
            return new ObservableGitlabImage(this, filename, defaultBitmap);
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

        public async Task RefreshFolderAsync(RepositoryFolder folder, CancellationToken ct)
        {
            void FillFolder(GitlabEntry[] fillEntries)
            {
                foreach (var entry in fillEntries)
                {
                    if (entry.type == "tree")
                    {
                        folder.Folders.Add(new RepositoryFolder(this, entry.name, entry.path));
                    }
                    else if (entry.type == "blob")
                    {
                        folder.Files.Add(new RepositoryFile(this, entry.name, entry.path));
                    }
                }
            }

            var requestPath = $"/api/v4/projects/{HttpUtility.UrlEncode(settings.Project)}/repository/tree?path={HttpUtility.UrlEncode(folder.Path)}&pagination=keyset&per_page=100&order_by=name&sort=asc";
            if (!string.IsNullOrWhiteSpace(settings.Branch))
                requestPath += $"&ref={settings.Branch}";

            var response = await httpClient.GetAsync(requestPath, ct);
            ct.ThrowIfCancellationRequested();
            response.EnsureSuccessStatusCode();

            var entries = await response.Content.ReadFromJsonAsync<GitlabEntry[]>(JsonSerializerOptions.Default, ct);
            if (entries != null)
            {
                folder.Folders.Clear();
                folder.Files.Clear();
                FillFolder(entries);
                while (response.Headers.TryGetValues("Link", out var linkValues) && ParseLinkHeaderUri(linkValues.First(), "next", out var next))
                {
                    response = await httpClient.GetAsync(next, ct);
                    ct.ThrowIfCancellationRequested();
                    response.EnsureSuccessStatusCode();
                    entries = await response.Content.ReadFromJsonAsync<GitlabEntry[]>(JsonSerializerOptions.Default, ct);
                    if (entries != null)
                    {
                        FillFolder(entries);
                    }
                }
            }
        }

        private static readonly Regex LinkHeaderRegex = new Regex(@"<(.*)>; *rel=\""(.*)\""", RegexOptions.Compiled);

        private static bool ParseLinkHeaderUri(string linkHeaderValue, ReadOnlySpan<char> rel, [MaybeNullWhen(returnValue: false)] out string uri)
        {
            var matches = LinkHeaderRegex.Matches(linkHeaderValue);
            foreach (Match match in matches)
            {
                if (match.Groups[1].ValueSpan == rel)
                {
                    uri = match.Groups[0].Value;
                    return true;
                }
            }

            uri = null;
            return false;
        }

        public async Task<BitmapSource> DownloadImageAsync(string filename, CancellationToken ct)
        {
            var requestPath = $"/api/v4/projects/{HttpUtility.UrlEncode(Settings.Project)}/repository/files/{HttpUtility.UrlEncode(filename)}/raw";
            if (!string.IsNullOrWhiteSpace(Settings.Branch))
                requestPath += $"?ref={HttpUtility.UrlEncode(Settings.Branch.Trim())}";
            var response = await HttpClient.GetAsync(requestPath, ct).ConfigureAwait(true);
            ct.ThrowIfCancellationRequested();
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            ct.ThrowIfCancellationRequested();
            return FileUtils.LoadImageFromStream(stream, Path.GetExtension(filename));
        }
    }
}
