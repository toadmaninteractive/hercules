using System.Threading.Tasks;

namespace Hercules.Documents
{
    public enum AttachmentState
    {
        NotLoaded,
        Loading,
        Loaded,
    }

    public class AttachmentFile : NotifyPropertyChanged, IFile
    {
        public string? FileName { get; private set; }
        public AttachmentState State { get; private set; }

        public bool IsLoaded => State == AttachmentState.Loaded;
        public bool IsLoading => State == AttachmentState.Loading;

        private readonly Task<string>? loader;

        public AttachmentFile(string fileName)
        {
            State = AttachmentState.Loaded;
            FileName = fileName;
        }

        public AttachmentFile(Task<string> loader)
        {
            this.State = AttachmentState.NotLoaded;
            this.loader = loader;
        }

        public async Task LoadAsync()
        {
            if (State == AttachmentState.Loaded)
                return;

            State = AttachmentState.Loading;
            var fileName = await loader!.ConfigureAwait(true);
            Loaded(fileName);
        }

        void Loaded(string fileName)
        {
            if (State == AttachmentState.Loaded)
                return;
            this.State = AttachmentState.Loaded;
            this.FileName = fileName;
            RaisePropertyChanged(nameof(FileName));
            RaisePropertyChanged(nameof(IsLoaded));
        }
    }
}
