using Hercules.Shell;
using System;

namespace Hercules.Repository
{
    public enum RepositoryType
    {
        Local,
        Http,
        Gitlab,
    }

    public class ProjectSettings
    {
        public RepositoryType RepositoryType { get; private set; } = RepositoryType.Local;
        public string ProjectRootFolder { get; private set; } = string.Empty;
        public string RemoteBaseUrl { get; private set; } = string.Empty;
        public string GitlabUrl { get; private set; } = string.Empty;
        public string GitlabAccessToken { get; private set; } = string.Empty;
        public string GitlabBranch { get; private set; } = string.Empty;

        public IRepository? Repository { get; private set; }

        private readonly IDialogService dialogService;

        public bool ShowDialog()
        {
            var dialog = new ProjectSettingsDialog(this);
            if (dialogService.ShowDialog(dialog))
            {
                ProjectRootFolder = dialog.ProjectRootFolder!;
                GitlabUrl = dialog.GitlabUrl;
                GitlabAccessToken = dialog.GitlabAccessToken;
                GitlabBranch = dialog.GitlabBranch;
                RemoteBaseUrl = dialog.RemoteBaseUrl;
                RepositoryType = dialog.RepositoryType;
                UpdateRepository();
                return true;
            }

            return false;
        }

        public ProjectSettings(IDialogService dialogService)
        {
            this.dialogService = dialogService;
        }

        public void Load(ISettingsReader settingsReader)
        {
            try
            {
                if (settingsReader.Read<string>(nameof(ProjectRootFolder), out var projectRootFolder))
                    ProjectRootFolder = projectRootFolder;
                if (settingsReader.Read<string>(nameof(RemoteBaseUrl), out var remoteBaseUrl))
                    RemoteBaseUrl = remoteBaseUrl;
                if (settingsReader.Read<string>(nameof(GitlabUrl), out var gitlabUrl))
                    GitlabUrl = gitlabUrl;
                if (settingsReader.Read<string>(nameof(GitlabAccessToken), out var gitlabAccessToken))
                    GitlabAccessToken = gitlabAccessToken;
                if (settingsReader.Read<string>(nameof(GitlabBranch), out var gitlabBranch))
                    GitlabBranch = gitlabBranch;
                if (settingsReader.Read<RepositoryType>(nameof(RepositoryType), out var repositoryType))
                    RepositoryType = repositoryType;
            }
            catch (Exception ex)
            {
                Logger.LogException("Failed to load project settings", ex);
            }

            UpdateRepository();
        }

        public void Save(ISettingsWriter settingsWriter)
        {
            settingsWriter.Write(nameof(ProjectRootFolder), ProjectRootFolder);
            settingsWriter.Write(nameof(RemoteBaseUrl), RemoteBaseUrl);
            settingsWriter.Write(nameof(GitlabUrl), GitlabUrl);
            settingsWriter.Write(nameof(GitlabAccessToken), GitlabAccessToken);
            settingsWriter.Write(nameof(GitlabBranch), GitlabBranch);
            settingsWriter.Write(nameof(RepositoryType), RepositoryType);
        }

        private void UpdateRepository()
        {
            switch (RepositoryType)
            {
                case RepositoryType.Local:
                    if (!string.IsNullOrWhiteSpace(ProjectRootFolder))
                    {
                        if (Repository is LocalRepository local)
                            local.RepositoryPath = ProjectRootFolder;
                        else
                            Repository = new LocalRepository { RepositoryPath = ProjectRootFolder };
                    }
                    else
                    {
                        Repository = null;
                    }
                    break;

                case RepositoryType.Http:
                    Repository = !string.IsNullOrWhiteSpace(RemoteBaseUrl) ? new HttpRepository(new Uri(RemoteBaseUrl), dialogService) : null;
                    break;

                case RepositoryType.Gitlab:
                    var gitlabSettings = GetGitlabSettings();
                    Repository = gitlabSettings != null ? new GitlabRepository(gitlabSettings, dialogService) : null;
                    break;

                default:
                    Repository = null;
                    break;
            }

            GitlabSettings? GetGitlabSettings()
            {
                if (string.IsNullOrWhiteSpace(GitlabUrl))
                    return null;
                var uri = new Uri(GitlabUrl);
                var gitlabUrl = new Uri(uri.GetLeftPart(UriPartial.Authority));
                var gitlabProject = uri.PathAndQuery.RemoveSuffix(".git").TrimStart('/');
                return new GitlabSettings(gitlabUrl, gitlabProject, GitlabAccessToken, GitlabBranch);
            }
        }
    }
}
