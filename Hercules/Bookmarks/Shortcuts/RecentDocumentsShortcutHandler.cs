using Hercules.Shortcuts;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Bookmarks
{
    public class RecentDocumentsShortcutHandler : ShortcutHandler<RecentDocumentsShortcut>
    {
        private readonly BookmarksModule bookmarksModule;

        public RecentDocumentsShortcutHandler(BookmarksModule bookmarksModule)
        {
            this.bookmarksModule = bookmarksModule;
        }

        protected override Uri DoGetUri(RecentDocumentsShortcut shortcut)
        {
            return new Uri("hercules:////?special_folder=recent_documents", UriKind.RelativeOrAbsolute);
        }

        protected override bool DoOpen(RecentDocumentsShortcut shortcut) => false;

        protected override string DoGetTitle(RecentDocumentsShortcut shortcut) => "Recent Documents";

        protected override string DoGetIcon(RecentDocumentsShortcut shortcut) => Fugue.Icons.BlueFolderOpen;

        public override bool IsFolder => true;

        protected override IEnumerable DoGetItems(RecentDocumentsShortcut shortcut) => bookmarksModule.RecentDocuments;

        protected override bool DoTryParseUri(Uri uri, [MaybeNullWhen(false)] out RecentDocumentsShortcut shortcut)
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (query.Get("special_folder") == "recent_documents")
            {
                shortcut = new RecentDocumentsShortcut();
                return true;
            }

            shortcut = null;
            return false;
        }
    }
}
