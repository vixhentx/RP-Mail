using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RPMailUI.ViewModels;

namespace RPMailUI.Views;

public partial class MainWindow : Window
{
    private bool _opened;

    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            _opened = true;
            AssignStorageProvider();
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_opened)
            AssignStorageProvider();
    }

    private void AssignStorageProvider()
    {
        if (DataContext is MainWindowViewModel vm && vm.StorageProvider is null)
            vm.StorageProvider = StorageProvider;
    }
}
