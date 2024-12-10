using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    [ViewModelType(typeof(RenameDocumentDialog))]
    public partial class RenameDocumentDialogView : Window
    {
        public RenameDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
