using System;
using System.Globalization;
using System.Windows.Data;

using Humanizer;

namespace MediaDownloader.UI.Converters;

[ValueConversion(typeof(DateTime), typeof(string))]
public class HumanizedDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var dateTime = (DateTime?)value;
        return dateTime?.ToUniversalTime().Humanize();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("HumanizedDateTimeConverter: reverse-conversion not supported.");
    }
}