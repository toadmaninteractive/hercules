using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for SchemaChangedNotification.xaml
    /// </summary>
    [ViewModelType(typeof(SchemaChangedNotification))]
    public partial class SchemaChangedNotificationView : UserControl
    {
        public SchemaChangedNotificationView()
        {
            InitializeComponent();
        }
    }
}
