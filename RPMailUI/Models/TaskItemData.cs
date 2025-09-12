using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RPMailUI.Models;

public class TaskItemData : ObservableObject
{
    public Dictionary<string, string> Data { get; init; } = [];
    private TaskStatus _status = TaskStatus.Pending;

    public TaskStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string this[string key]
    {
        get => Data[key];
        init => Data[key] = value;
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