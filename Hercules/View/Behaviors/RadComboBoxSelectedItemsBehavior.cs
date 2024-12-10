using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using Telerik.Windows.Controls;

namespace Hercules.Controls
{
    public class RadComboBoxSelectedItemsBehavior : Behavior<RadComboBox>
    {
        private RadComboBox ComboBox => AssociatedObject;

        public INotifyCollectionChanged SelectedItems
        {
            get => (INotifyCollectionChanged)this.GetValue(SelectedItemsProperty);
            set => this.SetValue(SelectedItemsProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedItemsProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(INotifyCollectionChanged), typeof(RadComboBoxSelectedItemsBehavior), new PropertyMetadata(OnSelectedItemsPropertyChanged));

        private static void OnSelectedItemsPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue is INotifyCollectionChanged collection)
            {
                ((RadComboBoxSelectedItemsBehavior)target).UpdateTransfer(args.NewValue);
                collection.CollectionChanged += ((RadComboBoxSelectedItemsBehavior)target).ContextSelectedItems_CollectionChanged;
            }
        }

        private void UpdateTransfer(object items)
        {
            if (ComboBox != null)
            {
                Transfer(items as IList, this.ComboBox.SelectedItems);
                this.ComboBox.SelectionChanged += this.ComboSelectionChanged;
            }
        }

        private void ContextSelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UnsubscribeFromEvents();
            Transfer(SelectedItems as IList, this.ComboBox.SelectedItems);
            this.SubscribeToEvents();
        }

        private void ComboSelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.UnsubscribeFromEvents();
            Transfer(this.ComboBox.SelectedItems, SelectedItems as IList);
            this.SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            this.ComboBox.SelectionChanged += this.ComboSelectionChanged;
            if (this.SelectedItems != null)
            {
                this.SelectedItems.CollectionChanged += this.ContextSelectedItems_CollectionChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            this.ComboBox.SelectionChanged -= this.ComboSelectionChanged;
            if (this.SelectedItems != null)
            {
                this.SelectedItems.CollectionChanged -= this.ContextSelectedItems_CollectionChanged;
            }
        }

        public static void Transfer(IList? source, IList? target)
        {
            if (source == null || target == null)
                return;

            target.Clear();
            foreach (var o in source)
            {
                target.Add(o);
            }
        }
    }
}