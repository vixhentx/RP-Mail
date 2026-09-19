using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
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
		// 配置产生
        var confOut = Observable.CombineLatest(
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
		.Where(_ => !_synching);

		var confImport = ImportCommand
			.SelectAwait((_,_) => fileDialog.ReadConf(RPMailJsonContext.Default.OutputConfig))
			.WhereNotNull();

		Observable.Merge(confOut, confImport)
			.DistinctUntilChanged()
			.Subscribe(conf.Root, static (m, root) => root.Value = root.Value with
				{
					Output = m
				})
			.AddTo(ref _d);

		// 配置传入
		conf.Root
			.Where(_ => !_synching)
			.Select(static x => x.Output)
			.DistinctUntilChanged()
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

		// 配置导出
		ExportCommand
			.WithLatestFrom(conf.Root, static (_, conf) => conf.Output)
			.SubscribeAwait((output,_) => fileDialog.WriteConf(output,RPMailJsonContext.Default.OutputConfig,"output_config_module.json"))
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
