using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using R3;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class ContentSettingsViewModel : IDisposable
{
    private readonly Func<string, Task<string?>> _readJson;
    private readonly Func<string, string, string, Task<bool>> _writeJson;
    private readonly Subject<string> _errors = new();
    private DisposableBag _bag = new();
    private bool _synchronizing;
    private bool _disposed;

    public ContentSettingsViewModel(
        Func<string, Task<string?>> readJson,
        Func<string, string, string, Task<bool>> writeJson)
    {
        _readJson = readJson;
        _writeJson = writeJson;

        ImportCommand.Subscribe(async _ => await ImportAsync()).AddTo(ref _bag);
        ExportCommand.Subscribe(async _ => await ExportAsync()).AddTo(ref _bag);

        _bag.Add(Observable.Merge(
                CsvPath.AsObservable().Select(static _ => Unit.Default),
                BodyHtmlPath.AsObservable().Select(static _ => Unit.Default),
                Subject.AsObservable().Select(static _ => Unit.Default),
                CharSet.AsObservable().Select(static _ => Unit.Default),
                Attachments.Changes,
                ExtraAttributes.Changes)
            .Prepend(Unit.Default)
            .Subscribe(_ =>
            {
                if (_synchronizing)
                    return;

                _synchronizing = true;
                try
                {
                    Configuration.Value = new ContentModuleConfig
                    {
                        CsvFile = CsvPath.Value,
                        BodyHtmlPath = BodyHtmlPath.Value,
                        Subject = Subject.Value,
                        CharSet = CharSet.Value,
                        Attachments = Attachments.Items
                            .Select(static a => new PersistedAttachment(a.SourceText.Value, a.DestinationText.Value))
                            .ToImmutableArray(),
                        ExtraAttributes = ExtraAttributes.Items
                            .Where(static a => !string.IsNullOrWhiteSpace(a.Key.Value))
                            .Select(static a => new PersistedExtraAttribute(a.Key.Value, a.Value.Value))
                            .ToImmutableArray(),
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
                ApplyConfiguration(config);
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

    public BindableReactiveProperty<ContentModuleConfig> Configuration { get; } = new(ContentModuleConfig.Empty);

    public BindableReactiveProperty<string> CsvPath { get; } = new("");
    public BindableReactiveProperty<string> BodyHtmlPath { get; } = new("");
    public BindableReactiveProperty<string> Subject { get; } = new("");
    public BindableReactiveProperty<string> CharSet { get; } = new("utf-8");

    public AttachmentListViewModel Attachments { get; } = new();
    public ExtraAttributeListViewModel ExtraAttributes { get; } = new();

    private void ApplyConfiguration(ContentModuleConfig config)
    {
        CsvPath.Value = config.CsvFile;
        BodyHtmlPath.Value = config.BodyHtmlPath;
        Subject.Value = config.Subject;
        CharSet.Value = config.CharSet;

        Attachments.Items.Clear();
        foreach (var attachment in config.Attachments)
        {
            var item = new AttachmentItemData();
            item.SourceText.Value = attachment.SourceText;
            item.DestinationText.Value = attachment.DestinationText;
            Attachments.Items.Add(item);
        }

        ExtraAttributes.Items.Clear();
        foreach (var attribute in config.ExtraAttributes)
        {
            var item = new ExtraAttributeItemData();
            item.Key.Value = attribute.Key;
            item.Value.Value = attribute.Value;
            ExtraAttributes.Items.Add(item);
        }
    }

    private async Task ImportAsync()
    {
        var json = await _readJson("Import Content Settings");
        if (json is null) return;

        try
        {
            var config = JsonSerializer.Deserialize(json, ModuleConfigsContext.Default.ContentModuleConfig);
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
        var json = JsonSerializer.Serialize(Configuration.Value, ModuleConfigsContext.Default.ContentModuleConfig);
        await _writeJson("Export Content Settings", "content-module.json", json);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        Attachments.Dispose();
        ExtraAttributes.Dispose();

        _bag.Dispose();
        _errors.Dispose();
        ImportCommand.Dispose();
        ExportCommand.Dispose();
        Configuration.Dispose();
        CsvPath.Dispose();
        BodyHtmlPath.Dispose();
        Subject.Dispose();
        CharSet.Dispose();
    }
}
