using System.Windows.Controls;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for GeneralSettingsTabView.xaml
    /// </summary>
    [ViewModelType(typeof(GeneralSettingsTab))]
    public partial class GeneralSettingsTabView : UserControl
    {
        public GeneralSettingsTabView()
        {
            InitializeComponent();
        }
    }
}
