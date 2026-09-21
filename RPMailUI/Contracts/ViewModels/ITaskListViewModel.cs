using System;
using System.Collections.Immutable;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Contracts.ViewModels;

public interface ITaskListViewModel : IDisposable
{
    BindableReactiveProperty<string> SearchText { get; }
    BindableReactiveProperty<string?> SelectedHeader { get; }
    IReadOnlyBindableReactiveProperty<ImmutableArray<string>> AvailableHeaders { get; }
    IReadOnlyBindableReactiveProperty<ImmutableArray<TaskItemData>> Tasks { get; }
}
