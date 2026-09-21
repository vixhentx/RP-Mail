using System;
using R3;

namespace RPMailUI.Contracts.ViewModels;

public interface IConvertSettingsViewModel : IDisposable
{
    ReactiveCommand ImportCommand { get; }
    ReactiveCommand ExportCommand { get; }
    BindableReactiveProperty<string> OutputDir { get; }
    BindableReactiveProperty<bool> DeleteAfterSent { get; }
    BindableReactiveProperty<bool> ConvertOnly { get; }
    BindableReactiveProperty<bool> SaveRawDocs { get; }
    BindableReactiveProperty<bool> SaveHtmlFile { get; }
}
