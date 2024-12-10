using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Controls
{
    public class ComboBoxExpandBehavior : Behavior<ComboBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.KeyUp += HandelKeyUp;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.KeyUp -= HandelKeyUp;
            base.OnDetaching();
        }

        private void HandelKeyUp(object sender, KeyEventArgs e)
        {
            bool isAlphaNumeric = (e.Key >= Key.A && e.Key <= Key.Z) || (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isAltOrCtrl = (e.KeyboardDevice.Modifiers & (~ModifierKeys.Shift)) != ModifierKeys.None;
            if (isAlphaNumeric && !isAltOrCtrl)
                OpenDropDown();
            if (e.Key == Key.Escape || e.Key == Key.Enter)
                AssociatedObject.IsDropDownOpen = false;
        }

        private void OpenDropDown()
        {
            if (!AssociatedObject.IsDropDownOpen)
            {
                var rootElement = VisualTreeHelper.GetChild(AssociatedObject, 0) as FrameworkElement;
                TextBox? textBox = rootElement?.FindName("PART_EditableTextBox") as TextBox;
                int? caretIndex = null;
                if (textBox != null)
                    caretIndex = textBox.CaretIndex;
                AssociatedObject.IsDropDownOpen = true;
                if (textBox != null && caretIndex.HasValue)
                    textBox.Select(caretIndex.Value, textBox.Text.Length - caretIndex.Value);
            }
        }
    }
}