using System;
using System.Text.Json;
using System.Threading.Tasks;
using R3;
using RPMailCore.Models;
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
    public ConvertSettingsViewModel()
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
