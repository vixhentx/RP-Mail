using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Jab;
using RPMailCore.Processors;
using RPMailUI.Contracts.ViewModels;
using RPMailUI.Services;
using RPMailUI.ViewModels;
using RPMailUI.Views;

namespace RPMailUI;

[ServiceProvider]
// Views
[Scoped<MainWindow>]
[Scoped<TopLevel>(Factory = nameof(TopLevelFactory))]

// ViewModels
[Transient<IMainWindowViewModel, MainWindowViewModel>]
[Transient<IContentSettingsViewModel, ContentSettingsViewModel>]
[Transient<IConvertSettingsViewModel, ConvertSettingsViewModel>]
[Transient<ISenderSettingsViewModel, SenderSettingsViewModel>]
[Transient<IErrorViewModel, ErrorViewModel>]
[Transient<IMailRunViewModel, MailRunViewModel>]
[Transient<ITaskListViewModel>(Factory = nameof(TaskListViewModelFactory))]
[Transient<IAttachmentListViewModel, AttachmentListViewModel>]
[Transient<IExtraAttributeListViewModel, ExtraAttributeListViewModel>]
[Transient<IWorkspaceConfigViewModel, WorkspaceConfigViewModel>]

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
