using System.Windows;

namespace Hercules.Repository.View
{
    [ViewModelType(typeof(ProjectSettingsDialog))]
    public partial class ProjectSettingsDialogView : Window
    {
        public ProjectSettingsDialogView()
        {
            InitializeComponent();
        }
    }
}
