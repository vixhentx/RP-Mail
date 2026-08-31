using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ObservableCollections;
using R3;
using RPMailCore.Models;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class TaskListView : UserControl
{
    private readonly DisposableBag _disposables = new();

    public static readonly StyledProperty<ObservableList<TaskItemData>> TasksProperty = AvaloniaProperty.Register<TaskListView, ObservableList<TaskItemData>>(
        nameof(Tasks), [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableList<TaskItemData> Tasks
    {
        get => GetValue(TasksProperty);
        set => SetValue(TasksProperty, value);
    }

    public static readonly StyledProperty<string> SearchTextProperty = AvaloniaProperty.Register<TaskListView, string>(
        nameof(SearchText), "");

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public IReadOnlyBindableReactiveProperty<List<TaskItemData>> TasksView { get; }
    public IReadOnlyBindableReactiveProperty<List<string>> AvailableHeaders { get; }

    public TaskListView()
    {
        InitializeComponent();

        var tasksChange = this.GetObservable(TasksProperty)
            .ToObservable()
            .Select(tasks => tasks.ObserveChanged().Select(_ => Unit.Default).Prepend(Unit.Default))
            .Switch();

        var searchChange = this.GetObservable(SearchTextProperty)
            .ToObservable()
            .Select(_ => Unit.Default);

        TasksView = tasksChange.Merge(searchChange)
            .Select(_ => ComputeTasksView())
            .ToReadOnlyBindableReactiveProperty(ComputeTasksView())
            .AddTo(ref _disposables);

        AvailableHeaders = tasksChange
            .Select(_ => Tasks.SelectMany(t => t.Data.Keys).Distinct().ToList())
            .ToReadOnlyBindableReactiveProperty(Tasks.SelectMany(t => t.Data.Keys).Distinct().ToList())
            .AddTo(ref _disposables);

        tasksChange.Subscribe(_ =>
        {
            PropertyComboBox.SelectedIndex = -1;
            PropertyComboBox.SelectedIndex = 0;
        }).AddTo(ref _disposables);
    }

    private List<TaskItemData> ComputeTasksView()
    {
        List<TaskItemData> result = Tasks.ToList();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string[] rawFilters = SearchText.Split(' ', ',', '&');
            result = result.FindAll(t =>
            {
                foreach (var rawFilter in rawFilters)
                {
                    var filter = rawFilter.Trim();
                    if (string.IsNullOrEmpty(filter)) continue;
                    if (t.SearchTokens.FindIndex(token => token.Contains(filter)) != -1) return true;
                }
                return false;
            });
        }
        return result.OrderBy(t => t.Status.Ordinal).ToList();
    }
}
