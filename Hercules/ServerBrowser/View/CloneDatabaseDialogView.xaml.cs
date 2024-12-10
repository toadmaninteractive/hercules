using System.Windows;

namespace Hercules.ServerBrowser.View
{
    /// <summary>
    /// Interaction logic for CloneDatabaseDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(CloneDatabaseDialog))]
    public partial class CloneDatabaseDialogView : Window
    {
        public CloneDatabaseDialogView()
        {
            InitializeComponent();
        }
    }
}
