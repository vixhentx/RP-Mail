using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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
        nameof(AvailableHeaders),[]);

    public ObservableCollection<string> AvailableHeaders
    {
        get => GetValue(AvailableHeadersProperty);
        set => SetValue(AvailableHeadersProperty, value);
    }

    [RelayCommand]
    private void AppendAttachment()
    {
        Attachments.Add(new());
    }

    [RelayCommand]
    private void RemoveAttachment()
    {
        var selectedIndex = AttachmentListBox.SelectedIndex;
        AttachmentListBox.SelectedIndex = -1;
        if(selectedIndex >= 0)
            Attachments.RemoveAt(selectedIndex);
    }


    public AttachmentView()
    {
        InitializeComponent();
        this.GetObservable(IsKeyboardFocusWithinProperty).Subscribe(hasFocus =>
        {
            if (!hasFocus) AttachmentListBox.SelectedIndex = -1;
        });
    }
}