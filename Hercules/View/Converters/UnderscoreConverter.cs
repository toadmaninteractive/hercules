using System;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class UnderscoreConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = (string)value;
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            else
                return "_" + str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
