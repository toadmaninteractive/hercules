using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for DocumentDeletedNotification.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentDeletedNotification))]
    public partial class DocumentDeletedNotificationView : UserControl
    {
        public DocumentDeletedNotificationView()
        {
            InitializeComponent();
        }
    }
}
