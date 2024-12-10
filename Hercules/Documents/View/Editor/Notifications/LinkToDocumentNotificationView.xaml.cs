using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for SchemalessNotification.xaml
    /// </summary>
    [ViewModelType(typeof(LinkToDocumentNotification))]
    public partial class LinkToDocumentNotificationView : UserControl
    {
        public LinkToDocumentNotificationView()
        {
            InitializeComponent();
        }
    }
}
