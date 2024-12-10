using Microsoft.Xaml.Behaviors;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Hercules.Controls
{
    /// <summary>
    /// For History scroll
    /// </summary>
    public sealed class GridViewItemBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty IsScrollSelectedIntoViewProperty =
            DependencyProperty.RegisterAttached(
            "IsScrollSelectedIntoView",
            typeof(bool),
            typeof(GridViewItemBehavior),
            new UIPropertyMetadata(false, OnIsScrollSelectedIntoViewChanged));

        private GridViewItemBehavior()
        {
        }

        public static bool GetIsScrollSelectedIntoView(DependencyObject gridView)
        {
            return (bool)gridView.GetValue(IsScrollSelectedIntoViewProperty);
        }

        public static void SetIsScrollSelectedIntoView(DependencyObject gridView, bool value)
        {
            gridView.SetValue(IsScrollSelectedIntoViewProperty, value);
        }

        private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadGridView dg && VisualTreeHelper.GetChildrenCount(dg) > 0)
            {
                var scroll = dg.ChildrenOfType<GridViewScrollViewer>().FirstOrDefault();
                if (scroll != null)
                    scroll.PreviewMouseWheel += Scroll_PreviewMouseWheel;
            }
        }

        static void Scroll_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scroll = (GridViewScrollViewer)sender;
            scroll.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private static void OnIsScrollSelectedIntoViewChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var dg = (RadGridView)depObj;

            if (e.NewValue != null && (bool)e.NewValue)
                dg.Loaded += OnDataGridLoaded;
            else
                dg.Loaded -= OnDataGridLoaded;
        }
    }
}