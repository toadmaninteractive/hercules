using Hercules.Documents.Editor;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for JsonSourceEditor.xaml
    /// </summary>
    [ViewModelType(typeof(JsonSourceTab))]
    public partial class JsonSourceTabView : UserControl
    {
        public JsonSourceTabView()
        {
            InitializeComponent();
        }

        JsonSourceTab ViewModel => (JsonSourceTab)DataContext;

        public void GoToSourceLocation(int location)
        {
            UpdateLayout();
            SourceEditor.TextArea.Focus();
            SourceEditor.CaretOffset = location;
            var loc = SourceEditor.Document.GetLocation(location);
            SourceEditor.ScrollTo(loc.Line, loc.Column);
            ViewModel.LastSourceLocation = null;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.LastSourceLocation.HasValue)
                GoToSourceLocation(ViewModel.LastSourceLocation.Value);
        }
    }
}
