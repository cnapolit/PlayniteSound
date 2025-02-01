using System;
using System.Globalization;
using System.Windows.Data;

namespace PlayniteSounds.Common;

public class TimeSpanToDoubleConverter : IValueConverter
{
    object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var timeSpan = (TimeSpan)value;
        return timeSpan.TotalMilliseconds;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => TimeSpan.FromMilliseconds((double)value);
}