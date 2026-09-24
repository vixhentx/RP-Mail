using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace RPMailUI.Converter;

public class ResourceColorConverter : IValueConverter
{
	public static FuncValueConverter<string, ThemeVariant, Color> Instance { get; } = new(
		static (v,p) =>
			v is {} && Application.Current is { ActualThemeVariant: {} theme } app
			&& app.TryGetResource(v, theme, out var resource)
			&& resource is Color color
				? color
				: Colors.Transparent
	);
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Instance.Convert(value, targetType, parameter, culture);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Instance.ConvertBack(value,targetType,parameter,culture);
}
