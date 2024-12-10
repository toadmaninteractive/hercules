using Hercules.Documents.Dialogs;
using System.Windows;

namespace Hercules.Documents.View.Dialogs
{
    /// <summary>
    /// Interaction logic for WindowValidatedQuery.xaml
    /// </summary>
    [ViewModelType(typeof(CloneDocumentDialog))]
    public partial class CloneDocumentDialogView : Window
    {
        public CloneDocumentDialogView()
        {
            InitializeComponent();
        }
    }
}
