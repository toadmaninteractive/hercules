using Hercules.Shell;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Hercules.Converters
{
    public class DialogButtonCaptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DialogButtons))
                return string.Empty;

            var button = (DialogButtons)value;
            return button switch
            {
                DialogButtons.Ok => "OK",
                DialogButtons.Cancel => "Cancel",
                DialogButtons.Yes => "Yes",
                DialogButtons.YesToAll => "Yes to All",
                DialogButtons.No => "No",
                DialogButtons.NoToAll => "No to All",
                DialogButtons.Close => "Close",
                DialogButtons.Reset => "Reset",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
