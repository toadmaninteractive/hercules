using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class InheritedOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 0.5 : 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static readonly InheritedOpacityConverter Default = new();
    }
}
