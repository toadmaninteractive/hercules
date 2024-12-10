using Hercules.DB;
using Hercules.Documents;
using ICSharpCode.AvalonEdit.Document;
using Json;
using JsonDiff;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Replication
{
    public abstract class SelectableDiffPart : NotifyPropertyChanged
    {
        public DatabaseComparerDiffEntry Entry { get; }

        bool selected;

        public bool Selected
        {
            get => Entry.Selected ?? selected;
            set
            {
                if ((Entry.Selected ?? selected) != value)
                {
                    selected = value;
                    Entry.UpdateSelected(this, value);
                }
            }
        }

        public void SetSelectedSilently(bool value, bool raisePropertyChanged)
        {
            selected = value;
            if (raisePropertyChanged)
                RaisePropertyChanged(nameof(Selected));
        }

        protected SelectableDiffPart(DatabaseComparerDiffEntry entry)
        {
            Entry = entry;
        }
    }

    public sealed class AttachmentComparison : SelectableDiffPart
    {
        public DocumentCommitType ChangeType { get; }
        public AttachmentRevision? Left { get; }
        public AttachmentRevision? Right { get; }

        public AttachmentComparison(DatabaseComparerDiffEntry entry, AttachmentRevision? left, AttachmentRevision? right)
            : base(entry)
        {
            Left = left;
            Right = right;
            ChangeType = left == null ? DocumentCommitType.Added : right == null ? DocumentCommitType.Deleted : DocumentCommitType.Modified;
        }
    }

    public sealed class FormattedJsonDiffChunk : SelectableDiffPart, IDiffChunk
    {
        public JsonDiffChunk Chunk { get; }
        public int Line { get; }

        public FormattedJsonDiffChunk(DatabaseComparerDiffEntry entry, JsonDiffChunk chunk, int line)
            : base(entry)
        {
            this.Chunk = chunk;
            this.Line = line;
        }
    }

    public sealed class DatabaseComparerDocumentEntryDetails
    {
        public TextDocument Editor { get; }
        public bool IsHuge => Editor.LineCount > 100;

        public DatabaseComparerDocumentEntryDetails(DatabaseComparerDocumentEntry entry)
        {
            var text = entry.Content.Json.ToString(JsonFormat.Multiline);
            Editor = new TextDocument(text);
        }
    }

    public sealed class DatabaseComparerDiffEntryDetails
    {
        static readonly string[] IgnoredKeys = { "_rev", "hercules_metadata", "_attachments" };

        public IDiffLines DiffLines { get; }
        public IDiffChunks DiffChunks { get; }
        public IReadOnlyList<FormattedJsonDiffChunk> Chunks { get; }
        public string Text { get; }
        public TextDocument Editor { get; }
        public bool IsHuge { get; }

        public DatabaseComparerDiffEntryDetails(DatabaseComparerDiffEntry entry)
        {
            var diff = new JsonDiffEngine().Process(entry.Left!.Json, entry.Right!.Json, IgnoredKeys);
            var formatter = new JsonDiffChangesFormatter();
            var chunks = new List<FormattedJsonDiffChunk>();
            formatter.Process(diff, (chunk, line) => chunks.Add(new FormattedJsonDiffChunk(entry, chunk, line)));
            Text = formatter.HasChanges ? formatter.Builder.ToString() : "No changes";
            Editor = new TextDocument(Text);
            DiffLines = new DiffLines(formatter.Lines);
            Chunks = chunks;
            var whenChanged = entry.OnPropertyChanged(nameof(DatabaseComparerDiffEntry.Selected));
            DiffChunks = new DiffChunks(Chunks, whenChanged);
            IsHuge = Editor.LineCount > 100;
        }
    }

    public abstract class DatabaseComparerEntry : NotifyPropertyChanged
    {
        public string DocumentId { get; }
        public DocumentRevision? Left { get; }
        public DocumentRevision? Right { get; }
        public DocumentCommitType ChangeType { get; }

        private bool? selected;

        public bool? Selected
        {
            get => selected;
            set
            {
                if (SetField(ref selected, value ?? false))
                    OnSelectedChanged();
            }
        }

        protected void ResetSelectedSilently() => selected = null;

        public abstract DocumentDraft GetDraft(TempStorage tempStorage);

        protected abstract object CreateDetails();

        protected virtual void OnSelectedChanged()
        {
        }

        object? cachedDetails;

        public object Details => cachedDetails ??= CreateDetails();

        protected DatabaseComparerEntry(string documentId, DocumentCommitType changeType, DocumentRevision? left, DocumentRevision? right)
        {
            DocumentId = documentId;
            ChangeType = changeType;
            Left = left;
            Right = right;
            selected = false;
        }
    }

    public class DatabaseComparerDocumentEntry : DatabaseComparerEntry
    {
        public DocumentRevision Content { get; }

        public DatabaseComparerDocumentEntry(string documentId, DocumentRevision? left, DocumentRevision? right)
            : base(documentId, left == null ? DocumentCommitType.Added : DocumentCommitType.Deleted, left, right)
        {
            Content = (left ?? right)!;
        }

        protected override object CreateDetails()
        {
            return new DatabaseComparerDocumentEntryDetails(this);
        }

        public override DocumentDraft GetDraft(TempStorage tempStorage)
        {
            return new DocumentDraft(Content.Json, Content.Attachments.Select(a => AttachmentDraft.CopyFrom(a, tempStorage)).ToList());
        }
    }

    public class DatabaseComparerDiffEntry : DatabaseComparerEntry
    {
        ImmutableJsonObject GetJson()
        {
            if (Selected == null)
            {
                var builder = new JsonBuilder(Left!.Json);
                foreach (var chunk in changes.Chunks)
                {
                    if (chunk.Selected)
                    {
                        if (chunk.Chunk.Right == null)
                            builder.Delete(chunk.Chunk.Path);
                        else
                            builder.ForceUpdate(chunk.Chunk.Path, chunk.Chunk.Right);
                    }
                }
                return builder.ToImmutable().AsObject;
            }
            else
                return Right!.Json;
        }

        public override DocumentDraft GetDraft(TempStorage tempStorage)
        {
            var newAttachments = Left.Attachments.Cast<Attachment>().ToList();
            foreach (var attachment in Attachments)
            {
                if (attachment.Selected)
                {
                    switch (attachment.ChangeType)
                    {
                        case DocumentCommitType.Added:
                            newAttachments.Add(AttachmentDraft.CopyFrom(attachment.Right!, tempStorage));
                            break;
                        case DocumentCommitType.Deleted:
                            newAttachments.Remove(attachment.Left!);
                            break;
                        case DocumentCommitType.Modified:
                            newAttachments[newAttachments.IndexOf(attachment.Left!)] = AttachmentDraft.CopyFrom(attachment.Right!, tempStorage);
                            break;
                    }
                }
            }
            return new DocumentDraft(GetJson(), newAttachments);
        }

        public void UpdateSelected(SelectableDiffPart selectedChunk, bool selectedValue)
        {
            cachedAttachments ??= InitAttachments();
            var selectables = changes.Chunks.Cast<SelectableDiffPart>().Concat(cachedAttachments.Cast<SelectableDiffPart>());
            foreach (var chunk in selectables)
            {
                if (chunk == selectedChunk)
                    continue;
                if (selectedValue != chunk.Selected)
                {
                    if (Selected.HasValue)
                    {
                        foreach (var c in selectables)
                        {
                            if (c != selectedChunk)
                                c.SetSelectedSilently(Selected.Value, false);
                        }
                        ResetSelectedSilently();
                    }
                    RaisePropertyChanged(nameof(Selected));
                    return;
                }
            }
            Selected = selectedValue;
        }

        DatabaseComparerDiffEntryDetails changes;

        protected override object CreateDetails()
        {
            changes = new DatabaseComparerDiffEntryDetails(this);
            return changes;
        }

        protected override void OnSelectedChanged()
        {
            if (Selected.HasValue)
                foreach (var attachment in Attachments)
                    attachment.SetSelectedSilently(Selected.Value, true);
        }

        IReadOnlyCollection<AttachmentComparison>? cachedAttachments;

        public IReadOnlyCollection<AttachmentComparison> Attachments => cachedAttachments ??= InitAttachments();

        IReadOnlyCollection<AttachmentComparison> InitAttachments()
        {
            if (Left!.Attachments.Count == 0 && Right!.Attachments.Count == 0)
                return Array.Empty<AttachmentComparison>();
            var attachments = new List<AttachmentComparison>();
            foreach (var (a1, a2) in Left.Attachments.ZipByKey(Right!.Attachments, a => a.Name))
            {
                if (a1 == null || a2 == null || a1.Length != a2.Length || a1.Digest != a2.Digest)
                    attachments.Add(new AttachmentComparison(this, a1, a2));
            }
            return attachments;
        }

        public DatabaseComparerDiffEntry(string documentId, DocumentRevision doc1, DocumentRevision doc2)
            : base(documentId, DocumentCommitType.Modified, doc1, doc2)
        {
        }
    }

    public class DatabaseComparer
    {
        public ObservableCollection<DatabaseComparerEntry> Documents { get; }

        public DatabaseComparer(Database db1, Database db2)
        {
            var documentRevisionEqualityComparer = new DocumentRevisionEqualityComparer();
            Documents = new ObservableCollection<DatabaseComparerEntry>();
            var docs1 = db1.Documents.Values.Where(doc => doc.IsExisting).Select(doc => doc.DocumentId);
            var docs2 = db2.Documents.Values.Where(doc => doc.IsExisting).Select(doc => doc.DocumentId);
            var allIds = docs1.Concat(docs2).Distinct().OrderBy(key => key);
            foreach (var id in allIds)
            {
                var doc1 = db1.Documents.GetValueOrDefault(id)?.CurrentRevision;
                var doc2 = db2.Documents.GetValueOrDefault(id)?.CurrentRevision;

                DatabaseComparerEntry? entry = null;
                if (doc1 == null || doc2 == null)
                    entry = new DatabaseComparerDocumentEntry(id, doc1, doc2);
                else if (!documentRevisionEqualityComparer.Equals(doc1, doc2))
                    entry = new DatabaseComparerDiffEntry(id, doc1, doc2);

                if (entry != null)
                    Documents.Add(entry);
            }
        }
    }
}
