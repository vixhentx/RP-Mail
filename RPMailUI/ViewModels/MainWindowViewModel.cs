using System;
using System.IO;
using System.Text.Json;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
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

    public Observable<MailConfig> ConfOut { get; }

    public IStorageProvider? StorageProvider
    {
        get => _jsonFiles.StorageProvider;
        set => _jsonFiles.StorageProvider = value;
    }

    public MainWindowViewModel()
    {
        Content = new ContentSettingsViewModel();
        Sender = new SenderSettingsViewModel();
        Convert = new ConvertSettingsViewModel();

		ConfOut =
			Observable.CombineLatest(
				Content.ConfOut,
				Sender.ConfOut,
				Convert.ConfOut,
				static (content, sender, convert) =>
					new MailConfig()
					{
						Sender = sender,
						Template = content,
						Output = convert
					}
			);

        Run = new MailRunViewModel(
            ConfOut,
			Observable.Empty<string>()
		);
    }


    public void Dispose()
    {
        _d.Dispose();
        Run.Dispose();
        Content.Dispose();
        Sender.Dispose();
        Convert.Dispose();
        _jsonFiles.Dispose();
    }
}
