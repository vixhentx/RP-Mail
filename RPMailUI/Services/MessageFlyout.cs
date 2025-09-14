using System;
using RPMailUI.Controls;
using RPMailUI.Models;
using RPMailUI.ViewModels;

namespace RPMailUI.Services;

public static class MessageFlyout
{
    private static MainWindowViewModel? _vm;

    public static void Initialize(MainWindowViewModel vm) =>
        _vm = vm;
    public static void ShowMessage(string caption, string message)
    {
        _vm?.Errors.Add(new($"{caption}: {message}"));
    }

    public static void ShowError(string message)
    {
        ShowMessage("Error", message);
    }

    public static void ShowInfo(string message)
    {
        ShowMessage("Info", message);
    }
}