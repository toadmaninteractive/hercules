using Hercules.Documents;
using Hercules.Shortcuts;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.DB
{
    public sealed class DatabaseDocument : IDocument, IComparable<IDocument>, IShortcutProvider
    {
        public event DocumentChanged? OnChanged;

        public string DocumentId { get; }
        public DocumentPreview Preview { get; } = new();

        public bool IsDesign { get; }

        public DocumentRevision? CurrentRevision { get; private set; }

        public DocumentDraft? Draft { get; private set; }

        public ImmutableJsonObject Json => IsExisting ? CurrentRevision!.Json : Draft!.Json;

        public IReadOnlyList<Attachment> Attachments
        {
            get
            {
                if (IsExisting)
                    return CurrentRevision!.Attachments;
                else
                    return Draft!.Attachments;
            }
        }

        public bool IsDeleting { get; private set; }

        public bool IsExisting => CurrentRevision != null;

        public IDocumentEditor? Editor { get; set; }

        public bool IsEditing => Editor != null;

        public void RemoteUpdated(DocumentRevision currentRevision)
        {
            CurrentRevision = currentRevision;
            Draft = null;
            Changed(DocumentChange.RemoteUpdated);
        }

        public void Deleted(Func<AttachmentRevision, AttachmentDraft> attachmentCopy)
        {
            if (IsExisting)
            {
                Draft = new DocumentDraft(CurrentRevision!.Json, CurrentRevision.Attachments.Select(attachmentCopy).ToList());
                CurrentRevision = null;
                var change = IsDeleting ? DocumentChange.UserDeleted : DocumentChange.RemoteDeleted;
                IsDeleting = false;
                Changed(change);
            }
        }

        public void Deleting()
        {
            IsDeleting = true;
            Changed(DocumentChange.UserDeleting);
        }

        public void DeleteFailed()
        {
            if (IsDeleting)
            {
                IsDeleting = false;
                Changed(DocumentChange.UserDeleteFailed);
            }
        }

        void Changed(DocumentChange change)
        {
            OnChanged?.Invoke(change);
        }

        private DatabaseDocument(string docId)
        {
            this.IsDesign = docId.Contains("/", StringComparison.Ordinal);
            this.DocumentId = docId;
        }

        public DatabaseDocument(string docId, DocumentRevision currentRevision)
            : this(docId)
        {
            this.CurrentRevision = currentRevision;
        }

        public DatabaseDocument(string docId, DocumentDraft draft)
            : this(docId)
        {
            this.Draft = draft;
        }

        public int CompareTo(IDocument? other)
        {
            if (other is null)
                return 1;
            return string.CompareOrdinal(this.DocumentId, other.DocumentId);
        }

        public IShortcut Shortcut => new DocumentShortcut(DocumentId);
    }
}
