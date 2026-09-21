using ObservableCollections;
using R3;
using RPMailUI.Contracts.ViewModels;
using RPMailUI.Models;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignAttachmentListViewModel : IAttachmentListViewModel
{
    readonly ObservableList<AttachmentItemData> _items =
    [
        Create("Assets/banner.png", "banner.png"),
        Create("Assets/guide.pdf", "getting-started.pdf")
    ];
    readonly DisposableBag _d = new();

    public NotifyCollectionChangedSynchronizedViewList<AttachmentItemData> ItemsView { get; }
    public BindableReactiveProperty<AttachmentItemData?> SelectedItem { get; } = new();
    public BindableReactiveProperty<string> WorkspaceDirectory { get; } =
        new("/home/example/RPMail");
    public IReadOnlyBindableReactiveProperty<bool> ShouldRemoveItem { get; }
    public ReactiveCommand AppendCommand { get; } = new();
    public ReactiveCommand RemoveCommand { get; } = new();

    public DesignAttachmentListViewModel()
    {
        ItemsView = _items.ToNotifyCollectionChangedSlim().AddTo(ref _d);
        ShouldRemoveItem = new BindableReactiveProperty<bool>(true);
    }

    static AttachmentItemData Create(string source, string destination)
    {
        var item = new AttachmentItemData();
        item.SourceText.Value = source;
        item.DestinationText.Value = destination;
        return item;
    }

    public void Dispose()
    {
        _d.Dispose();
        SelectedItem.Dispose();
        WorkspaceDirectory.Dispose();
        ShouldRemoveItem.Dispose();
        AppendCommand.Dispose();
        RemoveCommand.Dispose();
    }
}
