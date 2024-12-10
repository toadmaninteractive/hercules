using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class AppTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var connectionTitle = value as string;
            if (string.IsNullOrEmpty(connectionTitle))
                return "Hercules";
            else
                return connectionTitle + " - Hercules";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
