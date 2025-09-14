using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using RPMailUI.Models;

namespace RPMailUI.Controls;

public partial class AttachmentView : PersistedUserControl
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
        Attachments.CollectionChanged += OnAttachmentChanged;
        this.GetObservable(IsKeyboardFocusWithinProperty).Subscribe(hasFocus =>
        {
            if (!hasFocus)
            {
                OnAttachmentChangedHard();
            }
        });
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e) =>
        OnAttachmentChanged(sender);

    //TODO: Implement persist
    private void OnAttachmentChanged(object? sender = null, EventArgs? e = null)
        => IsDirty = true;

    private void OnAttachmentChangedHard(object? sender = null, EventArgs? e = null)
    {
        var value = Attachments;
        Attachments = [];
        Attachments = value;
    }
}
