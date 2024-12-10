using Hercules.Documents.Editor;
using System;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for AddCustomFieldNotification.xaml
    /// </summary>
    [ViewModelType(typeof(AddCustomFieldNotification))]
    public partial class AddCustomFieldNotificationView : UserControl
    {
        public AddCustomFieldNotificationView()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(new Action(Activate), System.Windows.Threading.DispatcherPriority.Loaded, null);
        }

        public void Activate()
        {
            UpdateLayout();
            NameBox.Focus();
        }
    }
}
