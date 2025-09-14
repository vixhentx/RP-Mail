using System.ComponentModel;
using System.Linq;
using System.Timers;
using DynamicData;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : IPersistable<MainWindowViewModel.PersistedData>
{
    public Timer SaveTimer { get; set; } = null!;

    public bool IsDirtySetter
    {
        set => (this as IPersistable<PersistedData>).IsDirty = value;
    }

    public PersistedData Data
    {
        get => new()
        {
            CsvFile = CsvFile,
            HtmlFile = HtmlFile,
            Subject = Subject,
            CharSet = CharSet,
            ReceiverHeader = ReceiverHeader,
            Attachments = Attachments.ToArray(),
            SenderEmail = SenderEmail,
            SenderPassword = SenderPassword,
            SmtpHost = SmtpHost,
            OutputFolder = OutputFolder,
            IsDeleteAfterSent = IsDeleteAfterSent,
            IsConvertOnly = IsConvertOnly,
            IsSaveRawDoc = IsSaveRawDoc,
            IsSaveHtml = IsSaveHtml
        };
        set
        {
            CsvFile = value.CsvFile ?? _csvFile;
            HtmlFile = value.HtmlFile ?? _htmlFile;
            Subject = value.Subject ?? _subject;
            CharSet = value.CharSet ?? _charSet;
            ReceiverHeader = value.ReceiverHeader ?? _receiverHeader;
            if (value.Attachments is not null)
            {
                Attachments = new(value.Attachments);
            }
            SenderEmail = value.SenderEmail ?? _senderEmail;
            SenderPassword = value.SenderPassword ?? _senderPassword;
            SmtpHost = value.SmtpHost ?? _smtpHost;
            OutputFolder = value.OutputFolder ?? _outputFolder;
            IsDeleteAfterSent = value.IsDeleteAfterSent;
            IsConvertOnly = value.IsConvertOnly;
            IsSaveRawDoc = value.IsSaveRawDoc;
            IsSaveHtml = value.IsSaveHtml;
        }
    }

    public class PersistedData
    {
        public string? CsvFile { get; set; }
        public string? HtmlFile { get; set; }
        public string? Subject { get; set; }
        public string? CharSet { get; set; }
        public string? ReceiverHeader { get; set; }
        public AttachmentItemData[]? Attachments { get; set; }
        public string? SenderEmail { get; set; }
        public string? SenderPassword { get; set; }
        public string? SmtpHost { get; set; }
        public string? OutputFolder { get; set; }
        public bool IsDeleteAfterSent { get; set; }
        public bool IsConvertOnly { get; set; }
        public bool IsSaveRawDoc { get; set; }
        public bool IsSaveHtml { get; set; }
    }
    
    public void OnWindowClosing(object sender, CancelEventArgs e) => PersistHelper.SaveInstantly(this);

    public void Load()
    {
        PersistHelper.Load(this);
        Attachments.CollectionChanged += (_,_) => PersistHelper.ScheduleSave(this);
    }

}