using System;
using R3;

namespace RPMailUI.Contracts.ViewModels;

public interface IMailRunViewModel : IDisposable
{
    ITaskListViewModel TaskList { get; }
    IReadOnlyBindableReactiveProperty<string> ConsoleLog { get; }
    IReadOnlyBindableReactiveProperty<double> Progress { get; }
    IReadOnlyBindableReactiveProperty<bool> ShouldRetry { get; }
    IReadOnlyBindableReactiveProperty<bool> ShouldOpenOutputFolder { get; }
    IReadOnlyBindableReactiveProperty<bool> ShouldStart { get; }
    IReadOnlyBindableReactiveProperty<bool> ShouldCancel { get; }
    ReactiveCommand OpenOutputFolderCommand { get; }
    ReactiveCommand RetryCommand { get; }
    ReactiveCommand StartCommand { get; }
    ReactiveCommand CancelCommand { get; }
}
