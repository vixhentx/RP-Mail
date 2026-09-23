using R3;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignContentSettingsViewModel : IContentSettingsViewModel
{
    public ReactiveCommand ImportCommand { get; } = new();
    public ReactiveCommand ExportCommand { get; } = new();

    public BindableReactiveProperty<string> CsvPath { get; } =
        new("Data/contacts.csv");
    public BindableReactiveProperty<string> BodyHtmlPath { get; } =
        new("Templates/welcome.html");
    public BindableReactiveProperty<string> Subject { get; } =
        new("Welcome to our monthly newsletter");
    public BindableReactiveProperty<string> CharSet { get; } =
        new("utf-8");

	public IReadOnlyBindableReactiveProperty<string> WorkspaceDirectory { get; } =
		new BindableReactiveProperty<string>("/tmp/RPMail");

    public IAttachmentListViewModel Attachments { get; } =
        new DesignAttachmentListViewModel();
    public IExtraAttributeListViewModel ExtraAttributes { get; } =
        new DesignExtraAttributeListViewModel();


	public void Dispose()
    {
        ImportCommand.Dispose();
        ExportCommand.Dispose();
        CsvPath.Dispose();
        BodyHtmlPath.Dispose();
        Subject.Dispose();
        CharSet.Dispose();
		WorkspaceDirectory.Dispose();
        Attachments.Dispose();
        ExtraAttributes.Dispose();
    }
}
