using R3;
using RPMailCore.Models;

namespace RPMailUI.ViewModels;

public sealed class ContentSettingsViewModel : IDisposable
{
    readonly DisposableBag _d = new();


	// 暴露给View
    public ReactiveCommand ImportCommand { get; } = new();
    public ReactiveCommand ExportCommand { get; } = new();

    public BindableReactiveProperty<string> CsvPath { get; } = new("");
    public BindableReactiveProperty<string> BodyHtmlPath { get; } = new("");
    public BindableReactiveProperty<string> Subject { get; } = new("");
    public BindableReactiveProperty<string> CharSet { get; } = new("utf-8");

    public AttachmentListViewModel Attachments { get; } = new();
    public ExtraAttributeListViewModel ExtraAttributes { get; } = new();

	// 暴露给上机
	
	public Observable<TemplateConfig> ConfOut { get; }

    public ContentSettingsViewModel()
    {
		ConfOut = Observable.CombineLatest(
			CsvPath,
			BodyHtmlPath,
			Subject,
			CharSet,
			Attachments.ConfOut,
			ExtraAttributes.ConfOut,
			static (csv,body,subject,charset,attachments,extraAttributes) => new TemplateConfig()
			{
				CsvPath = csv,
				BodyHtmlPath = body,
				Subject = subject,
				CharSet = charset,
				Attachments = attachments,
				ExtraAttributes = extraAttributes
			}
		);
		// TODO: 模块化导入导出
    }



    public void Dispose()
    {
        Attachments.Dispose();
        ExtraAttributes.Dispose();

        _d.Dispose();
        ImportCommand.Dispose();
        ExportCommand.Dispose();
        CsvPath.Dispose();
        BodyHtmlPath.Dispose();
        Subject.Dispose();
        CharSet.Dispose();
    }
}
