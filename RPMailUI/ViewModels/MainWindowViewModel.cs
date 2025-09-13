using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<TaskItemData> _tasks = [];

    public MainWindowViewModel()
    {
        
    }
}