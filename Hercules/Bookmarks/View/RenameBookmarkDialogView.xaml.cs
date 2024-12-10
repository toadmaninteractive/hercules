using System.Windows;

namespace Hercules.Bookmarks.View
{
    /// <summary>
    /// Interaction logic for RenameBookmarkDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(RenameBookmarkDialog))]
    public partial class RenameBookmarkDialogView : Window
    {
        public RenameBookmarkDialogView()
        {
            InitializeComponent();
        }
    }
}
