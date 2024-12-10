using Hercules.Shortcuts;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Bookmarks.View
{
    /// <summary>
    /// Interaction logic for BookmarksBar.xaml
    /// </summary>
    [ViewModelType(typeof(BookmarksBar))]
    public partial class BookmarksBarView : UserControl
    {
        public BookmarksBarView()
        {
            InitializeComponent();
        }

        public BookmarksBar Bookmarks => (BookmarksBar)DataContext;

        private void BookmarksBar_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(HerculesDragData.DragDataFormat))
            {
                e.Effects = DragDropEffects.Link;
                e.Handled = true;
            }
        }

        private void BookmarksBar_Drop(object sender, DragEventArgs e)
        {
            var item = e.Data.GetData(HerculesDragData.DragDataFormat);
            if (item != null)
            {
                var targetBookmark = sender is FrameworkElement frameworkElement ? frameworkElement.DataContext as Bookmark : null;
                int? index = null;
                if (targetBookmark != null)
                    index = Bookmarks.Root.Items.IndexOf(targetBookmark);
                void Adder(Bookmark bookmark)
                {
                    if (targetBookmark == null)
                        Bookmarks.Root.Add(bookmark, index);
                    else if (targetBookmark is BookmarkFolder folder)
                        folder.Add(bookmark);
                    else
                        targetBookmark.Parent!.Add(bookmark, index);
                }
                AddBookmark(item, Adder, targetBookmark);
                e.Handled = true;
            }
        }

        void AddBookmark(object item, Action<Bookmark> adder, Bookmark? target)
        {
            if (item is Bookmark bookmarkItem)
            {
                if (bookmarkItem != target)
                {
                    adder(bookmarkItem);
                }
            }
            else if (item is IShortcutProvider shortcutProvider)
            {
                var shortcut = shortcutProvider.Shortcut;
                var bookmark = Bookmarks.FromShortcut(shortcut);
                adder(bookmark);
            }
            else if (item is IEnumerable enumerable)
            {
                foreach (var element in enumerable)
                {
                    AddBookmark(element!, adder, target);
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }

        Point dragDropStartPoint;
        MenuItem? dragDropItem;

        private void Bookmark_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragDropStartPoint = e.GetPosition(null);
            dragDropItem = sender as MenuItem;
        }

        private void Bookmark_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && dragDropItem != null)
            {
                var mousePos = e.GetPosition(null);
                var diff = dragDropStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var dragData = new DataObject(HerculesDragData.DragDataFormat, dragDropItem.DataContext);
                    DragDrop.DoDragDrop(dragDropItem, dragData, DragDropEffects.Move | DragDropEffects.Link);
                    dragDropItem = null;
                }
            }
            else
                dragDropItem = null;
        }
    }
}
