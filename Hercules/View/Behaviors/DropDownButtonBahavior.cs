using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Hercules.Controls
{
    public class DropDownButtonBehavior : Behavior<Button>
    {
        public static readonly DependencyProperty DropDownMenuProperty = DependencyProperty.Register("DropDownMenu", typeof(ContextMenu), typeof(DropDownButtonBehavior));

        public ContextMenu? DropDownMenu
        {
            get => (ContextMenu)GetValue(DropDownMenuProperty);
            set => SetValue(DropDownMenuProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.Click += Button_Click;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Click -= Button_Click;
        }

        void Button_Click(object button, RoutedEventArgs args)
        {
            var b = button as Button;
            var dropDown = DropDownMenu;
            if (dropDown != null)
            {
                dropDown.PlacementTarget = b;
                dropDown.Placement = PlacementMode.Bottom;
                dropDown.IsOpen = !dropDown.IsOpen;
            }
        }
    }
}
