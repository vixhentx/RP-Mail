using R3;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignWorkspaceConfigViewModel : IWorkspaceConfigViewModel
{
    public BindableReactiveProperty<string> WorkspaceDirectory { get; } =
        new("/home/example/RPMail");

    public ReactiveCommand BrowseWorkspaceCommand { get; } = new();
    public ReactiveCommand ImportMailConfigCommand { get; } = new();
    public ReactiveCommand ExportMailConfigCommand { get; } = new();

    public void Dispose()
    {
        WorkspaceDirectory.Dispose();
        BrowseWorkspaceCommand.Dispose();
        ImportMailConfigCommand.Dispose();
        ExportMailConfigCommand.Dispose();
    }
}
