namespace Hercules.Shell
{
    public class AppearanceSettingsTab : PageTab
    {
        public Setting<VisualTheme> Theme { get; }

        public AppearanceSettingsTab(Setting<VisualTheme> theme)
        {
            this.Theme = theme;
            this.Title = "Appearance";
        }
    }
}
