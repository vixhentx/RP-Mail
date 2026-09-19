using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class ConvertSettingsViewModel : IDisposable
{
    readonly DisposableBag _d = new();

	// 暴露给View

    public ReactiveCommand ImportCommand { get; } = new();
    public ReactiveCommand ExportCommand { get; } = new();

    public BindableReactiveProperty<string> OutputDir { get; } = new("Output");
    public BindableReactiveProperty<bool> DeleteAfterSent { get; } = new(false);
    public BindableReactiveProperty<bool> ConvertOnly { get; } = new(false);
    public BindableReactiveProperty<bool> SaveRawDocs { get; } = new(false);
    public BindableReactiveProperty<bool> SaveHtmlFile { get; } = new(false);

	// 暴露给上层
	public Observable<OutputConfig> ConfOut { get; }

    public ConvertSettingsViewModel(
		ConfigService conf,
		JsonFileDialogService fileDialog
	)
	{
        ConfOut = Observable.CombineLatest(
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
		);

		// 配置产生
		var confOut = ConfOut;
		var confImport = ImportCommand
			.SelectAwait((_,_) => fileDialog.ReadConf(RPMailJsonContext.Default.OutputConfig))
			.WhereNotNull();

		Observable.Merge(confOut, confImport)
			.DistinctUntilChanged()
			.Subscribe(conf.Root, static (module, root) =>
				root.Value = root.Value with { Output = module }
			)
			.AddTo(ref _d);

		// 配置传入
		conf.Root
			.Select(static x => x.Output)
			.DistinctUntilChanged()
			.Subscribe(output =>
			{
				OutputDir.Value = output.OutputDir;
				DeleteAfterSent.Value = output.DeleteAfterSent;
				ConvertOnly.Value = output.ConvertOnly;
				SaveRawDocs.Value = output.SaveRawDocs;
				SaveHtmlFile.Value = output.SaveHtmlFile;
			})
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(confOut, static (_,output) => output)
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
