using System;

namespace RPMailUI.Contracts.ViewModels;

public interface IMainWindowViewModel : IDisposable
{
    IWorkspaceConfigViewModel Workspace { get; }
    IContentSettingsViewModel Content { get; }
    ISenderSettingsViewModel Sender { get; }
    IConvertSettingsViewModel Convert { get; }
    IMailRunViewModel Run { get; }
    IErrorViewModel Error { get; }
}
