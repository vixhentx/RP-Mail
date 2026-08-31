using Avalonia;
using RPMailCore.Models;

namespace RPMailUI.Models;

public static class MailTaskStatusUiExtensions
{
    extension(MailTaskStatus status)
    {
        public string Text =>
            Application.Current is { } app
            && app.TryGetResource($"{status.Key}.Text", app.ActualThemeVariant, out var value)
            && value is string text
                ? text
                : status.ToString();
    }
}
