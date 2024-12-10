namespace Hercules.Shell
{
    public class SpreadsheetSettingsTab : PageTab
    {
        public SpreadsheetSettings Settings { get; }

        public SpreadsheetSettingsTab(SpreadsheetSettings settings)
        {
            this.Settings = settings;
            this.Title = "Spreadsheets";
        }
    }
}
