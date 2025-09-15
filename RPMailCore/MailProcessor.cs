using System.Collections.Immutable;
using System.Text;
using MailKit;
using MailKitSimplified.Sender.Services;

namespace RPMailCore;


#region Sender

public abstract class MailSender
{
    public event EventHandler<ContentParsed>? OnBeforeSend;
    public event EventHandler<(ContentParsed content,Exception e)>? OnSendFailed;
    public event EventHandler<ContentParsed>? OnSendCompleted;
    
    public bool DeleteAttachmentsAfterSent { get; set; } = false;

    public async Task SendAsync(ContentParsed content, CancellationToken cancellationToken = default)
    {
        OnBeforeSend?.Invoke(this, content);
        try
        {
            await SendAsyncInner(content, cancellationToken);
            OnSendCompleted?.Invoke(this, content);
        }
        catch (Exception e)
        {
            OnSendFailed?.Invoke(this, (content, e));
            return;
        }
    }
    protected abstract Task SendAsyncInner(ContentParsed content, CancellationToken cancellationToken = default);
}
public class SmtpMailSender(string host, string senderEmail, string smtpPassword) : MailSender
{
    readonly SmtpSender _smtpSender = SmtpSender
        .Create(host)
        .SetCredential(senderEmail, smtpPassword);

    protected override Task SendAsyncInner(ContentParsed content, CancellationToken cancellationToken = default) =>
         _smtpSender.WriteEmail
                        .From(senderEmail)
                        .To(content.Receiver)
                        .Subject(content.Subject)
                        .BodyHtml(content.HtmlBody)
                        .Attach(content.Attachments)
                        .SendAsync(cancellationToken);
}
#endregion

#region Content

//Store Patterns
public class ContentTemplate
{
    public required string CsvPath { get; set; }
    public required string Receiver { get; set; }
    public required string Subject { get; set; }
    public required string HtmlPath { get; set; }
    public Dictionary<string, string>? AttachmentMap { get; set; }
}

//Store Real Data
public class ContentParsed
{
    public required string Receiver { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public required string[] Attachments { get; set; }
    public required string HtmlPath { get; set; }
    
    public required string OutputDir {get; set;}
    
    public required Dictionary<string, string> RawRow { get; set; }
}

public class ContentParser
{
    public string OutputDir { get; set; } = "Output";
    public bool SaveRawDocs { get; set; } = false;
    public bool SaveHtmlFile { get; set; } = false;
    public required InputFileHelper InputHelper { get; set; }
    public required OutputFileHelper OutputHelper { get; set; }

    private readonly string _realOutputDir;
    private DataParser? _dataParser;

    public event EventHandler<ContentTemplate>? OnBeforeLoad;
    public event EventHandler<(ContentTemplate template,List<Dictionary<string,string>> rows)>? OnBeforeParse;
    public event EventHandler<(ContentTemplate template,Exception e)>? OnParseFailed;
    public event EventHandler<(ContentTemplate template,ContentParsed[] result)>? OnParseCompleted;
    public event EventHandler<(int index, Dictionary<string, string> row)>? OnRowLoadCompleted;
    public event EventHandler<(int index,Dictionary<string,string> row)>? OnBeforeParseRow;
    public event EventHandler<(ContentTemplate template,int index,string property,string value)>? OnParsePropertyCompleted;
    public event EventHandler<(int index,Dictionary<string,string> row)>? OnParseRowCompleted;
    public event EventHandler<(int index,Dictionary<string,string> row,Exception e)>? OnParseRowFailed;
    
    public event EventHandler<string>? OnBeforeWriteCsv;
    public event EventHandler<(string file,Exception e)>? OnWriteCsvFailed;
    public event EventHandler<string>? OnWriteCsvCompleted;

    public ContentParser(DateTime time)
    {
        _realOutputDir = Path.Combine(OutputDir, $"{time:yyyy-MM-dd_HH-mm-ss}");
    }
    
