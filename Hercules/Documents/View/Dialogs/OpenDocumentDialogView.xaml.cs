using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    [ViewModelType(typeof(OpenDocumentDialog))]
    public partial class OpenDocumentDialogView : Window
    {
        public OpenDocumentDialogView()
        {
            InitializeComponent();
        }

        private void DocumentComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var ok = ((OpenDocumentDialog)DataContext).OkCommand;
                if (ok.CanExecute(null))
                    ok.Execute(null);
            }
        }
    }
}
