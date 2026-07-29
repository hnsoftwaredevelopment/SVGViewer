using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SVGViewer.Converters;

/// <summary>
/// Two-way converter for binding a group of toggle/radio buttons to an enum
/// property. <c>IsChecked</c> is true when the bound value equals the button's
/// <c>ConverterParameter</c>; checking a button writes that value back.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value.Equals(parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is not null ? parameter : Binding.DoNothing;
}
