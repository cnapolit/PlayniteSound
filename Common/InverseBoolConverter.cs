using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace PlayniteSounds.Common;

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is false;
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
