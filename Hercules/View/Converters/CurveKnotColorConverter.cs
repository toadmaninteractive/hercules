using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Hercules.Converters
{
    public class CurveKnotColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSelected = (bool)value;
            var color = isSelected ? Color.FromRgb(254, 0, 0) : Color.FromRgb(0, 0, 254);
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}