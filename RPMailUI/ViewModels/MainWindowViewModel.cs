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
    private DisposableBag _d = new();
    private readonly Dictionary<AttachmentItemData, IDisposable> _attachmentSubscriptions = [];
    private readonly Dictionary<ExtraAttributeItemData, IDisposable> _extraAttributeSubscriptions = [];

    public MailRunCoordinator Coordinator { get; }

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
        Coordinator = new MailRunCoordinator();

        OpenOutputFolderCommand.Subscribe(_ =>
        {
            if (Coordinator.LastResult is { } result)
                PathOpenHelper.OpenDirectory(result.RealOutputDir);
        }).AddTo(ref _d);

        RetryCommand.Subscribe(_ =>
        {
            if (Coordinator.LastResult?.FailedCsvPath is { } failedCsv)
                Coordinator.CsvPath.Value = failedCsv;
        }).AddTo(ref _d);

        ImportContentCommand.Subscribe(async _ => await ImportContentAsync()).AddTo(ref _d);
        ExportContentCommand.Subscribe(async _ => await ExportContentAsync()).AddTo(ref _d);
        ImportSenderCommand.Subscribe(async _ => await ImportSenderAsync()).AddTo(ref _d);
        ExportSenderCommand.Subscribe(async _ => await ExportSenderAsync()).AddTo(ref _d);
        ImportConvertCommand.Subscribe(async _ => await ImportConvertAsync()).AddTo(ref _d);
        ExportConvertCommand.Subscribe(async _ => await ExportConvertAsync()).AddTo(ref _d);

        LoadSettings();
        WirePersistence();
        WireCoreStreams();
    }

    private void WireCoreStreams()
    {
        Coordinator.Output
            .ObserveOnUIThreadDispatcher()
            .Subscribe(output =>
            {
                switch (output)
                {
                    case RowsLoadedOutput rows:
                        Tasks.Clear();
                        foreach (var row in rows.Rows)
                            Tasks.Add(new TaskItemData(row, MailTaskStatus.Ready, Strings.ReadyToSend));
                        break;
                    case TaskStateOutput state when state.Index < Tasks.Count:
                        var task = Tasks[state.Index];
                        Tasks[state.Index] = task with
                        {
                            Status = state.Status,
                            Tooltip = state.Message ?? task.Tooltip,
                        };
                        break;
                    case ProgressOutput progress:
                        Progress.Value = progress.Progress;
                        break;
                    case MessageOutput message when message.Level >= LogLevel.Warning:
                        AddError(message.Level, message.Text);
                        break;
                    case RunCompletedOutput completed:
                        ShouldOpenOutputFolder.Value = completed.Result.Success;
                        ShouldRetry.Value = completed.Result.Success && completed.Result.FailedRows > 0;
                        break;
                }

                if (output is not RowsLoadedOutput and not TaskStateOutput and not ProgressOutput
                    && !string.IsNullOrEmpty(output.Text))
                    ConsoleLog.Value += output.Text + Environment.NewLine;
            }).AddTo(ref _d);

        Coordinator.IsRunning.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Where(running => running)
            .Subscribe(_ =>
            {
                SyncAttachmentPatterns();
                SyncExtraAttributes();
                Errors.Clear();
                Progress.Value = 0;
            }).AddTo(ref _d);

        Coordinator.IsRunning.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Where(running => !running)
            .Skip(1)
            .Subscribe(_ =>
            {
                var result = Coordinator.LastResult;
                ShouldOpenOutputFolder.Value = result?.Success == true;
                ShouldRetry.Value = result is { Success: true, FailedRows: > 0 };
            }).AddTo(ref _d);
    }

    private void WirePersistence()
    {
        Watch(Coordinator.CsvPath);
        Watch(Coordinator.BodyHtmlPath);
        Watch(Coordinator.Subject);
        Watch(Coordinator.CharSet);
        Watch(Coordinator.SmtpHost);
        Watch(Coordinator.SenderEmail);
        Watch(Coordinator.SenderPassword);
        Watch(Coordinator.OutputDir);
        Watch(Coordinator.SaveHtmlFile);
        Watch(Coordinator.SaveRawDocs);
        Watch(Coordinator.ConvertOnly);
        Watch(Coordinator.DeleteAfterSent);

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
        }).AddTo(ref _d);

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
        }).AddTo(ref _d);

        foreach (var item in ExtraAttributes)
            SubscribeExtraAttribute(item);

        _dirty.ThrottleLast(TimeSpan.FromMilliseconds(500))
            .SubscribeAwait(async (_, _) => await SaveSettingsAsync())
            .AddTo(ref _d);
    }

    private void Watch<T>(BindableReactiveProperty<T> prop) =>
        prop.AsObservable().Subscribe(_ => MarkDirty()).AddTo(ref _d);

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
        Coordinator.AttachmentPatterns.Clear();
        foreach (var item in Attachments)
            Coordinator.AttachmentPatterns.Add(new() { Source = item.SourceText.Value, Name = item.DestinationText.Value });
    }

    private void SyncExtraAttributes()
    {
        Coordinator.ExtraAttributes.Clear();
        foreach (var item in ExtraAttributes)
        {
            if (string.IsNullOrWhiteSpace(item.Key.Value)) continue;
            Coordinator.ExtraAttributes[item.Key.Value] = item.Value.Value;
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var settings = JsonSerializer.Deserialize(File.ReadAllText(SettingsPath), PersistedSettingsContext.Default.PersistedSettings);
            if (settings is null) return;

            Coordinator.CsvPath.Value = settings.CsvFile;
            Coordinator.BodyHtmlPath.Value = settings.BodyHtmlPath;
            Coordinator.Subject.Value = settings.Subject;
            Coordinator.CharSet.Value = settings.CharSet;
            Coordinator.SmtpHost.Value = settings.SmtpHost;
            Coordinator.SenderEmail.Value = settings.SenderEmail;
            Coordinator.SenderPassword.Value = settings.SenderPassword;
            Coordinator.OutputDir.Value = settings.OutputFolder;
            Coordinator.SaveHtmlFile.Value = settings.IsSaveHtml;
            Coordinator.SaveRawDocs.Value = settings.IsSaveRawDoc;
            Coordinator.ConvertOnly.Value = settings.IsConvertOnly;
            Coordinator.DeleteAfterSent.Value = settings.IsDeleteAfterSent;

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
                CsvFile = Coordinator.CsvPath.Value,
                BodyHtmlPath = Coordinator.BodyHtmlPath.Value,
                Subject = Coordinator.Subject.Value,
                CharSet = Coordinator.CharSet.Value,
                SmtpHost = Coordinator.SmtpHost.Value,
                SenderEmail = Coordinator.SenderEmail.Value,
                SenderPassword = Coordinator.SenderPassword.Value,
                OutputFolder = Coordinator.OutputDir.Value,
                IsDeleteAfterSent = Coordinator.DeleteAfterSent.Value,
                IsConvertOnly = Coordinator.ConvertOnly.Value,
                IsSaveRawDoc = Coordinator.SaveRawDocs.Value,
                IsSaveHtml = Coordinator.SaveHtmlFile.Value,
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

            Coordinator.CsvPath.Value = config.CsvFile;
            Coordinator.BodyHtmlPath.Value = config.BodyHtmlPath;
            Coordinator.Subject.Value = config.Subject;
            Coordinator.CharSet.Value = config.CharSet;

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
            CsvFile = Coordinator.CsvPath.Value,
            BodyHtmlPath = Coordinator.BodyHtmlPath.Value,
            Subject = Coordinator.Subject.Value,
            CharSet = Coordinator.CharSet.Value,
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

            Coordinator.SenderEmail.Value = config.SenderEmail;
            Coordinator.SenderPassword.Value = config.SenderPassword;
            Coordinator.SmtpHost.Value = config.SmtpHost;
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
            SenderEmail = Coordinator.SenderEmail.Value,
            SenderPassword = Coordinator.SenderPassword.Value,
            SmtpHost = Coordinator.SmtpHost.Value,
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

            Coordinator.OutputDir.Value = config.OutputFolder;
            Coordinator.DeleteAfterSent.Value = config.IsDeleteAfterSent;
            Coordinator.ConvertOnly.Value = config.IsConvertOnly;
            Coordinator.SaveRawDocs.Value = config.IsSaveRawDoc;
            Coordinator.SaveHtmlFile.Value = config.IsSaveHtml;
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
            OutputFolder = Coordinator.OutputDir.Value,
            IsDeleteAfterSent = Coordinator.DeleteAfterSent.Value,
            IsConvertOnly = Coordinator.ConvertOnly.Value,
            IsSaveRawDoc = Coordinator.SaveRawDocs.Value,
            IsSaveHtml = Coordinator.SaveHtmlFile.Value,
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

        _d.Dispose();
        _dirty.Dispose();
        Coordinator.Dispose();
    }

}
