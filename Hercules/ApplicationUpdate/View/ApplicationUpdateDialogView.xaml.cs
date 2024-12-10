using System.Windows;

namespace Hercules.ApplicationUpdate.View
{
    /// <summary>
    /// Interaction logic for ApplicationUpdateDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(ApplicationUpdateDialog))]
    public partial class ApplicationUpdateDialogView : Window
    {
        public ApplicationUpdateDialogView()
        {
            InitializeComponent();
        }
    }
}
