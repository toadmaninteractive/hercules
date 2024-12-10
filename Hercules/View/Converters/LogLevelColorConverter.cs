using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Hercules.Converters
{
    public class LogLevelColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (LogLevel)value;
            return level switch
            {
                LogLevel.Debug => Brushes.Gray,
                LogLevel.Error => Brushes.Red,
                LogLevel.Warning => Brushes.DarkGoldenrod,
                LogLevel.User => Brushes.Green,
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
