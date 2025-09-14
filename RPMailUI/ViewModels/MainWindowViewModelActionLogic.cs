using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using RPMailCore;
using RPMailUI.Models;
using TaskStatus = RPMailUI.Models.TaskStatus;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel
{
    private InputFileHelper _inputFileHelper;
    private OutputFileHelper _outputFileHelper;
    private MailSender _mailSender;
    private ContentTemplate _contentTemplate;
    private ContentParser _contentParser;
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
            Info($"Failed to get encoding: {e.Message}, use default encoding: {encoding.BodyName}");
        }

        _inputFileHelper = new InputFileHelper
        {
            Encoding = encoding
        };
        _inputFileHelper.OnReadFileFailed += (o, args) =>
            Info($"Failed to read file: {args.file}: {args.e.Message}");

        #endregion

        #region OutputFileHelper

        _outputFileHelper = new OutputFileHelper
        {
            Encoding = encoding
        };
        _outputFileHelper.OnWriteFileFailed += (o, args) =>
            Info($"Failed to write file: {args.file}: {args.e.Message}");
        _outputFileHelper.OnCopyFileFailed += (o, args) =>
            Info($"Failed to copy {args.source} to {args.destination}: {args.e.Message}");
        _outputFileHelper.OnDeleteFileFailed += (o, args) =>
            Info($"Failed to delete file: {args.file}: {args.e.Message}");
        _outputFileHelper.OnMoveFileFailed += (o, args) =>
            Info($"Failed to move file: {args.source} to {args.destination}: {args.e.Message}");
        _outputFileHelper.OnCreateDirFailed += (o, args) =>
            Info($"Failed to create directory: {args.path}: {args.e.Message}");

        #endregion

        #region MailSender

        _mailSender = new SmtpMailSender(SmtpHost, SenderEmail, SenderPassword);
        _mailSender.OnBeforeSend += (sender, parsed) => 
            Log($"Sending Email To: {parsed.Receiver}, Subject: {parsed.Subject}");
        _mailSender.OnSendFailed += (sender, args) =>
            Info($"Failed to send email to {args.content.Receiver}: {args.e.Message}");
        _mailSender.OnSendCompleted += (sender, args) =>
            Log("Email sent.");
        if (!IsConvertOnly && IsDeleteAfterSent)
            _mailSender.OnSendCompleted += (sender, args) =>
            {
                foreach (var attachment in args.Attachments)
                    _outputFileHelper.Delete(attachment);
            };
        
        //Task Update
        _mailSender.OnBeforeSend += (sender, args) =>
            Tasks.First(x => x.Data[ReceiverHeader] == args.Receiver).Status = TaskStatus.Running;
        _mailSender.OnSendFailed += (sender, args) =>
            Tasks.First(x => x.Data[ReceiverHeader] == args.content.Receiver).Status = TaskStatus.Failed;
        _mailSender.OnSendCompleted += (sender, args) =>
            Tasks.First(x => x.Data[ReceiverHeader] == args.Receiver).Status = TaskStatus.Success;
            

        #endregion
        
        #region ContentTemplate

        Dictionary<string, string> attachmentMap = [];
        foreach (var item in Attachments)
        {
            attachmentMap[item.SourceText] = item.DestinationText;
        }
        
        _contentTemplate = new ContentTemplate
        {
            Receiver = ReceiverHeader,
            Subject = Subject,
            CsvPath = CsvFile,
            HtmlPath = HtmlFile,
            AttachmentMap = attachmentMap,
        };

        #endregion
        
        #region ContentParser

        _contentParser = new(DateTime.Now)
        {
            InputHelper = _inputFileHelper,
            OutputHelper = _outputFileHelper,
            OutputDir = OutputFolder,
            SaveRawDocs = IsSaveRawDoc,
            SaveHtmlFile = IsConvertOnly,
        };
        _contentParser.OnBeforeParse += (sender, args) =>
            Log($"Parsing Contents from {args.CsvPath} ");
        _contentParser.OnParseReceiver += (sender, args) =>
            Log($"Parsing Receiver: {args.receiver}");
        _contentParser.OnParseFailed += (o, args) =>
            Info($"Failed to parse content: {args.e.Message}");
        _contentParser.OnParseRowFailed += (o, args) =>
        {
            Info($"Failed to parse row {args.index+1}: {args.e.Message}");
            string[] data = ((ContentParser) o!).GenCsvString([args.row]);
            Info("--- Data ---");
            Info(data[0]);
            Info(data[1]);
        };
        _contentParser.OnParseCompleted += (o, args) =>
            Log($"Parsed {args.result.Length} contents from {args.template.CsvPath} ");
        //Update Task
        _contentParser.OnParseCompleted += (o, args) =>
        {
            List<TaskItemData> tasks = [];
            foreach (var content in args.result)
            {
                TaskItemData task = new()
                {
                    Data = content.RawRow
                };
                tasks.Add(task);
            }

            Tasks = new(tasks);
        };
        _contentParser.OnWriteCsvCompleted += (o, args) =>
            Info($"Written FailedList to CSV File: {args}");
        _contentParser.OnWriteCsvFailed += (o, args) =>
            Info($"Failed to write failed list to CSV file: {args.e.Message}");
            

        #endregion
    }

    [RelayCommand]
    private async Task Start()
    {
        try
        {
            InitService();
            var parsedContents = _contentParser.Parse(_contentTemplate);
            if (!IsConvertOnly)
            {
                foreach (var content in parsedContents)
                {
                    await _mailSender.SendAsync(content);
                }
            }

            var failList = Tasks.Where(x => x.Status == TaskStatus.Failed).ToList();
            if (failList.Count > 0)
                _contentParser.WriteCsv(failList.Select(x => x.Data).ToList());
            else
                Log("All Done!");
        }
        catch (ApplicationException)
        {
            Info("Failed.");
        }
        catch (Exception e)
        {
            Info($"Unexpected Error: {e.Message}");
        }
    }

    private void Log(string message)
    {
        ConsoleLog += message + Environment.NewLine;
        #if DEBUG
        Console.WriteLine(message);
        #endif
    }

    private void Info(string message)
    {
        Log(message);
        MessageBoxManager.GetMessageBoxStandard("Error", message).ShowAsync().Wait();
    }
}