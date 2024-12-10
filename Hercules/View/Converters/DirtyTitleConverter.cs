using System;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class DirtyTitleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var title = (string)values[0];
            var isDirty = (bool)values[1];
            if (isDirty)
                return title + " *";
            else
                return title;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
