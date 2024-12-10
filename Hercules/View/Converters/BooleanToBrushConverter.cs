using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Hercules.Converters
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public required Brush FalseBrush { get; set; }
        public required Brush TrueBrush { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion IValueConverter Members
    }
}
