using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RPMailUI.Models;

public class TaskItemData : ObservableObject
{
    public Dictionary<string, string> Data { get; init; } = [];
    private TaskStatus _status = TaskStatus.Ready;
    private string _tooltip = "Ready to send";

    public TaskStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
    public string? Tooltip
    {
        get => _tooltip;
        set => SetProperty(ref _tooltip, value);
    }

    public string this[string key]
    {
        get => Data[key];
        set => Data[key] = value;
    }

    public List<string> SearchTokens
    {
        get
        {
            List<string> tokens = [];
            tokens.AddRange(Data.Values);
            tokens.Add(Status.Text);
            return tokens;
        }
    }
}