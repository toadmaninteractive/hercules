using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Diagrams
{
    public class ArchetypeToBottomMargin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ArchetypeType)value) switch
            {
                ArchetypeType.Hexagon => 20d,
                _ => 10d,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}