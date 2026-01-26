using Hercules.Forms.Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace Hercules.Forms.Presentation
{
    public abstract class NumberEditorBehavior<T, TElement> : VirtualRowItemBehavior where T : struct where TElement : SimpleElement<T?>
    {
        private TextBox? textBox;
        private object? undoRedoGroup;
        private T originalValue;
        private bool isChangingValue;

        public TElement Element { get; }

        protected abstract T DefaultValue { get; }

        protected NumberEditorBehavior(VirtualRowItem item, TElement element) : base(item)
        {
            Element = element;
        }

        public override void OnDeselect()
        {
            if (textBox == null)
                return;
            textBox.TextChanged -= TextBox_TextChanged;
            textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            textBox.PreviewTextInput -= TextBox_PreviewTextInput;
            Element.PropertyChanged -= Element_PropertyChanged;
            textBox = null;
        }

        public override void OnSelect()
        {
            undoRedoGroup = new();
            originalValue = Element.Value ?? DefaultValue;
            textBox = VisualTreeHelperEx.GetDescendant<TextBox>(Item.Editor!)!;
            textBox.Text = FormatValue(Element.Value ?? DefaultValue);
            textBox.TextChanged += TextBox_TextChanged;
            textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            textBox.PreviewTextInput += TextBox_PreviewTextInput;
            Element.PropertyChanged += Element_PropertyChanged;
            if (Element.Value == null)
            {
                SetElementValue(DefaultValue);
            }
        }

        private void Element_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.Assert(textBox != null);

            if (e.PropertyName == nameof(SimpleElement<T>.Value) && !isChangingValue)
            {
                textBox.Text = FormatValue(Element.Value ?? DefaultValue);
            }
        }

        private void SetElementValue(T? value)
        {
            if (EqualityComparer<T?>.Default.Equals(value, Element.Value))
                return;

            isChangingValue = true;
            try
            {
                Element.Form.Run(t => Element.SetValue(value, t, undoRedoGroup));
            }
            finally
            {
                isChangingValue = false;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.Assert(textBox != null);
            switch (e.Key)
            {
                case Key.Escape:
                    if (Item.Popup is { IsOpen: true })
                    {
                        Item.Popup.IsOpen = false;
                    }
                    else
                    {
                        textBox.Text = FormatValue(originalValue);
                    }
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    if (textBox.Text.Contains("-", StringComparison.Ordinal))
                    {
                        textBox.Text = textBox.Text.Replace("-", "", StringComparison.Ordinal);
                        textBox.CaretIndex = 0;
                    }
                    else
                    {
                        textBox.Text = "-" + textBox.Text.Replace("+", "", StringComparison.Ordinal);
                        textBox.Select(1, textBox.Text.Length - 1);
                    }
                    e.Handled = true;
                    break;
                case Key.Up:
                    {
                        if (TryParseValue(textBox.Text, out var value))
                        {
                            var nextValue = NextValue(value);
                            if (!EqualityComparer<T>.Default.Equals(value, nextValue))
                            {
                                textBox.Text = FormatValue(nextValue);
                                textBox.SelectAll();
                            }
                        }
                        e.Handled = true;
                    }
                    break;
                case Key.Down:
                    {
                        if (TryParseValue(textBox.Text, out var value))
                        {
                            var prevValue = PrevValue(value);
                            if (!EqualityComparer<T>.Default.Equals(value, prevValue))
                            {
                                textBox.Text = FormatValue(prevValue);
                                textBox.SelectAll();
                            }
                        }
                        e.Handled = true;
                    }
                    break;
                case Key.OemPlus:
                    if (Keyboard.Modifiers == ModifierKeys.None)
                    {
                        if (Item.Popup == null)
                            Item.CreatePopup(false).IsOpen = true;
                        else
                            Item.Popup.IsOpen = true;
                        FocusPopup();
                        Item.Popup!.Dispatcher.BeginInvoke(FocusPopup, DispatcherPriority.Input);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void FocusPopup()
        {
            if (Item.Popup != null)
            {
                var calculator = VisualTreeHelperEx.GetDescendant<RadCalculator>(Item.Popup.Child);
                if (calculator != null)
                {
                    Keyboard.Focus(calculator);
                    calculator!.Focus();
                }
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Text.All(IsAllowedChar))
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            T? value = null;
            if (TryParseValue(textBox!.Text, out var val))
            {
                value = val;
            }

            if (!EqualityComparer<T?>.Default.Equals(Element.Value, value))
            {
                SetElementValue(value);
            }
        }

        protected abstract bool IsAllowedChar(char c);
        protected abstract bool TryParseValue(string text, out T value);

        protected abstract string FormatValue(T value);

        protected abstract T NextValue(T value);
        protected abstract T PrevValue(T value);
    }
}