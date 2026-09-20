using System.Collections.Immutable;
using System.Diagnostics;
using R3;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Resources;
using SmartFormat;

namespace RPMailUI.ViewModels;

public sealed class TaskListViewModel : IDisposable
{
    public static char[] Separators => [' ', ',', '&'];

    readonly DisposableBag _d = new();

	// 暴露给View
    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);

    public BindableReactiveProperty<string?> SelectedHeader { get; } = new(null);

    public IReadOnlyBindableReactiveProperty<ImmutableArray<string>> AvailableHeaders { get; }

    public IReadOnlyBindableReactiveProperty<ImmutableArray<TaskItemData>> Tasks { get; }

    public TaskListViewModel(Observable<MailRunOutput> output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var taskItemsOnThreadPool = output
            .ObserveOnThreadPool()
            .Where(static x => x is RowsLoadedOutput or TaskStateOutput)
            .Scan(ImmutableArray<TaskItemData>.Empty, static (items, x) => Reduce(items, x))
            .ToReadOnlyBindableReactiveProperty([])
            .AddTo(ref _d);

        AvailableHeaders =
			taskItemsOnThreadPool
            .AsObservable()
            .Select(static items => items
                .SelectMany(static item => item.Data.Keys)
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray())
			.ObserveOnUIThreadDispatcher()
            .ToReadOnlyBindableReactiveProperty([]);

        Tasks =
			taskItemsOnThreadPool
            .AsObservable()
            .CombineLatest(
                SearchText.AsObservable(),
                SelectedHeader.AsObservable(),
                static (items, search, _) => Filter(items, search)
			)
			.ObserveOnUIThreadDispatcher()
            .ToReadOnlyBindableReactiveProperty([]);

        AvailableHeaders
            .AsObservable()
            .Subscribe(headers => SelectedHeader.Value = headers.FirstOrDefault())
            .AddTo(ref _d);

        _d.Add(SearchText);
        _d.Add(SelectedHeader);
        _d.Add(AvailableHeaders);
        _d.Add(Tasks);
    }

    private static ImmutableArray<TaskItemData> Reduce(ImmutableArray<TaskItemData> items, MailRunOutput output) => output switch
    {
        RowsLoadedOutput rows => rows.Rows
            .Select(static row => new TaskItemData(row, MailTaskStatus.Ready, Strings.ReadyToSend))
            .ToImmutableArray(),
        TaskStateOutput state => items.SetItem(
            state.Index,
            items[state.Index] with
            {
                Status = state.Status,
                Tooltip = state.Message ?? state.Status.Text
            }),
        _ => throw new UnreachableException(),
    };

    private static ImmutableArray<TaskItemData> Filter(ImmutableArray<TaskItemData> items, string? searchText)
    {
        string[] filters = string.IsNullOrWhiteSpace(searchText)
            ? []
            : searchText.Split(
				Separators,
				StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
			);

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

    public void Dispose() => _d.Dispose();
}
