using System.Linq;

namespace Hercules.Shell
{
    public class SettingsPage : TabbedPage
    {
        public SettingsPage(WorkspaceSettings workspaceSettings, SpreadsheetSettings spreadsheetSettings, Setting<VisualTheme> theme)
        {
            this.Title = "Settings";
            this.ContentId = "{Settings}";
            Tabs.Add(new GeneralSettingsTab(workspaceSettings));
            Tabs.Add(new AppearanceSettingsTab(theme));
            Tabs.Add(new SpreadsheetSettingsTab(spreadsheetSettings));
            ActiveTab = Tabs.First();
        }

        public void OpenTab(string group)
        {
            var tab = Tabs.FirstOrDefault(t => t.Title == group);
            if (tab != null)
                ActiveTab = tab;
        }
    }
}
