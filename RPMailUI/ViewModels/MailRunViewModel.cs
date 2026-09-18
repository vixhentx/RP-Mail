using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Models;
using RPMailCore.Processors;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class MailRunViewModel : IDisposable
{
    public const int MaxErrorCount = 5;

    readonly DisposableBag _d = new();
    readonly MailRunProcessor _processor = new();
	readonly ReadOnlyReactiveProperty<RunResult> _result;

	// 暴露给View
    public TaskListViewModel TaskList { get; }
    public IReadOnlyBindableReactiveProperty<ImmutableArray<ErrorItemData>> Errors { get; }

    public IReadOnlyBindableReactiveProperty<string> ConsoleLog { get; }
    public IReadOnlyBindableReactiveProperty<double> Progress { get; }
    public IReadOnlyBindableReactiveProperty<bool> ShouldRetry { get; }
    public IReadOnlyBindableReactiveProperty<bool> ShouldOpenOutputFolder { get; }

    public ReactiveCommand OpenOutputFolderCommand { get; }
    public ReactiveCommand RetryCommand { get; }
    public ReactiveCommand StartCommand { get; } = new();

	// 对MainViewModel 暴露属性
	public Observable<string> RetryCsvPath { get; }

    public MailRunViewModel(
        Observable<MailConfig> configuration,
        Observable<string> moduleErrors
	)
    {
        TaskList = new TaskListViewModel(_processor.Output);

		var trigger = StartCommand.AsUnitObservable();

		// 绑定StartCommand, 并收集运行结果
		_result = 
			trigger
				.WithLatestFrom(configuration, static (_,conf) => conf)
				.SelectAwait(_processor.RunAsync)
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
			.WithLatestFrom(_result, static (_,r) => r!)
			.ObserveOnUIThreadDispatcher()
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
				.WithLatestFrom(_result, static (_,r) => r!.FailedCsvPath!);

		Progress = _processor.Progress
			.ObserveOnUIThreadDispatcher()
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);

		ConsoleLog = _processor.Output
			.ObserveOnUIThreadDispatcher()
			.Scan("",static (lastStr, output) =>
				lastStr + output.Text + Environment.NewLine
			)
			.ToReadOnlyBindableReactiveProperty("")
			.AddTo(ref _d);

		// Error显示
		static ErrorItemData CreateError(LogLevel level, string message)
		{
			string key = level >= LogLevel.Error ? "Flyout.Error" : "Flyout.Warning";
			return new(message, key);
		}
		Errors =
			Observable.Merge(
				moduleErrors
					.Select(static e => (reset: false, text: e, level: LogLevel.Error)),
				_processor.Output
					.Where(static o => o.Level >= LogLevel.Warning)
					.Select(static o => (reset: false, text: o.Text, level: o.Level)),
				trigger
					.Select(static _ => (reset: true, text: default(string)!, level: default(LogLevel)))
			)
			.Scan(
				ImmutableArray<ErrorItemData>.Empty,
				static (acc, e) => (acc,e) switch
				{
					{ e.reset: true } => [],
					{ acc.Length: < MaxErrorCount } => [ ..acc, CreateError(e.level, e.text) ],
					{ acc.Length: >= MaxErrorCount } => [ ..acc[^(MaxErrorCount-1)..], CreateError(e.level, e.text)]
				}
			)
			.ToReadOnlyBindableReactiveProperty()
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
        _processor.Dispose();
    }
}
