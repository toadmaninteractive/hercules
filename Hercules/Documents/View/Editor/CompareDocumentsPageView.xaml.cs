using Hercules.Documents.Editor;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for CompareDocuments.xaml
    /// </summary>
    [ViewModelType(typeof(CompareDocumentsPage))]
    public partial class CompareDocumentsPageView : UserControl
    {
        public CompareDocumentsPageView()
        {
            InitializeComponent();
        }

        private void Editor_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ReferenceEquals(sender, LeftEditor))
            {
                RightEditor.ScrollToVerticalOffset(e.VerticalOffset);
            }
            else
            {
                LeftEditor.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }
    }
}
