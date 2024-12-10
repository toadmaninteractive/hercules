using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for DocumentChangedNotification.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentChangedNotification))]
    public partial class DocumentChangedNotificationView : UserControl
    {
        public DocumentChangedNotificationView()
        {
            InitializeComponent();
        }
    }
}
