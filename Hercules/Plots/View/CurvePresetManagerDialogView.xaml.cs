using System.Windows;

namespace Hercules.Plots.View
{
    /// <summary>
    /// Interaction logic for CurvePresetManagerView.xaml
    /// </summary>
    [ViewModelType(typeof(CurvePresetManagerDialog))]
    public partial class CurvePresetManagerDialogView : Window
    {
        public CurvePresetManagerDialogView()
        {
            InitializeComponent();
        }
    }
}