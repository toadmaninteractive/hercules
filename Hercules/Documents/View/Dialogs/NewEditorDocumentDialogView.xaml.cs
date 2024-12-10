using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    /// <summary>
    /// Interaction logic for WindowNewDocument.xaml
    /// </summary>
    [ViewModelType(typeof(NewEditorDocumentDialog))]
    public partial class NewEditorDocumentDialogView : Window
    {
        public NewEditorDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
