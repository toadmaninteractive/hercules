using Hercules.Shortcuts;
using System.Collections;
using System.Collections.ObjectModel;

namespace Hercules.Bookmarks
{
    public abstract class Bookmark : NotifyPropertyChanged, IShortcutProvider
    {
        string name;

        public string Name
        {
            get => name;
            set => SetField(ref name, value);
        }

        string icon = "image-" + Fugue.Icons.BlueFolderOpen;

        public string Icon
        {
            get => icon;
            set => SetField(ref icon, value);
        }

        public abstract IShortcut Shortcut { get; }
        public BookmarkFolder? Parent { get; set; }

        public abstract void Invalidate(IShortcutHandler shortcutHandler);

        protected Bookmark(string name)
        {
            this.name = name;
        }
    }

    public sealed class BookmarkFolder : Bookmark, IShortcut
    {
        public ObservableCollection<Bookmark> Items { get; } = new();

        public override IShortcut Shortcut => this;

        public void Remove(Bookmark bookmark)
        {
            Items.Remove(bookmark);
            bookmark.Parent = null;
        }

        public void Add(Bookmark bookmark, int? index = null)
        {
            bookmark.Parent?.Remove(bookmark);
            if (index.HasValue)
                Items.Insert(index.Value, bookmark);
            else
                Items.Add(bookmark);
            bookmark.Parent = this;
        }

        public override void Invalidate(IShortcutHandler shortcutHandler)
        {
            foreach (var bookmark in Items)
            {
                bookmark.Invalidate(shortcutHandler);
            }
        }

        public BookmarkFolder(string name) : base(name)
        {
        }
    }

    public sealed class BookmarkShortcut : Bookmark, IShortcutProvider
    {
        IEnumerable? items;

        public IEnumerable? Items
        {
            get => items;
            set => SetField(ref items, value);
        }

        public bool IsFolder { get; private set; }

        public override void Invalidate(IShortcutHandler shortcutHandler)
        {
            if (shortcutHandler.Type == shortcut.GetType())
            {
                this.Name = shortcutHandler.GetTitle(shortcut);
                this.Icon = "image-" + shortcutHandler.GetIcon(shortcut);
                this.IsFolder = shortcutHandler.IsFolder;
                if (IsFolder)
                    this.Items = shortcutHandler.GetItems(shortcut);
            }
        }

        private readonly IShortcut shortcut;
        public override IShortcut Shortcut => shortcut;

        public BookmarkShortcut(IShortcut shortcut, IShortcutHandler shortcutHandler) : base(default!)
        {
            this.shortcut = shortcut;
            Invalidate(shortcutHandler);
        }
    }
}
