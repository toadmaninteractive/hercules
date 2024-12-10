namespace Hercules.Shell
{
    public class JobsTool : Tool
    {
        public BackgroundJobScheduler Scheduler { get; }

        public JobsTool(BackgroundJobScheduler scheduler)
        {
            this.Scheduler = scheduler;
            this.Title = "Tasks";
            this.IsVisible = true;
            this.ContentId = "{Jobs}";
            this.Pane = "BottomToolsPane";
        }
    }
}
