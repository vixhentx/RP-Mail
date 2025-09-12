using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using RPMailUI.Models;
using TaskStatus = RPMailUI.Models.TaskStatus;

namespace RPMailUI.ViewModels;

public class TaskListViewModel : ViewModelBase
{
    private ObservableCollection<TaskItemData> _tasks = [];

    public ObservableCollection<TaskItemData> Tasks
    {
        get => _tasks;
        set => this.RaiseAndSetIfChanged(ref _tasks, value);
    }

    private ObservableCollection<string> _availableProperties = [];
    public ObservableCollection<string> AvailableProperties
    {
        get => _availableProperties;
        set => this.RaiseAndSetIfChanged(ref _availableProperties, value);
    }
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
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
        

    public TaskListViewModel()
    {
        this.WhenAnyValue(x => x.Tasks, x => x.SearchText,
                (_, _) => Unit.Default)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(TasksView)));
    }
}