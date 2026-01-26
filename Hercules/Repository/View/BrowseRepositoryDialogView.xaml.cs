using System.Windows;
using Telerik.Windows;
using Telerik.Windows.Controls;

namespace Hercules.Repository.View
{
    /// <summary>
    /// Interaction logic for BrowseRepositoryDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(BrowseRepositoryDialog))]
    public partial class BrowseRepositoryDialogView : Window
    {
        public BrowseRepositoryDialogView()
        {
            InitializeComponent();
        }

        private void RadTreeView_LoadOnDemand(object sender, RadRoutedEventArgs e)
        {
            if (e.OriginalSource is not RadTreeViewItem expandedItem)
                return;

            RepositoryFolder folder = (RepositoryFolder)expandedItem.Item;
            folder.LoadAsync().Track();
        }
    }
}
