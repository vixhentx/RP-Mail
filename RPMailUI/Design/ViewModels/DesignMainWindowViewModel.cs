using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignMainWindowViewModel : IMainWindowViewModel
{
    public IWorkspaceConfigViewModel Workspace { get; } = new DesignWorkspaceConfigViewModel();
    public IContentSettingsViewModel Content { get; } = new DesignContentSettingsViewModel();
    public ISenderSettingsViewModel Sender { get; } = new DesignSenderSettingsViewModel();
    public IConvertSettingsViewModel Convert { get; } = new DesignConvertSettingsViewModel();
    public IMailRunViewModel Run { get; } = new DesignMailRunViewModel();
    public IErrorViewModel Error { get; } = new DesignErrorViewModel();

    public void Dispose()
    {
        Workspace.Dispose();
        Content.Dispose();
        Sender.Dispose();
        Convert.Dispose();
        Run.Dispose();
        Error.Dispose();
    }
}
