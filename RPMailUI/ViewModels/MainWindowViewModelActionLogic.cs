using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using RPMailCore;
using RPMailUI.Models;
using RPMailUI.Services;
using TaskStatus = RPMailUI.Models.TaskStatus;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel
{
    private InputFileHelper? _inputFileHelper;
    private OutputFileHelper? _outputFileHelper;
    private MailSender? _mailSender;
    private ContentTemplate? _contentTemplate;
    private ContentParser? _contentParser;

    [RelayCommand]
    private async Task Start()
    {
        Errors.Clear();
        try
        {
            #region InitService

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
            {
                var failedTask = Tasks.First(x => x.Data[ReceiverHeader] == args.content.Receiver);
                failedTask.Status = TaskStatus.Failed;
                failedTask.Tooltip = args.e.Message;
            };
            _mailSender.OnSendCompleted += (sender, args) =>
                Tasks.First(x => x.Data[ReceiverHeader] == args.Receiver).Status = TaskStatus.Success;

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
            _contentParser.OnBeforeParse += (sender, args) =>
                Log($"Parsing Contents from {args.CsvPath} ");
            _contentParser.OnParseReceiver += (sender, args) =>
                Log($"Parsing Receiver: {args.receiver}");
            _contentParser.OnParseFailed += (o, args) =>
                Log($"Failed to parse content: {args.e.Message}");
            _contentParser.OnParseRowFailed += (o, args) =>
            {
                Error($"Failed to parse row {args.index + 1}: {args.e.Message}");
                var data = ((ContentParser)o!).GenCsvString([args.row]);
                Log("--- Data ---");
                Log(data[0]);
                Log(data[1]);
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

                Tasks = new ObservableCollection<TaskItemData>(tasks);
            };
            _contentParser.OnWriteCsvCompleted += (o, args) =>
                Log($"Written FailedList to CSV File: {args}");
            _contentParser.OnWriteCsvFailed += (o, args) =>
                Error($"Failed to write failed list to CSV file: {args.e.Message}");

            #endregion

            #endregion

            //parse
            var parsedContents = _contentParser.Parse(_contentTemplate);
            List<TaskItemData> tasks = [];
            foreach (var content in parsedContents)
            {
                TaskItemData task = new()
                {
                    Data = content.RawRow
                };
                tasks.Add(task);
            }
            Tasks = new(tasks);

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
                _contentParser.WriteCsv(failList.Select(x => x.Data).ToList());
            else
                Log("All Done!");
        }
        catch (ApplicationException)
        {
            Log("Failed.");
        }
        catch (Exception e)
        {
            Error($"Unexpected Error: {e.Message}");
        }
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