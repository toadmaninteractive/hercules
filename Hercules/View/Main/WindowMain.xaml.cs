using Hercules.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Hercules.View
{
    /// <summary>
    /// Interaction logic for WindowMain.xaml
    /// </summary>
    ///
    public partial class WindowMain : Window
    {
        private const double MinPanelSize = 100;
        private const double MinDocumentAreaSize = 200;

        public MainViewModel ViewModel { get; }

        public WindowMain()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(this);
            this.DataContext = ViewModel;
            foreach (var panel in new[] { LeftPanel, RightPanel, BottomPanel, MainGroupTabs, SideGroupTabs })
                panel.AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(Panel_GotKeyboardFocus), true);
            ViewModel.WindowService.SideGroup.Pages.CollectionChanged += (_, _) => UpdateSideGroupColumn();
        }

        // Splitting gives both groups half of the width; the GridSplitter changes the proportion afterwards
        private void UpdateSideGroupColumn()
        {
            var isShown = ViewModel.WindowService.SideGroup.Pages.Count > 0;
            if (isShown == SideGroupColumn.Width.Value > 0)
                return;
            MainGroupColumn.Width = new GridLength(1, GridUnitType.Star);
            SideGroupColumn.Width = isShown ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
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
            var tabs = ViewModel.WindowService.ActiveGroup == ViewModel.WindowService.SideGroup ? SideGroupTabs : MainGroupTabs;
            if (e.NewFocus == this && tabs.SelectedItem != null && tabs.Template.FindName("PART_SelectedContentHost", tabs) is ContentPresenter content)
            {
                e.Handled = true;
                content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }

        // The panel that has keyboard focus provides the active content
        private void Panel_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedItem is DockingLayoutItem item)
                ViewModel.WindowService.ActiveContent = item;
        }

        private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabs = (TabControl)sender;
            if (e.OriginalSource != tabs)
                return;
            // the tab of a just opened page is generated later
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (tabs.ItemContainerGenerator.ContainerFromItem(tabs.SelectedItem) is TabItem tab)
                    tab.BringIntoView();
            });
        }

        private void DocumentTabStrip_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var tabStrip = (ScrollViewer)sender;
            tabStrip.ScrollToHorizontalOffset(tabStrip.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private void DocumentList_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem { DataContext: Shell.Page page })
                ViewModel.WindowService.ActiveContent = page;
        }

        private void Splitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var splitter = (Thumb)sender;
            var panel = (FrameworkElement)splitter.Tag;
            var parent = (FrameworkElement)panel.Parent;
            switch (DockPanel.GetDock(splitter))
            {
                case Dock.Left:
                    panel.Width = ClampPanelSize(panel.Width + e.HorizontalChange, parent.ActualWidth);
                    break;
                case Dock.Right:
                    panel.Width = ClampPanelSize(panel.Width - e.HorizontalChange, parent.ActualWidth);
                    break;
                case Dock.Bottom:
                    panel.Height = ClampPanelSize(panel.Height - e.VerticalChange, parent.ActualHeight);
                    break;
            }
        }

        private static double ClampPanelSize(double size, double available)
        {
            return Math.Max(MinPanelSize, Math.Min(size, available - MinDocumentAreaSize));
        }

        // Panel sizes are stored in settings only when a drag ends, so the settings file isn't written on every mouse move
        private void Splitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var splitter = (Thumb)sender;
            var panel = (FrameworkElement)splitter.Tag;
            var property = DockPanel.GetDock(splitter) == Dock.Bottom ? HeightProperty : WidthProperty;
            panel.GetBindingExpression(property)!.UpdateSource();
        }
    }
}
