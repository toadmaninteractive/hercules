using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Telerik.Windows.Controls;

namespace Hercules.Summary
{
    public class SummaryGridViewBehavior : Behavior<RadGridView>
    {
        protected override void OnAttached()
        {
            AssociatedObject.KeyDown += DataGrid_KeyDown;
            AssociatedObject.CopyingCellClipboardContent += DataGrid_CopyingCellClipboardContent;
            AssociatedObject.PastingCellClipboardContent += DataGrid_PastingCellClipboardContent;
            AssociatedObject.PreparedCellForEdit += DataGrid_PreparedCellForEdit;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.KeyDown -= DataGrid_KeyDown;
            AssociatedObject.CopyingCellClipboardContent -= DataGrid_CopyingCellClipboardContent;
            AssociatedObject.PastingCellClipboardContent -= DataGrid_PastingCellClipboardContent;
            AssociatedObject.PreparedCellForEdit -= DataGrid_PreparedCellForEdit;
        }

        private void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (!AssociatedObject.IsReadOnly && e.Key == Key.Delete)
            {
                bool isEditing = AssociatedObject.CurrentCell is { IsInEditMode: true };
                if (!isEditing && AssociatedObject.SelectedCells != null)
                {
                    foreach (var cell in AssociatedObject.SelectedCells)
                    {
                        if (cell.Column.Tag is TableColumn { IsReadOnly: false } column)
                        {
                            var row = (TableRow)cell.Item;
                            row.GetCell(column).Clear();
                        }
                    }
                    e.Handled = true;
                }
            }
        }

        private void DataGrid_CopyingCellClipboardContent(object? sender, GridViewCellClipboardEventArgs e)
        {
            var column = (TableColumn)e.Cell.Column.Tag;
            var row = (TableRow)e.Cell.Item;
            e.Value = row.GetCell(column).StringValue;
        }

        private void DataGrid_PastingCellClipboardContent(object? sender, GridViewCellClipboardEventArgs e)
        {
            var column = (TableColumn)e.Cell.Column.Tag;
            var row = (TableRow)e.Cell.Item;
            row.GetCell(column).StringValue = e.Value.ToString()!;
        }

        private void DataGrid_PreparedCellForEdit(object? sender, GridViewPreparingCellForEditEventArgs e)
        {
            switch (e.EditingElement)
            {
                case ComboBox comboBox:
                    comboBox.IsDropDownOpen = true;
                    break;
                case CheckBox checkBox:
                    checkBox.Margin = new Thickness(6, 1, 0, 0);
                    checkBox.IsChecked = !checkBox.IsChecked;
                    break;
            }
        }
    }
}
