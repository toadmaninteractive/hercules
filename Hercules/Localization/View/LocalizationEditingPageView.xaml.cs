using System.Windows.Controls;

namespace Hercules.Localization.View
{
    /// <summary>
    /// Interaction logic for LocalizationEditingPageView.xaml
    /// </summary>
    [ViewModelType(typeof(LocalizationEditingPage))]
    public partial class LocalizationEditingPageView : UserControl
    {
        public LocalizationEditingPageView()
        {
            InitializeComponent();
        }
    }
}
