using System;
using System.Text.Json;
using System.Threading.Tasks;
using R3;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class ConvertSettingsViewModel : IDisposable
{
    private readonly Func<string, Task<string?>> _readJson;
    private readonly Func<string, string, string, Task<bool>> _writeJson;
    private readonly Subject<string> _errors = new();
    private DisposableBag _bag = new();
    private bool _synchronizing;
    private bool _disposed;

    public ConvertSettingsViewModel(
        Func<string, Task<string?>> readJson,
        Func<string, string, string, Task<bool>> writeJson)
    {
        _readJson = readJson;
        _writeJson = writeJson;

        ImportCommand.Subscribe(async _ => await ImportAsync()).AddTo(ref _bag);
        ExportCommand.Subscribe(async _ => await ExportAsync()).AddTo(ref _bag);

        _bag.Add(Observable.Merge(
                OutputDir.AsObservable().Select(static _ => Unit.Default),
                DeleteAfterSent.AsObservable().Select(static _ => Unit.Default),
                ConvertOnly.AsObservable().Select(static _ => Unit.Default),
                SaveRawDocs.AsObservable().Select(static _ => Unit.Default),
                SaveHtmlFile.AsObservable().Select(static _ => Unit.Default))
            .Prepend(Unit.Default)
            .Subscribe(_ =>
            {
                if (_synchronizing)
                    return;

                _synchronizing = true;
                try
                {
                    Configuration.Value = new ConvertModuleConfig
                    {
                        OutputFolder = OutputDir.Value,
                        IsDeleteAfterSent = DeleteAfterSent.Value,
                        IsConvertOnly = ConvertOnly.Value,
                        IsSaveRawDoc = SaveRawDocs.Value,
                        IsSaveHtml = SaveHtmlFile.Value,
                    };
                }
                finally
                {
                    _synchronizing = false;
                }
            }));

        _bag.Add(Configuration.Subscribe(config =>
        {
            if (config is null || _synchronizing)
                return;

            _synchronizing = true;
            try
            {
                OutputDir.Value = config.OutputFolder;
                DeleteAfterSent.Value = config.IsDeleteAfterSent;
                ConvertOnly.Value = config.IsConvertOnly;
                SaveRawDocs.Value = config.IsSaveRawDoc;
                SaveHtmlFile.Value = config.IsSaveHtml;
            }
            finally
            {
                _synchronizing = false;
            }
        }));
    }

    public Observable<string> Errors => _errors;

    public ReactiveCommand ImportCommand { get; } = new();
    public ReactiveCommand ExportCommand { get; } = new();

    public BindableReactiveProperty<ConvertModuleConfig> Configuration { get; } = new(ConvertModuleConfig.Empty);

    public BindableReactiveProperty<string> OutputDir { get; } = new("Output");
    public BindableReactiveProperty<bool> DeleteAfterSent { get; } = new(false);
    public BindableReactiveProperty<bool> ConvertOnly { get; } = new(false);
    public BindableReactiveProperty<bool> SaveRawDocs { get; } = new(false);
    public BindableReactiveProperty<bool> SaveHtmlFile { get; } = new(false);

    private async Task ImportAsync()
    {
        var json = await _readJson("Import Convert Settings");
        if (json is null) return;

        try
        {
            var config = JsonSerializer.Deserialize(json, ModuleConfigsContext.Default.ConvertModuleConfig);
            if (config is null) return;

            Configuration.Value = config;
        }
        catch (Exception ex)
        {
            _errors.OnNext($"Import failed: {ex.Message}");
        }
    }

    private async Task ExportAsync()
    {
        var json = JsonSerializer.Serialize(Configuration.Value, ModuleConfigsContext.Default.ConvertModuleConfig);
        await _writeJson("Export Convert Settings", "convert-module.json", json);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _bag.Dispose();
        _errors.Dispose();
        ImportCommand.Dispose();
        ExportCommand.Dispose();
        Configuration.Dispose();
        OutputDir.Dispose();
        DeleteAfterSent.Dispose();
        ConvertOnly.Dispose();
        SaveRawDocs.Dispose();
        SaveHtmlFile.Dispose();
    }
}
