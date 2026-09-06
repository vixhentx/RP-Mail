using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using RPMailCore.Coordination;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Resources;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public class MainWindowViewModel : IDisposable
{
    private const int MaxErrorCount = 5;
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "RPMailUI-Persisted.json");

    private readonly Subject<Unit> _dirty = new();
    private DisposableBag _disposables = new();
    private readonly Dictionary<AttachmentItemData, IDisposable> _attachmentSubscriptions = [];
    private readonly Dictionary<ExtraAttributeItemData, IDisposable> _extraAttributeSubscriptions = [];

    public MailRunCoordinator Core { get; }

    public ObservableList<TaskItemData> Tasks { get; } = [];
    public ObservableList<AttachmentItemData> Attachments { get; } = [new()];
    public ObservableList<ExtraAttributeItemData> ExtraAttributes { get; } = [new()];
    public ObservableList<ErrorItemData> Errors { get; } = [];

    public BindableReactiveProperty<string> ConsoleLog { get; } = new("");
    public BindableReactiveProperty<double> Progress { get; } = new(0);
    public BindableReactiveProperty<bool> ShouldRetry { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOpenOutputFolder { get; } = new(false);

    public ReactiveCommand OpenOutputFolderCommand { get; } = new();
    public ReactiveCommand RetryCommand { get; } = new();

    public ReactiveCommand ImportContentCommand { get; } = new();
    public ReactiveCommand ExportContentCommand { get; } = new();
    public ReactiveCommand ImportSenderCommand { get; } = new();
    public ReactiveCommand ExportSenderCommand { get; } = new();
    public ReactiveCommand ImportConvertCommand { get; } = new();
    public ReactiveCommand ExportConvertCommand { get; } = new();

    public IStorageProvider? StorageProvider { get; set; }

    public MainWindowViewModel()
    {
        Core = new MailRunCoordinator(new UiLogger(this));

        OpenOutputFolderCommand.Subscribe(_ =>
        {
            if (Core.LastResult is { } result)
                PathOpenHelper.OpenDirectory(result.RealOutputDir);
        }).AddTo(ref _disposables);

        RetryCommand.Subscribe(_ =>
        {
            if (Core.LastResult?.FailedCsvPath is { } failedCsv)
                Core.CsvPath.Value = failedCsv;
        }).AddTo(ref _disposables);

        ImportContentCommand.Subscribe(async _ => await ImportContentAsync()).AddTo(ref _disposables);
        ExportContentCommand.Subscribe(async _ => await ExportContentAsync()).AddTo(ref _disposables);
        ImportSenderCommand.Subscribe(async _ => await ImportSenderAsync()).AddTo(ref _disposables);
        ExportSenderCommand.Subscribe(async _ => await ExportSenderAsync()).AddTo(ref _disposables);
        ImportConvertCommand.Subscribe(async _ => await ImportConvertAsync()).AddTo(ref _disposables);
        ExportConvertCommand.Subscribe(async _ => await ExportConvertAsync()).AddTo(ref _disposables);

        LoadSettings();
        WirePersistence();
        WireCoreStreams();
    }

    private void WireCoreStreams()
    {
        Core.RowsLoaded
            .ObserveOnUIThreadDispatcher()
            .Subscribe(rows =>
            {
                Tasks.Clear();
                foreach (var row in rows)
                    Tasks.Add(new TaskItemData(row, MailTaskStatus.Ready, Strings.ReadyToSend));
            }).AddTo(ref _disposables);

        Core.TaskStateChanged
            .ObserveOnUIThreadDispatcher()
            .Subscribe(e =>
            {
                if (e.Index >= Tasks.Count) return;
                var task = Tasks[e.Index];
                Tasks[e.Index] = task with
                {
                    Status = e.Status,
                    Tooltip = e.Message ?? task.Tooltip,
                };
            }).AddTo(ref _disposables);

        Core.ProgressChanged
            .ObserveOnUIThreadDispatcher()
            .Subscribe(p => Progress.Value = p).AddTo(ref _disposables);

        Core.IsRunning.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Where(running => running)
            .Subscribe(_ =>
            {
                SyncAttachmentPatterns();
                SyncExtraAttributes();
                Errors.Clear();
                Progress.Value = 0;
            }).AddTo(ref _disposables);

        Core.IsRunning.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Where(running => !running)
            .Skip(1)
            .Subscribe(_ =>
            {
                var result = Core.LastResult;
                ShouldOpenOutputFolder.Value = result?.Success == true;
                ShouldRetry.Value = result is { Success: true, FailedRows: > 0 };
            }).AddTo(ref _disposables);
    }

    private void WirePersistence()
    {
        Watch(Core.CsvPath);
        Watch(Core.BodyHtmlPath);
        Watch(Core.Subject);
        Watch(Core.CharSet);
        Watch(Core.SmtpHost);
        Watch(Core.SenderEmail);
        Watch(Core.SenderPassword);
        Watch(Core.OutputDir);
        Watch(Core.SaveHtmlFile);
        Watch(Core.SaveRawDocs);
        Watch(Core.ConvertOnly);
        Watch(Core.DeleteAfterSent);

        Attachments.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SubscribeAttachment(e.NewItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (_attachmentSubscriptions.Remove(e.OldItem, out var removed))
                        removed.Dispose();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var sub in _attachmentSubscriptions.Values)
                        sub.Dispose();
                    _attachmentSubscriptions.Clear();
                    foreach (var item in Attachments)
                        SubscribeAttachment(item);
                    break;
            }
            MarkDirty();
        }).AddTo(ref _disposables);

        foreach (var item in Attachments)
            SubscribeAttachment(item);

        ExtraAttributes.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SubscribeExtraAttribute(e.NewItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (_extraAttributeSubscriptions.Remove(e.OldItem, out var removedExtra))
                        removedExtra.Dispose();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var sub in _extraAttributeSubscriptions.Values)
                        sub.Dispose();
                    _extraAttributeSubscriptions.Clear();
                    foreach (var item in ExtraAttributes)
                        SubscribeExtraAttribute(item);
                    break;
            }
            MarkDirty();
        }).AddTo(ref _disposables);

        foreach (var item in ExtraAttributes)
            SubscribeExtraAttribute(item);

        _dirty.ThrottleLast(TimeSpan.FromMilliseconds(500))
            .SubscribeAwait(async (_, _) => await SaveSettingsAsync())
            .AddTo(ref _disposables);
    }

    private void Watch<T>(BindableReactiveProperty<T> prop) =>
        prop.AsObservable().Subscribe(_ => MarkDirty()).AddTo(ref _disposables);

    private void SubscribeAttachment(AttachmentItemData item)
    {
        var d1 = item.SourceText.AsObservable().Subscribe(_ => MarkDirty());
        var d2 = item.DestinationText.AsObservable().Subscribe(_ => MarkDirty());
        _attachmentSubscriptions[item] = Disposable.Combine(d1, d2);
    }

    private void SubscribeExtraAttribute(ExtraAttributeItemData item)
    {
        var d1 = item.Key.AsObservable().Subscribe(_ => MarkDirty());
        var d2 = item.Value.AsObservable().Subscribe(_ => MarkDirty());
        _extraAttributeSubscriptions[item] = Disposable.Combine(d1, d2);
    }

    private void MarkDirty() => _dirty.OnNext(Unit.Default);

    private void SyncAttachmentPatterns()
    {
        Core.AttachmentPatterns.Clear();
        foreach (var item in Attachments)
            Core.AttachmentPatterns.Add(new() { Source = item.SourceText.Value, Name = item.DestinationText.Value });
    }

    private void SyncExtraAttributes()
    {
        Core.ExtraAttributes.Clear();
        foreach (var item in ExtraAttributes)
        {
            if (string.IsNullOrWhiteSpace(item.Key.Value)) continue;
            Core.ExtraAttributes[item.Key.Value] = item.Value.Value;
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var settings = JsonSerializer.Deserialize(File.ReadAllText(SettingsPath), PersistedSettingsContext.Default.PersistedSettings);
            if (settings is null) return;

            Core.CsvPath.Value = settings.CsvFile;
            Core.BodyHtmlPath.Value = settings.BodyHtmlPath;
            Core.Subject.Value = settings.Subject;
            Core.CharSet.Value = settings.CharSet;
            Core.SmtpHost.Value = settings.SmtpHost;
            Core.SenderEmail.Value = settings.SenderEmail;
            Core.SenderPassword.Value = settings.SenderPassword;
            Core.OutputDir.Value = settings.OutputFolder;
            Core.SaveHtmlFile.Value = settings.IsSaveHtml;
            Core.SaveRawDocs.Value = settings.IsSaveRawDoc;
            Core.ConvertOnly.Value = settings.IsConvertOnly;
            Core.DeleteAfterSent.Value = settings.IsDeleteAfterSent;

            Attachments.Clear();
            foreach (var attachment in settings.Attachments)
            {
                var item = new AttachmentItemData();
                item.SourceText.Value = attachment.SourceText;
                item.DestinationText.Value = attachment.DestinationText;
                Attachments.Add(item);
            }

            ExtraAttributes.Clear();
            foreach (var attr in settings.ExtraAttributes)
            {
                var item = new ExtraAttributeItemData();
                item.Key.Value = attr.Key;
                item.Value.Value = attr.Value;
                ExtraAttributes.Add(item);
            }
        }
        catch
        {
            // corrupt settings file: keep defaults
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new PersistedSettings
            {
                CsvFile = Core.CsvPath.Value,
                BodyHtmlPath = Core.BodyHtmlPath.Value,
                Subject = Core.Subject.Value,
                CharSet = Core.CharSet.Value,
                SmtpHost = Core.SmtpHost.Value,
                SenderEmail = Core.SenderEmail.Value,
                SenderPassword = Core.SenderPassword.Value,
                OutputFolder = Core.OutputDir.Value,
                IsDeleteAfterSent = Core.DeleteAfterSent.Value,
                IsConvertOnly = Core.ConvertOnly.Value,
                IsSaveRawDoc = Core.SaveRawDocs.Value,
                IsSaveHtml = Core.SaveHtmlFile.Value,
                Attachments = Attachments.Select(a => new PersistedAttachment(a.SourceText.Value, a.DestinationText.Value)).ToList(),
                ExtraAttributes = ExtraAttributes.Select(e => new PersistedExtraAttribute(e.Key.Value, e.Value.Value)).ToList(),
            };
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(settings, PersistedSettingsContext.Default.PersistedSettings));
        }
        catch
        {
            // persistence is best-effort
        }
    }

    private void AddError(LogLevel level, string message)
    {
        string key = level >= LogLevel.Error ? "Flyout.Error" : "Flyout.Warning";
        Errors.Add(new ErrorItemData(message, key));
        if (Errors.Count > MaxErrorCount)
            Errors.RemoveAt(0);
    }

    private async Task<string?> PickOpenJsonAsync(string title)
    {
        if (StorageProvider is not { } provider)
        {
            AddError(LogLevel.Error, "Storage provider unavailable.");
            return null;
        }

        IReadOnlyList<IStorageFile> files;
        try
        {
            files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            });
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"File picker failed: {ex.Message}");
            return null;
        }

        if (files is not { Count: > 0 })
            return null;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"Failed to read file: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> PickSaveJsonAsync(string title, string suggestedName, string json)
    {
        if (StorageProvider is not { } provider)
        {
            AddError(LogLevel.Error, "Storage provider unavailable.");
            return false;
        }

        IStorageFile? file;
        try
        {
            file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName,
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            });
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"File picker failed: {ex.Message}");
            return false;
        }

        if (file is null)
            return false;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
            return true;
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"Failed to write file: {ex.Message}");
            return false;
        }
    }

    private async Task ImportContentAsync()
    {
        var json = await PickOpenJsonAsync("Import Content Settings");
        if (json is null) return;

        try
        {
            var config = JsonSerializer.Deserialize(json, ModuleConfigsContext.Default.ContentModuleConfig);
            if (config is null) return;

            Core.CsvPath.Value = config.CsvFile;
            Core.BodyHtmlPath.Value = config.BodyHtmlPath;
            Core.Subject.Value = config.Subject;
            Core.CharSet.Value = config.CharSet;

            Attachments.Clear();
            foreach (var att in config.Attachments ?? [])
            {
                var item = new AttachmentItemData();
                item.SourceText.Value = att.SourceText;
                item.DestinationText.Value = att.DestinationText;
                Attachments.Add(item);
            }

            ExtraAttributes.Clear();
            foreach (var attr in config.ExtraAttributes ?? [])
            {
                var item = new ExtraAttributeItemData();
                item.Key.Value = attr.Key;
                item.Value.Value = attr.Value;
                ExtraAttributes.Add(item);
            }
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"Import failed: {ex.Message}");
        }
    }

    private async Task ExportContentAsync()
    {
        var config = new ContentModuleConfig
        {
            CsvFile = Core.CsvPath.Value,
            BodyHtmlPath = Core.BodyHtmlPath.Value,
            Subject = Core.Subject.Value,
            CharSet = Core.CharSet.Value,
            Attachments = Attachments
                .Select(a => new PersistedAttachment(a.SourceText.Value, a.DestinationText.Value))
                .ToList(),
            ExtraAttributes = ExtraAttributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Key.Value))
                .Select(a => new PersistedExtraAttribute(a.Key.Value, a.Value.Value))
                .ToList(),
        };

        var json = JsonSerializer.Serialize(config, ModuleConfigsContext.Default.ContentModuleConfig);
        await PickSaveJsonAsync("Export Content Settings", "content-module.json", json);
    }

    private async Task ImportSenderAsync()
    {
        var json = await PickOpenJsonAsync("Import Sender Settings");
        if (json is null) return;

        try
        {
            var config = JsonSerializer.Deserialize(json, ModuleConfigsContext.Default.SenderModuleConfig);
            if (config is null) return;

            Core.SenderEmail.Value = config.SenderEmail;
            Core.SenderPassword.Value = config.SenderPassword;
            Core.SmtpHost.Value = config.SmtpHost;
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"Import failed: {ex.Message}");
        }
    }

    private async Task ExportSenderAsync()
    {
        var config = new SenderModuleConfig
        {
            SenderEmail = Core.SenderEmail.Value,
            SenderPassword = Core.SenderPassword.Value,
            SmtpHost = Core.SmtpHost.Value,
        };

        var json = JsonSerializer.Serialize(config, ModuleConfigsContext.Default.SenderModuleConfig);
        await PickSaveJsonAsync("Export Sender Settings", "sender-module.json", json);
    }

    private async Task ImportConvertAsync()
    {
        var json = await PickOpenJsonAsync("Import Convert Settings");
        if (json is null) return;

        try
        {
            var config = JsonSerializer.Deserialize(json, ModuleConfigsContext.Default.ConvertModuleConfig);
            if (config is null) return;

            Core.OutputDir.Value = config.OutputFolder;
            Core.DeleteAfterSent.Value = config.IsDeleteAfterSent;
            Core.ConvertOnly.Value = config.IsConvertOnly;
            Core.SaveRawDocs.Value = config.IsSaveRawDoc;
            Core.SaveHtmlFile.Value = config.IsSaveHtml;
        }
        catch (Exception ex)
        {
            AddError(LogLevel.Error, $"Import failed: {ex.Message}");
        }
    }

    private async Task ExportConvertAsync()
    {
        var config = new ConvertModuleConfig
        {
            OutputFolder = Core.OutputDir.Value,
            IsDeleteAfterSent = Core.DeleteAfterSent.Value,
            IsConvertOnly = Core.ConvertOnly.Value,
            IsSaveRawDoc = Core.SaveRawDocs.Value,
            IsSaveHtml = Core.SaveHtmlFile.Value,
        };

        var json = JsonSerializer.Serialize(config, ModuleConfigsContext.Default.ConvertModuleConfig);
        await PickSaveJsonAsync("Export Convert Settings", "convert-module.json", json);
    }

    public void Dispose()
    {
        foreach (var sub in _attachmentSubscriptions.Values)
            sub.Dispose();
        _attachmentSubscriptions.Clear();

        foreach (var sub in _extraAttributeSubscriptions.Values)
            sub.Dispose();
        _extraAttributeSubscriptions.Clear();

        _disposables.Dispose();
        _dirty.Dispose();
        Core.Dispose();
    }

    private sealed class UiLogger(MainWindowViewModel owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            Dispatcher.UIThread.Post(() =>
            {
                owner.ConsoleLog.Value += message + Environment.NewLine;
                if (logLevel >= LogLevel.Warning)
                    owner.AddError(logLevel, message);
            });
        }
    }
}
