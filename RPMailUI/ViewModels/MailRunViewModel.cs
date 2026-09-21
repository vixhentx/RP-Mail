using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Models;
using RPMailCore.Processors;
using RPMailUI.Models;
using RPMailUI.Services;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.ViewModels;

public sealed class MailRunViewModel : IMailRunViewModel
{
	readonly DisposableBag _d = new();
	readonly MailRunProcessor _processor;
	readonly ReadOnlyReactiveProperty<RunResult> _result;

	// 暴露给View
	public ITaskListViewModel TaskList { get; }

	public IReadOnlyBindableReactiveProperty<string> ConsoleLog { get; }
	public IReadOnlyBindableReactiveProperty<double> Progress { get; }
	public IReadOnlyBindableReactiveProperty<bool> ShouldRetry { get; }
	public IReadOnlyBindableReactiveProperty<bool> ShouldOpenOutputFolder { get; }

	public ReactiveCommand OpenOutputFolderCommand { get; }
	public ReactiveCommand RetryCommand { get; }
	public ReactiveCommand StartCommand { get; } = new();

	// 对MainViewModel暴露属性
	public Observable<string> RetryCsvPath { get; }

	public MailRunViewModel(
		ErrorRouteService es,
		MailRunProcessor processor,
		ITaskListViewModel taskList,
		ConfigService conf
	)
	{
		_processor = processor;

		TaskList = taskList;

		var trigger = StartCommand.AsUnitObservable();

		// 绑定 StartCommand，并收集运行结果
		_result =
			trigger
				.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value)
				.ObserveOnThreadPool()
				.SelectAwait(
					(config, ct) => _processor.RunAsync(config.Mail, config.WorkspaceDirectory, ct),
					awaitOperation: AwaitOperation.Drop
				)
				.ObserveOnUIThreadDispatcher()
				.ToReadOnlyReactiveProperty(null!)
				.AddTo(ref _d);

		ShouldOpenOutputFolder =
			_result.Select(static r => r is not null)
				.ToReadOnlyBindableReactiveProperty()
				.AddTo(ref _d);

		OpenOutputFolderCommand =
			_result.Select(static r => r is not null)
				.ToReactiveCommand()
				.AddTo(ref _d);

		OpenOutputFolderCommand
			.WithLatestFrom(_result, static (_, r) => r!)
			.Subscribe(r => PathOpenHelper.OpenDirectory(r.RealOutputDir))
			.AddTo(ref _d);

		ShouldRetry =
			_result.Select(static r => r is { Success: true, FailedRows: > 0 })
				.ToReadOnlyBindableReactiveProperty()
				.AddTo(ref _d);

		RetryCommand =
			_result.Select(static r => r is { Success: true, FailedRows: > 0 })
				.ToReactiveCommand()
				.AddTo(ref _d);

		RetryCsvPath =
			RetryCommand
				.WithLatestFrom(_result, static (_, r) => r!.FailedCsvPath!);

		Progress = _processor.Progress
			.ObserveOnUIThreadDispatcher()
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);

		ConsoleLog = _processor.Output
			.Where(static x => x is MessageOutput)
			.ObserveOnUIThreadDispatcher()
			.Scan(
				"",
				static (lastStr, output) =>
					lastStr + output.Text + Environment.NewLine
			)
			.ToReadOnlyBindableReactiveProperty("")
			.AddTo(ref _d);

		_processor.Output
			.Where(static o => o.Level >= LogLevel.Warning)
			.Select(static o => ErrorItemData.Create(o.Level, o.Text))
			.Subscribe(es.Error.OnNext)
			.AddTo(ref _d);

		OpenOutputFolderCommand.AddTo(ref _d);
		RetryCommand.AddTo(ref _d);
		ConsoleLog.AddTo(ref _d);
		Progress.AddTo(ref _d);
		ShouldRetry.AddTo(ref _d);
		ShouldOpenOutputFolder.AddTo(ref _d);
	}

	public void Dispose()
	{
		_d.Dispose();
		TaskList.Dispose();
	}
}
