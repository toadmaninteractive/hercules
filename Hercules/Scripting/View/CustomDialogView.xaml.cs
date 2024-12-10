using System.Windows;

namespace Hercules.Scripting.View
{
    /// <summary>
    /// Interaction logic for CustomDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(CustomDialog))]
    public partial class CustomDialogView : Window
    {
        public CustomDialogView()
        {
            InitializeComponent();
        }
    }
}
