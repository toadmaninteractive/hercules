using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for DocumentAttachmentsView.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentAttachments))]
    public partial class DocumentAttachmentsView : UserControl
    {
        public DocumentAttachmentsView()
        {
            InitializeComponent();
        }
    }
}
