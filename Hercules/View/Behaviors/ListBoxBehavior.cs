using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Controls
{
    public class ListBoxBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
           "SelectedItems",
           typeof(IList),
           typeof(ListBoxBehavior));

        public IList SelectedItems
        {
            get => AssociatedObject.SelectedItems;
            set => throw new InvalidOperationException();
        }

        protected override void OnAttached()
        {
            SetCurrentValue(SelectedItemsProperty, AssociatedObject.SelectedItems);
        }

        protected override void OnDetaching()
        {
            SetCurrentValue(SelectedItemsProperty, null);
        }
    }
}
