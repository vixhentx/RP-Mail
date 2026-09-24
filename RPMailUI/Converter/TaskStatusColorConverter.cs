using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using RPMailCore.Models;

namespace RPMailUI.Converter;

public class TaskStatusColorConverter : IValueConverter
{
	
    public static FuncValueConverter<MailTaskStatus, Color> Instance { get; } =
        new(static status =>
        {
            if (Application.Current is { ActualThemeVariant: {} theme } app
                && app.TryGetResource(status.Key, theme, out var resource)
                && resource is Color color)
            {
                return color;
            }

            return Colors.Transparent;
        });

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Instance.Convert(value, targetType, parameter, culture);

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Instance.ConvertBack(value,targetType,parameter,culture);
}