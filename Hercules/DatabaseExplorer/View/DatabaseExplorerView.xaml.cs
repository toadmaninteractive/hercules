using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.DatabaseExplorer.View
{
    [ViewModelType(typeof(DatabaseExplorerTool))]
    public partial class DatabaseExplorerView : UserControl
    {
        public DatabaseExplorerView()
        {
            InitializeComponent();
        }

        private void ClearFilter(object sender, RoutedEventArgs e)
        {
            FilterBox.Focus();
        }

        Point dragDropStartPoint;
        ListBoxItem? dragDropItem;
        bool preventUnselect;

        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragDropStartPoint = e.GetPosition(null);
            dragDropItem = sender as ListBoxItem;
            if (dragDropItem != null && DocsTreeView.SelectedItems.Count > 1 && dragDropItem.IsSelected && e.ClickCount == 1 && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                preventUnselect = true;
            }
        }

        private void TreeViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (preventUnselect && sender == dragDropItem)
            {
                var itemsToRemove = DocsTreeView.SelectedItems.OfType<DatabaseExplorerItem>().Where(i => i != dragDropItem.DataContext).ToList();
                foreach (var item in itemsToRemove)
                    DocsTreeView.SelectedItems.Remove(item);
            }
            dragDropItem = null;
            preventUnselect = false;
        }

        private void TreeViewItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            preventUnselect = false;
            if (e.LeftButton == MouseButtonState.Pressed && dragDropItem != null)
            {
                var mousePos = e.GetPosition(null);
                var diff = dragDropStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (DocsTreeView.SelectedItems.Count == 0)
                        return;

                    var dragData = GetDragData();
                    DragDrop.DoDragDrop(dragDropItem, new DataObject(HerculesDragData.DragDataFormat, dragData), DragDropEffects.Move | DragDropEffects.Link);
                    dragDropItem = null;
                }
            }
            else
                dragDropItem = null;
        }

        object GetDragData()
        {
            var selectedItems = DocsTreeView.SelectedItems;
            if (selectedItems.Count > 1)
                return selectedItems.OfType<DatabaseExplorerDocument>().Select(doc => doc.Document).ToList();
            else if (selectedItems.Count == 1 && selectedItems[0] is DatabaseExplorerDocument databaseExplorerDocument)
                return databaseExplorerDocument.Document;
            else if (selectedItems.Count == 1 && selectedItems[0] is DatabaseExplorerCategory databaseExplorerCategory)
                return databaseExplorerCategory.Category;
            else
                return selectedItems;
        }

        private void DocsTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DocsTreeView.SelectedItem is DatabaseExplorerDocumentGroup databaseExplorerDocumentGroup)
                databaseExplorerDocumentGroup.ToggleExpand();
            else if (DocsTreeView.SelectedItem is DatabaseExplorerDocument databaseExplorerDocument)
                ((DatabaseExplorerTool)DataContext).DocumentsModule.EditDocument(databaseExplorerDocument.DocumentId);
        }

        public void SelectItem(DatabaseExplorerItem selectedItem)
        {
            DocsTreeView.SelectedItem = selectedItem;
            DocsTreeView.UpdateLayout(); // Pre-generates item containers 
            DocsTreeView.ScrollIntoView(DocsTreeView.SelectedItem);
            var listBoxItem = (ListBoxItem)DocsTreeView.ItemContainerGenerator.ContainerFromItem(DocsTreeView.SelectedItem);
            listBoxItem.Focus();
        }
    }
}
