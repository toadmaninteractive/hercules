using Hercules.Shortcuts;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Bookmarks
{
    public class BookmarkFolderShortcutHandler : ShortcutHandler<BookmarkFolder>
    {
        protected override Uri DoGetUri(BookmarkFolder shortcut) => throw new NotSupportedException();

        protected override string DoGetIcon(BookmarkFolder shortcut) => shortcut.Icon;

        protected override string DoGetTitle(BookmarkFolder shortcut) => shortcut.Name;

        protected override bool DoOpen(BookmarkFolder shortcut) => false;

        protected override bool DoTryParseUri(Uri uri, [MaybeNullWhen(false)] out BookmarkFolder shortcut)
        {
            shortcut = default!;
            return false;
        }

        protected override IEnumerable DoGetItems(BookmarkFolder shortcut) => shortcut.Items;

        public override bool IsFolder => true;
    }
}
