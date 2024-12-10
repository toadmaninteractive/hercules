namespace Hercules
{
    public enum JobStatus
    {
        Pending,
        Progress,
        Canceled,
    }

    public class BackgroundJob : NotifyPropertyChanged
    {
        public string Title { get; }

        JobStatus status;

        public JobStatus Status
        {
            get => status;
            set => SetField(ref status, value);
        }

        public void Cancel()
        {
            Status = JobStatus.Canceled;
        }

        public BackgroundJob(string title, JobStatus status = JobStatus.Pending)
        {
            this.Title = title;
            this.status = status;
        }
    }
}
