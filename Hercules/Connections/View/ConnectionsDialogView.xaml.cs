using System.Windows;

namespace Hercules.Connections.View
{
    /// <summary>
    /// Interaction logic for WindowConnections.xaml
    /// </summary>
    [ViewModelType(typeof(ConnectionsDialog))]
    public partial class ConnectionsDialogView : Window
    {
        public ConnectionsDialogView()
        {
            InitializeComponent();
        }
    }
}
