using Hercules.Documents.Editor;
using Hercules.Shell;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for SearchNotification.xaml
    /// </summary>
    [ViewModelType(typeof(SearchNotification))]
    public partial class SearchNotificationView : UserControl
    {
        public SearchNotificationView()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(new Action(Activate), System.Windows.Threading.DispatcherPriority.Loaded, null);
        }

        public void Activate()
        {
            UpdateLayout();
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                ((Notification)DataContext).Close();
        }
    }
}
