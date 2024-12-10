using Hercules.Shell;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Controls
{
    public class ContextMenuBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty ProviderProperty = DependencyProperty.Register(nameof(Provider),
            typeof(IWorkspaceContextMenuProvider), typeof(ContextMenuBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty StyleProperty = DependencyProperty.Register(nameof(Style),
            typeof(Style), typeof(ContextMenuBehavior), new PropertyMetadata(null));

        public IWorkspaceContextMenuProvider Provider
        {
            get => (IWorkspaceContextMenuProvider)GetValue(ProviderProperty);
            set => SetValue(ProviderProperty, value);
        }

        public Style Style
        {
            get => (Style)GetValue(StyleProperty);
            set => SetValue(StyleProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.ContextMenuOpening += AssociatedObject_ContextMenuOpening;
            AssociatedObject.ContextMenu = new ContextMenu { Style = Style };
        }

        private void AssociatedObject_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            AssociatedObject.ContextMenu.ItemsSource = Provider?.ContextMenu?.Items;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ContextMenuOpening -= AssociatedObject_ContextMenuOpening;
            AssociatedObject.ContextMenu = null;
        }
    }
}
