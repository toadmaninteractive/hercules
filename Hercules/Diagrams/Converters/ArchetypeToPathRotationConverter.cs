using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Diagrams
{
    public class ArchetypeToPathRotationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((ArchetypeType)value)
            {
                case ArchetypeType.Hexagon:
                case ArchetypeType.LiteHexagon:
                case ArchetypeType.PentaLeft:
                    return 90d;
                case ArchetypeType.PentaUp:
                    return 180d;
                case ArchetypeType.PentaRight:
                    return 270d;
                default:
                    return 0d;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}