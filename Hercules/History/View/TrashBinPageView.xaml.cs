using System.Windows.Controls;

namespace Hercules.History.View
{
    /// <summary>
    /// Interaction logic for TrashBinPageView.xaml
    /// </summary>
    [ViewModelType(typeof(TrashBinPage))]
    public partial class TrashBinPageView : UserControl
    {
        public TrashBinPageView()
        {
            InitializeComponent();
        }
    }
}
