using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class ActualWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value - (double)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static readonly ActualWidthConverter Default = new();
    }
}