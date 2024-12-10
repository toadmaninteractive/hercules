using Hercules.Forms.Elements;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Hercules.Forms.Presentation
{
    public class AutoCompleteBehavior : VirtualRowItemBehavior
    {
        public IAutoCompleteElement Element { get; }

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.RegisterAttached("Items", typeof(ICollectionView), typeof(AutoCompleteBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ICollectionView? GetItems(DependencyObject d) => (ICollectionView)d.GetValue(ItemsProperty);
        public static void SetItems(DependencyObject d, ICollectionView? value) => d.SetValue(ItemsProperty, value);

        public static readonly DependencyProperty ToggleDropDownCommandProperty = DependencyProperty.RegisterAttached(nameof(ToggleDropDownCommand), typeof(ICommand), typeof(AutoCompleteBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ICommand? GetToggleDropDownCommand(DependencyObject d) => (ICommand)d.GetValue(ToggleDropDownCommandProperty);
        public static void SetToggleDropDownCommand(DependencyObject d, ICommand? value) => d.SetValue(ToggleDropDownCommandProperty, value);

        public static readonly DependencyProperty SubmitCommandProperty = DependencyProperty.RegisterAttached(nameof(SubmitCommand), typeof(ICommand), typeof(AutoCompleteBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ICommand? GetSubmitCommand(DependencyObject d) => (ICommand)d.GetValue(SubmitCommandProperty);
        public static void SetSubmitCommand(DependencyObject d, ICommand? value) => d.SetValue(SubmitCommandProperty, value);

        public AutoCompleteBehavior(VirtualRowItem item, IAutoCompleteElement element) : base(item)
        {
            Element = element;
            SubmitCommand = Commands.Execute<object?>(Submit);
            ToggleDropDownCommand = Commands.Execute(ToggleDropDown);

            itemsSource = new CollectionViewSource { Source = element.Items };
        }

        public ICommand<object?> SubmitCommand { get; }
        public ICommand? ToggleDropDownCommand { get; }
        private readonly CollectionViewSource itemsSource;
        private string? originalText;

        private TextBox? textBox;

        public void Submit(object? suggestion)
        {
            Element.Submit(suggestion);
            if (Item.Popup != null)
            {
                Item.Popup.IsOpen = false;
            }
            Item.Element.Form.SealUndo();
        }

        public override void OnSelect()
        {
            textBox = VisualTreeHelperEx.GetDescendant<TextBox>(Item.Editor);
            textBox.TextChanged += AssociatedObject_TextChanged;
            textBox.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            originalText = Element.Value ?? "";
        }

        public override void OnDeselect()
        {
            textBox.TextChanged -= AssociatedObject_TextChanged;
            textBox.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            textBox = null;
        }

        public override void OnCreateVisual(FrameworkElement view)
        {
            SetItems(view, itemsSource.View);
            SetToggleDropDownCommand(view, ToggleDropDownCommand);
            SetSubmitCommand(view, SubmitCommand);
        }

        public override void OnDisposeVisual(FrameworkElement view)
        {
            view.ClearValue(ItemsProperty);
            view.ClearValue(ToggleDropDownCommandProperty);
            view.ClearValue(SubmitCommandProperty);
        }

        private void ToggleDropDown()
        {
            var popup = Item.CreatePopup(true);
            if (!popup.IsOpen)
            {
                itemsSource.View.Filter = null;
            }

            popup.IsOpen = !popup.IsOpen;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    {
                        var popup = GetPopup(true);
                        if (popup != null)
                        {
                            if (popup.IsOpen)
                            {
                                var listBox = VisualTreeHelperEx.GetDescendant<ListBox>(popup.Child);
                                if (listBox != null && listBox.SelectedIndex < listBox.Items.Count - 1)
                                {
                                    listBox.SelectedIndex++;
                                    listBox.ScrollIntoView(listBox.SelectedItem);
                                }
                                e.Handled = true;
                            }
                            else
                            {
                                popup.IsOpen = true;
                            }
                        }
                    }
                    break;
                case Key.Up:
                    {
                        var popup = GetPopup(false);
                        if (popup is { IsOpen: true })
                        {
                            var listBox = VisualTreeHelperEx.GetDescendant<ListBox>(popup.Child);
                            if (listBox is { SelectedIndex: > 0 })
                            {
                                listBox.SelectedIndex--;
                                listBox.ScrollIntoView(listBox.SelectedItem);
                            }
                            e.Handled = true;
                        }
                    }
                    break;
                case Key.Return:
                    {
                        var popup = GetPopup(false);
                        if (popup != null)
                        {
                            var listBox = VisualTreeHelperEx.GetDescendant<ListBox>(popup.Child);
                            if (popup.IsOpen && listBox is { SelectedIndex: >= 0 })
                            {
                                Submit(listBox.SelectedItem);
                                popup.IsOpen = false;
                            }
                            else
                            {
                                Submit(null);
                            }
                        }
                    }
                    break;
                case Key.Escape:
                    if (originalText != null)
                    {
                        Submit(originalText);
                    }
                    break;
            }
        }

        private void AssociatedObject_TextChanged(object sender, TextChangedEventArgs e)
        {
            var popup = GetPopup(true);
            if (popup != null)
            {
                itemsSource.View.Filter = Element.Filter;
                itemsSource.View.Refresh();
                popup.IsOpen = true;
            }
        }

        private Popup? GetPopup(bool create)
        {
            return create ? Item?.CreatePopup(true) : Item?.Popup;
        }
    }
}
