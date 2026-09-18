using System.Collections.Immutable;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public sealed class ExtraAttributeListViewModel : IDisposable
{
    readonly DisposableBag _d = new();
    readonly ObservableList<ExtraAttributeItemData> _items = [new()];
	// 暴露给View
    public NotifyCollectionChangedSynchronizedViewList<ExtraAttributeItemData> ItemsView { get; }

    public BindableReactiveProperty<ExtraAttributeItemData?> SelectedItem { get; } = new();

	public IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }

    public ReactiveCommand AppendCommand { get; }

    public ReactiveCommand RemoveCommand { get; }

	// 暴露给上层
    public Observable<ImmutableDictionary<string, string>> ConfOut { get; }

    public ExtraAttributeListViewModel()
    {
        ItemsView = _items.ToNotifyCollectionChangedSlim()
			.AddTo(ref _d);

		// 集合变化检测与model变换
		ConfOut = _items
			.ObserveChanged()
			.Select(_items, static (_,items) =>
					Observable.Merge(
						items
							.Select(static x => 
								Observable.Merge(
									x.Key.AsUnitObservable(),
									x.Value.AsUnitObservable()
								)
							)
					)
				)
			.Switch() // change sig
			.Select(
				_items,
				static (_,items) =>
					items
						.ToImmutableDictionary(
							keySelector: static x => x.Key.Value,
							elementSelector: static x => x.Value.Value
						)
			); // modelize


		AppendCommand = new();
        AppendCommand.Subscribe(_ => _items.Add(new()))
            .AddTo(ref _d);

		var shouldRemove = SelectedItem
			.Select(static x => x is not null);

		ShouldRemoveItem = shouldRemove
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);
        RemoveCommand = shouldRemove.ToReactiveCommand();
		RemoveCommand
			.WithLatestFrom(SelectedItem, static (_,item) => item!)
			.Subscribe(item =>
            {
                _items.Remove(item);
                SelectedItem.Value = null;
            })
            .AddTo(ref _d);
    }

    public void Dispose()
    {
        _d.Dispose();
        SelectedItem.Dispose();
    }
}
