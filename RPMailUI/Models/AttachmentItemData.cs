using System.Collections.Generic;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RPMailUI.Models;

public class AttachmentItemData : ObservableObject
{
    private string _sourceText = "";
    public string SourceText
    {
        get => _sourceText;
        set => SetProperty(ref _sourceText, value);
    }
    private string _destinationText = "";
    public string DestinationText
    {
        get => _destinationText;
        set => SetProperty(ref _destinationText, value);
    }
}