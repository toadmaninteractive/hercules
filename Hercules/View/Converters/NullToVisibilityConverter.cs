using System;
using System.Windows;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // reverse conversion (false=>Visible, true=>collapsed) on any given parameter
            bool input = (null == parameter) ? value != null : value == null;
            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion IValueConverter Members
    }
}
