using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Plots.View
{
    /// <summary>
    /// Interaction logic for CurveDialogView.xaml
    /// </summary>
    [ViewModelType(typeof(CurveDialog))]
    public partial class CurveDialogView : Window
    {
        public CurveDialogView()
        {
            InitializeComponent();
        }

        private void AutoScaleButtonClick(object sender, RoutedEventArgs e)
        {
            CartesianPanel.AutoScale();
        }

        private void CartesianPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var pos = CartesianPanel.RenderToModel(e.GetPosition(CartesianPanel));
            TextX.Text = pos.X.ToString("F", CultureInfo.InvariantCulture);
            TextY.Text = pos.Y.ToString("F", CultureInfo.InvariantCulture);
        }
    }
}