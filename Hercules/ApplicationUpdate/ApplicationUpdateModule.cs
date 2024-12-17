using Hercules.Shell;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.ApplicationUpdate
{
    public class ApplicationUpdateModule : CoreModule
    {
        public ApplicationUpdateModule(Core core)
            : base(core)
        {
            this.applicationUpdater = new ApplicationUpdater();

            CheckForUpdatesCommand = Commands.ExecuteAsync(CheckForUpdatesAsync);

            applicationUpdateSubscription = applicationUpdater.WhenUpdateAvailable.Subscribe(_ => NotifyAboutUpdate());

            settingsPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<SettingsPage>().Subscribe(p => p.Tabs.Add(new ApplicationUpdateSettingsTab(UpdateChannel, UpdateOnLaunch, CheckForUpdatesCommand)));

            var onlineHelpOption = new UiCommandOption("Help", Fugue.Icons.QuestionWhite, ApplicationCommands.Help);
            Workspace.OptionManager.AddMenuOption(onlineHelpOption, "Help#0", showInToolbar: true);
            var releaseNotesOption = new UiCommandOption("Release Notes", null, ShowReleaseNotes);
            Workspace.OptionManager.AddMenuOption(releaseNotesOption, "Help#10");
            var checkForUpdatesOption = new UiCommandOption("Check for Updates", null, CheckForUpdatesCommand);
            Workspace.OptionManager.AddMenuOption(checkForUpdatesOption, "Help#10");

            core.SettingsService.AddSetting(UpdateChannel);
            core.SettingsService.AddSetting(UpdateOnLaunch);

            Workspace.ShortcutService.RegisterSpecialPage("Release Notes", "release_notes", ShowReleaseNotes, ReleaseNotesShortcut.Instance);

            Workspace.BindCommand(ApplicationCommands.Help, ShowOnlineHelp);
        }

        public Setting<ApplicationUpdateChannel> UpdateChannel { get; } = new Setting<ApplicationUpdateChannel>(nameof(UpdateChannel), ApplicationUpdateChannel.Stable);

        public Setting<bool> UpdateOnLaunch { get; } = new Setting<bool>(nameof(UpdateOnLaunch), true);

        public ICommand CheckForUpdatesCommand { get; }

        private readonly ApplicationUpdater applicationUpdater;
        private readonly IDisposable applicationUpdateSubscription;
        private readonly IDisposable settingsPageSubscription;

        public override void OnShutdown()
        {
            applicationUpdateSubscription.Dispose();
            settingsPageSubscription.Dispose();
            var updateFileName = applicationUpdater.VersionInfo?.FileName;
            if (!string.IsNullOrEmpty(updateFileName))
                Process.Start(updateFileName, "/SILENT");
        }

        public override void OnLoaded()
        {
            if (UpdateOnLaunch.Value && !Core.IsBatch)
                CheckForUpdatesAsync().Track();
        }

        private Task CheckForUpdatesAsync()
        {
            var progress = new Progress<DownloadProgress>(ApplicationUpdaterUpdaterDownloadProgress);
            return applicationUpdater.CheckForUpdatesAsync(UpdateChannel.Value, progress);
        }

        public void ShowReleaseNotes()
        {
            Workspace.OpenBrowser("Release Notes", "{ReleaseNotes}", false, PathUtils.GetLocalHtmlUri(@"Help/ReleaseNotes.html"));
        }

        public void ShowOnlineHelp()
        {
            Workspace.OpenExternalBrowser(new Uri($"https://toadman-hercules.readthedocs.io/latest/index.html"));
        }

        void NotifyAboutUpdate()
        {
            var viewReleaseNotesCommand = Commands.Execute(() => Workspace.OpenExternalBrowser(applicationUpdater.VersionInfo!.ReleaseNotesUri));
            var dialog = new ApplicationUpdateDialog(viewReleaseNotesCommand);
            if (Core.Workspace.DialogService.ShowDialog(dialog))
            {
                Workspace.Quit();
            }
            else
            {
                Workspace.AdviceManager.RemoveByType("ApplicationUpdate");
                Workspace.AdviceManager.AddAdvice("ApplicationUpdate", "Restart and apply application update", Commands.Execute(Workspace.Quit));
            }
        }

        private void ApplicationUpdaterUpdaterDownloadProgress(DownloadProgress downloadProgress)
        {
            Core.Workspace.ProgressText = $"Application update: {(double)downloadProgress.BytesReceived * 100 / downloadProgress.TotalBytesToReceive:F2} %";
            Core.Workspace.Progress = downloadProgress.ProgressPercentage;
        }
    }
}
