using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class HorizontalToTextAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value switch
            {
                0 => TextAlignment.Left,
                1 => TextAlignment.Center,
                2 => TextAlignment.Right,
                _ => TextAlignment.Justify
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value switch
            {
                0 => HorizontalAlignment.Left,
                1 => HorizontalAlignment.Right,
                2 => HorizontalAlignment.Center,
                _ => HorizontalAlignment.Stretch
            };
        }
    }
}
