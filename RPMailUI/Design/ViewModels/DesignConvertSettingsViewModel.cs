using R3;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignConvertSettingsViewModel : IConvertSettingsViewModel
{
    public ReactiveCommand ImportCommand { get; } = new();
    public ReactiveCommand ExportCommand { get; } = new();
    public BindableReactiveProperty<string> OutputDir { get; } =
        new("Output");
    public BindableReactiveProperty<bool> DeleteAfterSent { get; } = new(true);
    public BindableReactiveProperty<bool> ConvertOnly { get; } = new(false);
    public BindableReactiveProperty<bool> SaveRawDocs { get; } = new(true);
    public BindableReactiveProperty<bool> SaveHtmlFile { get; } = new(true);

	public IReadOnlyBindableReactiveProperty<string> WorkspaceDirectory { get; } =
		new BindableReactiveProperty<string>("/tmp/RPMail");

    public void Dispose()
    {
        ImportCommand.Dispose();
        ExportCommand.Dispose();
		WorkspaceDirectory.Dispose();
        OutputDir.Dispose();
        DeleteAfterSent.Dispose();
        ConvertOnly.Dispose();
        SaveRawDocs.Dispose();
        SaveHtmlFile.Dispose();
    }
}
