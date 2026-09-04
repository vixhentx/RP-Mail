using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class AttachmentView : UserControl
{
    private readonly DisposableBag _disposables = new();

    public static readonly StyledProperty<ObservableList<AttachmentItemData>> AttachmentsProperty = AvaloniaProperty.Register<AttachmentView, ObservableList<AttachmentItemData>>(
        nameof(Attachments), [], defaultBindingMode: BindingMode.OneWay);

    public ObservableList<AttachmentItemData> Attachments
    {
        get => GetValue(AttachmentsProperty);
        set => SetValue(AttachmentsProperty, value);
    }

    public ReactiveCommand AppendCommand { get; } = new();
    public ReactiveCommand RemoveCommand { get; } = new();

    public AttachmentView()
    {
        InitializeComponent();

        AppendCommand.Subscribe(_ => Attachments.Add(new AttachmentItemData())).AddTo(ref _disposables);
        RemoveCommand.Subscribe(_ =>
        {
            var selectedIndex = AttachmentListBox.SelectedIndex;
            if (selectedIndex >= 0)
                Attachments.RemoveAt(selectedIndex);
        }).AddTo(ref _disposables);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AttachmentsProperty)
            AttachmentListBox.ItemsSource = Attachments.ToNotifyCollectionChangedSlim();
    }
}
