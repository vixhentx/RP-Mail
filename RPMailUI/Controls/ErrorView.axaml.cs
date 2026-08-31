using System.Collections.Specialized;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using ObservableCollections;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class ErrorView : UserControl
{
    private readonly DisposableBag _disposables = new();

    public static readonly StyledProperty<ObservableList<ErrorItemData>> ErrorsProperty = AvaloniaProperty.Register<ErrorView, ObservableList<ErrorItemData>>(
        nameof(Errors), [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableList<ErrorItemData> Errors
    {
        get => GetValue(ErrorsProperty);
        set => SetValue(ErrorsProperty, value);
    }

    public static readonly StyledProperty<int> CellHeightProperty = AvaloniaProperty.Register<ErrorView, int>(
        nameof(CellHeight), 50);

    public int CellHeight
    {
        get => GetValue(CellHeightProperty);
        set => SetValue(CellHeightProperty, value);
    }

    public IReadOnlyBindableReactiveProperty<NotifyCollectionChangedSynchronizedViewList<ErrorItemData>> ErrorsView { get; }

    public ErrorView()
    {
        InitializeComponent();

        ErrorsView = this.GetObservable(ErrorsProperty)
            .ToObservable()
            .Select(x => x.ToNotifyCollectionChangedSlim())
            .ToReadOnlyBindableReactiveProperty(Errors!.ToNotifyCollectionChangedSlim())
            .AddTo(ref _disposables);

        this.GetObservable(ErrorsProperty)
            .ToObservable()
            .Select(errors => errors.ObserveChanged())
            .Switch()
            .Subscribe(e =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    AfterAppend();
            }).AddTo(ref _disposables);
    }

    private void AfterAppend()
    {
        System.Timers.Timer timer = new(500)
        {
            AutoReset = false
        };
        timer.Elapsed += (_, _) =>
            Dispatcher.UIThread.Post(() => ScrollView.ScrollToEnd());
        timer.Start();
        IsVisible = true;
    }
}
