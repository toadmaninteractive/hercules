using System.Windows;

namespace Hercules.Connections.View
{
    [ViewModelType(typeof(OpenDatabaseDialog))]
    public partial class OpenDatabaseDialogView : Window
    {
        public OpenDatabaseDialogView()
        {
            InitializeComponent();
        }
    }
}
