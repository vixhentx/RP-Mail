using ObservableCollections;
using R3;
using RPMailUI.Contracts.ViewModels;
using RPMailUI.Models;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignExtraAttributeListViewModel : IExtraAttributeListViewModel
{
    readonly ObservableList<ExtraAttributeItemData> _items =
    [
        Create("CustomerName", "Alice"),
        Create("Campaign", "Spring launch")
    ];
    readonly DisposableBag _d = new();

    public NotifyCollectionChangedSynchronizedViewList<ExtraAttributeItemData> ItemsView { get; }
    public BindableReactiveProperty<ExtraAttributeItemData?> SelectedItem { get; } = new();
    public IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }
    public ReactiveCommand AppendCommand { get; } = new();
    public ReactiveCommand RemoveCommand { get; } = new();

    public DesignExtraAttributeListViewModel()
    {
        ItemsView = _items.ToNotifyCollectionChangedSlim().AddTo(ref _d);
        ShouldRemoveItem = new BindableReactiveProperty<bool>(true);
    }

    static ExtraAttributeItemData Create(string key, string value)
    {
        var item = new ExtraAttributeItemData();
        item.Key.Value = key;
        item.Value.Value = value;
        return item;
    }

    public void Dispose()
    {
        _d.Dispose();
        SelectedItem.Dispose();
        ShouldRemoveItem.Dispose();
        AppendCommand.Dispose();
        RemoveCommand.Dispose();
    }
}
