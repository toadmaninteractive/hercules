using System.Windows.Controls;

namespace Hercules.Scripting.View
{
    /// <summary>
    /// Interaction logic for TablePageView.xaml
    /// </summary>
    [ViewModelType(typeof(TablePage))]
    public partial class TablePageView : UserControl
    {
        public TablePageView()
        {
            InitializeComponent();
        }
    }
}
