using System.Windows;
using System.Windows.Controls;

namespace Hercules.History.View
{
    /// <summary>
    /// Interaction logic for JsonRevisionTab.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentRevisionTab))]
    public partial class DocumentRevisionTabView : UserControl
    {
        public DocumentRevisionTabView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SourceEditor.Focus();
        }
    }
}
