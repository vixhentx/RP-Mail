using System.Collections.Immutable;
using ObservableCollections;
using R3;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class AttachmentListViewModel : IDisposable
{
	readonly DisposableBag _d = new();
	readonly ObservableList<AttachmentItemData> _items = [new()];
	bool _synching;

	// 暴露给View
	public NotifyCollectionChangedSynchronizedViewList<AttachmentItemData> ItemsView { get; }

	public BindableReactiveProperty<AttachmentItemData?> SelectedItem { get; } = new();
	public BindableReactiveProperty<string> WorkspaceDirectory { get; } = new(Directory.GetCurrentDirectory());

	public IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }

	public ReactiveCommand AppendCommand { get; }

	public ReactiveCommand RemoveCommand { get; }

	// 暴露给上层
	public Observable<ImmutableArray<AttachmentPattern>> ConfOut { get; }

	public AttachmentListViewModel(ConfigService conf)
	{
		ItemsView = _items.ToNotifyCollectionChangedSlim()
			.AddTo(ref _d);

		// 配置传入
		conf.Pipe
			.Select(static p => p.Value.WorkspaceDirectory)
			.DistinctUntilChanged()
			.Subscribe(WorkspaceDirectory, static (directory, property) => property.Value = directory)
			.AddTo(ref _d);

		conf.Pipe
			.DistinctUntilChangedBy(static p => p.Value.Mail.Template.Attachments)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Select(static p => p.Value.Mail.Template.Attachments)
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
									x.SourceText.AsUnitObservable(),
									x.DestinationText.AsUnitObservable()
								)
						)
					)
			)
			.Switch()
			.Where(_ => !_synching);

		ConfOut =
			changeSignal
			.Prepend(value: default)
			.Select(
				_items,
				static (_, items) =>
					items
						.Select(static x => new AttachmentPattern()
						{
							Source = x.SourceText.Value,
							Name = x.DestinationText.Value,
						})
						.ToImmutableArray()
			);

		// 用户编辑产生配置
		ConfOut
			.WithLatestFrom(
				conf.Pipe,
				static (attachments, pipe) => new ConfPipe(
					pipe.Value with
					{
						Mail = pipe.Value.Mail with
						{
							Template = pipe.Value.Mail.Template with { Attachments = attachments }
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
	}

	public void Dispose()
	{
		_d.Dispose();
		SelectedItem.Dispose();
		WorkspaceDirectory.Dispose();
	}
}
