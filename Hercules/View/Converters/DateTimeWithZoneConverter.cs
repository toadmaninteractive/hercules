using Hercules.Forms.Schema;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class DateTimeWithZoneConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is DateTime? && values[1] is TimeZoneInfo)
            {
                DateTime? dateTime = (DateTime?)values[0];
                TimeZoneInfo? timeZone = (TimeZoneInfo?)values[1];
                DateTime dt = timeZone != null ? TimeZoneInfo.ConvertTimeFromUtc(dateTime.Value, timeZone) : dateTime.Value;
                return dt.ToString(DateTimeSchemaType.Culture);
            }
            else
            {
                return string.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
