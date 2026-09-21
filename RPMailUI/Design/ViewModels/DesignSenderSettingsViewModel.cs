using R3;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignSenderSettingsViewModel : ISenderSettingsViewModel
{
    public ReactiveCommand ImportCommand { get; } = new();
    public ReactiveCommand ExportCommand { get; } = new();
    public BindableReactiveProperty<string> SenderEmail { get; } =
        new("sender@example.com");
    public BindableReactiveProperty<string> SenderPassword { get; } =
        new("design-password");
    public BindableReactiveProperty<string> SmtpHost { get; } =
        new("smtp.example.com");

    public void Dispose()
    {
        ImportCommand.Dispose();
        ExportCommand.Dispose();
        SenderEmail.Dispose();
        SenderPassword.Dispose();
        SmtpHost.Dispose();
    }
}
