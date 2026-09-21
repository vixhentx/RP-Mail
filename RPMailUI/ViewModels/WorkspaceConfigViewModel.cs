using R3;
using RPMailCore.Serialization;
using RPMailUI.Models;
using RPMailUI.Services;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.ViewModels;

/// <summary>
/// 管理工作区以及根配置加载
/// </summary>
public sealed class WorkspaceConfigViewModel : IWorkspaceConfigViewModel
{
	readonly DisposableBag _d = new();

	public BindableReactiveProperty<string> WorkspaceDirectory { get; } = new(Directory.GetCurrentDirectory());
	public ReactiveCommand BrowseWorkspaceCommand { get; } = new();
	public ReactiveCommand ImportMailConfigCommand { get; } = new();
	public ReactiveCommand ExportMailConfigCommand { get; } = new();

	public WorkspaceConfigViewModel(ConfigService conf, JsonFileDialogService fileDialog)
	{
		conf.Pipe
			.Select(static pipe => pipe.Value.WorkspaceDirectory)
			.DistinctUntilChanged()
			.Subscribe(WorkspaceDirectory, static (directory, property) => property.Value = directory)
			.AddTo(ref _d);

		WorkspaceDirectory
			.Select(Path.GetFullPath)
			.WithLatestFrom(
				conf.Pipe,
				static (directory, pipe) => directory == pipe.Value.WorkspaceDirectory
					? null
					: new ConfPipe(pipe.Value with { WorkspaceDirectory = directory }, ConfChangingSource.UserEdit)
			)
			.WhereNotNull()
			.Subscribe(conf.Pipe, static (pipe, target) => target.Value = pipe)
			.AddTo(ref _d);

		BrowseWorkspaceCommand
			.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value.WorkspaceDirectory)
			.SelectAwait((directory, ct) => fileDialog.PickDirectory(directory))
			.WhereNotNull()
			.Subscribe(WorkspaceDirectory, static (directory, property) => property.Value = directory)
			.AddTo(ref _d);

		ImportMailConfigCommand
			.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value.WorkspaceDirectory)
			.SelectAwait((directory, ct) => fileDialog.ReadConf(RPMailJsonContext.Default.MailConfig, directory, ct))
			.WhereNotNull()
			.WithLatestFrom(
				conf.Pipe,
				static (loaded, pipe) => new ConfPipe(
					pipe.Value with
					{
						Mail = loaded.Value,
						WorkspaceDirectory = loaded.Directory,
					},
					ConfChangingSource.Import
				)
			)
			.Subscribe(conf.Pipe, static (pipe, target) => target.Value = pipe)
			.AddTo(ref _d);

		ExportMailConfigCommand
			.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value)
			.SubscribeAwait((config, ct) => fileDialog.WriteConf(
				config.Mail,
				RPMailJsonContext.Default.MailConfig,
				"mail_config.json",
				config.WorkspaceDirectory,
				ct
			))
			.AddTo(ref _d);
	}

	public void Dispose()
	{
		_d.Dispose();
		BrowseWorkspaceCommand.Dispose();
		ImportMailConfigCommand.Dispose();
		ExportMailConfigCommand.Dispose();
		WorkspaceDirectory.Dispose();
	}
}
