using System.Windows.Controls;

namespace Hercules.ApplicationUpdate.View
{
    /// <summary>
    /// Interaction logic for ApplicationUpdateSettingsTabView.xaml
    /// </summary>
    [ViewModelType(typeof(ApplicationUpdateSettingsTab))]
    public partial class ApplicationUpdateSettingsTabView : UserControl
    {
        public ApplicationUpdateSettingsTabView()
        {
            InitializeComponent();
        }
    }
}
