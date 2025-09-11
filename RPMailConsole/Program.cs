using System.ComponentModel.DataAnnotations;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using RPMailCore;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable ReplaceAutoPropertyWithComputedProperty
#pragma warning disable CS8618

namespace RPMailConsole;

public class Program
{
    public static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);
    
    #region sender settings
    [Required]
    [Option(Template = "-s|--sender", Description = "Sender email address")]
    public string Sender { get;}
    
    [Required]
    [Option(Template = "-h|--host", Description = "SMTP Host & Port")]
    public string Host { get;}
    
    [Required]
    [Option(Template = "-p|--pwd|--password", Description = "Password")]
    public string Password { get;}
    
    //parse settings
    [Option(Template = "-r|--receiver-header", Description = "Receiver header in CSV file")]
    public string ReceiverHeader { get;} = "Receiver";
    
    #endregion
    
    #region content settings
    
    [Required]
    [Option(Template = "-d|--csv|--data", Description = "CSV Data File Path")]
    public string DataFile { get;}
    
    [Required]
    [Option(Template = "-t|--title|--subject", Description = "Email Subject Pattern")]
    public string SubjectPattern { get;}
    
    [Required]
    [Option(Template = "-m|-b|--html|--body|--message", Description = "HTML Email Body Pattern File Path")]
    public string BodyPattern { get;}
    
    [Option(Template = "-a|--attachment", Description = "PDF Attachment Pattern File Path")]
    public string[]? AttachmentPatterns { get;} = null;
    
    [Option(Template = "-n|--attachment-name", Description = "Attachment Name Pattern File Path")]
    public string[]? AttachmentNamePattern { set; get;} = null;
    
    [Option(Template = "-c|--charset", Description = "All Files Encoding if BOM absent")]
    public string CharSet { get; } = "utf-8";
    
    #endregion
    
    #region misc
    
    [Option(Template = "-o|--output", Description = "Attachment Convert File Directory")]
    public string OutputFileDir { get; } = "Output";
    
    [Option(Template = "--delete-after-convert", Description = "Delete Attachment Output after convert")]
    public bool DeleteAfterConvert { get; } = false;
    
    [Option]
    public bool Quiet { get; } = false;
    [Option(Template = "--convert-only", Description = "Only Convert Attachments and Exit")]
    public bool ConvertOnly { get; } = false;
    [Option(Template = "--save-raw-doc", Description = "Save Raw Document in Output Directory")]
    public bool SaveRawDoc { get; } = false;
    
    #endregion

    #region excution

    private string GetDefaultPattern(int index) => $"{DataParser.Format(ReceiverHeader)}_attachment_{index + 1}.pdf";

    private Encoding _encoding;
    
    private void InitArgs()
    {
        //Logger
        if (Quiet) Log = (s, c) => { };
        
        //GB2312
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        if (AttachmentPatterns is not null)
        {
            var newNamePatterns = new string[AttachmentPatterns.Length];
            int nameLength = 0;
            if (AttachmentNamePattern is not null)
            {
                for (int i = 0; i < AttachmentNamePattern.Length; i++)
                {
                    newNamePatterns[i] = AttachmentNamePattern[i];
                }
                nameLength = AttachmentNamePattern.Length;
            }
            for (int i = nameLength; i < AttachmentPatterns.Length; i++)
            {
                newNamePatterns[i] = GetDefaultPattern(i);
            }
            AttachmentNamePattern = newNamePatterns;
        }

        if(string.IsNullOrWhiteSpace(CharSet))
            _encoding = Encoding.Default;
        else
        {
            try
            {
                _encoding = Encoding.GetEncoding(CharSet);
            }
            catch (ArgumentException)
            {
                Log($"Invalid CharSet: {CharSet}, using default encoding", ConsoleColor.Yellow);
                _encoding = Encoding.Default;
            }
        }
    }

    #region Services
    private InputFileHelper _inputFileHelper;
    private OutputFileHelper _outputFileHelper;
    private MailSender _mailSender;
    private ContentTemplate _contentTemplate;
    private ContentParser _contentParser;
    private void InitServices()
    {
        #region InputFileHelper

        _inputFileHelper = new InputFileHelper
        {
            Encoding = _encoding
        };
        _inputFileHelper.OnReadFileFailed += (o, args) =>
            Info($"Failed to read file: {args.file}: {args.e.Message}", ConsoleColor.Red);

        #endregion

        #region OutputFileHelper

        _outputFileHelper = new OutputFileHelper
        {
            DeleteAfterConvert = DeleteAfterConvert,
            Encoding = _encoding
        };
        _outputFileHelper.OnWriteFileFailed += (o, args) =>
            Info($"Failed to write file: {args.file}: {args.e.Message}", ConsoleColor.Red);
        _outputFileHelper.OnCopyFileFailed += (o, args) =>
            Info($"Failed to copy {args.source} to {args.destination}: {args.e.Message}", ConsoleColor.Red);
        _outputFileHelper.OnDeleteFileFailed += (o, args) =>
            Info($"Failed to delete file: {args.file}: {args.e.Message}", ConsoleColor.Red);
        _outputFileHelper.OnMoveFileFailed += (o, args) =>
            Info($"Failed to move file: {args.source} to {args.destination}: {args.e.Message}", ConsoleColor.Red);
        _outputFileHelper.OnCreateDirCompleted += (o, args) =>
            Log($"Directory Created: {args}", ConsoleColor.Green);
        _outputFileHelper.OnCreateDirFailed += (o, args) =>
            Info($"Failed to create directory: {args.path}: {args.e.Message}", ConsoleColor.Red);

        #endregion

        #region MailSender

        _mailSender = new SmtpMailSender(Host, Sender, Password);
        _mailSender.OnBeforeSend += (sender, parsed) => 
            Log($"Sending Email To: {parsed.Receiver}, Subject: {parsed.Subject}", ConsoleColor.Cyan);
        _mailSender.OnSendFailed += (sender, args) =>
            Info($"Failed to send email to {args.content.Receiver}: {args.e.Message}", ConsoleColor.Red);
        _mailSender.OnSendCompleted += (sender, args) =>
            Log("Email sent.", ConsoleColor.Green);
            

        #endregion
        
        #region ContentTemplate

        Dictionary<string, string> attachmentMap = [];
        for (int i = 0; i < AttachmentPatterns?.Length; i++)
        {
            var key  = AttachmentPatterns[i];
            var value = AttachmentNamePattern![i];
            attachmentMap[key] = value;
        }
        
        _contentTemplate = new ContentTemplate
        {
            Receiver = ReceiverHeader,
            Subject = SubjectPattern,
            CsvPath = DataFile,
            HtmlPath = BodyPattern,
            AttachmentMap = attachmentMap,
        };

        #endregion
        
        #region ContentParser

        _contentParser = new(DateTime.Now)
        {
            InputHelper = _inputFileHelper,
            OutputHelper = _outputFileHelper,
            OutputDir = OutputFileDir,
            SaveRawDocs = SaveRawDoc
        };
        _contentParser.OnBeforeParse += (sender, args) =>
            Log($"Parsing Contents from {args.CsvPath} ", ConsoleColor.Cyan);
        _contentParser.OnParseFailed += (o, args) =>
            Info($"Failed to parse content: {args.e.Message}", ConsoleColor.Red);
        _contentParser.OnParseRowFailed += (o, args) =>
        {
            Info($"Failed to parse row {args.index+1}: {args.e.Message}", ConsoleColor.Red);
            string[] data = ((ContentParser) o!).GenCsvString([args.row]);
            Info("--- Data ---",ConsoleColor.Red);
            Info(data[0],ConsoleColor.Yellow);
            Info(data[1],ConsoleColor.Yellow);
        };
        _contentParser.OnParseCompleted += (o, args) =>
            Log($"Parsed {args.result.Length} contents from {args.template.CsvPath} ", ConsoleColor.Green);
        _contentParser.OnWriteCsvCompleted += (o, args) =>
            Info($"Written FailedList to CSV File: {args}", ConsoleColor.Yellow);
        _contentParser.OnWriteCsvFailed += (o, args) =>
            Info($"Failed to write failed list to CSV file: {args.e.Message}", ConsoleColor.Red);
            

        #endregion
        

    }
    #endregion

    private async Task OnExecute()
    {
        InitArgs();
        InitServices();
        try
        {
            var parsedContents = _contentParser.Parse(_contentTemplate);
            List<(ContentParsed content, string reason)> failList = [];
            _mailSender.OnSendFailed += (o, args) =>
                failList.Add((args.content, args.e.Message));
            foreach (var content in parsedContents)
            {
                await _mailSender.SendAsync(content);
            }

            _contentParser.WriteCsv(failList.Select(x => x.content).ToList());

            Log("All Done!", ConsoleColor.Green);
        }
        catch (ApplicationException)
        {
            Info("Failed.", ConsoleColor.DarkRed);
        }
    }

    private Action<string, ConsoleColor> Log = Info;

    private static void Info(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    
    #endregion
}