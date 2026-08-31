using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using RPMailCore.Models;
using RPMailCore.Services;

namespace RPMailCore.Processors;

public class MailRunProcessor : IDisposable
{
    private readonly ILogger _logger;
    private readonly MailSendProcessor? _mailSender;

    public Subject<IReadOnlyList<ImmutableDictionary<string, string>>> RowsLoaded { get; } = new();
    public Subject<TaskStateChanged> TaskStateChanged { get; } = new();
    public Subject<double> ProgressChanged { get; } = new();

    public MailRunProcessor(ILogger? logger = null, MailSendProcessor? mailSender = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _mailSender = mailSender;
    }

    public async Task<RunResult> RunAsync(MailConfig config, CancellationToken ct = default)
    {
        var encoding = EncodingResolver.Resolve(config.Template.CharSet);
        string realOutputDir = Path.Combine(config.Output.OutputDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
        int sentCount = 0;
        List<FailedRow> failedRows = [];
        HtmlToPdfProcessor? pdfProcessor = null;
        try
        {
            _logger.LogInformation("Parsing Contents from {CsvPath}", config.Template.CsvPath);
            FileIo.CreateDirectory(realOutputDir);

            var csv = CsvProcessor.Read(config.Template.CsvPath, encoding);
            RowsLoaded.OnNext(csv.Rows);

            var engine = new TemplateEngine();
            var contents = new List<ContentParsed>();
            pdfProcessor = new HtmlToPdfProcessor(config.Output.SaveRawDocs);

            for (int index = 0; index < csv.Rows.Length; index++)
            {
                ct.ThrowIfCancellationRequested();
                var row = csv.Rows[index];
                try
                {
                    string receiver = row[config.Template.ReceiverHeader];
                    _logger.LogInformation("Parsing Receiver: {Receiver}", receiver);

                    string subject = engine.Render(config.Template.Subject, row);
                    string htmlPath = engine.Render(config.Template.HtmlPath, row);
                    string htmlBody = engine.Render(FileIo.ReadAllText(htmlPath, encoding), row);

                    string outputDir = Path.Combine(realOutputDir, receiver);
                    if (config.Output.SaveHtmlFile || config.Output.ConvertOnly)
                        FileIo.WriteAllText(Path.Combine(outputDir, "body.html"), htmlBody, encoding);

                    var attachments = await BuildAttachments(engine, pdfProcessor, config.Output.Attachments, outputDir, row, encoding);

                    contents.Add(new ContentParsed
                    {
                        RowIndex = index,
                        Receiver = receiver,
                        Subject = subject,
                        HtmlBody = htmlBody,
                        Attachments = attachments,
                        HtmlPath = htmlPath,
                        OutputDir = outputDir,
                        RawRow = row,
                    });
                    TaskStateChanged.OnNext(new(index, MailTaskStatus.Pending, "Pending..."));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    failedRows.Add(new(row, e.Message));
                    TaskStateChanged.OnNext(new(index, MailTaskStatus.Failed, e.Message));
                    _logger.LogError(e, "Failed to parse row {Index}", index + 1);
                }
            }

            _logger.LogInformation("Parsed {Count} contents from {CsvPath}", contents.Count, config.Template.CsvPath);

            if (!config.Output.ConvertOnly)
            {
                var mailSender = _mailSender ?? new MailSendProcessor(config.Sender.SmtpHost, config.Sender.SenderEmail, config.Sender.SenderPassword);
                double progress = 0;
                double step = contents.Count > 0 ? 100.0 / contents.Count : 0;

                foreach (var content in contents)
                {
                    ct.ThrowIfCancellationRequested();
                    TaskStateChanged.OnNext(new(content.RowIndex, MailTaskStatus.Running, "Sending email..."));
                    _logger.LogInformation("Sending Email To: {Receiver}, Subject: {Subject}", content.Receiver, content.Subject);
                    try
                    {
                        await mailSender.SendAsync(content, ct);
                        TaskStateChanged.OnNext(new(content.RowIndex, MailTaskStatus.Success, "Email sent."));
                        _logger.LogInformation("Email sent.");
                        if (config.Output.DeleteAfterSent)
                        {
                            foreach (var attachment in content.Attachments)
                                FileIo.Delete(attachment);
                        }
                        sentCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        failedRows.Add(new(content.RawRow, e.Message));
                        TaskStateChanged.OnNext(new(content.RowIndex, MailTaskStatus.Failed, e.Message));
                        _logger.LogError(e, "Failed to send email to {Receiver}", content.Receiver);
                    }
                    progress += step;
                    ProgressChanged.OnNext(Math.Min(progress, 100));
                }
            }

            string? failedCsvPath = null;
            if (failedRows.Count > 0)
            {
                failedCsvPath = Path.Combine(realOutputDir, "data_failed.csv");
                FileIo.WriteCsv(failedCsvPath, csv.Headers, failedRows.Select(f => f.Row), encoding);
                _logger.LogWarning("Written FailedList to CSV File: {FailedCsvPath}", failedCsvPath);
            }
            else
            {
                _logger.LogInformation("All Done!");
            }

            return new RunResult(true, csv.Rows.Length, failedRows.Count, sentCount, realOutputDir, failedCsvPath, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected Error");
            return new RunResult(false, 0, 0, 0, realOutputDir, null, e);
        }
        finally
        {
            if (pdfProcessor is not null)
                await pdfProcessor.DisposeAsync();
        }
    }

    private async Task<ImmutableArray<string>> BuildAttachments(TemplateEngine engine, HtmlToPdfProcessor pdfProcessor, ImmutableArray<AttachmentPattern> attachmentPatterns, string outputDir, ImmutableDictionary<string, string> row, Encoding encoding)
    {
        var ret = ImmutableArray.CreateBuilder<string>();
        foreach (var attachment in attachmentPatterns)
        {
            string patternPath = engine.Render(attachment.Source, row);
            if (string.IsNullOrWhiteSpace(patternPath)) continue;

            string targetFile = engine.Render(attachment.Name, row);
            if (string.IsNullOrWhiteSpace(targetFile)) continue;

            if (string.IsNullOrWhiteSpace(Path.GetExtension(targetFile)))
                targetFile = Path.ChangeExtension(targetFile, ".pdf");

            string outputPath = Path.Combine(outputDir, targetFile);
            string renderedHtml = engine.Render(FileIo.ReadAllText(patternPath, encoding), row);
            await pdfProcessor.ConvertAsync(renderedHtml, outputPath, encoding);
            ret.Add(outputPath);
        }
        return ret.ToImmutable();
    }

    public void Dispose()
    {
        RowsLoaded.Dispose();
        TaskStateChanged.Dispose();
        ProgressChanged.Dispose();
    }
}
