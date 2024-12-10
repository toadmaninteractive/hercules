using Hercules.DB;
using Hercules.Documents;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.History
{
    public class DocumentHistorySummary : NotifyPropertyChanged
    {
        public string DocumentId { get; }

        public DocumentCommitType ChangeType { get; }

        public ObservableCollection<AttachmentCommit> Attachments { get; } = new ObservableCollection<AttachmentCommit>();

        public bool HasAttachments => Current?.Attachments.Count > 0;

        public DocumentRevision Current { get; }
        public DocumentRevision? Previous { get; }

        DocumentCommitChanges? changes;

        public DocumentCommitChanges Changes => changes ??= new DocumentCommitChanges(Current, Previous);

        public DocumentHistorySummary(string documentId, DocumentCommitType changeType, DocumentRevision current, DocumentRevision? previous)
        {
            this.DocumentId = documentId;
            this.Current = current;
            this.Previous = previous;
            this.ChangeType = changeType;
            if (previous == null)
            {
                Attachments.AddRange(current.Attachments.Select(a => new AttachmentCommit(AttachmentCommitType.Added, a)));
            }
            if (current != null && previous != null)
            {
                Attachments.AddRange(previous.Attachments.ZipByKey(current.Attachments, a => a.Name, AttachmentCommit.CreateDiff));
            }
        }
    }
}
