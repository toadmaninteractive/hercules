using System.Windows.Controls;

namespace Hercules.Shell.View
{
    [ViewModelType(typeof(PropertyEditorTool))]
    public partial class PropertyEditorView : UserControl
    {
        public PropertyEditorView()
        {
            InitializeComponent();
        }
    }
}