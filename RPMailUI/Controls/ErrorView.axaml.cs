using System.Collections.Immutable;
using System.Collections.Specialized;
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
    public static readonly StyledProperty<IReadOnlyList<ErrorItemData>> ErrorsProperty = AvaloniaProperty.Register<ErrorView, IReadOnlyList<ErrorItemData>>(
        nameof(Errors), [], defaultBindingMode: BindingMode.OneWay);

    public IReadOnlyList<ErrorItemData> Errors
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

    public ErrorView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ErrorsProperty)
        {
            ErrorsControl.ItemsSource = Errors;
			AfterAppend();
		}
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
