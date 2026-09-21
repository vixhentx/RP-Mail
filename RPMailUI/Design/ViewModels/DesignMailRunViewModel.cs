using R3;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignMailRunViewModel : IMailRunViewModel
{
    public ITaskListViewModel TaskList { get; } = new DesignTaskListViewModel();
    public IReadOnlyBindableReactiveProperty<string> ConsoleLog { get; } =
        new BindableReactiveProperty<string>("Loading contacts...\n2 messages sent\n1 message failed");
    public IReadOnlyBindableReactiveProperty<double> Progress { get; } =
        new BindableReactiveProperty<double>(0.618);
    public IReadOnlyBindableReactiveProperty<bool> ShouldRetry { get; } =
        new BindableReactiveProperty<bool>(true);
    public IReadOnlyBindableReactiveProperty<bool> ShouldOpenOutputFolder { get; } =
        new BindableReactiveProperty<bool>(true);
	public IReadOnlyBindableReactiveProperty<bool> ShouldStart =>
		new BindableReactiveProperty<bool>(true);
	public IReadOnlyBindableReactiveProperty<bool> ShouldCancel =>
		new BindableReactiveProperty<bool>(true);

    public ReactiveCommand OpenOutputFolderCommand { get; } = new();
    public ReactiveCommand RetryCommand { get; } = new();
    public ReactiveCommand StartCommand { get; } = new();
	public ReactiveCommand CancelCommand { get; } = new();


	public void Dispose()
    {
        TaskList.Dispose();
        ConsoleLog.Dispose();
        Progress.Dispose();
        ShouldRetry.Dispose();
        ShouldOpenOutputFolder.Dispose();
		ShouldStart.Dispose();
		ShouldCancel.Dispose();
        OpenOutputFolderCommand.Dispose();
        RetryCommand.Dispose();
        StartCommand.Dispose();
		CancelCommand.Dispose();
    }
}
