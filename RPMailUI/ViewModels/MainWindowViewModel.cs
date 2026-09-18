using System;
using System.IO;
using System.Text.Json;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using RPMailCore.Coordination;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public class MainWindowViewModel : IDisposable
{
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "RPMailUI-Persisted.json");

    private DisposableBag _d = new();
    private readonly JsonFileDialogService _jsonFiles = new();

    public ContentSettingsViewModel Content { get; }
    public SenderSettingsViewModel Sender { get; }
    public ConvertSettingsViewModel Convert { get; }

    public MailRunViewModel Run { get; }

    public BindableReactiveProperty<PersistedSettings> Configuration { get; } = new(PersistedSettings.Empty);

    public IStorageProvider? StorageProvider
    {
        get => _jsonFiles.StorageProvider;
        set => _jsonFiles.StorageProvider = value;
    }

    public MainWindowViewModel()
    {
        Content = new ContentSettingsViewModel(_jsonFiles.ReadAsync, _jsonFiles.WriteAsync);
        Sender = new SenderSettingsViewModel(_jsonFiles.ReadAsync, _jsonFiles.WriteAsync);
        Convert = new ConvertSettingsViewModel(_jsonFiles.ReadAsync, _jsonFiles.WriteAsync);

        Content.Configuration.AsObservable()
            .CombineLatest(
                Sender.Configuration.AsObservable(),
                static (content, sender) => (content, sender))
            .CombineLatest(
                Convert.Configuration.AsObservable(),
                static (partial, convert) => new PersistedSettings
                {
                    CsvFile = partial.content.CsvFile,
                    BodyHtmlPath = partial.content.BodyHtmlPath,
                    Subject = partial.content.Subject,
                    CharSet = partial.content.CharSet,
                    Attachments = partial.content.Attachments,
                    ExtraAttributes = partial.content.ExtraAttributes,
                    SenderEmail = partial.sender.SenderEmail,
                    SenderPassword = partial.sender.SenderPassword,
                    SmtpHost = partial.sender.SmtpHost,
                    OutputFolder = convert.OutputFolder,
                    IsDeleteAfterSent = convert.IsDeleteAfterSent,
                    IsConvertOnly = convert.IsConvertOnly,
                    IsSaveRawDoc = convert.IsSaveRawDoc,
                    IsSaveHtml = convert.IsSaveHtml,
                })
            .Subscribe(settings => Configuration.Value = settings)
            .AddTo(ref _d);

        Run = new MailRunViewModel(
            Configuration,
            csv => Content.CsvPath.Value = csv,
            Content.Errors.Merge(Sender.Errors).Merge(Convert.Errors));

        _jsonFiles.Errors
            .ObserveOnUIThreadDispatcher()
            .Subscribe(message => Run.AddError(LogLevel.Error, message))
            .AddTo(ref _d);

        LoadSettings();
        WirePersistence();
    }

    private void WirePersistence()
    {
        Configuration.AsObservable()
            .ThrottleLast(TimeSpan.FromMilliseconds(500))
            .SubscribeAwait(async (settings, _) => await SaveSettingsAsync(settings))
            .AddTo(ref _d);
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var settings = JsonSerializer.Deserialize(File.ReadAllText(SettingsPath), PersistedSettingsContext.Default.PersistedSettings);
            if (settings is null) return;

            Content.Configuration.Value = new ContentModuleConfig
            {
                CsvFile = settings.CsvFile,
                BodyHtmlPath = settings.BodyHtmlPath,
                Subject = settings.Subject,
                CharSet = settings.CharSet,
                Attachments = settings.Attachments,
                ExtraAttributes = settings.ExtraAttributes,
            };

            Sender.Configuration.Value = new SenderModuleConfig
            {
                SenderEmail = settings.SenderEmail,
                SenderPassword = settings.SenderPassword,
                SmtpHost = settings.SmtpHost,
            };

            Convert.Configuration.Value = new ConvertModuleConfig
            {
                OutputFolder = settings.OutputFolder,
                IsDeleteAfterSent = settings.IsDeleteAfterSent,
                IsConvertOnly = settings.IsConvertOnly,
                IsSaveRawDoc = settings.IsSaveRawDoc,
                IsSaveHtml = settings.IsSaveHtml,
            };
        }
        catch
        {
            // corrupt settings file: keep defaults
        }
    }

    private async Task SaveSettingsAsync(PersistedSettings settings)
    {
        try
        {
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(settings, PersistedSettingsContext.Default.PersistedSettings));
        }
        catch
        {
            // persistence is best-effort
        }
    }

    public void Dispose()
    {
        _d.Dispose();
        Configuration.Dispose();
        Run.Dispose();
        Content.Dispose();
        Sender.Dispose();
        Convert.Dispose();
        _jsonFiles.Dispose();
    }
}
