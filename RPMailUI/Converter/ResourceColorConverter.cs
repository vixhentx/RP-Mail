using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using RPMailCore.Models;

namespace RPMailUI.Converter;

public class ResourceColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = value switch
        {
            MailTaskStatus status => status.Key,
            string s => s,
            _ => null,
        };
        if (key is null) return Colors.Transparent;
        return Application.Current is { } app
            && app.TryGetResource(key, app.ActualThemeVariant, out var resource)
            && resource is Color color
                ? color
                : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
