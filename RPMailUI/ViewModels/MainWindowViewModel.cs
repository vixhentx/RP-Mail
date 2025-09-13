using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<TaskItemData> _tasks = [];

    public ObservableCollection<TaskItemData> Tasks
    {
        get => _tasks;
        set => this.RaiseAndSetIfChanged(ref _tasks, value);
    }
    public MainWindowViewModel()
    {
        
    }
}