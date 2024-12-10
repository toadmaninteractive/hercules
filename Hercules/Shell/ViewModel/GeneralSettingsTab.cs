namespace Hercules.Shell
{
    public class GeneralSettingsTab : PageTab
    {
        public WorkspaceSettings Settings { get; }

        public GeneralSettingsTab(WorkspaceSettings settings)
        {
            this.Settings = settings;
            this.Title = "General";
        }
    }
}
