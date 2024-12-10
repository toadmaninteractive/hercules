using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] sizes = { "b", "Kb", "Mb", "Gb", "Tb" };
            long len = (long)value;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = string.Format(CultureInfo.InvariantCulture, "{0:0.##}{1}", len, sizes[order]);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
