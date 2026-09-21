using System.Collections.Immutable;
using System.Diagnostics;
using R3;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.ViewModels;

file readonly record struct RawItem(
	IReadOnlyDictionary<string, string> Data,
	MailTaskStatus Status,
	string? OverrideTooltip
)
{
	public IEnumerable<string> SearchTokens => [ ..Data.Values, Status.Text ];
}

public sealed class TaskListViewModel : ITaskListViewModel
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

		AvailableHeaders = output
			.Where(static x => x is RowsLoadedOutput { Rows.Count: >0 })
			.Select(static x => (RowsLoadedOutput)x)
			.Select(static x => x.Rows[0].Keys.ToImmutableArray())
			.ObserveOnUIThreadDispatcher()
			.ToReadOnlyBindableReactiveProperty([])
			.AddTo(ref _d);
		AvailableHeaders
			.AsObservable()
			.Subscribe(headers => SelectedHeader.Value = headers.FirstOrDefault())
			.AddTo(ref _d);

		var searchTokens = SearchText
			.Debounce(TimeSpan.FromSeconds(0.5))
			.Select(static t =>
				string.IsNullOrWhiteSpace(t)
				? []
				: t.Split(
					Separators,
					StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
				)
			);

		Tasks = output
			.ObserveOnThreadPool() // 处理比较耗时, 在线程池里跑
			.Where(static x => x is RowsLoadedOutput or TaskStateOutput)
			.WithLatestFrom(SelectedHeader, static (output,selected) => (output,selected))
			.Scan(ImmutableArray<RawItem>.Empty, static (items, pipe) =>
				pipe.output switch
				{
					RowsLoadedOutput rl =>
						rl.Rows
						.Select(static row => new RawItem(
							Data: row,
							Status: MailTaskStatus.Ready,
							OverrideTooltip: null
						))
						.ToImmutableArray(),
					TaskStateOutput s =>
						items.SetItem(
							s.Index,
							items[s.Index] with
							{
								Status = s.Status,
								OverrideTooltip = s.Message
							}),
					_ => throw new UnreachableException(),
				}
			) // RawItems On ThreadPool
			.CombineLatest(
				searchTokens,
				SelectedHeader,
				static (items, searchs, header) =>
					items
					.Where(item => searchs.Length == 0 || searchs.Any(s => item.SearchTokens.Contains(s))) // apply search filter
					.Select(item => // apply header selection and transform into final model
						new TaskItemData(
							Text: (header is {} && item.Data.TryGetValue(header, out var t))
								|| item.Data.TryGetValue("email", out t)
								? t
								: "unspecified",
							Status: item.Status,
							TooltipText: item.OverrideTooltip ?? item.Status.Text
						)
					)
					.OrderBy(static x => x.Ordinal) // sort
					.ToImmutableArray()
			)
			.ObserveOnUIThreadDispatcher()
			.ToReadOnlyBindableReactiveProperty([])
			.AddTo(ref _d);
	}

    public void Dispose() => _d.Dispose();
}
