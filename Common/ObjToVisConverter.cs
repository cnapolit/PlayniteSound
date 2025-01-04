using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace PlayniteSounds.Common;

[ValueConversion(typeof(object), typeof(Visibility))]
internal class ObjToVisConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null ? Visibility.Visible : Visibility.Hidden;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

[ValueConversion(typeof(Visibility), typeof(bool))]
internal class VisToBoolConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
