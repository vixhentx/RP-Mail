using Avalonia.Media;
using RPMailUI.ViewModels;

namespace RPMailUI.Services;

public static class MessageFlyout
{
    private static MainWindowViewModel? _vm;

    public static void Initialize(MainWindowViewModel vm) =>
        _vm = vm;
    public static void ShowMessage(string caption, string message, Color? foregroundColor = null, Color? backgroundColor = null)
    {
        _vm?.Errors.Add(new($"{caption}: {message}", foregroundColor, backgroundColor));
    }

    public static void ShowError(string message, Color? foregroundColor = null, Color? backgroundColor = null)
    {
        ShowMessage("Error", message, foregroundColor, backgroundColor);
    }

    public static void ShowInfo(string message, Color? foregroundColor = null, Color? backgroundColor = null)
    {
        ShowMessage("Info", message, foregroundColor, backgroundColor??Colors.DarkCyan);
    }
}