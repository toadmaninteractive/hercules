using Hercules.Documents.Editor;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Documents.View.Editor
{
    [ViewModelType(typeof(DocumentEditorPage))]
    public partial class DocumentEditorPageView : UserControl
    {
        public DocumentEditorPageView()
        {
            InitializeComponent();
        }

        private void TabControl_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is TabItem)
            {
                var method = typeof(TabItem).GetMethod("SetBoolField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method.Invoke(e.NewFocus, new object[] { 16, false });
            }
        }
    }
}
