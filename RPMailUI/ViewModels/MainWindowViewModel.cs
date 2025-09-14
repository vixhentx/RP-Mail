using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ReactiveUI;
using RPMailUI.Models;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Content Settings

    [JsonProperty]
    public string CsvFile
    {
        get => _csvFile;
        set => this.RaiseAndSetIfChanged(ref _csvFile, value);
    }

    private string _csvFile = "";

    [JsonProperty]
    public string HtmlFile
    {
        get => _htmlFile;
        set => this.RaiseAndSetIfChanged(ref _htmlFile, value);
    }

    private string _htmlFile = "";

    [JsonProperty]
    public string Subject
    {
        get => _subject;
        set => this.RaiseAndSetIfChanged(ref _subject, value);
    }

    private string _subject = "";

    [JsonProperty]
    public string CharSet
    {
        get => _charSet;
        set => this.RaiseAndSetIfChanged(ref _charSet, value);
    }

    private string _charSet = "utf-8";

    [JsonProperty]
    public string ReceiverHeader
    {
        get => _receiverHeader;
        set => this.RaiseAndSetIfChanged(ref _receiverHeader, value);
    }

    private string _receiverHeader = "Receiver";

    [JsonProperty]
    public ObservableCollection<AttachmentItemData> Attachments
    {
        get => _attachments;
        set => this.RaiseAndSetIfChanged(ref _attachments, value);
    }

    private ObservableCollection<AttachmentItemData> _attachments = new();

    #endregion

    #region Sender Settings

    [JsonProperty]
    public string SenderEmail
    {
        get => _senderEmail;
        set => this.RaiseAndSetIfChanged(ref _senderEmail, value);
    }

    private string _senderEmail = "";

    [JsonProperty]
    public string SenderPassword
    {
        get => _senderPassword;
        set => this.RaiseAndSetIfChanged(ref _senderPassword, value);
    }

    private string _senderPassword = "";

    [JsonProperty]
    public string SmtpHost
    {
        get => _smtpHost;
        set => this.RaiseAndSetIfChanged(ref _smtpHost, value);
    }

    private string _smtpHost = "";

    #endregion

    #region Convert Settings

    [JsonProperty]
    public string OutputFolder
    {
        get => _outputFolder;
        set => this.RaiseAndSetIfChanged(ref _outputFolder, value);
    }

    private string _outputFolder = "Output";

    public bool IsDeleteAfterSent
    {
        get => _isDeleteAfterSent;
        set => this.RaiseAndSetIfChanged(ref _isDeleteAfterSent, value);
    }

    private bool _isDeleteAfterSent = false;

    public bool IsConvertOnly
    {
        get => _isConvertOnly;
        set => this.RaiseAndSetIfChanged(ref _isConvertOnly, value);
    }

    private bool _isConvertOnly = false;

    [JsonProperty]
    public bool IsSaveRawDoc
    {
        get => _isSaveRawDoc;
        set => this.RaiseAndSetIfChanged(ref _isSaveRawDoc, value);
    }

    private bool _isSaveRawDoc = false;

    [JsonProperty]
    public bool IsSaveHtml
    {
        get => _isSaveHtml;
        set => this.RaiseAndSetIfChanged(ref _isSaveHtml, value);
    }

    private bool _isSaveHtml = false;

    #endregion

    #region Runtime Properties

    public ObservableCollection<TaskItemData> Tasks
    {
        get => _tasks;
        set => this.RaiseAndSetIfChanged(ref _tasks, value);
    }

    private ObservableCollection<TaskItemData> _tasks = new();

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    private double _progress = 0;

    public string ConsoleLog
    {
        get => _consoleLog;
        set => this.RaiseAndSetIfChanged(ref _consoleLog, value);
    }

    private string _consoleLog = "";

    private ObservableCollection<ErrorItemData> _errors = [];

    public ObservableCollection<ErrorItemData> Errors
    {
        get => _errors;
        set => this.RaiseAndSetIfChanged(ref _errors, value);
    }

    #endregion

    public MainWindowViewModel()
    {
        Load();
    }
}