using CouchDB;
using Hercules.DB;
using Hercules.Shell;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.Connections
{
    public class OpenDatabaseDialog : Dialog
    {
        public enum OpenDatabaseResult
        {
            Aborted,
            LoadCacheError,
            SynchronizeError,
            Success,
        }

        private enum Stage
        {
            None,
            LoadCache,
            Synchronize,
        }

        public OpenDatabaseDialog(BackgroundJobScheduler scheduler, DbConnection connection, TempStorage tempStorage)
        {
            Logger.Log("Loading " + connection.Title);
            var couchDatabase = new CdbServer(connection.Url, connection.Username, connection.Password).GetDatabase(connection.Database);
            var cdbBackend = new CouchDatabaseBackend(couchDatabase, tempStorage);
            IDatabaseBackend backend = new JobProxyDatabaseBackend(cdbBackend, scheduler);
            IDatabaseCache cache = new DatabaseCache(Path.Combine(connection.Path, "Cache"));
            this.Database = new Database(backend, cache, tempStorage);
            this.Title = "Loading " + connection.Title;
            cancellationTokenSource = new CancellationTokenSource();
            this.AbortCommand = Commands.Execute(Abort);
            OpenDatabase(cdbBackend, cancellationTokenSource.Token).Track();
        }

        readonly CancellationTokenSource cancellationTokenSource;

        public Database Database { get; }

        public OpenDatabaseResult? Result { get; private set; }

        string? statusMessage;

        public string? StatusMessage
        {
            get => statusMessage;
            set => SetField(ref statusMessage, value);
        }

        Stage currentStage = Stage.None;

        Stage CurrentStage
        {
            get => currentStage;
            set
            {
                if (currentStage != value)
                {
                    currentStage = value;
                    StatusMessage = StatusMessageByStage(value);
                }
            }
        }

        static string StatusMessageByStage(Stage stage)
        {
            return stage switch
            {
                Stage.LoadCache => "Loading local cache",
                Stage.Synchronize => "Synchronizing with remote database",
                _ => string.Empty
            };
        }

        void SetResult(OpenDatabaseResult result)
        {
            if (!this.Result.HasValue)
            {
                this.Result = result;
                SetDialogResult(result == OpenDatabaseResult.Success);
            }
        }

        void Abort()
        {
            Database.Close();
            Logger.Log("Loading database was cancelled");
            SetResult(OpenDatabaseResult.Aborted);
            cancellationTokenSource.Cancel();
        }

        public ICommand AbortCommand { get; }

        async Task OpenDatabase(IDatabaseBackend backend, CancellationToken cancellationToken)
        {
            try
            {
                var startAt = DateTime.Now;
                CurrentStage = Stage.LoadCache;
                // Request backend changes, but don't await yet
                var changesTask = backend.GetChangesAsync(Database.Cache.ReadLastSequence(), true, cancellationToken);
                await Task.Run(() => Database.LoadCache(cancellationToken), cancellationToken).ConfigureAwait(true);
                var cacheLoadedAt = DateTime.Now;
                Logger.LogDebug($"Local cache loaded in {(cacheLoadedAt - startAt).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)}s");
                CurrentStage = Stage.Synchronize;
                var changes = await changesTask.ConfigureAwait(true);
                await Task.Run(() => Database.Synchronize(changes), cancellationToken).ConfigureAwait(true);
                var synchronizedAt = DateTime.Now;
                Logger.LogDebug($"Remote database synchronized in {(synchronizedAt - cacheLoadedAt).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)}s");
                SetResult(OpenDatabaseResult.Success);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (CurrentStage == Stage.LoadCache)
                {
                    Logger.LogException("Error while loading database cache", ex.GetInnerException());
                    SetResult(OpenDatabaseResult.LoadCacheError);
                }
                else
                {
                    Logger.LogException("Error while synchronizing backend database", ex.GetInnerException());
                    SetResult(OpenDatabaseResult.SynchronizeError);
                }
            }
        }
    }
}
