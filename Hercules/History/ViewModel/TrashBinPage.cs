using Hercules.DB;
using Hercules.Documents;
using Hercules.Shell;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.History
{
    public class TrashBinItem : NotifyPropertyChanged
    {
        public string DocumentId { get; private set; }

        public TrashBinItem(string documentId)
        {
            this.DocumentId = documentId;
        }
    }

    public class TrashBinPage : Page
    {
        public ObservableCollection<TrashBinItem> Documents { get; }
        public ICommand<TrashBinItem> RestoreCommand { get; }
        public DatabaseHistory History { get; }
        public bool IsLoading
        {
            get => field;
            set => SetField(ref field, value);
        }

        private readonly Action<string, DocumentDraft> createDocumentAction;

        public TrashBinPage(DatabaseHistory history, Action<string, DocumentDraft> createDocumentAction)
        {
            this.Title = "Trash Bin";
            this.ContentId = "{Trash Bin}";
            History = history;
            this.createDocumentAction = createDocumentAction;
            Refresh(cancellationTokenSource.Token).Track();
            Documents = new ObservableCollection<TrashBinItem>();
            RestoreCommand = Commands.ExecuteAsync<TrashBinItem>(Restore).IfNotNull();
        }

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        protected override void OnClose()
        {
            cancellationTokenSource.Cancel();
        }

        async Task Refresh(CancellationToken cancellationToken)
        {
            Status = "Loading";
            IsLoading = true;
            try
            {
                var ids = await History.GetDeletedDocumentsAsync(cancellationToken).ConfigureAwait(true);
                Documents.AddRange(ids.Reverse().Select(id => new TrashBinItem(id)));
            }
            finally
            {
                Status = "Loaded";
                IsLoading = false;
            }
        }

        private async Task Restore(TrashBinItem item)
        {
            Logger.Log("Restore " + item.DocumentId);
            Operation = "Restoring";
            try
            {
                var draft = await History.GetDeletedDocumentAsync(item.DocumentId, cancellationTokenSource.Token).ConfigureAwait(true);
                createDocumentAction(item.DocumentId, draft);
            }
            catch (WebException exception) when (exception.Response is HttpWebResponse { StatusCode: HttpStatusCode.NotFound })
            {
                Notifications.AddMessage($"Failed to restore <{item.DocumentId}>:{Environment.NewLine}Document not found", MessageNotificationType.Error);
            }
            catch (WebException exception)
            {
                Notifications.AddMessage($"Failed to restore <{item.DocumentId}>", MessageNotificationType.Error);
                Logger.LogException($"Failed to restore <{item.DocumentId}>", exception);
            }
            finally
            {
                Operation = "Loaded";
            }
        }
    }
}
