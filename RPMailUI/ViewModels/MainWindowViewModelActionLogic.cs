using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using RPMailCore;
using RPMailUI.Models;
using RPMailUI.Services;
using TaskStatus = RPMailUI.Models.TaskStatus;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel
{
    private InputFileHelper _inputFileHelper = null!;
    private OutputFileHelper _outputFileHelper = null!;
    private MailSender _mailSender = null!;
    private ContentTemplate _contentTemplate = null!;
    private ContentParser _contentParser = null!;

    private void InitService()
    {
        #region InputFileHelper

        Encoding encoding;
        try
        {
            encoding = Encoding.GetEncoding(CharSet);
        }
        catch (Exception e)
        {
            encoding = Encoding.UTF8;
            Error($"Failed to get encoding: {e.Message}, use default encoding: {encoding.BodyName}");
        }

        _inputFileHelper = new InputFileHelper
        {
            Encoding = encoding
        };
        _inputFileHelper.OnReadFileFailed += (o, args) =>
            Log($"Failed to read file: {args.file}: {args.e.Message}");

        #endregion

        #region OutputFileHelper

        _outputFileHelper = new OutputFileHelper
        {
            Encoding = encoding
        };
        _outputFileHelper.OnWriteFileFailed += (o, args) =>
            Log($"Failed to write file: {args.file}: {args.e.Message}");
        _outputFileHelper.OnCopyFileFailed += (o, args) =>
            Log($"Failed to copy {args.source} to {args.destination}: {args.e.Message}");
        _outputFileHelper.OnDeleteFileFailed += (o, args) =>
            Log($"Failed to delete file: {args.file}: {args.e.Message}");
        _outputFileHelper.OnMoveFileFailed += (o, args) =>
            Log($"Failed to move file: {args.source} to {args.destination}: {args.e.Message}");
        _outputFileHelper.OnCreateDirFailed += (o, args) =>
            Log($"Failed to create directory: {args.path}: {args.e.Message}");

        #endregion

        #region MailSender

        _mailSender = new SmtpMailSender(SmtpHost, SenderEmail, SenderPassword);
        _mailSender.OnBeforeSend += (sender, parsed) =>
            Log($"Sending Email To: {parsed.Receiver}, Subject: {parsed.Subject}");
        _mailSender.OnSendFailed += (sender, args) =>
            Log($"Failed to send email to {args.content.Receiver}: {args.e.Message}");
        if (!IsConvertOnly && IsDeleteAfterSent)
            _mailSender.OnSendCompleted += (sender, args) =>
            {
                foreach (var attachment in args.Attachments)
                    _outputFileHelper.Delete(attachment);
            };

        //Task Update
        _mailSender.OnBeforeSend += (sender, args) =>
        {
            var task = Tasks.First(x => x.Data[ReceiverHeader] == args.Receiver);
            task.Status = TaskStatus.Running;
            task.Tooltip = "Sending email...";
        };
        _mailSender.OnSendFailed += (sender, args) =>
        {
            var failedTask = Tasks.First(x => x.Data[ReceiverHeader] == args.content.Receiver);
            failedTask.Status = TaskStatus.Failed;
            failedTask.Tooltip = args.e.Message;
        };
        _mailSender.OnSendCompleted += (sender, args) =>
        {
            var successTask = Tasks.First(x => x.Data[ReceiverHeader] == args.Receiver);
            successTask.Status = TaskStatus.Success;
            successTask.Tooltip = "Email sent.";
        };

        #endregion

        #region ContentTemplate

        Dictionary<string, string> attachmentMap = [];
        foreach (var item in Attachments) attachmentMap[item.SourceText] = item.DestinationText;

        _contentTemplate = new ContentTemplate
        {
            Receiver = ReceiverHeader,
            Subject = Subject,
            CsvPath = CsvFile,
            HtmlPath = HtmlFile,
            AttachmentMap = attachmentMap
        };

        #endregion

        #region ContentParser

        _contentParser = new ContentParser(DateTime.Now)
        {
            InputHelper = _inputFileHelper,
            OutputHelper = _outputFileHelper,
            OutputDir = OutputFolder,
            SaveRawDocs = IsSaveRawDoc,
            SaveHtmlFile = IsConvertOnly
        };
        _contentParser.OnBeforeLoad += (sender, args) => Dispatcher.UIThread.Post(() =>
            Log($"Parsing Contents from {args.CsvPath} "));
        _contentParser.OnParsePropertyCompleted += (sender, args) => Dispatcher.UIThread.Post(() =>
        {
            if(args.property!= "HtmlBody")
                Log($"Parsing {args.property}: {args.value}");
        });
        _contentParser.OnParseFailed += (o, args) => Dispatcher.UIThread.Post(() =>
            Log($"Failed to parse content: {args.e.Message}"));
        _contentParser.OnParseRowFailed += (o, args) => Dispatcher.UIThread.Invoke(() =>
        {
            Error($"Failed to parse row {args.index + 1}: {args.e.Message}");
            var data = ((ContentParser)o!).GenCsvString([args.row]);
            Log("--- Data ---");
            Log(data[0]);
            Log(data[1]);
        });
        _contentParser.OnParseCompleted += (o, args) => Dispatcher.UIThread.Post(() =>
            Log($"Parsed {args.result.Length} contents from {args.template.CsvPath} "));
        _contentParser.OnWriteCsvCompleted += (o, args) => Dispatcher.UIThread.Post(() =>
            Log($"Written FailedList to CSV File: {args}"));
        _contentParser.OnWriteCsvFailed += (o, args) => Dispatcher.UIThread.Post(() =>
            Error($"Failed to write failed list to CSV file: {args.e.Message}"));
        
         _contentParser.OnParseRowCompleted += (sender, args) => Dispatcher.UIThread.Post(() =>
         {
             var task = Tasks[args.index];
             task.Status = TaskStatus.Pending;
             task.Tooltip = "Pending...";
         });
         //load tasks
         _contentParser.OnBeforeParse += (sender, args) =>
         {
             List<TaskItemData> tasks = args.rows
                 .Select(row => new TaskItemData { Data = row })
                 .ToList();
             Dispatcher.UIThread.Post(() =>Tasks = new(tasks));
         };
         _contentParser.OnParseRowFailed += (sender, args) => Dispatcher.UIThread.Post(() =>
         {
             var task = Tasks[args.index];
             task.Status = TaskStatus.Failed;
             task.Tooltip = args.e.Message;
         });

        #endregion
    }

    [RelayCommand]
    private async Task Start()
    {
        Errors.Clear();
        Tasks.Clear();
        ShouldRetry = false;
        ShouldOpenOutputFolder = false;
        try
        {
            InitService();
            //parse
            _contentParser.OnBeforeParse += (_,_) => ShouldOpenOutputFolder = true;
            var parsedContents = await _contentParser.ParseAsync(_contentTemplate);

            if (!IsConvertOnly)
            {
                Progress = 0;
                double step = 100.0f / parsedContents.Length;
                foreach (var content in parsedContents)
                {
                    await _mailSender.SendAsync(content);
                    Progress += step;
                }
            }

            var failList = Tasks.Where(x => x.Status == TaskStatus.Failed).ToList();
            if (failList.Count > 0)
            {
                _contentParser.WriteCsv(failList.Select(x => x.Data).ToList());
                ShouldRetry = true;
            }
            else
                Log("All Done!");
        }
        catch (RPMailAbortException)
        {
            Log("Failed.");
        }
        catch (Exception e)
        {
            Error($"Unexpected Error: {e.Message}");
        }
    }
    
    [RelayCommand]
    private void OpenOutputFolder()
    {
        PathOpenHelper.OpenDirectory(_contentParser.RealOutputDir);
    }

    [RelayCommand]
    private void Retry()
    {
        CsvFile = _contentParser.CsvFailed;
    }

    private void Log(string message)
    {
        ConsoleLog += message + Environment.NewLine;
#if DEBUG
        Console.WriteLine(message);
#endif
    }

    private void Error(string message)
    {
        Log(message);
        MessageFlyout.ShowError(message);
    }
}