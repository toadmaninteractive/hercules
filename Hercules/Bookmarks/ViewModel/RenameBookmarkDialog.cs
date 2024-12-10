using Hercules.Shell;
using System;

namespace Hercules.Bookmarks
{
    public class RenameBookmarkDialog : Dialog
    {
        private string bookmarkTitle;

        public string BookmarkTitle
        {
            get => bookmarkTitle;
            set => SetField(ref bookmarkTitle, value);
        }

        private readonly Action<string> applyBookmarkTitle;

        public RenameBookmarkDialog(string bookmarkTitle, string dialogTitle, Action<string> applyBookmarkTitle)
        {
            this.Title = dialogTitle;
            this.bookmarkTitle = bookmarkTitle;
            this.applyBookmarkTitle = applyBookmarkTitle;
        }

        protected override void OnClose(bool result)
        {
            if (result)
                applyBookmarkTitle(bookmarkTitle);
        }
    }
}
