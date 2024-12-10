using System.Windows.Controls;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for MessageNotificationView.xaml
    /// </summary>
    [ViewModelType(typeof(MessageNotification))]
    public partial class MessageNotificationView : UserControl
    {
        public MessageNotificationView()
        {
            InitializeComponent();
        }
    }
}
