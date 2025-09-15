using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class AttachmentView : UserControl
{
    public static readonly StyledProperty<ObservableCollection<AttachmentItemData>> AttachmentsProperty = AvaloniaProperty.Register<AttachmentView, ObservableCollection<AttachmentItemData>>(
        nameof(Attachments),[new()],defaultBindingMode:BindingMode.TwoWay);

    public ObservableCollection<AttachmentItemData> Attachments
    {
        get => GetValue(AttachmentsProperty);
        set => SetValue(AttachmentsProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<string>> AvailableHeadersProperty = AvaloniaProperty.Register<AttachmentView, ObservableCollection<string>>(
        nameof(AvailableHeaders),[],defaultBindingMode:BindingMode.TwoWay);

    public ObservableCollection<string> AvailableHeaders
    {
        get => GetValue(AvailableHeadersProperty);
        set => SetValue(AvailableHeadersProperty, value);
    }

    [RelayCommand]
    private async Task AppendAttachment()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            Attachments.Add(new()));
    }

    [RelayCommand]
    private async Task RemoveAttachment()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var selectedIndex = AttachmentListBox.SelectedIndex;
            if (selectedIndex >= 0)
                Attachments.RemoveAt(selectedIndex);
        });
    }


    public AttachmentView()
    {
        InitializeComponent();
    }
}
