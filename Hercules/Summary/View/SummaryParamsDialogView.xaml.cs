using System.Windows;

namespace Hercules.Summary.View
{
    /// <summary>
    /// Interaction logic for WindowSummaryParams.xaml
    /// </summary>
    [ViewModelType(typeof(SummaryParamsDialog))]
    public partial class SummaryParamsDialogView : Window
    {
        public SummaryParamsDialogView()
        {
            InitializeComponent();
        }
    }
}