    public async Task<ContentParsed[]> ParseAsync(ContentTemplate template) => await Task.Run(() =>
    {
        try
        {
            OutputHelper.CreateDirIfNotExist(_realOutputDir);
            _dataParser = new();
            List<ContentParsed> list = [];
            OnBeforeLoad?.Invoke(this, template);
            _dataParser.Parse(InputHelper.CreateReader(template.CsvPath), arg => 
                OnRowLoadCompleted?.Invoke(this, arg));
            OnBeforeParse?.Invoke(this, (template, _dataParser.Rows));
            for(int index = 0; index < _dataParser.Rows.Count; index++)
            {
                var row = _dataParser.Rows[index];
                OnBeforeParseRow?.Invoke(this, (index, row));
                try{
                    string receiver = row[template.Receiver];
                    OnParsePropertyCompleted?.Invoke(this, (template,index,"Receiver", receiver));
            
                    string subject = _dataParser.Parse(template.Subject, row);
                    OnParsePropertyCompleted?.Invoke(this, (template,index,"Subject", subject));
            
                    string htmlPath = _dataParser.Parse(template.HtmlPath, row);
                    OnParsePropertyCompleted?.Invoke(this, (template,index,"HtmlPath", htmlPath));
            
                    string htmlBody = _dataParser.Parse(InputHelper.Read(htmlPath), row);
                    OnParsePropertyCompleted?.Invoke(this, (template,index,"HtmlBody",htmlBody));
            
                    string outputDir = Path.Combine(_realOutputDir, receiver);

                    if (SaveHtmlFile)
                    {
                        OutputHelper.Write(htmlBody, Path.Combine(outputDir, "body.html"));
                    }

                    var attachments = BuildAttachments(template.AttachmentMap, _dataParser, outputDir, row);
                    list.Add(new ContentParsed
                    {
                        Receiver = receiver,
                        Subject = subject,
                        HtmlBody = htmlBody,
                        Attachments = attachments,
                        HtmlPath = htmlPath,
                        OutputDir = outputDir,
                        RawRow = row
                    });
                    OnParseRowCompleted?.Invoke(this, (index, row));
                }
                catch (Exception e)
                {
                    OnParseRowFailed?.Invoke(this, (index, row, e));
                    throw;
                }
            }
            var ret = list.ToArray();
            OnParseCompleted?.Invoke(this, (template, ret));
            return ret;
        }
        catch (Exception e)
        {
            OnParseFailed?.Invoke(this, (template, e));
            throw new RPMailAbortException();
        }
    });


    public void WriteCsv(List<Dictionary<string, string>> rows, string? outputPath = null)
    {
        outputPath ??= Path.Combine(_realOutputDir, "data_failed.csv");
        OnBeforeWriteCsv?.Invoke(this, outputPath);
        try
        {
            string[] ret = GenCsvString(rows);

            OutputHelper.Write(ret, outputPath);
            OnWriteCsvCompleted?.Invoke(this, outputPath);
        }
        catch (RPMailAbortException)
        {
            
        }
        catch (Exception e)
        {
            OnWriteCsvFailed?.Invoke(this, (outputPath, e));
        }
    }
    public void WriteCsv(IEnumerable<ContentParsed> contents, string? outputPath = null) =>
        WriteCsv(contents.Select(c => c.RawRow).ToList(), outputPath);

    public string[] GenCsvString(List<Dictionary<string,string>> rows)
    {
        List<string> ret = [];
        StringBuilder sb = new();
        sb.AppendJoin(",", _dataParser!.Headers);
        ret.Add(sb.ToString());
        foreach (var row in rows)
        {
            sb.Clear();
            sb.AppendJoin(",", _dataParser.GetPropertiesOf(row));
            ret.Add(sb.ToString());
        }
        return ret.ToArray();
    }
    private string[] BuildAttachments(Dictionary<string,string>? attachmentMap,DataParser dataParser,string outputDir, Dictionary<string,string> row)
    {
        if (attachmentMap is null) return [];

        List<string> ret = [];
        DocConverter docConverter = new(dataParser, SaveRawDocs);

        foreach (var attachment in attachmentMap)
        {
            string patternPath = dataParser.Parse(attachment.Key, row);
            if (string.IsNullOrWhiteSpace(patternPath)) continue;

            string targetFile = dataParser.Parse(attachment.Value, row);
            if (string.IsNullOrWhiteSpace(targetFile)) continue;

            if (string.IsNullOrWhiteSpace(Path.GetExtension(targetFile)))
            {
                targetFile = Path.ChangeExtension(targetFile, ".pdf");
            }

            string outputPath = Path.Combine(outputDir, targetFile);

            //write the file
            if (Path.GetExtension(targetFile) == ".pdf")
            {
                //parse attachment
                docConverter.Parse(patternPath, outputPath, row);
            }
            else
            {
                //simply copy attachment
                OutputHelper.Copy(patternPath, outputPath);
            }
            ret.Add(outputPath);
        }
        return ret.ToArray();
    }
}

#endregion

#region File Helper

public class InputFileHelper
{
    public required Encoding Encoding { get; set; }
    
