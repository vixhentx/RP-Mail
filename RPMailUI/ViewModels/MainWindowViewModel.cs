using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using RPMailUI.Models;
using RPMailUI.Services;
using RPMailUI.Services.Attribute;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Content Settings
    
    [Persisted]
    [ObservableProperty] private string _csvFile = "";
    [Persisted]
    [ObservableProperty] private string _htmlFile = "";
    [Persisted]
    [ObservableProperty] private string _subject = "";
    [Persisted]
    [ObservableProperty] private string _charSet = "utf-8";
    [Persisted]
    [ObservableProperty] private string _receiverHeader = "Receiver";
    [Persisted]
    [ObservableProperty] private ObservableCollection<AttachmentItemData> _attachments = [new()];

    #endregion

    #region Sender Settings

    [Persisted]
    [ObservableProperty] private string _senderEmail = "";
    [Persisted]
    [ObservableProperty] private string _senderPassword = "";
    [Persisted]
    [ObservableProperty] private string _smtpHost = "";

    #endregion

    #region Convert Settings

    [Persisted]
    [ObservableProperty] private string _outputFolder = "Output";
    [Persisted]
    [ObservableProperty] private bool _isDeleteAfterSent = false;
    [ObservableProperty] private bool _isConvertOnly = false;
    [Persisted]
    [ObservableProperty] private bool _isSaveRawDoc = false;
    [Persisted]
    [ObservableProperty] private bool _isSaveHtml = false;

    #endregion

    #region Runtime Properties

    [ObservableProperty]
    ObservableCollection<TaskItemData> _tasks = [];
    
    [ObservableProperty]
    int _progress = 0;
    
    [ObservableProperty]
    string _consoleLog = "";

    #endregion

    public MainWindowViewModel()
    {
        (this as IPersistable).Load();
    }
}