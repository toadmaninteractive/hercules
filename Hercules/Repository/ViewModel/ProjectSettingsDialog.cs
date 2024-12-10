using Hercules.Shell;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Repository
{
    public class ProjectSettingsDialog : ValidatedDialog
    {
        private RepositoryType repositoryType;
        public RepositoryType RepositoryType
        {
            get => repositoryType;
            set => SetField(ref repositoryType, value);
        }

        public string ProjectRootFolder
        {
            get => projectRootFolder;
            set => SetField(ref projectRootFolder, value);
        }

        public string GitlabUrl
        {
            get => gitlabUrl;
            set => SetField(ref gitlabUrl, value);
        }

        public string GitlabAccessToken
        {
            get => gitlabAccessToken;
            set => SetField(ref gitlabAccessToken, value);
        }

        public string GitlabBranch
        {
            get => gitlabBranch;
            set => SetField(ref gitlabBranch, value);
        }

        public string RemoteBaseUrl
        {
            get => remoteBaseUrl;
            set => SetField(ref remoteBaseUrl, value);
        }

        public string GitlabTestStatus
        {
            get => gitlabTestStatus;
            set => SetField(ref gitlabTestStatus, value);
        }

        public SolidColorBrush GitlabTestColor
        {
            get => gitlabTestColor;
            set => SetField(ref gitlabTestColor, value);
        }

        public ICommand SelectProjectRootFolderCommand { get; }
        public ICommand TestGitlabCommand { get; }
        public ICommand GenerateGitlabAssetTokenCommand { get; }

        private string projectRootFolder;
        private string gitlabUrl;
        private string gitlabAccessToken;
        private string gitlabBranch;
        private string remoteBaseUrl;
        private string gitlabTestStatus = string.Empty;
        private SolidColorBrush gitlabTestColor = Brushes.Gray;
        private CancellationTokenSource? gitlabTestCts;
        private int gitlabTestVersion;

        public ProjectSettingsDialog(ProjectSettings settings)
        {
            projectRootFolder = settings.ProjectRootFolder;
            repositoryType = settings.RepositoryType;
            gitlabUrl = settings.GitlabUrl;
            gitlabAccessToken = settings.GitlabAccessToken;
            gitlabBranch = settings.GitlabBranch;
            remoteBaseUrl = settings.RemoteBaseUrl;

            Title = "Project Settings";
            SelectProjectRootFolderCommand = Commands.Execute(SelectProjectRootFolder);
            TestGitlabCommand = Commands.Execute(TestGitlab);
            GenerateGitlabAssetTokenCommand = Commands.Execute(GenerateGitlabAssetToken);
        }

        private void SelectProjectRootFolder()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
                ProjectRootFolder = dialog.SelectedPath;
        }

        private void GenerateGitlabAssetToken()
        {
            if (string.IsNullOrWhiteSpace(gitlabUrl))
                return;
            var uri = new Uri(gitlabUrl).GetLeftPart(UriPartial.Authority) + "/-/profile/personal_access_tokens";
            Workspace.OpenExternalBrowser(new Uri(uri));
        }

        private void TestGitlab()
        {
            if (gitlabTestCts != null)
                gitlabTestCts.Cancel();

            gitlabTestCts = new CancellationTokenSource();
            gitlabTestVersion++;
            _ = TestGitlabAsync(gitlabTestVersion, gitlabTestCts.Token);
        }

        private async Task TestGitlabAsync(int version, CancellationToken ct)
        {
            GitlabTestStatus = "Connecting...";
            GitlabTestColor = Brushes.Gray;
            try
            {
                var uri = new Uri(GitlabUrl);
                var gitlabUrl = new Uri(uri.GetLeftPart(UriPartial.Authority));
                using var httpClient = HttpClientFactory.Create(gitlabUrl);
                var gitlabProject = uri.PathAndQuery.RemoveSuffix(".git").TrimStart('/');
                if (!string.IsNullOrWhiteSpace(gitlabAccessToken))
                    httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", gitlabAccessToken);

                var requestPath = $"/api/v4/projects/{HttpUtility.UrlEncode(gitlabProject)}/repository/tree";
                if (!string.IsNullOrWhiteSpace(gitlabBranch))
                    requestPath += $"?ref={gitlabBranch}";
                var response = await httpClient.GetAsync(requestPath, ct).ConfigureAwait(true);
                response.EnsureSuccessStatusCode();
                ct.ThrowIfCancellationRequested();
                if (version == gitlabTestVersion)
                {
                    GitlabTestStatus = "OK";
                    GitlabTestColor = Brushes.Green;
                }
            }
            catch (Exception exception)
            {
                if (version == gitlabTestVersion)
                {
                    Logger.LogException("Test Gitlab connection", exception);
                    GitlabTestStatus = FormatError(exception);
                    GitlabTestColor = Brushes.Red;
                }
            }
        }

        private string FormatError(Exception exception)
        {
            return exception switch
            {
                UriFormatException _ => "Invalid URL",
                HttpRequestException { StatusCode: HttpStatusCode.NotFound } => "Not Found",
                HttpRequestException httpRequestEx when httpRequestEx.StatusCode.HasValue => httpRequestEx.StatusCode.Value.ToString(),
                HttpRequestException { InnerException: SocketException socketEx } => socketEx.SocketErrorCode.ToString(),
                _ => "Error"
            };
        }
    }
}
