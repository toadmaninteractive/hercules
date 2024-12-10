using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Hercules.Converters
{
    [ContentProperty("Items")]
    public class EnumToObjectConverter : IValueConverter
    {
        public ResourceDictionary? Items { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? key = Enum.GetName(value.GetType(), value);
            return Items?[key];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("This converter only works for one way binding");
        }
    }
}
