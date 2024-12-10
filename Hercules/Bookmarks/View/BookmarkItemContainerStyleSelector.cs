using Hercules.Documents;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Bookmarks.View
{
    public class BookmarkItemContainerStyleSelector : StyleSelector
    {
        public Style? DocumentStyle { get; set; }
        public Style? BookmarkStyle { get; set; }
        public Style? ShortcutFolderStyle { get; set; }
        public Style? BookmarkFolderStyle { get; set; }

        public override Style? SelectStyle(object item, DependencyObject container)
        {
            var style = item switch
            {
                IDocument => DocumentStyle,
                BookmarkShortcut bookmarkShortcut => bookmarkShortcut.IsFolder ? ShortcutFolderStyle : BookmarkStyle,
                BookmarkFolder => BookmarkFolderStyle,
                _ => null
            };
            return style ?? base.SelectStyle(item, container);
        }
    }
}
