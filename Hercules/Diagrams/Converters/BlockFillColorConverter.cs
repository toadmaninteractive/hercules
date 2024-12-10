using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Hercules.Diagrams
{
    public class BlockFillColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            BlockBase block = (BlockBase)value;

            if (block.Prototype.Color.HasValue)
                return new SolidColorBrush(block.Prototype.Color.Value);

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5B7199"));
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}