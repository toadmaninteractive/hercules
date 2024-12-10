using AvalonDock.Themes;
using Hercules.Shell;
using System;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class ThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var theme = (VisualTheme)value;
            return theme switch
            {
                VisualTheme.Aero => new AeroTheme(),
                VisualTheme.Metro => new MetroTheme(),
                VisualTheme.VS2010 => new VS2010Theme(),
                _ => new GenericTheme()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
