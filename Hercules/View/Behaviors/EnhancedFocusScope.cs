using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Hercules.Controls
{
    /// <summary>
    /// Attached behavior for improved focus scope handling.
    /// </summary>
    public static class EnhancedFocusScope
    {
        public static void SetFocusOnActiveElementInScope(UIElement scope)
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(scope);
            if (focusedElement != null)
            {
                Keyboard.Focus(focusedElement);
            }
            else
            {
                scope.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }

        public static bool GetIsEnhancedFocusScope(UIElement element)
        {
            return (bool)element.GetValue(IsEnhancedFocusScopeProperty);
        }

        public static void SetIsEnhancedFocusScope(UIElement element, bool value)
        {
            element.SetValue(IsEnhancedFocusScopeProperty, value);
        }

        public static readonly DependencyProperty IsEnhancedFocusScopeProperty =
            DependencyProperty.RegisterAttached(
                "IsEnhancedFocusScope",
                typeof(bool),
                typeof(EnhancedFocusScope),
                new UIPropertyMetadata(false, OnIsEnhancedFocusScopeChanged));

        static void OnIsEnhancedFocusScopeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            UIElement? item = depObj as UIElement;
            if (item == null)
                return;

            if ((bool)e.NewValue)
            {
                FocusManager.SetIsFocusScope(item, true);
                item.GotKeyboardFocus += OnGotKeyboardFocus;
            }
            else
            {
                FocusManager.SetIsFocusScope(item, false);
                item.GotKeyboardFocus -= OnGotKeyboardFocus;
            }
        }

        static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            IInputElement focusedElement = e.NewFocus;
            if (focusedElement is RadAutoSuggestBox)
            {
                return;
            }
            for (DependencyObject? d = focusedElement as DependencyObject; d is Visual; d = VisualTreeHelper.GetParent(d))
            {
                if (FocusManager.GetIsFocusScope(d))
                {
                    d.SetValue(FocusManager.FocusedElementProperty, focusedElement);
                    if (!(bool)d.GetValue(IsEnhancedFocusScopeProperty))
                    {
                        break;
                    }
                }
            }
        }
    }
}
