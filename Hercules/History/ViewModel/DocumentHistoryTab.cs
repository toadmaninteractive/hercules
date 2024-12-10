using Hercules.DB;
using Hercules.Documents;
using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.History
{
    public class DocumentHistoryTab : PageTab
    {
        public IDocument Document { get; }
        public DatabaseHistory DatabaseHistory { get; }

        bool isLoaded;

        public bool IsLoaded
        {
            get => isLoaded;
            set => SetField(ref isLoaded, value);
        }

        bool isReloading;

        string? loadedForRevision;

        public ObservableCollection<DocumentCommit> Revisions { get; } = new();

        public Dictionary<int, DocumentCommit> RevisionsByNumber { get; } = new();

        readonly CancellationTokenSource cancellationTokenSource;

        public DocumentHistoryTab(DatabaseHistory databaseHistory, IDocument document)
        {
            DatabaseHistory = databaseHistory;
            Document = document;
            cancellationTokenSource = new CancellationTokenSource();
            Title = "History";
        }

        public void Reload()
        {
            if (isReloading)
                return;
            if (Document.CurrentRevision == null)
                return;
            if (IsLoaded && Document.CurrentRevision.Rev == loadedForRevision)
                return;
            ReloadAsync(cancellationTokenSource.Token).Track();
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public override void OnActivate()
        {
            Reload();
        }

        public override void OnClose()
        {
            Cancel();
        }

        async Task ReloadAsync(CancellationToken cancellationToken)
        {
            isReloading = true;
            try
            {
                loadedForRevision = Document.CurrentRevision.Rev;
                var revs = await DatabaseHistory.GetDocumentRevisionsAsync(Document.DocumentId, cancellationToken).ConfigureAwait(true);
                RevisionsByNumber.Clear();
                Revisions.Clear();
                Revisions.AddRange(revs);
                foreach (var rev in revs)
                {
                    RevisionsByNumber[rev.RevisionNumber] = rev;
                }
                IsLoaded = true;
                var progress = new Progress<DatabaseHistoryLoadedData>(data =>
                {
                    data.Apply();
                    CommandManager.InvalidateRequerySuggested();
                });
                await DatabaseHistory.LoadDocumentRevisionsAsync(revs, progress, cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Logger.LogException($"Failed to fetch revision list for {Document.DocumentId}", exception);
            }
            finally
            {
                isReloading = false;
            }
        }
    }
}
