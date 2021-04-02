using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;

namespace Genius.PriceChecker.UI.ValueConverters
{
    public class DateTimeToHumanizedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime dt))
            {
                return null;
            }
            return dt.Humanize(false);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}