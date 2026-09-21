using System;
using R3;

namespace RPMailUI.Contracts.ViewModels;

public interface IWorkspaceConfigViewModel : IDisposable
{
    BindableReactiveProperty<string> WorkspaceDirectory { get; }
    ReactiveCommand BrowseWorkspaceCommand { get; }
    ReactiveCommand ImportMailConfigCommand { get; }
    ReactiveCommand ExportMailConfigCommand { get; }
}
