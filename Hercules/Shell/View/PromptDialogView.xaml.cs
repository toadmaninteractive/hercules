using System.Windows;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for MessageBoxDialog.xaml
    /// </summary>
    [ViewModelType(typeof(PromptDialog))]
    public partial class PromptDialogView : Window
    {
        public PromptDialogView()
        {
            InitializeComponent();
        }
    }
}
