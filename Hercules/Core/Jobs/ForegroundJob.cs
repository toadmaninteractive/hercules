using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules
{
    public sealed class ForegroundJob : NotifyPropertyChanged
    {
        bool isBusy;

        public bool IsBusy
        {
            get => isBusy;
            private set => SetField(ref isBusy, value);
        }

        string? title;

        public string? Title
        {
            get => title;
            private set => SetField(ref title, value);
        }

        string? status;

        public string? Status
        {
            get => status;
            private set => SetField(ref status, value);
        }

        ICommand abort;

        public ICommand Abort
        {
            get => abort;
            private set => SetField(ref abort, value);
        }

        public ForegroundJob()
        {
            abort = CannotCommand;
        }

        public async Task Run(string title, Func<IProgress<string>, CancellationToken, Task> task)
        {
            ArgumentNullException.ThrowIfNull(task);
            var progress = new Progress<string>(str => Status = str);
            var cancellationTokenSource = new CancellationTokenSource();
            this.title = title;
            IsBusy = true;
            Abort = Commands.Execute(cancellationTokenSource.Cancel);
            try
            {
                await task(progress, cancellationTokenSource.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Abort = CannotCommand;
                IsBusy = false;
                cancellationTokenSource.Dispose();
            }
        }

        static readonly ICommand CannotCommand = Commands.Execute(() => { }).If(() => false);
    }
}
