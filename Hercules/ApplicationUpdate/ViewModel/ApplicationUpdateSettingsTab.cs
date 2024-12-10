using Hercules.Shell;
using System.Windows.Input;

namespace Hercules.ApplicationUpdate
{
    public class ApplicationUpdateSettingsTab : PageTab
    {
        public Setting<ApplicationUpdateChannel> UpdateChannel { get; }
        public Setting<bool> UpdateOnLaunch { get; }
        public ICommand CheckForUpdatesCommand { get; }

        public ApplicationUpdateSettingsTab(Setting<ApplicationUpdateChannel> updateChannel, Setting<bool> updateOnLaunch, ICommand checkForUpdatesCommand)
        {
            this.UpdateChannel = updateChannel;
            this.UpdateOnLaunch = updateOnLaunch;
            this.CheckForUpdatesCommand = checkForUpdatesCommand;
            this.Title = "Application Update";
        }
    }
}
