using AvalonDock.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Hercules.View
{
    /// <summary>
    /// Interaction logic for WindowMain.xaml
    /// </summary>
    ///
    public partial class WindowMain : Window
    {
        public MainViewModel ViewModel { get; }

        public WindowMain()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(this, DockingManager);
            this.DataContext = ViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(ViewModel.OnLoad);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel.Core.Project != null)
                ViewModel.Core.SaveProject();
            e.Cancel = !ViewModel.Workspace.IsQuitting && !ViewModel.Workspace.MaybeConfirmLoseUnsavedProgress("Quit");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ViewModel.Core.Shutdown();
        }

        private void MainWindow_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == this && ViewModel.Workspace.WindowService.ActiveContent != null)
            {
                var child = this.FindVisualChildren<LayoutDocumentControl>().FirstOrDefault(d => d.LayoutItem.IsActive);
                if (child != null)
                {
                    child.Focusable = false;
                    e.Handled = true;
                    child.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }
            }
        }
    }
}
