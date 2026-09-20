using System.Collections.Immutable;
using ObservableCollections;
using R3;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class ExtraAttributeListViewModel : IDisposable
{
	readonly DisposableBag _d = new();
	readonly ObservableList<ExtraAttributeItemData> _items = [new()];
	bool _synching = false;

	// 暴露给View
	public NotifyCollectionChangedSynchronizedViewList<ExtraAttributeItemData> ItemsView { get; }

	public BindableReactiveProperty<ExtraAttributeItemData?> SelectedItem { get; } = new();

	public IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }

	public ReactiveCommand AppendCommand { get; }

	public ReactiveCommand RemoveCommand { get; }

	// 暴露给上层
	public Observable<ImmutableDictionary<string, string>> ConfOut { get; }

	public ExtraAttributeListViewModel(ConfigService conf)
	{
		ItemsView = _items.ToNotifyCollectionChangedSlim()
			.AddTo(ref _d);

		// 配置传入
		conf.Pipe
			.DistinctUntilChangedBy(static p => p.Value.Mail.Template.ExtraAttributes)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Select(static p => p.Value.Mail.Template.ExtraAttributes)
			.Subscribe(LoadItems)
			.AddTo(ref _d);

		// 集合变化检测与 model 变换
		var changeSignal = _items
			.ObserveChanged()
			.Select(
				_items,
				static (_, items) =>
					Observable.Merge(
						items.Select(
							static x =>
								Observable.Merge(
									x.Key.AsUnitObservable(),
									x.Value.AsUnitObservable()
								)
						)
					)
			)
			.Switch()
			.Where(_ => !_synching);

		ConfOut = Observable.Merge(
				Observable.Return(default(Unit)),
				changeSignal
			)
			.Select(
				_items,
				static (_, items) =>
					items.ToImmutableDictionary(
						keySelector: static x => x.Key.Value,
						elementSelector: static x => x.Value.Value
					)
			);

		// 用户编辑产生配置
		ConfOut
			.WithLatestFrom(
				conf.Pipe,
				static (attributes, pipe) => new ConfPipe(
					pipe.Value with
					{
						Mail = pipe.Value.Mail with
						{
							Template = pipe.Value.Mail.Template with { ExtraAttributes = attributes }
						}
					},
					ConfChangingSource.UserEdit
				)
			)
			.Where(static p => p.Source is ConfChangingSource.UserEdit)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		AppendCommand = new();
		AppendCommand
			.Subscribe(_ => _items.Add(new()))
			.AddTo(ref _d);

		var shouldRemove = SelectedItem
			.Select(static x => x is not null);

		ShouldRemoveItem = shouldRemove
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(ref _d);

		RemoveCommand = shouldRemove.ToReactiveCommand();
		RemoveCommand
			.WithLatestFrom(SelectedItem, static (_, item) => item!)
			.Subscribe(item =>
			{
				_items.Remove(item);
				SelectedItem.Value = null;
			})
			.AddTo(ref _d);
	}

	// 从配置载入项集合
	public void LoadItems(ImmutableDictionary<string, string> attributes)
	{
		_synching = true;

		_items.Clear();
		foreach (var (key, value) in attributes)
		{
			var item = new ExtraAttributeItemData();
			item.Key.Value = key;
			item.Value.Value = value;
			_items.Add(item);
		}

		_synching = false;
	}

	public void Dispose()
	{
		_d.Dispose();
		SelectedItem.Dispose();
	}
}
