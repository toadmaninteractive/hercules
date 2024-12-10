using Hercules.Documents;
using Hercules.Shell;
using Hercules.Shortcuts;
using Json;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;

namespace Hercules.Bookmarks
{
    public class BookmarksBar
    {
        public ShortcutService ShortcutService { get; }
        public BookmarkFolder Root { get; } = new BookmarkFolder(string.Empty);
        public IReadOnlyObservableValue<bool> ViewBookmarksBar { get; }

        public ICommand<Bookmark> RemoveBookmarkCommand { get; }
        public ICommand RemoveAllBookmarksCommand { get; }
        public ICommand AddRecentDocumentsBookmarkCommand { get; }
        public ICommand<IDocument> EditDocumentCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RenameFolderCommand { get; }
        public ICommand OpenBookmarkCommand { get; }

        private readonly CompositeDisposable invalidateDisposable;

        private readonly IDialogService dialogService;

        public BookmarksBar(ShortcutService shortcutService, IDialogService dialogService, IReadOnlyObservableValue<bool> viewBookmarksBar, ICommand<IDocument> editDocumentCommand)
        {
            this.dialogService = dialogService;
            RemoveBookmarkCommand = Commands.Execute<Bookmark>(RemoveBookmark);
            RemoveAllBookmarksCommand = Commands.Execute(RemoveAllBookmarks);
            AddRecentDocumentsBookmarkCommand = Commands.Execute(AddRecentDocumentsBookmark);
            EditDocumentCommand = editDocumentCommand;
            AddFolderCommand = Commands.Execute(AddFolder);
            RenameFolderCommand = Commands.Execute<BookmarkFolder>(RenameFolder);
            OpenBookmarkCommand = Commands.Execute<Bookmark>(OpenBookmark);
            ShortcutService = shortcutService;
            ViewBookmarksBar = viewBookmarksBar;
            invalidateDisposable = new CompositeDisposable();
            foreach (var handler in ShortcutService.AllHandlers)
            {
                var onChange = handler.OnChange;
                if (onChange != null)
                    invalidateDisposable.Add(onChange.Subscribe(Invalidate));
            }
        }

        private void OpenBookmark(Bookmark bookmark)
        {
            if (!ShortcutService.Open(bookmark.Shortcut))
            {
                MessageBoxDialog mb = new MessageBoxDialog(
                        "Bookmarked content does not exist. Remove the bookmark?",
                        "Invalid bookmark",
                        DialogButtons.Yes | DialogButtons.No, DialogButtons.No, DialogIcon.Error);

                dialogService.ShowDialog(mb);
                if (mb.Result == DialogButtons.Yes)
                {
                    RemoveBookmark(bookmark);
                }
            }
        }

        private void AddFolder()
        {
            dialogService.ShowDialog(new RenameBookmarkDialog("Bookmarks", "Create Bookmark Folder", CreateFolder));
        }

        private void RenameFolder(BookmarkFolder folder)
        {
            void DoRenameFolder(string newTitle)
            {
                folder.Name = newTitle;
            }
            dialogService.ShowDialog(new RenameBookmarkDialog(folder.Name, "Rename Bookmark Folder", DoRenameFolder));
        }

        private void CreateFolder(string title)
        {
            Root.Add(new BookmarkFolder(title));
        }

        private void RemoveBookmark(Bookmark bookmark)
        {
            Root.Remove(bookmark);
        }

        private void RemoveAllBookmarks()
        {
            Root.Items.Clear();
        }

        public void AddRecentDocumentsBookmark()
        {
            var shortcut = new RecentDocumentsShortcut();
            var bookmark = FromShortcut(shortcut);
            Root.Add(bookmark);
        }

        public void Dispose()
        {
            invalidateDisposable.Dispose();
        }

        public void Clear()
        {
            Root.Items.Clear();
        }

        public void Load(ImmutableJsonArray json)
        {
            Root.Items.Clear();
            foreach (var item in json)
            {
                var bookmark = BookmarkFromJson(item);
                if (bookmark != null)
                    Root.Add(bookmark);
            }
        }

        public ImmutableJsonArray Save()
        {
            var result = new JsonArray();
            result.AddRange(Root.Items.Select(BookmarkToJson));
            return result;
        }

        private ImmutableJson BookmarkToJson(Bookmark bookmark)
        {
            if (bookmark is BookmarkFolder folder)
            {
                var jsonArray = new JsonArray();
                jsonArray.AddRange(folder.Items.Select(BookmarkToJson));
                var jsonObject = new JsonObject
                {
                    ["Title"] = folder.Name,
                    ["Items"] = jsonArray
                };
                return jsonObject;
            }
            else
            {
                var shortcut = bookmark.Shortcut;
                return ShortcutService.GetHandler(shortcut).GetUri(shortcut).ToString();
            }
        }

        private Bookmark? BookmarkFromJson(ImmutableJson json)
        {
            if (json.IsObject)
            {
                var title = json["Title"].AsString;
                var items = json["Items"].AsArray;
                var folder = new BookmarkFolder(title);
                foreach (var item in items)
                {
                    var child = BookmarkFromJson(item);
                    if (child != null)
                        folder.Add(child);
                }
                return folder;
            }
            else if (json.IsString)
            {
                return FromUrl(json.AsString);
            }
            else
                return null;
        }

        void Invalidate(IShortcutHandler handler)
        {
            Root.Invalidate(handler);
        }

        public Bookmark FromShortcut(IShortcut shortcut)
        {
            var handler = ShortcutService.GetHandler(shortcut);
            return new BookmarkShortcut(shortcut, handler);
        }

        private Bookmark? FromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            if (ShortcutService.TryParseUri(new Uri(url), out var shortcut))
                return FromShortcut(shortcut);
            else
                return null;
        }
    }
}
