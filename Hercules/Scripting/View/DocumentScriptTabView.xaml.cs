using System.Windows.Controls;

namespace Hercules.Scripting.View
{
    /// <summary>
    /// Interaction logic for DocumentScriptTabView.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentScriptTab))]
    public partial class DocumentScriptTabView : UserControl
    {
        public DocumentScriptTabView()
        {
            InitializeComponent();
        }
    }
}
