using System;
using System.Collections.Specialized;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public sealed class ExtraAttributeListViewModel : IDisposable
{
    private DisposableBag _d = new();

    private readonly Subject<Unit> _itemChanges = new();
    private readonly ISynchronizedView<ExtraAttributeItemData, IDisposable> _view;

    public ObservableList<ExtraAttributeItemData> Items { get; } = [new()];

    public NotifyCollectionChangedSynchronizedViewList<ExtraAttributeItemData> ItemsView { get; }

    public BindableReactiveProperty<ExtraAttributeItemData?> SelectedItem { get; } = new();

    public ReactiveCommand AppendCommand { get; } = new();

    public ReactiveCommand RemoveCommand { get; } = new();

    public Observable<Unit> Changes { get; }

    public ExtraAttributeListViewModel()
    {
        ItemsView = Items.ToNotifyCollectionChangedSlim();

        _view = Items.CreateView(item => Observable
            .Merge(
                item.Key.Select(static _ => Unit.Default),
                item.Value.Select(static _ => Unit.Default))
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

    private void OnViewChanged(in SynchronizedViewChangedEventArgs<ExtraAttributeItemData, IDisposable> e)
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
