using System;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Threading;
using R3;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Resources;

namespace RPMailUI.ViewModels;

public sealed class TaskListViewModel : IDisposable
{
    private static readonly char[] Separators = [' ', ',', '&'];

    private DisposableBag _bag = new();

    public TaskListViewModel(Observable<MailRunOutput> output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var taskItems = output
            .Where(static x => x is RowsLoadedOutput or TaskStateOutput)
            .Scan(ImmutableArray<TaskItemData>.Empty, static (items, x) => Reduce(items, x))
            .ObserveOnUIThreadDispatcher()
            .ToReadOnlyBindableReactiveProperty(ImmutableArray<TaskItemData>.Empty)
            .AddTo(ref _bag);

        AvailableHeaders = taskItems
            .AsObservable()
            .Select(static items => items
                .SelectMany(static item => item.Data.Keys)
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray())
            .ToReadOnlyBindableReactiveProperty(ImmutableArray<string>.Empty);

        Tasks = taskItems
            .AsObservable()
            .CombineLatest(
                SearchText.AsObservable(),
                SelectedHeader.AsObservable(),
                static (items, search, _) => Filter(items, search))
            .ToReadOnlyBindableReactiveProperty(ImmutableArray<TaskItemData>.Empty);

        AvailableHeaders
            .AsObservable()
            .Subscribe(headers => SelectedHeader.Value = headers.FirstOrDefault())
            .AddTo(ref _bag);

        _bag.Add(SearchText);
        _bag.Add(SelectedHeader);
        _bag.Add(AvailableHeaders);
        _bag.Add(Tasks);
    }

    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);

    public BindableReactiveProperty<string?> SelectedHeader { get; } = new(null);

    public IReadOnlyBindableReactiveProperty<ImmutableArray<string>> AvailableHeaders { get; }

    public IReadOnlyBindableReactiveProperty<ImmutableArray<TaskItemData>> Tasks { get; }

    private static ImmutableArray<TaskItemData> Reduce(ImmutableArray<TaskItemData> items, MailRunOutput output) => output switch
    {
        RowsLoadedOutput rows => rows.Rows
            .Select(static row => new TaskItemData(row, MailTaskStatus.Ready, Strings.ReadyToSend))
            .ToImmutableArray(),
        TaskStateOutput state when state.Index >= 0 && state.Index < items.Length => items.SetItem(
            state.Index,
            items[state.Index] with
            {
                Status = state.Status,
                Tooltip = state.Message ?? items[state.Index].Tooltip,
            }),
        _ => items,
    };

    private static ImmutableArray<TaskItemData> Filter(ImmutableArray<TaskItemData> items, string? searchText)
    {
        string[] filters = string.IsNullOrWhiteSpace(searchText)
            ? []
            : searchText.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var query = items.AsEnumerable();

        if (filters.Length > 0)
        {
            query = query.Where(item => filters.Any(filter =>
                item.SearchTokens.Any(token => token.Contains(filter))));
        }

        return query
            .OrderBy(static item => item.Status.Ordinal)
            .ToImmutableArray();
    }

    public void Dispose() => _bag.Dispose();
}
