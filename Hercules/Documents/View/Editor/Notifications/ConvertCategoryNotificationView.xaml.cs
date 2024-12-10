using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for SchemalessNotification.xaml
    /// </summary>
    [ViewModelType(typeof(ConvertCategoryNotification))]
    public partial class ConvertCategoryNotificationView : UserControl
    {
        public ConvertCategoryNotificationView()
        {
            InitializeComponent();
        }
    }
}
