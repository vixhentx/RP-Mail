using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class ConvertSettingsViewModel : IDisposable
{
	readonly DisposableBag _d = new();
	bool _synching = false;

	// 暴露给View
	public ReactiveCommand ImportCommand { get; } = new();
	public ReactiveCommand ExportCommand { get; } = new();

	public BindableReactiveProperty<string> OutputDir { get; } = new("Output");
	public BindableReactiveProperty<bool> DeleteAfterSent { get; } = new(false);
	public BindableReactiveProperty<bool> ConvertOnly { get; } = new(false);
	public BindableReactiveProperty<bool> SaveRawDocs { get; } = new(false);
	public BindableReactiveProperty<bool> SaveHtmlFile { get; } = new(false);

	public ConvertSettingsViewModel(
		ConfigService conf,
		JsonFileDialogService fileDialog
	)
	{
		// 配置传入
		conf.Pipe
			.DistinctUntilChangedBy(static p => p.Value.Output)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Select(static p => p.Value.Output)
			.Subscribe(output =>
			{
				_synching = true;

				OutputDir.Value = output.OutputDir;
				DeleteAfterSent.Value = output.DeleteAfterSent;
				ConvertOnly.Value = output.ConvertOnly;
				SaveRawDocs.Value = output.SaveRawDocs;
				SaveHtmlFile.Value = output.SaveHtmlFile;

				_synching = false;
			})
			.AddTo(ref _d);

		// 用户编辑产生配置
		Observable.CombineLatest(
				OutputDir,
				DeleteAfterSent,
				ConvertOnly,
				SaveRawDocs,
				SaveHtmlFile,
				static (outputDir, deleteAfterSent, convertOnly, saveRawDocs, saveHtmlFile) =>
					new OutputConfig
					{
						OutputDir = outputDir,
						DeleteAfterSent = deleteAfterSent,
						ConvertOnly = convertOnly,
						SaveRawDocs = saveRawDocs,
						SaveHtmlFile = saveHtmlFile
					}
			)
			.Where(_ => !_synching)
			.WithLatestFrom(
				conf.Pipe,
				static (output, pipe) => new ConfPipe(
					pipe.Value with
					{
						Output = output
					},
					ConfChangingSource.UserEdit
				)
			)
			.Where(static p => p.Source is ConfChangingSource.UserEdit)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		// 配置导入
		ImportCommand
			.SelectAwait((_, _) => fileDialog.ReadConf(RPMailJsonContext.Default.OutputConfig))
			.WhereNotNull()
			.WithLatestFrom(
				conf.Pipe,
				static (output, pipe) => new ConfPipe(
					pipe.Value with
					{
						Output = output
					},
					ConfChangingSource.Import
				)
			)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value.Output)
			.SubscribeAwait((output, _) =>
				fileDialog.WriteConf(
					output,
					RPMailJsonContext.Default.OutputConfig,
					"output_config_module.json"
				)
			)
			.AddTo(ref _d);
	}

	public void Dispose()
	{
		_d.Dispose();
		ImportCommand.Dispose();
		ExportCommand.Dispose();
		OutputDir.Dispose();
		DeleteAfterSent.Dispose();
		ConvertOnly.Dispose();
		SaveRawDocs.Dispose();
		SaveHtmlFile.Dispose();
	}
}
