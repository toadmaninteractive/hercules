using System;
using System.Windows.Controls;

namespace Hercules.AI.View
{
    /// <summary>
    /// Interaction logic for AiSettingsTabView.xaml
    /// </summary>
    [ViewModelType(typeof(AiSettingsTab))]
    public partial class AiSettingsTabView : UserControl
    {
        public AiSettingsTabView()
        {
            InitializeComponent();
        }

        private void AntropicModel_DropDownOpened(object sender, EventArgs e)
        {
            ((AiSettingsTab)DataContext).RefreshModelsCommand.Execute(null);
        }
    }
}
