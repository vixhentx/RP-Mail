using System.Collections.Immutable;
using ObservableCollections;
using R3;
using RPMailCore.Models;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public sealed class AttachmentListViewModel : IDisposable
{
    readonly DisposableBag _d = new();
    readonly ObservableList<AttachmentItemData> _items = [new()];
	readonly Subject<Unit> _syncComplete = new();
	bool _synching;
	// 暴露给View
    public NotifyCollectionChangedSynchronizedViewList<AttachmentItemData> ItemsView { get; }

    public BindableReactiveProperty<AttachmentItemData?> SelectedItem { get; } = new();

	public IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }

    public ReactiveCommand AppendCommand { get; }

    public ReactiveCommand RemoveCommand { get; }

	// 暴露给上层
    public Observable<ImmutableArray<AttachmentPattern>> ConfOut { get; }

    public AttachmentListViewModel()
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
									x.SourceText.AsUnitObservable(),
									x.DestinationText.AsUnitObservable()
								)
							)
					)
				)
			.Switch() // change sig
			.Where(_ => !_synching)
			.Merge(_syncComplete)
			.Select(
				_items,
				static (_,items) =>
					items
						.Select(static x => new AttachmentPattern()
						{
							Source = x.SourceText.Value,
							Name = x.DestinationText.Value,
						})
						.ToImmutableArray()
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

    // 从配置载入项集合
    public void LoadItems(ImmutableArray<AttachmentPattern> attachments)
    {
		_synching = true;
        _items.Clear();
        foreach (var attachment in attachments)
        {
            var item = new AttachmentItemData();
            item.SourceText.Value = attachment.Source;
            item.DestinationText.Value = attachment.Name;
            _items.Add(item);
        }
		_synching = false;
		_syncComplete.OnNext(default);
    }

    public void Dispose()
    {
        _d.Dispose();
		_syncComplete.Dispose();
        SelectedItem.Dispose();
    }
}
