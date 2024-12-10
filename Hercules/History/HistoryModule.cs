using Hercules.DB;
using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Scripting;
using Hercules.Shell;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Hercules.History
{
    public class HistoryModule : CoreModule
    {
        public HistoryModule(Core core)
            : base(core)
        {
            this.LoadRevisionCommand = Commands.Execute<DocumentCommit>(LoadRevision).If(rev => rev != null && rev.IsLoaded);
            this.ViewRevisionCommand = Commands.Execute<DocumentCommit>(ViewRevision).If(rev => rev != null && rev.IsLoaded);

            void OpenDatabaseHistory() => Workspace.WindowService.OpenPage(new DatabaseHistoryPage(GetDatabaseHistory(), Core.Workspace.SpreadsheetSettings.OpenSpreadsheetAfterExport));
            void OpenTrashBin() => Workspace.WindowService.OpenPage(new TrashBinPage(GetDatabaseHistory(), (name, draft) => DocumentsModule.CreateDocument(name, draft)));

            var databaseHistoryOption = new UiCommandOption("Database History", Fugue.Icons.ClockHistory, Commands.Execute(OpenDatabaseHistory).If(HasDatabase));
            Workspace.OptionManager.AddMenuOption(databaseHistoryOption, "Data#0", showInToolbar: true);

            var trashBinOption = new UiCommandOption("Trash Bin", Fugue.Icons.Bin, Commands.Execute(OpenTrashBin).If(HasDatabase));
            Workspace.OptionManager.AddMenuOption(trashBinOption, "Data#0", showInToolbar: false);

            Workspace.BindCommand(RoutedCommands.LoadRevision, LoadRevisionCommand);
            Workspace.BindCommand(RoutedCommands.ViewRevision, ViewRevisionCommand);

            documentPageSubscription = core.Workspace.WindowService.WhenAddingPage.OfType<DocumentEditorPage>().Subscribe(page => page.Tabs.Add(new DocumentHistoryTab(GetDatabaseHistory(), page.Document)));
        }

        public DocumentsModule DocumentsModule => Core.GetModule<DocumentsModule>();

        public DatabaseHistory GetDatabaseHistory() => new DatabaseHistory(Core.Project.Database);

        private readonly IDisposable documentPageSubscription;

        public ICommand<DocumentCommit> LoadRevisionCommand { get; }
        public ICommand<DocumentCommit> ViewRevisionCommand { get; }

        void LoadRevision(DocumentCommit revision)
        {
            var editor = DocumentsModule.EditDocument(revision.DocumentId);
            var draft = DocumentDraft.CopyFrom(revision.Snapshot, Core.Project.Database.TempStorage, editor.Document.CurrentRevision);
            editor.SetupDraft(draft);
            editor.GoTo(DestinationTab.Form);
        }

        void ViewRevision(DocumentCommit revision)
        {
            var editor = DocumentsModule.EditDocument(revision.DocumentId);
            var tab = editor.Tabs.OfType<DocumentRevisionTab>().FirstOrDefault(rev => rev.Revision.RevisionNumber == revision.RevisionNumber);
            if (tab == null)
            {
                tab = new DocumentRevisionTab(editor, revision, Core.GetModule<ScriptingModule>());
                editor.Tabs.Add(tab);
            }
            editor.ActiveTab = tab;
        }

        bool HasDatabase()
        {
            return Core.Project != null;
        }
    }
}
