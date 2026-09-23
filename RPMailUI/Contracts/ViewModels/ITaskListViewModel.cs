using System;
using System.Collections.Immutable;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Contracts.ViewModels;

public interface ITaskListViewModel : IDisposable
{
    IReadOnlyBindableReactiveProperty<int> TotalCount { get; }
    IReadOnlyBindableReactiveProperty<int> SuccessCount { get; }
    IReadOnlyBindableReactiveProperty<int> FailedCount { get; }
    BindableReactiveProperty<string> SearchText { get; }
    BindableReactiveProperty<string?> SelectedHeader { get; }
    IReadOnlyBindableReactiveProperty<ImmutableArray<string>> AvailableHeaders { get; }
    IReadOnlyBindableReactiveProperty<ImmutableArray<TaskItemData>> Tasks { get; }
}
