using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Controls
{
    public static class RoutedCommandBindings
    {
        public static readonly DependencyProperty CommandBindingsProperty = DependencyProperty.RegisterAttached("CommandBindings", typeof(CommandBindingCollection), typeof(RoutedCommandBindings), new PropertyMetadata(null, OnCommandBindingsChanged));
        public static readonly DependencyProperty InputBindingsProperty = DependencyProperty.RegisterAttached("InputBindings", typeof(InputBindingCollection), typeof(RoutedCommandBindings), new PropertyMetadata(null, OnInputBindingsChanged));

        public static void SetCommandBindings(UIElement element, CommandBindingCollection value)
        {
            element?.SetValue(CommandBindingsProperty, value);
        }

        public static CommandBindingCollection? GetCommandBindings(UIElement element)
        {
            return element?.GetValue(CommandBindingsProperty) as CommandBindingCollection;
        }

        private static void OnCommandBindingsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                if (e.OldValue is CommandBindingCollection oldBindings)
                {
                    foreach (CommandBinding binding in oldBindings.Cast<CommandBinding>())
                        element.CommandBindings.Remove(binding);
                }

                if (e.NewValue is CommandBindingCollection bindings)
                {
                    element.CommandBindings.AddRange(bindings);
                }
            }
        }

        public static void SetInputBindings(UIElement element, InputBindingCollection value)
        {
            element?.SetValue(InputBindingsProperty, value);
        }

        public static InputBindingCollection? GetInputBindings(UIElement element)
        {
            return element?.GetValue(InputBindingsProperty) as InputBindingCollection;
        }

        private static void OnInputBindingsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                if (e.OldValue is InputBindingCollection oldBindings)
                {
                    foreach (InputBinding binding in oldBindings.Cast<InputBinding>())
                        element.InputBindings.Remove(binding);
                }

                if (e.NewValue is InputBindingCollection bindings)
                {
                    element.InputBindings.AddRange(bindings);
                }
            }
        }
    }
}
