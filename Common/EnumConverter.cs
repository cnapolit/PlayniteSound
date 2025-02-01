using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace PlayniteSounds.Common;

public class EnumConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Enum.ToObject(targetType, value);
}