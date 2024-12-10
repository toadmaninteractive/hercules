using System.Windows.Controls;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for AppearanceSettingsTabView.xaml
    /// </summary>
    [ViewModelType(typeof(AppearanceSettingsTab))]
    public partial class AppearanceSettingsTabView : UserControl
    {
        public AppearanceSettingsTabView()
        {
            InitializeComponent();
        }
    }
}
