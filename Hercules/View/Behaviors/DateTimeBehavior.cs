using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using Telerik.Windows.Controls;

namespace Hercules.Controls
{
    public class DateTimeBehavior : Behavior<RadDateTimePicker>
    {
        public static readonly DependencyProperty TimeZoneProperty = DependencyProperty.Register(nameof(TimeZone), typeof(TimeZoneInfo), typeof(DateTimeBehavior), new PropertyMetadata(OnUtcValueChanged));
        public static readonly DependencyProperty UtcValueProperty = DependencyProperty.Register(nameof(UtcValue), typeof(DateTime?), typeof(DateTimeBehavior), new PropertyMetadata(OnUtcValueChanged));

        private static void OnUtcValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimeBehavior target = (DateTimeBehavior)d;
            target.UpdateValue();
        }

        private void UpdateValue()
        {
            if (AssociatedObject == null)
                return;
            var utcValue = UtcValue;
            var timeZone = TimeZone;
            AssociatedObject.SelectedValue = utcValue.HasValue && timeZone != null ? TimeZoneInfo.ConvertTimeFromUtc(utcValue.Value, timeZone) : utcValue;
        }

        public TimeZoneInfo? TimeZone
        {
            get => (TimeZoneInfo?)GetValue(TimeZoneProperty);
            set => SetValue(TimeZoneProperty, value);
        }

        public DateTime? UtcValue
        {
            get => (DateTime?)GetValue(UtcValueProperty);
            set => SetValue(UtcValueProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.DropDownOpened += AssociatedObject_DropDownOpened;
            AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
            UpdateValue();
        }

        private void AssociatedObject_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var datetime = AssociatedObject.SelectedValue;
            var timeZone = TimeZone;
            UtcValue = datetime.HasValue && timeZone != null ? TimeZoneInfo.ConvertTimeToUtc(datetime.Value, timeZone) : datetime;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DropDownOpened -= AssociatedObject_DropDownOpened;
            AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
        }

        private void AssociatedObject_DropDownOpened(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Focus();
        }
    }
}