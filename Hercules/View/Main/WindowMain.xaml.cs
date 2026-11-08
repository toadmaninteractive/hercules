using Hercules.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

        private TabItem? draggedTab;
        private Point dragStart;

        private void DocumentTabs_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            draggedTab = FindHeaderTab((TabControl)sender, e.OriginalSource);
            dragStart = e.GetPosition(this);
        }

        private void DocumentTabs_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // the close button captures the mouse while pressed
            if (draggedTab == null || e.LeftButton != MouseButtonState.Pressed || Mouse.Captured is ButtonBase)
                return;
            var offset = e.GetPosition(this) - dragStart;
            if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;
            var tab = draggedTab;
            draggedTab = null;
            DragDrop.DoDragDrop(tab, new DataObject(typeof(Shell.Page), tab.DataContext), DragDropEffects.Move);
        }

        // Tabs of the same group are reordered while dragging, so the tab follows the mouse
        private void DocumentTabs_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Shell.Page)) is not Shell.Page page)
                return;
            var tabs = (TabControl)sender;
            var group = GetEditorGroup(tabs);
            var windowService = ViewModel.WindowService;
            e.Effects = windowService.CanMovePage(page, group) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
            if (windowService.GetGroup(page) == group && GetDropIndex(tabs, group, page, e) is { } index && index != group.Pages.IndexOf(page))
                windowService.MovePage(page, group, index);
        }

        // A page from the other group goes before or after the tab it is dropped on, or to the end of the group
        private void DocumentTabs_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Shell.Page)) is not Shell.Page page)
                return;
            e.Handled = true;
            var tabs = (TabControl)sender;
            var group = GetEditorGroup(tabs);
            var windowService = ViewModel.WindowService;
            if (!windowService.CanMovePage(page, group))
                return;
            var defaultIndex = windowService.GetGroup(page) == group ? group.Pages.IndexOf(page) : group.Pages.Count;
            windowService.MovePage(page, group, GetDropIndex(tabs, group, page, e) ?? defaultIndex);
        }

        private EditorGroup GetEditorGroup(TabControl tabs) => tabs == SideGroupTabs ? ViewModel.WindowService.SideGroup : ViewModel.WindowService.MainGroup;

        // Index the page gets in the group when dropped before or after the tab under the mouse; null when not over a tab
        private static int? GetDropIndex(TabControl tabs, EditorGroup group, Shell.Page page, DragEventArgs e)
        {
            var tab = FindHeaderTab(tabs, e.OriginalSource);
            if (tab == null)
                return null;
            var index = group.Pages.IndexOf((Shell.Page)tab.DataContext);
            if (e.GetPosition(tab).X >= tab.ActualWidth / 2)
                index++;
            var oldIndex = group.Pages.IndexOf(page);
            if (oldIndex >= 0 && oldIndex < index)
                index--;
            return index;
        }

        // The tab header containing the element; tabs inside the page content belong to other tab controls
        private static TabItem? FindHeaderTab(TabControl tabs, object element)
        {
            for (var d = element as DependencyObject; d != null && d != tabs; d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d))
            {
                if (d is TabItem tab && ItemsControl.ItemsControlFromItemContainer(tab) == tabs)
                    return tab;
            }
            return null;
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
