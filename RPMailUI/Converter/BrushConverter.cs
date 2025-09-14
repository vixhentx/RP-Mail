using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RPMailUI.Converter;

public class BrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color color? new SolidColorBrush(color) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}