using System.Windows;

namespace Hercules.Plots.View
{
    /// <summary>
    /// Interaction logic for SprayPatternDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(PlotDialog))]
    public partial class PlotDialogView : Window
    {
        public PlotDialogView()
        {
            InitializeComponent();
        }
    }
}