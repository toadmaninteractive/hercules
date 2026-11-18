using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules
{
    public sealed class ForegroundJob : NotifyPropertyChanged
    {

        public bool IsBusy
        {
            get;
            private set => SetField(ref field, value);
        }

        string? title;

        public string? Title
        {
            get => title;
            private set => SetField(ref title, value);
        }

        public string? Status
        {
            get;
            private set => SetField(ref field, value);
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
