using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Models;
using RPMailCore.Processors;
using RPMailUI.Models;
using RPMailUI.Services;
using RPMailUI.Contracts.ViewModels;
using System.Diagnostics;

namespace RPMailUI.ViewModels;

file enum TriggerOperation
{
	Start,
	Cancel
}

public sealed class MailRunViewModel : IMailRunViewModel
{
	readonly DisposableBag _d = new();
	public ITaskListViewModel TaskList { get; }

	public IReadOnlyBindableReactiveProperty<string> ConsoleLog { get; }
	public IReadOnlyBindableReactiveProperty<double> Progress { get; }

	public IReadOnlyBindableReactiveProperty<bool> ShouldRetry { get; }
	public IReadOnlyBindableReactiveProperty<bool> ShouldOpenOutputFolder { get; }
	public IReadOnlyBindableReactiveProperty<bool> ShouldStart { get; }
	public IReadOnlyBindableReactiveProperty<bool> ShouldCancel { get; }


	public ReactiveCommand OpenOutputFolderCommand { get; }
	public ReactiveCommand RetryCommand { get; }
	public ReactiveCommand StartCommand { get; }
	public ReactiveCommand CancelCommand {  get; }

	public MailRunViewModel(
		ErrorRouteService es,
		MailRunProcessor processor,
		ITaskListViewModel taskList,
		ConfigService conf
	)
	{
		TaskList = taskList;

		var shouldStart = processor.Running
			.Select(static x => !x)
			.ObserveOnUIThreadDispatcher();
		ShouldStart = shouldStart
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);
		StartCommand = shouldStart
			.ToReactiveCommand()
			.AddTo(ref _d);

		var shouldCancel = processor.Running
			.ObserveOnUIThreadDispatcher();
		ShouldCancel = shouldCancel
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);
		CancelCommand = shouldCancel
			.ToReactiveCommand()
			.AddTo(ref _d);

		var trigger =
			Observable.Merge(
				StartCommand.Select(_ => TriggerOperation.Start),
				CancelCommand.Select(_ => TriggerOperation.Cancel)
			);
		// 绑定 Start/Stop, 并收集运行结果
		var result =
			trigger
				.WithLatestFrom(conf.Pipe, static (trigger,pipe) => (trigger, conf: pipe.Value))
				.ObserveOnThreadPool()
				.SelectAwait(
					async (pipe, ct) => pipe.trigger switch
					{
						TriggerOperation.Start =>
							await processor.RunAsync(pipe.conf.Mail, pipe.conf.WorkspaceDirectory, ct),
						TriggerOperation.Cancel =>
							await ValueTask.FromResult<RunResult?>(null),
						_ => throw new UnreachableException()
					},
					awaitOperation: AwaitOperation.Switch
				)
				.WhereNotNull() // 滤掉取消状态
				.ObserveOnUIThreadDispatcher()
				.Publish();

		var hasResult = result
				.Select(_ => true)
				.Prepend(false)
				.Publish();

		ShouldOpenOutputFolder =
			hasResult
				.ToReadOnlyBindableReactiveProperty(false)
				.AddTo(ref _d);

		OpenOutputFolderCommand =
			hasResult
				.ToReactiveCommand()
				.AddTo(ref _d);

		OpenOutputFolderCommand
			.WithLatestFrom(result, static (_, r) => r)
			.Subscribe(r => PathOpenHelper.OpenDirectory(r.RealOutputDir))
			.AddTo(ref _d);

		var shouldRetry = 
			result.Select(static r => r is { Success: true, FailedRows: > 0 });
		ShouldRetry =
			shouldRetry
				.ToReadOnlyBindableReactiveProperty()
				.AddTo(ref _d);
		RetryCommand =
			shouldRetry
				.ToReactiveCommand()
				.AddTo(ref _d);

		Progress = processor.Progress
			.ObserveOnUIThreadDispatcher()
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);

		ConsoleLog = processor.Output
			.Where(static x => x is MessageOutput)
			.ObserveOnUIThreadDispatcher()
			.Scan(
				"",
				static (lastStr, output) =>
					lastStr + output.Text + Environment.NewLine
			)
			.ToReadOnlyBindableReactiveProperty("")
			.AddTo(ref _d);

		processor.Output
			.Where(static o => o.Level >= LogLevel.Warning)
			.Select(static o => ErrorItemData.Create(o.Level, o.Text))
			.Subscribe(es.Error.OnNext)
			.AddTo(ref _d);

		result.Connect()
			.AddTo(ref _d);
		hasResult.Connect()
			.AddTo(ref _d);
	}

	public void Dispose()
	{
		_d.Dispose();
		TaskList.Dispose();
	}
}
