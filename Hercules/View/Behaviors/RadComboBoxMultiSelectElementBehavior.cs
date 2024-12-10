using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace Hercules.Controls
{
    public class RadComboBoxMultiSelectElementBehavior : Behavior<RadComboBox>
    {
        public IReadOnlyList<string>? SelectedItems
        {
            get => (IReadOnlyList<string>)this.GetValue(SelectedItemsProperty);
            set => this.SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(IReadOnlyList<string>), typeof(RadComboBoxMultiSelectElementBehavior), new PropertyMetadata(ElementChanged));

        private static void ElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (RadComboBoxMultiSelectElementBehavior)d;
            owner.SynchronizeFromSource();
        }

        protected override void OnAttached()
        {
            AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
            SynchronizeFromSource();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
        }

        private bool synchronizingFromSource;

        private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!synchronizingFromSource && AssociatedObject.DataContext != null)
                SelectedItems = AssociatedObject.SelectedItems.Cast<string>().ToList();
        }

        private void SynchronizeFromSource()
        {
            if (SelectedItems == null || AssociatedObject == null)
                return;
            synchronizingFromSource = true;
            try
            {
                AssociatedObject.SelectedItems.Clear();
                var value = SelectedItems;
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        AssociatedObject.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                synchronizingFromSource = false;
            }
        }
    }
}