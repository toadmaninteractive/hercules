using System.Windows.Controls;

namespace Hercules.Scripting.View
{
    /// <summary>
    /// Interaction logic for TextPageView.xaml
    /// </summary>
    [ViewModelType(typeof(TextPage))]
    public partial class TextPageView : UserControl
    {
        public TextPageView()
        {
            InitializeComponent();
        }
    }
}
