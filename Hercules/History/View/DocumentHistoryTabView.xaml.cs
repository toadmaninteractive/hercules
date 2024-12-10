using Hercules.DB;
using System.Windows.Controls;

namespace Hercules.History.View
{
    /// <summary>
    /// Interaction logic for DocumentHistoryTab.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentHistoryTab))]
    public partial class DocumentHistoryTabView : UserControl
    {
        public DocumentHistoryTabView()
        {
            InitializeComponent();
        }

        private void ListBox_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var item = (ListBoxItem)sender;
            if (item.DataContext is DocumentCommit revision)
            {
                var changes = revision.Changes;
                if (changes != null)
                {
                    var tooltip = (ToolTip)Resources["DiffToolTip"];
                    tooltip.DataContext = changes;
                    tooltip.StaysOpen = true;
                    item.ToolTip = tooltip;
                }
                else
                    e.Handled = true;
            }
            else
                e.Handled = true;
        }

        private void ListBox_ToolTipClosing(object sender, ToolTipEventArgs e)
        {
            ((ListBoxItem)sender).ToolTip = "";
        }
    }
}
