using Hercules.Documents;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.DB
{
    public enum DocumentCommitType
    {
        Added,
        Modified,
        Deleted,
    }

    public class DocumentCommit : NotifyPropertyChanged
    {
        public string DocumentId { get; }

        public string Revision { get; }

        public int RevisionNumber { get; }

        public DocumentCommitType ChangeType { get; }

        public RevisionStatus Status { get; }

        public bool IsLoaded => snapshot != null;

        public string? PreviousRevision => snapshot?.Metadata.PrevRev;

        public string? User => snapshot?.Metadata.User;

        public DateTime? Time => snapshot?.Metadata.Time;

        public ObservableCollection<AttachmentCommit> Attachments { get; } = new ObservableCollection<AttachmentCommit>();

        public bool HasAttachments => Snapshot?.Attachments.Count > 0;

        private DocumentRevision? snapshot;

        public DocumentRevision? Snapshot
        {
            get => snapshot;
            set
            {
                if (value != null)
                {
                    snapshot = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(HasAttachments));
                    RaisePropertyChanged(nameof(User));
                    RaisePropertyChanged(nameof(Time));
                    RaisePropertyChanged(nameof(IsLoaded));
                    RaisePropertyChanged(nameof(Changes));
                    UpdateAttachments();
                }
            }
        }

        DocumentRevision? previousSnapshot;

        public DocumentRevision? PreviousSnapshot
        {
            get => previousSnapshot;
            set
            {
                if (previousSnapshot == null && value != null)
                {
                    previousSnapshot = value;
                    RaisePropertyChanged(nameof(Changes));
                    UpdateAttachments();
                }
            }
        }

        DocumentCommitChanges? changes;

        public DocumentCommitChanges? Changes => changes ??= CreateChanges(this);

        public static DocumentCommitChanges? CreateChanges(DocumentCommit revision)
        {
            if (revision.Snapshot != null && revision.PreviousSnapshot != null)
                return new DocumentCommitChanges(revision.Snapshot, revision.PreviousSnapshot);
            else if (revision.Snapshot != null && revision.ChangeType == DocumentCommitType.Added)
                return new DocumentCommitChanges(revision.Snapshot);
            else
                return null;
        }

        public DocumentCommit(string documentId, string revision, RevisionStatus status, DocumentRevision? snapshot = null)
        {
            this.DocumentId = documentId;
            this.Revision = revision;
            this.RevisionNumber = CouchUtils.GetRevisionNumber(revision);
            this.Status = status;
            this.Snapshot = snapshot;
            this.ChangeType = Status == RevisionStatus.Deleted ? DocumentCommitType.Deleted
                : RevisionNumber == 1 ? DocumentCommitType.Added : DocumentCommitType.Modified;
        }

        void UpdateAttachments()
        {
            Attachments.Clear();
            if (snapshot != null && previousSnapshot == null)
            {
                Attachments.AddRange(snapshot.Attachments.Select(a => new AttachmentCommit(a.RevPos == RevisionNumber ? AttachmentCommitType.Added : AttachmentCommitType.None, a)));
            }
            if (snapshot != null && previousSnapshot != null)
            {
                Attachments.AddRange(previousSnapshot.Attachments.ZipByKey(snapshot.Attachments, a => a.Name, AttachmentCommit.CreateDiff));
            }
        }
    }
}
