using System.Windows.Controls;

namespace Hercules.ServerBrowser.View
{
    /// <summary>
    /// Interaction logic for CompareDatabasesPageView.xaml
    /// </summary>
    [ViewModelType(typeof(ServerBrowserPage))]
    public partial class ServerBrowserPageView : UserControl
    {
        public ServerBrowserPageView()
        {
            InitializeComponent();
        }
    }
}
