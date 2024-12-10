using System;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Connections.View
{
    /// <summary>
    /// Interaction logic for WindowEditConnection.xaml
    /// </summary>
    [ViewModelType(typeof(EditConnectionDialog))]
    public partial class EditConnectionDialogView : Window
    {
        public EditConnectionDialogView()
        {
            InitializeComponent();
        }

        private void DatabaseBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (sender is UIElement element)
                    element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void AutoCompleteComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ((EditConnectionDialog)DataContext).MaybeRefetchDatabases();
        }

        private void AutoCompleteComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((EditConnectionDialog)DataContext).MaybeRefetchDatabases();
        }
    }
}
