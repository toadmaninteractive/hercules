using Hercules.Connections;
using Hercules.DB;
using Hercules.Documents;
using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.Replication
{
    public class SynchronizeDatabasePage : Page
    {
        public ObservableCollection<DbConnection> Connections { get; }

        public ICommand LoadCommand { get; }
        public ICommand CheckAllCommand { get; }
        public ICommand UncheckAllCommand { get; }
        public ICommand AdvancedFilterCommand { get; }
        public ICommand ImportAndEditCommand { get; }
        public ICommand ImportAndSaveCommand { get; }

        public ForegroundJob Job { get; set; }

        DbConnection? targetConnection;

        public DbConnection? TargetConnection
        {
            get => targetConnection;
            set => SetField(ref targetConnection, value);
        }

        string localTimeHeader;

        public string LocalTimeHeader
        {
            get => localTimeHeader;
            set => SetField(ref localTimeHeader, value);
        }

        string remoteTimeHeader;

        public string RemoteTimeHeader
        {
            get => remoteTimeHeader;
            set => SetField(ref remoteTimeHeader, value);
        }

        string localUserHeader;

        public string LocalUserHeader
        {
            get => localUserHeader;
            set => SetField(ref localUserHeader, value);
        }

        string remoteUserHeader;

        public string RemoteUserHeader
        {
            get => remoteUserHeader;
            set => SetField(ref remoteUserHeader, value);
        }

        public Database? TargetDatabase { get; private set; }

        public ConnectionsModule ConnectionsModule { get; }
        public DocumentsModule DocumentsModule { get; }

        DatabaseComparer? comparer;

        public DatabaseComparer? Comparer
        {
            get => comparer;
            set => SetField(ref comparer, value);
        }

        string? filter;

        public string? Filter
        {
            get => filter;
            set => SetField(ref filter, value);
        }

        private bool showAdded = true;

        public bool ShowAdded
        {
            get => showAdded;
            set => SetField(ref showAdded, value);
        }

        private bool showDeleted = true;

        public bool ShowDeleted
        {
            get => showDeleted;
            set => SetField(ref showDeleted, value);
        }

        private bool showModified = true;

        public bool ShowModified
        {
            get => showModified;
            set => SetField(ref showModified, value);
        }

        int savedCount = 0;
        int totalCount = 0;

        public Project Project { get; }

        public SynchronizeDatabasePage(Project project, DocumentsModule documentsModule, ConnectionsModule connectionsModule)
        {
            ConnectionsModule = connectionsModule;
            DocumentsModule = documentsModule;
            Title = "Synchronize Database";
            ContentId = "{SynchronizeDatabase}";
            Project = project;
            Job = new ForegroundJob();
            var currentConnection = ConnectionsModule.Connections.ActiveConnection!;
            localTimeHeader = "Time [" + currentConnection.Title + "]";
            localUserHeader = "User [" + currentConnection.Title + "]";
            remoteTimeHeader = "Time";
            remoteUserHeader = "User";
            Connections = new ObservableCollection<DbConnection>(connectionsModule.Connections.Items);
            Connections.Remove(currentConnection);
            LoadCommand = Commands.Execute(Load).If(() => TargetConnection != null);
            CheckAllCommand = Commands.Execute(CheckAll).If(() => Comparer != null);
            UncheckAllCommand = Commands.Execute(UncheckAll).If(() => Comparer != null);
            AdvancedFilterCommand = Commands.Execute(ShowAdvancedFilter).If(() => Comparer != null);
            ImportAndEditCommand = Commands.Execute(ImportAndEdit).If(() => Comparer != null);
            ImportAndSaveCommand = Commands.ExecuteAsync(ImportAndSave).If(() => Comparer != null);
        }

        private void ShowAdvancedFilter()
        {
            if (!Notifications.Items.OfType<SynchronizeDatabaseAdvancedFilterNotification>().Any())
            {
                var notification = new SynchronizeDatabaseAdvancedFilterNotification(ApplyAdvancedFilter);
                Notifications.Show(notification);
            }
        }

        private void ApplyAdvancedFilter(string advancedFilter, bool isRegex, bool matched, bool check)
        {
            foreach (var entry in Comparer!.Documents)
            {
                if (IsVisible(entry))
                {
                    if (entry.ChangeType == DocumentCommitType.Modified)
                    {
                        var docEntry = (DatabaseComparerDiffEntry)entry;
                        DatabaseComparerDiffEntryDetails details = (DatabaseComparerDiffEntryDetails)docEntry.Details;
                        foreach (var chunk in details.Chunks)
                        {
                            var match = chunk.Chunk.Path.ToString().Contains(advancedFilter);
                            if (match == matched)
                            {
                                chunk.Selected = check;
                            }
                        }
                    }
                }
            }
        }

        private bool IsVisible(DatabaseComparerEntry entry)
        {
            if (ShowDeleted || ShowModified || ShowAdded)
            {
                if (entry.ChangeType == DocumentCommitType.Added && !ShowAdded)
                    return false;
                if (entry.ChangeType == DocumentCommitType.Deleted && !ShowDeleted)
                    return false;
                if (entry.ChangeType == DocumentCommitType.Modified && !ShowModified)
                    return false;
            }
            if (!string.IsNullOrEmpty(Filter) && !entry.DocumentId.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private void CheckAll()
        {
            foreach (var entry in Comparer!.Documents)
            {
                if (IsVisible(entry))
                    entry.Selected = true;
            }
        }

        private void UncheckAll()
        {
            foreach (var entry in Comparer!.Documents)
            {
                if (IsVisible(entry))
                    entry.Selected = false;
            }
        }

        private void Load()
        {
            TargetDatabase = ConnectionsModule.OpenDatabase(TargetConnection!, TempStorage.Create());
            RemoteUserHeader = "User [" + TargetConnection!.Title + "]";
            RemoteTimeHeader = "Time [" + TargetConnection!.Title + "]";
            if (TargetDatabase != null)
                Compare(Project.Database, TargetDatabase);
        }

        void Compare(Database db1, Database db2)
        {
            Comparer = new DatabaseComparer(db1, db2);
        }

        void ImportAndEdit()
        {
            var db = Project.Database;
            var entries = Comparer!.Documents.Where(entry => IsVisible(entry) && entry.Selected != false).ToList();
            var deleted = entries.Where(entry => entry.ChangeType == DocumentCommitType.Deleted).Select(entry => db.Documents[entry.DocumentId]).ToList();
            if (deleted.Count > 0)
            {
                if (!DocumentsModule.DeleteDocuments(deleted))
                    return;
            }
            foreach (var entry in entries)
            {
                switch (entry.ChangeType)
                {
                    case DocumentCommitType.Added:
                        DocumentsModule.CreateDocument(entry.DocumentId, entry.GetDraft(db.TempStorage));
                        break;
                    case DocumentCommitType.Modified:
                        DocumentsModule.EditDocument(entry.DocumentId).SetupDraft(entry.GetDraft(db.TempStorage));
                        break;
                }
            }
            foreach (var entry in entries)
                Comparer.Documents.Remove(entry);
        }

        private async Task SaveDocumentAsync(Database db, IDocument document, DocumentDraft draft, IProgress<string> progress)
        {
            var metadata = new MetadataDraft(Project.Connection.Username);
            await db.SaveDocumentAsync(document, draft, metadata);

            Interlocked.Increment(ref savedCount);
            progress.Report($"{savedCount}/{totalCount} documents saved");
        }

        async Task ImportAndSave()
        {
            var db = Project.Database;
            var entries = Comparer!.Documents.Where(entry => IsVisible(entry) && entry.Selected != false).ToList();
            var deleted = entries.Where(entry => entry.ChangeType == DocumentCommitType.Deleted).Select(entry => db.Documents[entry.DocumentId]).ToList();
            if (deleted.Count > 0)
            {
                if (!DocumentsModule.DeleteDocuments(deleted))
                    return;
            }
            savedCount = 0;
            totalCount = entries.Count(entry => entry.ChangeType == DocumentCommitType.Added || entry.ChangeType == DocumentCommitType.Modified);
            foreach (var entry in entries)
                Comparer.Documents.Remove(entry);
            await Job.Run("Save documents",
                (progress, cancellationToken) =>
                {
                    var tasks = new List<Task>(totalCount);
                    foreach (var entry in entries)
                    {
                        switch (entry.ChangeType)
                        {
                            case DocumentCommitType.Added:
                                {
                                    var draft = entry.GetDraft(db.TempStorage);
                                    var doc = db.CreateDocument(entry.DocumentId, draft);
                                    tasks.Add(SaveDocumentAsync(db, doc, draft, progress));
                                }
                                break;
                            case DocumentCommitType.Modified:
                                {
                                    var doc = db.Documents[entry.DocumentId];
                                    tasks.Add(SaveDocumentAsync(db, doc, entry.GetDraft(db.TempStorage), progress));
                                }
                                break;
                        }
                    }
                    return Task.WhenAll(tasks);
                });
        }
    }
}
