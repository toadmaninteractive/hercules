using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Shell;
using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace Hercules.Bookmarks
{
    public class BookmarksModule : CoreModule
    {
        public BookmarksModule(Core core) : base(core)
        {
            Bookmarks = new BookmarksBar(Workspace.ShortcutService, Workspace.DialogService, ViewBookmarksBar, core.GetModule<DocumentsModule>().EditDocumentCommand.Single);

            pageSubscription = core.Workspace.WindowService.WhenAddingPage.OfType<DocumentEditorPage>().Subscribe(p => AddRecentDocument(p.Document));

            Workspace.ShortcutService.RegisterHandler(new RecentDocumentsShortcutHandler(this));
            Workspace.ShortcutService.RegisterHandler(new BookmarkFolderShortcutHandler());

            core.Workspace.Bars.Add(Bookmarks);

            var viewBookmarksBarOption = new UiToggleOption("Bookmarks Bar", null, ViewBookmarksBar);
            Workspace.OptionManager.AddMenuOption(viewBookmarksBarOption, "View#0");

            core.SettingsService.AddSetting(ViewBookmarksBar);
        }

        private readonly IDisposable pageSubscription;
        private IDisposable? databaseChangeSubscription;

        public ObservableCollection<IDocument> RecentDocuments { get; } = new();
        public BookmarksBar Bookmarks { get; }

        public Setting<bool> ViewBookmarksBar { get; } = new Setting<bool>(nameof(ViewBookmarksBar), true);

        private void AddRecentDocument(IDocument document)
        {
            RecentDocuments.Remove(document);
            RecentDocuments.Insert(0, document);
            while (RecentDocuments.Count > 15)
                RecentDocuments.RemoveAt(15);
        }

        public override void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
            RecentDocuments.Clear();
            if (settingsReader.Read<List<string>>("RecentDocuments", out var recentDocuments))
            {
                RecentDocuments.AddRange(
                    from docId in recentDocuments
                    where project.Database.Documents.ContainsKey(docId)
                    select project.Database.Documents[docId]);
            }

            if (settingsReader.Read<ImmutableJsonArray>("Bookmarks", out var bookmarks))
                Bookmarks.Load(bookmarks);
            else
                Bookmarks.AddRecentDocumentsBookmark();

            databaseChangeSubscription = project.Database.Changes.Where(change => change.Kind == DatabaseChangeKind.Remove).Subscribe(change => RecentDocuments.Remove(change.Document));
        }

        public override void OnSaveProject(ISettingsWriter settingsWriter)
        {
            settingsWriter.Write("Bookmarks", Bookmarks.Save());
            settingsWriter.Write("RecentDocuments", RecentDocuments.Select(doc => doc.DocumentId).ToList());
        }

        public override void OnCloseProject()
        {
            databaseChangeSubscription?.Dispose();
            databaseChangeSubscription = null;
            RecentDocuments.Clear();
            Bookmarks.Clear();
        }

        public override void OnShutdown()
        {
            pageSubscription.Dispose();
        }
    }
}
