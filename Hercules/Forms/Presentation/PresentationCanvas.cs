using Hercules.Controls;
using Hercules.Forms.Elements;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace Hercules.Forms.Presentation
{
    public class PresentationCanvas : VirtualCanvas
    {
        public static readonly DependencyProperty PresentationProperty =
            DependencyProperty.Register(nameof(Presentation), typeof(FormPresentation), typeof(PresentationCanvas), new FrameworkPropertyMetadata(null, OnPresentationChanged));

        public FormPresentation Presentation
        {
            get => (FormPresentation)GetValue(PresentationProperty);
            set => SetValue(PresentationProperty, value);
        }

        private static void OnPresentationChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var canvas = (PresentationCanvas)sender;
            if (args.OldValue != null)
                canvas.DisconnectFromPresentation((FormPresentation)args.OldValue);
            if (args.NewValue != null)
                canvas.ConnectToPresentation((FormPresentation)args.NewValue);
        }

        private void ConnectToPresentation(FormPresentation presentation)
        {
            presentation.OnRefreshItems += Presentation_OnRefreshItems;
            presentation.OnScrollIntoView += Presentation_OnScrollIntoView;
            presentation.OnFocusItem += Presentation_OnFocusItem;
            RefreshVisuals(presentation.FirstRow);
            if (presentation.SelectedItem != null)
            {
                ScrollIntoView(presentation.SelectedItem.Row);
                presentation.SelectedItem.Select();
            }
        }

        private void DisconnectFromPresentation(FormPresentation presentation)
        {
            presentation.OnRefreshItems -= Presentation_OnRefreshItems;
            presentation.OnScrollIntoView -= Presentation_OnScrollIntoView;
            presentation.OnFocusItem -= Presentation_OnFocusItem;
        }

        private void Presentation_OnFocusItem(VirtualRowItem item)
        {
            if (VisualTreeHelperEx.GetInputElement(item.FocusView) is { } inputElement)
                SetFocus(inputElement);
        }

        private void Presentation_OnScrollIntoView(VirtualRow row)
        {
            ScrollIntoView(row);
        }

        private void Presentation_OnRefreshItems()
        {
            RefreshVisuals(Presentation.FirstRow);
        }

        private static void SetFocus(IInputElement inputElement)
        {
            if (inputElement is UIElement uiElement)
            {
                inputElement.Focus();

                if (uiElement is TextBox textBox)
                {
                    textBox.SelectAll();
                    // return;
                }

                if (uiElement is RadNumericUpDown numericUpDown)
                {
                    numericUpDown.SelectAll();
                    // return;
                }

                if (uiElement is RadComboBox selector)
                {
                    var PART_textBox = (TextBox)selector.Template.FindName("PART_EditableTextBox", selector);
                    PART_textBox?.Focus();
                    PART_textBox?.SelectAll();
                    return;
                }

                if (uiElement is ComboBox combobox)
                {
                    combobox.IsDropDownOpen = true;
                }

                var dependencyObject = (DependencyObject)inputElement;

                Keyboard.Focus(inputElement);
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(dependencyObject), inputElement);
                dependencyObject.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => FocusManager.SetFocusedElement(FocusManager.GetFocusScope(dependencyObject), inputElement)));
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Subtract || e.Key == Key.Add)
            {
                if (e.OriginalSource is TextBox textBox && VirtualRowItem.GetItem(textBox) is not { Element: IntElement or FloatElement })
                {
                    textBox.SelectedText = e.Key == Key.Subtract ? "-" : "+";
                    textBox.CaretIndex += textBox.SelectedText.Length;
                    textBox.SelectionLength = 0;
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    Presentation.PrevTab();
                    e.Handled = true;
                }
                else if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    Presentation.NextTab();
                    e.Handled = true;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            var frameworkElement = e.OriginalSource as FrameworkElement;
            if (frameworkElement == null)
                return;
            var item = VirtualRowItem.GetItem(frameworkElement);
            if (item != null && item.IsSelectable)
                Presentation.Select(item);
            else
                Presentation.ClearSelection();
            // var el = GetElementForControl(frameworkElement, sender);
            // if (el is MultiSelectElement)
            //     return;
            // el?.Select();
            // if (!frameworkElement.Focusable)
            //     FocusManager.SetFocusedElement(FocusManager.GetFocusScope(frameworkElement), null);

            base.OnPreviewMouseDown(e);
        }
    }
}
