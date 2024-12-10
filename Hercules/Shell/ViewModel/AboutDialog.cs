namespace Hercules.Shell
{
    public class AboutDialog : Dialog
    {
        public AboutDialog()
        {
            this.Title = "About Hercules";
            var version = Core.GetVersion();
            this.Version = $"Version: {version}";
        }

        public string Version { get; }
    }
}
