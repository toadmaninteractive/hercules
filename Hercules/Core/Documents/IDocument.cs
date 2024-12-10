using Hercules.Shortcuts;
using Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Documents
{
    public interface IDocumentEditor
    {
        DocumentDraft GetDocumentDraft();
        void UserDeleting();
        void UserDeleteFailed();
        void UserDeleted();
        void RemoteUpdated(DocumentRevision revision);
        void RemoteDeleted(DocumentDraft draft);
    }

    public enum DocumentChange
    {
        UserDeleting,
        UserDeleted,
        UserDeleteFailed,
        RemoteDeleted,
        RemoteUpdated,
    }

    public delegate void DocumentChanged(DocumentChange change);

    public class DocumentPreview : NotifyPropertyChanged
    {
        private string? caption;
        public string? Caption
        {
            get => caption;
            set => SetField(ref caption, value);
        }
    }

    public interface IDocument : IComparable<IDocument>, IShortcutProvider
    {
        string DocumentId { get; }
        DocumentRevision? CurrentRevision { get; }
        DocumentDraft? Draft { get; }
        bool IsDesign { get; }

        [MemberNotNullWhen(returnValue: true, member: nameof(CurrentRevision))]
        [MemberNotNullWhen(returnValue: false, member: nameof(Draft))]
        bool IsExisting { get; }

        ImmutableJsonObject Json { get; }
        IReadOnlyList<Attachment> Attachments { get; }
        IDocumentEditor? Editor { get; }
        DocumentPreview Preview { get; }
        public event DocumentChanged? OnChanged;
    }
}