    public event EventHandler<string>? OnBeforeReadFile;
    public event EventHandler<(string file,Exception e)>? OnReadFileFailed;
    public event EventHandler<string>? OnReadFileCompleted;

    public StreamReader CreateReader(string path) => new(path, Encoding, true);
    
    public string Read(string path)
    {
        OnBeforeReadFile?.Invoke(this, path);
        try
        {
            using var reader = CreateReader(path);
            var content = reader.ReadToEnd();
            OnReadFileCompleted?.Invoke(this, path);
            return content;
        }
        catch (Exception e)
        {
            OnReadFileFailed?.Invoke(this, (path, e));
            throw;
        }
    }
}

public class OutputFileHelper
{
    public required Encoding Encoding { get; set; }
    
    public event EventHandler<string>? OnBeforeDeleteFile;
    public event EventHandler<(string file,Exception e)>? OnDeleteFileFailed;
    public event EventHandler<string>? OnDeleteFileCompleted;
    
    public event EventHandler<(string source,string destination)>? OnBeforeMoveFile;
    public event EventHandler<(string source,string destination,Exception e)>? OnMoveFileFailed;
    public event EventHandler<(string source,string destination)>? OnMoveFileCompleted;
    
    public event EventHandler<(string source,string destination)>? OnBeforeCopyFile;
    public event EventHandler<(string source,string destination,Exception e)>? OnCopyFileFailed;
    public event EventHandler<(string source,string destination)>? OnCopyFileCompleted;
    
    public event EventHandler<string>? OnBeforeCreateDir;
    public event EventHandler<(string path,Exception e)>? OnCreateDirFailed;
    public event EventHandler<string>? OnCreateDirCompleted;
    
    public event EventHandler<string>? OnBeforeWriteFile;
    public event EventHandler<(string file,Exception e)>? OnWriteFileFailed;
    public event EventHandler<string>? OnWriteFileCompleted;
    
    public StreamWriter CreateWriter(string path) {
        CreateDirIfNotExist(Path.GetDirectoryName(path)!);
        return new(File.Create(path), Encoding);
    }

    public void Copy(string source, string target)
    {
        OnBeforeCopyFile?.Invoke(this, (source, target));
        try
        {
            CreateDirIfNotExist(Path.GetDirectoryName(target)!);
            File.Copy(source, target, true);
            OnCopyFileCompleted?.Invoke(this, (source, target));
        }
        catch (Exception e)
        {
            OnCopyFileFailed?.Invoke(this, (source, target, e));
            throw;
        }
    }

    public void Write(string content, string outputPath)
    {
        OnBeforeWriteFile?.Invoke(this, outputPath);
        using var writer = CreateWriter(outputPath);
        try
        {
            writer.Write(content);
            OnWriteFileCompleted?.Invoke(this, outputPath);
        }
        catch (Exception e)
        {
            OnWriteFileFailed?.Invoke(this, (outputPath, e));
            throw;
        }
    }

    public void Write(IEnumerable<string> lines, string outputPath)
    {
        OnBeforeWriteFile?.Invoke(this, outputPath);
        using var writer = CreateWriter(outputPath);
        try
        {
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
            OnWriteFileCompleted?.Invoke(this, outputPath);
        }
        catch (Exception e)
        {
            OnWriteFileFailed?.Invoke(this, (outputPath, e));
            throw;
        }
    }

    public void Delete(string path)
    {
        if (!File.Exists(path)) return;
        OnBeforeDeleteFile?.Invoke(this, path);
        try
        {
            File.Delete(path);
            OnDeleteFileCompleted?.Invoke(this, path);
        }
        catch (Exception e)
        {
            OnDeleteFileFailed?.Invoke(this, (path, e));
        }
    }

    public void CreateDirIfNotExist(string path)
    {
        if (!Directory.Exists(path))
        {
            OnBeforeCreateDir?.Invoke(this, path);
            try
            {
                Directory.CreateDirectory(path);
                OnCreateDirCompleted?.Invoke(this, path);
            }
            catch (Exception e)
            {
                OnCreateDirFailed?.Invoke(this, (path, e));
                throw;
            }
        }
    }

}

#endregion
