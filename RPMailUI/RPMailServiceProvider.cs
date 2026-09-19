using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Jab;
using RPMailCore.Processors;
using RPMailUI.Services;
using RPMailUI.ViewModels;
using RPMailUI.Views;

namespace RPMailUI;

[ServiceProvider]
// Views
[Scoped<MainWindow>]
[Scoped<TopLevel>(Factory = nameof(TopLevelFactory))]

// ViewModels
[Transient<MainWindowViewModel>]
[Transient<ContentSettingsViewModel>]
[Transient<ConvertSettingsViewModel>]
[Transient<SenderSettingsViewModel>]
[Transient<ErrorViewModel>]
[Transient<MailRunViewModel>]
[Transient<TaskListViewModel>(Factory = nameof(TaskListViewModelFactory))]
[Transient<AttachmentListViewModel>]
[Transient<ExtraAttributeListViewModel>]

// Normal Service
[Scoped<ConfigService>]
[Scoped<ErrorRouteService>]
[Scoped<MailRunProcessor>]
[Scoped<PersistenceService>]
[Scoped<IStorageProvider>(Factory = nameof(StorageProviderFacotory))]
[Scoped<JsonFileDialogService>]
partial class RPMailServiceProvider
{
	static TopLevel TopLevelFactory
		(MainWindow window) => window;
	static IStorageProvider StorageProviderFacotory
		(TopLevel topLevel) => topLevel.StorageProvider;
	static TaskListViewModel TaskListViewModelFactory
		(MailRunProcessor processor) => new(processor.Output);

}