using System;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Contracts.ViewModels;

public interface IAttachmentListViewModel : IDisposable
{
    NotifyCollectionChangedSynchronizedViewList<AttachmentItemData> ItemsView { get; }
    BindableReactiveProperty<AttachmentItemData?> SelectedItem { get; }
    BindableReactiveProperty<string> WorkspaceDirectory { get; }
    IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }
    ReactiveCommand AppendCommand { get; }
    ReactiveCommand RemoveCommand { get; }
}
