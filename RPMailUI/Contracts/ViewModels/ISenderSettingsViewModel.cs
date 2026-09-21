using System;
using R3;

namespace RPMailUI.Contracts.ViewModels;

public interface ISenderSettingsViewModel : IDisposable
{
    ReactiveCommand ImportCommand { get; }
    ReactiveCommand ExportCommand { get; }
    BindableReactiveProperty<string> SenderEmail { get; }
    BindableReactiveProperty<string> SenderPassword { get; }
    BindableReactiveProperty<string> SmtpHost { get; }
}
