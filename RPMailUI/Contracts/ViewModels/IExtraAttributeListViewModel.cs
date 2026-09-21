using System;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Contracts.ViewModels;

public interface IExtraAttributeListViewModel : IDisposable
{
    NotifyCollectionChangedSynchronizedViewList<ExtraAttributeItemData> ItemsView { get; }
    BindableReactiveProperty<ExtraAttributeItemData?> SelectedItem { get; }
    IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }
    ReactiveCommand AppendCommand { get; }
    ReactiveCommand RemoveCommand { get; }
}
