using System.Windows.Controls;

namespace Hercules.Replication.View
{
    /// <summary>
    /// Interaction logic for SynchronizeDatabaseAdvancedFilter.xaml
    /// </summary>
    [ViewModelType(typeof(SynchronizeDatabaseAdvancedFilterNotification))]
    public partial class SynchronizeDatabaseAdvancedFilterNotificationView : UserControl
    {
        public SynchronizeDatabaseAdvancedFilterNotificationView()
        {
            InitializeComponent();
        }
    }
}
