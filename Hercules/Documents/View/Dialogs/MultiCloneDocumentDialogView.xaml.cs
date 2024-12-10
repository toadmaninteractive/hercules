using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    /// <summary>
    /// Interaction logic for WindowValidatedQuery.xaml
    /// </summary>
    [ViewModelType(typeof(MultiCloneDocumentDialog))]
    public partial class MultiCloneDocumentDialogView : Window
    {
        public MultiCloneDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
