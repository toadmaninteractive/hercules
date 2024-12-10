using System.Windows;

namespace Hercules.ServerBrowser.View
{
    /// <summary>
    /// Interaction logic for WindowEditConnection.xaml
    /// </summary>
    [ViewModelType(typeof(ServerConnectionDialog))]
    public partial class ServerConnectionDialogView : Window
    {
        public ServerConnectionDialogView()
        {
            InitializeComponent();
        }
    }
}
