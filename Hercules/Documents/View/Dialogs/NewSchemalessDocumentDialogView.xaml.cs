using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    /// <summary>
    /// Interaction logic for WindowNewDocument.xaml
    /// </summary>
    [ViewModelType(typeof(NewSchemalessDocumentDialog))]
    public partial class NewSchemalessDocumentDialogView : Window
    {
        public NewSchemalessDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
