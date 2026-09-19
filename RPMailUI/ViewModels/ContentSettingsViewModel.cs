using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Services;

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

    public AttachmentListViewModel Attachments { get; }
    public ExtraAttributeListViewModel ExtraAttributes { get; }

	public ContentSettingsViewModel(
		ConfigService conf,
		JsonFileDialogService fileDialog,
		AttachmentListViewModel attachments,
		ExtraAttributeListViewModel extraAttributes
	)
	{
		Attachments = attachments;
		ExtraAttributes = extraAttributes;

		// 配置产生
		var confOut = Observable.CombineLatest(
			CsvPath,
			BodyHtmlPath,
			Subject,
			CharSet,
			Attachments.ConfOut,
			ExtraAttributes.ConfOut,
			static (csv, body, subject, charset, attachments, extraAttributes) => new TemplateConfig()
			{
				CsvPath = csv,
				BodyHtmlPath = body,
				Subject = subject,
				CharSet = charset,
				Attachments = attachments,
				ExtraAttributes = extraAttributes
			}
		);
		var confImport = ImportCommand
			.SelectAwait((_, _) => fileDialog.ReadConf(RPMailJsonContext.Default.TemplateConfig))
			.WhereNotNull();

		Observable.Merge(confOut, confImport)
			.DistinctUntilChanged()
			.Subscribe(conf.Root, static (module, root) =>
				root.Value = root.Value with { Template = module }
			)
			.AddTo(ref _d);

		// 配置传入
		conf.Root
			.Select(static x => x.Template)
			.DistinctUntilChanged()
			.Subscribe(conf =>
			{
				CsvPath.Value = conf.CsvPath;
				BodyHtmlPath.Value = conf.BodyHtmlPath;
				Subject.Value = conf.Subject;
				CharSet.Value = conf.CharSet;
				Attachments.LoadItems(conf.Attachments);
				ExtraAttributes.LoadItems(conf.ExtraAttributes);
			})
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(confOut, static (_, conf) => conf)
			.SubscribeAwait((conf, _) => fileDialog.WriteConf(conf, RPMailJsonContext.Default.TemplateConfig, "content_template_module.json"))
			.AddTo(ref _d);
		Attachments = attachments;
		ExtraAttributes = extraAttributes;
	}

	public ContentSettingsViewModel(AttachmentListViewModel attachments, ExtraAttributeListViewModel extraAttributes)
	{
		Attachments = attachments;
		ExtraAttributes = extraAttributes;
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
