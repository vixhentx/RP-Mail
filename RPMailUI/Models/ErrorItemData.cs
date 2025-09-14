using Avalonia.Media;

namespace RPMailUI.Models;

public class ErrorItemData(string message, Color? foregroundColor = null, Color? backgroundColor = null)
{
    public string Message { get; set; } = message;
    public Color ForegroundColor { get; set; } = foregroundColor??Colors.LightYellow;
    public Color BackgroundColor { get; set; } = backgroundColor??Colors.Crimson;
}