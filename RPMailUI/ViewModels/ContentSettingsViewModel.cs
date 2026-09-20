using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class ContentSettingsViewModel : IDisposable
{
	readonly DisposableBag _d = new();
	bool _synching = false;

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

		// 配置传入
		conf.Pipe
			.DistinctUntilChangedBy(static p => p.Value.Template)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Select(static p => p.Value.Template)
			.Subscribe(template =>
			{
				_synching = true;

				CsvPath.Value = template.CsvPath;
				BodyHtmlPath.Value = template.BodyHtmlPath;
				Subject.Value = template.Subject;
				CharSet.Value = template.CharSet;

				_synching = false;
			})
			.AddTo(ref _d);

		// 用户编辑产生配置
		Observable.CombineLatest(
				CsvPath,
				BodyHtmlPath,
				Subject,
				CharSet,
				static (csv, body, subject, charset) =>
					new
					{
						CsvPath = csv,
						BodyHtmlPath = body,
						Subject = subject,
						CharSet = charset
					}
			)
			.Where(_ => !_synching)
			.WithLatestFrom(
				conf.Pipe,
				static (content, pipe) => new ConfPipe(
					pipe.Value with
					{
						Template = pipe.Value.Template with
						{
							CsvPath = content.CsvPath,
							BodyHtmlPath = content.BodyHtmlPath,
							Subject = content.Subject,
							CharSet = content.CharSet
						}
					},
					ConfChangingSource.UserEdit
				)
			)
			.Where(static p => p.Source is ConfChangingSource.UserEdit)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		// 配置导入
		ImportCommand
			.SelectAwait((_, _) => fileDialog.ReadConf(RPMailJsonContext.Default.TemplateConfig))
			.WhereNotNull()
			.WithLatestFrom(
				conf.Pipe,
				static (template, pipe) => new ConfPipe(
					pipe.Value with
					{
						Template = template
					},
					ConfChangingSource.Import
				)
			)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value.Template)
			.SubscribeAwait((template, _) =>
				fileDialog.WriteConf(
					template,
					RPMailJsonContext.Default.TemplateConfig,
					"content_template_module.json"
				)
			)
			.AddTo(ref _d);
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
