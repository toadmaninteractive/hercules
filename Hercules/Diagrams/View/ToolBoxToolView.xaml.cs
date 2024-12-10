using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace Hercules.Diagrams.View
{
    /// <summary>
    /// Interaction logic for ToolBoxWindow.xaml
    /// </summary>
    [ViewModelType(typeof(ToolBoxTool))]
    public partial class ToolBoxToolView : UserControl
    {
        public ToolBoxToolView()
        {
            InitializeComponent();
            ToolBox.PreviewMouseDown += (sender, e) => ToolBox.Focus();
        }

        private void ToolBoxItemsFilter(object sender, FilterEventArgs e)
        {
            var toolBoxItem = (ToolBoxItem)e.Item;
            if (string.IsNullOrWhiteSpace(TbxFilter.Text))
                e.Accepted = true;
            else
                e.Accepted = toolBoxItem.Name.Contains(TbxFilter.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            ((CollectionViewSource)Resources["CvsToolBoxItems"]).View.Refresh();
        }

        private void ClearFilter(object sender, System.Windows.RoutedEventArgs e)
        {
            TbxFilter.Text = string.Empty;
        }
    }
}
