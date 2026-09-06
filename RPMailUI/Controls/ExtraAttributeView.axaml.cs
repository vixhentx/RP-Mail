using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class ExtraAttributeView : UserControl
{
    private readonly DisposableBag _disposables = new();

    public static readonly StyledProperty<ObservableList<ExtraAttributeItemData>> ExtraAttributesProperty = AvaloniaProperty.Register<ExtraAttributeView, ObservableList<ExtraAttributeItemData>>(
        nameof(ExtraAttributes), [], defaultBindingMode: BindingMode.OneWay);

    public ObservableList<ExtraAttributeItemData> ExtraAttributes
    {
        get => GetValue(ExtraAttributesProperty);
        set => SetValue(ExtraAttributesProperty, value);
    }

    public ReactiveCommand AppendCommand { get; } = new();
    public ReactiveCommand RemoveCommand { get; } = new();

    public ExtraAttributeView()
    {
        InitializeComponent();

        AppendCommand.Subscribe(_ => ExtraAttributes.Add(new ExtraAttributeItemData())).AddTo(ref _disposables);
        RemoveCommand.Subscribe(_ =>
        {
            var selectedIndex = ExtraAttributeListBox.SelectedIndex;
            if (selectedIndex >= 0)
                ExtraAttributes.RemoveAt(selectedIndex);
        }).AddTo(ref _disposables);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExtraAttributesProperty)
            ExtraAttributeListBox.ItemsSource = ExtraAttributes.ToNotifyCollectionChangedSlim();
    }
}
