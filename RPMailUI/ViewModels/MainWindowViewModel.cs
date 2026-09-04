using System.Collections.Specialized;
using System.Text.Json;
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

    public MailRunCoordinator Core { get; }

    public ObservableList<TaskItemData> Tasks { get; } = [];
    public ObservableList<AttachmentItemData> Attachments { get; } = [new()];
    public ObservableList<ErrorItemData> Errors { get; } = [];

    public BindableReactiveProperty<string> ConsoleLog { get; } = new("");
    public BindableReactiveProperty<double> Progress { get; } = new(0);
    public BindableReactiveProperty<bool> ShouldRetry { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOpenOutputFolder { get; } = new(false);

    public ReactiveCommand OpenOutputFolderCommand { get; } = new();
    public ReactiveCommand RetryCommand { get; } = new();

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
        Watch(Core.HtmlPath);
        Watch(Core.Subject);
        Watch(Core.ReceiverHeader);
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

    private void MarkDirty() => _dirty.OnNext(Unit.Default);

    private void SyncAttachmentPatterns()
    {
        Core.AttachmentPatterns.Clear();
        foreach (var item in Attachments)
            Core.AttachmentPatterns.Add(new() { Source = item.SourceText.Value, Name = item.DestinationText.Value });
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var settings = JsonSerializer.Deserialize(File.ReadAllText(SettingsPath), PersistedSettingsContext.Default.PersistedSettings);
            if (settings is null) return;

            Core.CsvPath.Value = settings.CsvFile;
            Core.HtmlPath.Value = settings.HtmlFile;
            Core.Subject.Value = settings.Subject;
            Core.ReceiverHeader.Value = settings.ReceiverHeader;
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
                HtmlFile = Core.HtmlPath.Value,
                Subject = Core.Subject.Value,
                ReceiverHeader = Core.ReceiverHeader.Value,
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

    public void Dispose()
    {
        foreach (var sub in _attachmentSubscriptions.Values)
            sub.Dispose();
        _attachmentSubscriptions.Clear();
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
