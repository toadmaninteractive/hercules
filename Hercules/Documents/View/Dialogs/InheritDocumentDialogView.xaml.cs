using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    /// <summary>
    /// Interaction logic for InheritDocumentDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(InheritDocumentDialog))]
    public partial class InheritDocumentDialogView : Window
    {
        public InheritDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
