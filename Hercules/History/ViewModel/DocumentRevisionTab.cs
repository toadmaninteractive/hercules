using Hercules.DB;
using Hercules.Documents.Editor;
using Hercules.Scripting;
using Hercules.Shell;
using ICSharpCode.AvalonEdit.Document;
using Json;
using JsonDiff;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Input;

namespace Hercules.History
{
    public class DocumentRevisionTab : PageTab
    {
        public const int ModeSource = 0;
        public const int ModeDiff = 1;
        public const int ModeChanges = 2;

        public static readonly string[] IgnoredKeys = { "_rev", "hercules_metadata", "_attachments" };

        DocumentCommit revision = default!;

        private readonly ScriptingModule scriptingModule;

        public DocumentCommit Revision
        {
            get => revision;
            set => SetField(ref revision, value);
        }

        public TextDocument JsonEditor { get; }

        DiffLines? diffLines;

        public DiffLines? DiffLines
        {
            get => diffLines;
            private set => SetField(ref diffLines, value);
        }

        string syntax = default!;

        public string Syntax
        {
            get => syntax;
            set => SetField(ref syntax, value);
        }

        public DocumentEditorPage Editor { get; }

        public DocumentHistoryTab History { get; }

        public ICommand SourceCommand { get; }
        public ICommand DiffCommand { get; }
        public ICommand DiffChangesCommand { get; }

        public ICommand NextDifferenceCommand { get; }
        public ICommand PreviousDifferenceCommand { get; }

        public ICommand NextRevisionCommand { get; }
        public ICommand PreviousRevisionCommand { get; }

        public ICommand RevertCommand { get; }

        public ICommand ApplyScriptCommand { get; }
        public ICommand RevertScriptCommand { get; }

        int mode = ModeChanges;

        public int Mode
        {
            get => mode;
            set => SetField(ref mode, value);
        }

        int line = 0;

        public int Line
        {
            get => line;
            set => SetField(ref line, value);
        }

        bool hasDifferences = false;

        public DocumentRevisionTab(DocumentEditorPage editor, DocumentCommit revision, ScriptingModule scriptingModule)
        {
            this.Editor = editor;
            this.History = editor.Tabs.OfType<DocumentHistoryTab>().First();
            this.JsonEditor = new TextDocument();
            this.scriptingModule = scriptingModule;

            SourceCommand = Commands.Execute(ShowSource);
            DiffCommand = Commands.Execute(ShowDiff).If(HasPreviousData);
            DiffChangesCommand = Commands.Execute(ShowDiffChanges).If(HasPreviousData);
            NextDifferenceCommand = Commands.Execute(NextDifference).If(HasDifferences);
            PreviousDifferenceCommand = Commands.Execute(PreviousDifference).If(HasDifferences);
            NextRevisionCommand = Commands.Execute(NextRevision).If(HasNextRevision);
            PreviousRevisionCommand = Commands.Execute(PreviousRevision).If(HasPreviousRevision);
            RevertCommand = Commands.Execute(Revert).If(HasPreviousData);
            ApplyScriptCommand = Commands.Execute(ApplyScript).If(HasPreviousData);
            RevertScriptCommand = Commands.Execute(RevertScript).If(HasPreviousData);

            SetRevision(revision);
        }

        public override void OnActivate()
        {
            History.Reload();
        }

        void ShowSource()
        {
            Mode = ModeSource;
            JsonEditor.Text = Revision.Snapshot.Json.ToString(JsonFormat.Multiline);
            DiffLines = null;
            Syntax = "SyntaxHighlight\\Json.xshd";
        }

        void ShowDiff()
        {
            Mode = ModeDiff;
            var diff = new JsonDiffEngine().Process(Revision.PreviousSnapshot.Json, Revision.Snapshot.Json);
            var formatter = new JsonDiffFormatter();
            formatter.Process(diff);
            this.JsonEditor.Text = formatter.Builder.ToString();
            DiffLines = new DiffLines(formatter.Lines);
            hasDifferences = formatter.Lines.Any(l => l != DiffLineStyle.Normal);
            Syntax = "SyntaxHighlight\\Json.xshd";
        }

        void ShowDiffChanges()
        {
            Mode = ModeChanges;
            var diff = new JsonDiffEngine().Process(Revision.PreviousSnapshot.Json, Revision.Snapshot.Json, IgnoredKeys);
            var formatter = new JsonDiffChangesFormatter();
            formatter.Process(diff);
            this.JsonEditor.Text = formatter.Builder.ToString();
            DiffLines = new DiffLines(formatter.Lines);
            hasDifferences = formatter.Lines.Any(l => l != DiffLineStyle.Normal);
            Syntax = "SyntaxHighlight\\Json.xshd";
        }

        void NextDifference()
        {
            Line = DiffLines!.NextDifference(Line);
        }

        void PreviousDifference()
        {
            Line = DiffLines!.PreviousDifference(Line);
        }

        bool HasDifferences()
        {
            return Mode == ModeDiff && hasDifferences;
        }

        void NextRevision()
        {
            var nextRevision = History.RevisionsByNumber.GetValueOrDefault(Revision.RevisionNumber + 1);
            SetRevision(nextRevision);
        }

        void PreviousRevision()
        {
            var prevRevision = History.RevisionsByNumber.GetValueOrDefault(Revision.RevisionNumber - 1);
            SetRevision(prevRevision);
        }

        [MemberNotNull(nameof(Revision))]
        void SetRevision(DocumentCommit newRevision)
        {
            this.Revision = newRevision;
            this.Title = "#" + newRevision.RevisionNumber;
            if (newRevision.PreviousSnapshot == null || Mode == ModeSource)
                ShowSource();
            else if (Mode == ModeDiff)
                ShowDiff();
            else
                ShowDiffChanges();
        }

        bool HasPreviousRevision()
        {
            var prevRevision = History.RevisionsByNumber.GetValueOrDefault(Revision.RevisionNumber - 1);
            return prevRevision?.Snapshot != null;
        }

        bool HasNextRevision()
        {
            var nextRevision = History.RevisionsByNumber.GetValueOrDefault(Revision.RevisionNumber + 1);
            return nextRevision?.Snapshot != null;
        }

        bool HasPreviousData()
        {
            return Revision.PreviousSnapshot != null;
        }

        void Revert()
        {
            var diff = new JsonDiffEngine().Process(Revision.PreviousSnapshot!.Json, Revision.Snapshot!.Json, IgnoredKeys);
            var patch = JsonPatch.FromDiff(diff, true);
            Editor.ApplyPatch(patch);
            Editor.GoTo(DestinationTab.Form);
        }

        void ApplyScript()
        {
            MakeScript(false);
        }

        void RevertScript()
        {
            MakeScript(true);
        }

        void MakeScript(bool left)
        {
            var diff = new JsonDiffEngine().Process(Revision.PreviousSnapshot!.Json, Revision.Snapshot!.Json, IgnoredKeys);
            var patch = JsonPatch.FromDiff(diff, left);
            var script = patch.ToJavaScript(JsonPath.Parse("doc"));
            scriptingModule.OpenScript(script);
        }
    }
}
