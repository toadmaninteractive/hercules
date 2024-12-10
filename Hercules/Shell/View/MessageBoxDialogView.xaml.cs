using System.Windows;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for MessageBoxDialog.xaml
    /// </summary>
    [ViewModelType(typeof(MessageBoxDialog))]
    public partial class MessageBoxDialogView : Window
    {
        public MessageBoxDialogView()
        {
            InitializeComponent();
        }
    }
}
