using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for SchemalessNotification.xaml
    /// </summary>
    [ViewModelType(typeof(RebaseDocumentNotification))]
    public partial class RebaseDocumentNotificationView : UserControl
    {
        public RebaseDocumentNotificationView()
        {
            InitializeComponent();
        }
    }
}
