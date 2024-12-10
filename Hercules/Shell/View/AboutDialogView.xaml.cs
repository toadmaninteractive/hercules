using System.Windows;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for WindowAbout.xaml
    /// </summary>
    [ViewModelType(typeof(AboutDialog))]
    public partial class AboutDialogView : Window
    {
        public AboutDialogView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Workspace.OpenExternalBrowser(e.Uri);
        }
    }
}
