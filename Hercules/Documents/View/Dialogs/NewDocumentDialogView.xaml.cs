using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    /// <summary>
    /// Interaction logic for WindowNewDocument.xaml
    /// </summary>
    [ViewModelType(typeof(NewDocumentDialog))]
    public partial class NewDocumentDialogView : Window
    {
        public NewDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
