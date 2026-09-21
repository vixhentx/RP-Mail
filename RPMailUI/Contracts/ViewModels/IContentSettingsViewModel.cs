using System;
using R3;

namespace RPMailUI.Contracts.ViewModels;

public interface IContentSettingsViewModel : IDisposable
{
    ReactiveCommand ImportCommand { get; }
    ReactiveCommand ExportCommand { get; }
    BindableReactiveProperty<string> CsvPath { get; }
    BindableReactiveProperty<string> BodyHtmlPath { get; }
    BindableReactiveProperty<string> Subject { get; }
    BindableReactiveProperty<string> CharSet { get; }
    IAttachmentListViewModel Attachments { get; }
    IExtraAttributeListViewModel ExtraAttributes { get; }
}
