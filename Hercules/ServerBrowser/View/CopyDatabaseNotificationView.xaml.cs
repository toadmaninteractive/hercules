using System.Windows.Controls;

namespace Hercules.ServerBrowser.View
{
    /// <summary>
    /// Interaction logic for CopyDatabaseDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(CopyDatabaseNotification))]
    public partial class CopyDatabaseNotificationView : UserControl
    {
        public CopyDatabaseNotificationView()
        {
            InitializeComponent();
        }
    }
}
