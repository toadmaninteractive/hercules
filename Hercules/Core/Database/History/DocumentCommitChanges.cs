using Hercules.Documents;
using ICSharpCode.AvalonEdit.Document;
using Json;
using JsonDiff;

namespace Hercules.DB
{
    public sealed class DocumentCommitChanges
    {
        public static readonly string[] IgnoredKeys = { "_rev", "_attachments", "hercules_metadata" };

        public IDiffLines DiffLines { get; }
        public string Text { get; }
        public TextDocument Editor { get; }

        public DocumentCommitChanges(DocumentRevision revision, DocumentRevision? previousRevision = null)
        {
            if (previousRevision == null)
            {
                Text = revision.Json.ToString(JsonFormat.Multiline);
                DiffLines = JsonDiff.DiffLines.Default;
            }
            else
            {
                var diff = new JsonDiffEngine().Process(previousRevision.Json, revision.Json, IgnoredKeys);
                var formatter = new JsonDiffChangesFormatter();
                formatter.Process(diff);
                Text = formatter.HasChanges ? formatter.Builder.ToString() : "No changes";
                DiffLines = new DiffLines(formatter.Lines);
            }
            Editor = new TextDocument(Text);
        }
    }
}
