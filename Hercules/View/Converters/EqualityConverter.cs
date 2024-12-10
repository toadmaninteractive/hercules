using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class EqualityConverter : IValueConverter
    {
        public object TrueValue { get; private set; }
        public object FalseValue { get; private set; }

        public EqualityConverter()
            : this(true, false)
        {
        }

        public EqualityConverter(object trueValue, object falseValue)
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result;
            if (value is int && parameter is string s)
                result = value.ToString() == s;
            else if (value is char && parameter is string chars)
                result = value.ToString() == chars;
            else
                result = Equals(value, parameter);
            return result ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
