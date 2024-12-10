using System;
using System.Windows;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var val = (int)value;
            var inverted = parameter != null;
            if (val == 0)
                return inverted ? Visibility.Visible : Visibility.Collapsed;
            else
                return inverted ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion IValueConverter Members
    }
}
