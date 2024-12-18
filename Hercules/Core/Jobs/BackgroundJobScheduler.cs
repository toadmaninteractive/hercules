using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Hercules
{
    public class BackgroundJobScheduler
    {
        const int MaxConcurrentCount = 50;

        public ObservableCollection<BackgroundJob> Jobs { get; } = new ObservableCollection<BackgroundJob>();

        readonly SemaphoreSlim semaphore;
        readonly Dispatcher dispatcher;

        public BackgroundJobScheduler(Dispatcher dispatcher)
        {
            this.semaphore = new SemaphoreSlim(MaxConcurrentCount);
            this.dispatcher = dispatcher;
        }

        public async Task<T> Schedule<T>(Func<Task<T>> task, string title, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(task);
            var job = new BackgroundJob(title);
            try
            {
                JobAdded(job);
                cancellationToken.Register(job.Cancel);
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                JobStarted(job);
                var result = await task().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
            finally
            {
                semaphore.Release();
                JobDone(job);
            }
        }

        public async Task Schedule(Func<Task> task, string title, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(task);
            var job = new BackgroundJob(title);
            try
            {
                JobAdded(job);
                cancellationToken.Register(job.Cancel);
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                JobStarted(job);
                await task().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                semaphore.Release();
                JobDone(job);
            }
        }

        public IObservable<T> Schedule<T>(IObservable<T> observable, string title)
        {
            var job = new BackgroundJob(title, JobStatus.Progress);
            return Observable.Create<T>(observer =>
            {
                JobAdded(job);
                return new CompositeDisposable(
                    observable.Do(_ => { }, _ => JobDone(job), () => JobDone(job)).Subscribe(observer),
                    Disposable.Create(() => JobDone(job)));
            });
        }

        void JobAdded(BackgroundJob job)
        {
            dispatcher.BeginInvoke(() => Jobs.Add(job));
        }

        void JobStarted(BackgroundJob job)
        {
            dispatcher.BeginInvoke(() => job.Status = JobStatus.Progress);
        }

        void JobDone(BackgroundJob job)
        {
            dispatcher.BeginInvoke(() => Jobs.Remove(job));
        }

        public void ScheduleForegroundIdleJob(Action action)
        {
            dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
        }

        public void ScheduleForegroundJob(Action action)
        {
            dispatcher.Invoke(action, DispatcherPriority.Normal);
        }

        public T ScheduleForegroundJob<T>(Func<T> action)
        {
            return dispatcher.Invoke(action, DispatcherPriority.Normal);
        }
    }
}
