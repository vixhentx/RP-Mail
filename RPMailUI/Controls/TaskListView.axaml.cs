using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class TaskListView : UserControl , INotifyPropertyChanged
{
    public static readonly StyledProperty<ObservableCollection<TaskItemData>> TasksProperty = AvaloniaProperty.Register<TaskListView, ObservableCollection<TaskItemData>>(
        nameof(Tasks),[],defaultBindingMode:BindingMode.TwoWay);

    public ObservableCollection<TaskItemData> Tasks
    {
        get => GetValue(TasksProperty);
        set => SetValue(TasksProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<string>> AvailableHeadersProperty = AvaloniaProperty.Register<TaskListView, ObservableCollection<string>>(
        nameof(AvailableHeaders),[]);

    public ObservableCollection<string> AvailableHeaders
    {
        get => GetValue(AvailableHeadersProperty);
        set => SetValue(AvailableHeadersProperty, value);
    }

    public static readonly StyledProperty<string> SearchTextProperty = AvaloniaProperty.Register<TaskListView, string>(
        nameof(SearchText),"");

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }
    public IOrderedEnumerable<TaskItemData> TasksView
    {
        get
        {
            List<TaskItemData> result = Tasks.ToList();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string[] rawFilters = SearchText.Split(' ', ',', '&');
                result = result.FindAll(t =>
                {
                    foreach(var rawFilter in rawFilters)
                    {
                        var filter = rawFilter.Trim();
                        if (string.IsNullOrEmpty(filter)) continue;

                        if(t.SearchTokens.FindIndex(token => token.Contains(filter)) != -1) return true;
                    }

                    return false;
                });
            }
            //status sorter
            return result.OrderBy(t => t.Status.Ordinal);
        }
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName]string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public TaskListView()
    {
        InitializeComponent();
        this.GetObservable(SearchTextProperty).Subscribe(_ =>OnPropertyChanged(nameof(TasksView)));
        this.GetObservable(TasksProperty).Subscribe(tasks =>
        {
            //Recalc TaskView
            // tasks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TasksView));
            OnPropertyChanged(nameof(TasksView));
            //Recalc AvailableHeaders
            var headers = tasks.SelectMany(t => t.Data.Keys).Distinct().ToList();
            AvailableHeaders = new ObservableCollection<string>(headers);
            OnPropertyChanged(nameof(AvailableHeaders));
            //Set ComboBoxSelectedIndex to 0
            PropertyComboBox.SelectedIndex = -1;
            PropertyComboBox.SelectedIndex = 0;
        });
    }
}