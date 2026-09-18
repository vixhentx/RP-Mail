using System;
using System.Collections.Specialized;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public sealed class AttachmentListViewModel : IDisposable
{
    private DisposableBag _d = new();

    private readonly Subject<Unit> _itemChanges = new();
    private readonly ISynchronizedView<AttachmentItemData, IDisposable> _view;

    public ObservableList<AttachmentItemData> Items { get; } = [new()];

    public NotifyCollectionChangedSynchronizedViewList<AttachmentItemData> ItemsView { get; }

    public BindableReactiveProperty<AttachmentItemData?> SelectedItem { get; } = new();

    public ReactiveCommand AppendCommand { get; } = new();

    public ReactiveCommand RemoveCommand { get; } = new();

    public Observable<Unit> Changes { get; }

    public AttachmentListViewModel()
    {
        ItemsView = Items.ToNotifyCollectionChangedSlim();

        _view = Items.CreateView(item => Observable
            .Merge(
                item.SourceText.Select(static _ => Unit.Default),
                item.DestinationText.Select(static _ => Unit.Default))
            .Subscribe(_ => _itemChanges.OnNext(Unit.Default)));

        _view.ViewChanged += OnViewChanged;

        Changes = Observable.Merge(
            Items.ObserveChanged().Select(static _ => Unit.Default),
            _itemChanges);

        AppendCommand.Subscribe(_ => Items.Add(new()))
            .AddTo(ref _d);

        RemoveCommand.Subscribe(_ =>
            {
                if (SelectedItem.Value is not { } selected)
                {
                    return;
                }

                Items.Remove(selected);
                SelectedItem.Value = null;
            })
            .AddTo(ref _d);
    }

    private void OnViewChanged(in SynchronizedViewChangedEventArgs<AttachmentItemData, IDisposable> e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                if (e.IsSingleItem)
                {
                    e.OldItem.View.Dispose();
                }
                else
                {
                    DisposeViews(e.OldViews);
                }

                break;
            case NotifyCollectionChangedAction.Reset:
                DisposeViews(e.OldViews);
                break;
        }
    }

    private static void DisposeViews(ReadOnlySpan<IDisposable> views)
    {
        foreach (var view in views)
        {
            view.Dispose();
        }
    }

    public void Dispose()
    {
        _view.ViewChanged -= OnViewChanged;

        foreach (var view in _view)
        {
            view.Dispose();
        }

        _view.Dispose();
        _itemChanges.Dispose();
        _d.Dispose();
        ItemsView.Dispose();
        SelectedItem.Dispose();
        AppendCommand.Dispose();
        RemoveCommand.Dispose();
    }
}
