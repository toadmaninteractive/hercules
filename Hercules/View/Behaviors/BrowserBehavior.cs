using System;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Controls
{
    public static class BrowserBehavior
    {
        public static readonly DependencyProperty BindableSourceProperty = DependencyProperty.RegisterAttached("BindableSource", typeof(Uri), typeof(BrowserBehavior), new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static string GetBindableSource(DependencyObject d)
        {
            return (string)d.GetValue(BindableSourceProperty);
        }

        public static void SetBindableSource(DependencyObject d, string value)
        {
            d.SetValue(BindableSourceProperty, value);
        }

        public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is WebBrowser browser)
            {
                var uri = e.NewValue as Uri;
                browser.Source = uri;
            }
        }
    }
}
