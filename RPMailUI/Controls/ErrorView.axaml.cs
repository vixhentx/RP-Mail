using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class ErrorView : UserControl
{

    public static readonly StyledProperty<ObservableCollection<ErrorItemData>> ErrorsProperty = AvaloniaProperty.Register<ErrorView, ObservableCollection<ErrorItemData>>(
        nameof(Errors),[], defaultBindingMode:BindingMode.TwoWay);

    public ObservableCollection<ErrorItemData> Errors
    {
        get => GetValue(ErrorsProperty);
        set => SetValue(ErrorsProperty, value);
    }
    
    public ErrorView()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<int> MaxErrorCountProperty = AvaloniaProperty.Register<ErrorView, int>(
        nameof(MaxErrorCount));

    public int MaxErrorCount
    {
        get => GetValue(MaxErrorCountProperty);
        set => SetValue(MaxErrorCountProperty, value);
    }

    public static readonly StyledProperty<int> CellHeightProperty = AvaloniaProperty.Register<ErrorView, int>(
        nameof(CellHeight),50);

    public int CellHeight
    {
        get => GetValue(CellHeightProperty);
        set => SetValue(CellHeightProperty, value);
    }
    
    public void AfterAppend()
    {
        List<ErrorItemData> tmp = [];
        for(int i = Errors.Count - MaxErrorCount; i < Errors.Count; i++)
        {
            tmp.Add(Errors[i]);
        }
        Errors = new(tmp);
        //Update View
        {
            Timer timer = new(500)
            {
                AutoReset = false
            };
            timer.Elapsed += (_,_) => 
                Dispatcher.UIThread.Post(() =>ScrollView.ScrollToEnd());
            timer.Start();
        }
        IsVisible = true;
    }
    
    public void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AfterAppend();
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            default:
                throw new ArgumentOutOfRangeException($"ErrorView Unsupported Collection Action : {nameof(e.Action)}");
                
        }
    }
}