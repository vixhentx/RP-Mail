using System;
using System.Text.Json;
using System.Threading.Tasks;
using R3;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class SenderSettingsViewModel : IDisposable
{
    private readonly Func<string, Task<string?>> _readJson;
    private readonly Func<string, string, string, Task<bool>> _writeJson;
    private readonly Subject<string> _errors = new();
    private DisposableBag _bag = new();
    private bool _synchronizing;
    private bool _disposed;

    public SenderSettingsViewModel(
        Func<string, Task<string?>> readJson,
        Func<string, string, string, Task<bool>> writeJson)
    {
        _readJson = readJson;
        _writeJson = writeJson;

        ImportCommand.Subscribe(async _ => await ImportAsync()).AddTo(ref _bag);
        ExportCommand.Subscribe(async _ => await ExportAsync()).AddTo(ref _bag);

        _bag.Add(Observable.Merge(
                SenderEmail.AsObservable().Select(static _ => Unit.Default),
                SenderPassword.AsObservable().Select(static _ => Unit.Default),
                SmtpHost.AsObservable().Select(static _ => Unit.Default))
            .Prepend(Unit.Default)
            .Subscribe(_ =>
            {
                if (_synchronizing)
                    return;

                _synchronizing = true;
                try
                {
                    Configuration.Value = new SenderModuleConfig
                    {
                        SenderEmail = SenderEmail.Value,
                        SenderPassword = SenderPassword.Value,
                        SmtpHost = SmtpHost.Value,
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
                SenderEmail.Value = config.SenderEmail;
                SenderPassword.Value = config.SenderPassword;
                SmtpHost.Value = config.SmtpHost;
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

    public BindableReactiveProperty<SenderModuleConfig> Configuration { get; } = new(SenderModuleConfig.Empty);

    public BindableReactiveProperty<string> SenderEmail { get; } = new("");
    public BindableReactiveProperty<string> SenderPassword { get; } = new("");
    public BindableReactiveProperty<string> SmtpHost { get; } = new("");

    private async Task ImportAsync()
    {
        var json = await _readJson("Import Sender Settings");
        if (json is null) return;

        try
        {
            var config = JsonSerializer.Deserialize(json, ModuleConfigsContext.Default.SenderModuleConfig);
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
        var json = JsonSerializer.Serialize(Configuration.Value, ModuleConfigsContext.Default.SenderModuleConfig);
        await _writeJson("Export Sender Settings", "sender-module.json", json);
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
        SenderEmail.Dispose();
        SenderPassword.Dispose();
        SmtpHost.Dispose();
    }
}
