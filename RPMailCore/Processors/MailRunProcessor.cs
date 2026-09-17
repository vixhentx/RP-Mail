using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Models;
using RPMailCore.Resources;
using RPMailCore.Services;
using SmartFormat;

namespace RPMailCore.Processors;

public class MailRunProcessor : IDisposable
{
    private const string EmailColumn = "email";
    private readonly MailSendProcessor? _mailSender;

    public Subject<MailRunOutput> Output { get; } = new();

    public MailRunProcessor(MailSendProcessor? mailSender = null)
    {
        _mailSender = mailSender;
    }

    public async Task<RunResult> RunAsync(MailConfig config, CancellationToken ct = default)
    {
        if (!config.Output.ConvertOnly && config.Sender is null)
        {
            string message = Strings.SenderConfigRequired;
            var result = new RunResult(false, 0, 0, 0, "", null, new InvalidDataException(message));
            Emit(new MessageOutput(message, LogLevel.Error, result.FatalException));
            Emit(new RunCompletedOutput(result));
            return result;
        }

        var encoding = EncodingResolver.Resolve(config.Template.CharSet);
        string realOutputDir = Path.Combine(config.Output.OutputDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
        int sentCount = 0;
        List<FailedRow> failedRows = [];
        TypstPdfProcessor? pdfProcessor = null;
        try
        {
            Emit(new MessageOutput(Smart.Format(Strings.ParsingContents, new { CsvPath = config.Template.CsvPath })));
            FileIo.CreateDirectory(realOutputDir);

            var csv = CsvProcessor.Read(config.Template.CsvPath, encoding);
            Emit(new RowsLoadedOutput(csv.Rows));

            var engine = new TemplateEngine();
            var contents = new List<ContentParsed>();
            pdfProcessor = new TypstPdfProcessor();

            for (int index = 0; index < csv.Rows.Length; index++)
            {
                ct.ThrowIfCancellationRequested();
                var row = csv.Rows[index];
                try
                {
                    string email = row[EmailColumn];
                    Emit(new MessageOutput(Smart.Format(Strings.ParsingEmail, new { Email = email })));

                    string subject = engine.Render(config.Template.Subject, row, config.Template.ExtraAttributes);
                    string bodyHtmlPath = engine.Render(config.Template.BodyHtmlPath, row, config.Template.ExtraAttributes);
                    string htmlBody = engine.Render(FileIo.ReadAllText(bodyHtmlPath, encoding), row, config.Template.ExtraAttributes);

                    string outputDir = Path.Combine(realOutputDir, email);
                    if (config.Output.SaveHtmlFile || config.Output.ConvertOnly)
                        FileIo.WriteAllText(Path.Combine(outputDir, "body.html"), htmlBody, encoding);

                    var attachments = BuildAttachments(engine, pdfProcessor, config.Template.Attachments, outputDir, row, encoding, config.Template.ExtraAttributes);

                    contents.Add(new ContentParsed
                    {
                        RowIndex = index,
                        Email = email,
                        Subject = subject,
                        BodyHtml = htmlBody,
                        Attachments = attachments,
                        BodyHtmlPath = bodyHtmlPath,
                        OutputDir = outputDir,
                        UserAttributes = row,
                        ExtraAttributes = config.Template.ExtraAttributes.ToImmutableDictionary(),
                    });
                    Emit(new TaskStateOutput(index, MailTaskStatus.Pending, Strings.StatusPending));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    failedRows.Add(new(row, e.Message));
                    Emit(new TaskStateOutput(index, MailTaskStatus.Failed, e.Message));
                    Emit(new MessageOutput(Smart.Format(Strings.FailedToParseRow, new { Index = index + 1 }) + $": {e.Message}", LogLevel.Error, e));
                }
            }

            Emit(new MessageOutput(Smart.Format(Strings.ParsedContents, new { Count = contents.Count, CsvPath = config.Template.CsvPath })));

            if (!config.Output.ConvertOnly)
            {
                var mailSender = _mailSender ?? new MailSendProcessor(config.Sender!.SmtpHost, config.Sender.SenderEmail, config.Sender.SenderPassword);
                double progress = 0;
                double step = contents.Count > 0 ? 100.0 / contents.Count : 0;

                foreach (var content in contents)
                {
                    ct.ThrowIfCancellationRequested();
                    Emit(new TaskStateOutput(content.RowIndex, MailTaskStatus.Running, Strings.StatusSending));
                    Emit(new MessageOutput(Smart.Format(Strings.SendingEmailTo, new { Email = content.Email, Subject = content.Subject })));
                    try
                    {
                        await mailSender.SendAsync(content, ct);
                        Emit(new TaskStateOutput(content.RowIndex, MailTaskStatus.Success, Strings.EmailSent));
                        Emit(new MessageOutput(Strings.EmailSent));
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
                        failedRows.Add(new(content.UserAttributes, e.Message));
                        Emit(new TaskStateOutput(content.RowIndex, MailTaskStatus.Failed, e.Message));
                        Emit(new MessageOutput(Smart.Format(Strings.FailedToSendEmail, new { Email = content.Email }) + $": {e.Message}", LogLevel.Error, e));
                    }
                    progress += step;
                    Emit(new ProgressOutput(Math.Min(progress, 100)));
                }
            }

            string? failedCsvPath = null;
            if (failedRows.Count > 0)
            {
                failedCsvPath = Path.Combine(realOutputDir, "data_failed.csv");
                FileIo.WriteCsv(failedCsvPath, csv.Headers, failedRows.Select(f => f.Row), encoding);
                Emit(new MessageOutput(Smart.Format(Strings.WrittenFailedList, new { FailedCsvPath = failedCsvPath }), LogLevel.Warning));
            }
            else
            {
                Emit(new MessageOutput(Strings.AllDone));
            }

            var result = new RunResult(true, csv.Rows.Length, failedRows.Count, sentCount, realOutputDir, failedCsvPath, null);
            Emit(new RunCompletedOutput(result));
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            var result = new RunResult(false, 0, 0, 0, realOutputDir, null, e);
            Emit(new MessageOutput($"{Strings.UnexpectedError}: {e.Message}", LogLevel.Error, e));
            Emit(new RunCompletedOutput(result));
            return result;
        }
        finally
        {
            pdfProcessor?.Dispose();
        }
    }

    private ImmutableArray<string> BuildAttachments(TemplateEngine engine, TypstPdfProcessor pdfProcessor, ImmutableArray<AttachmentPattern> attachmentPatterns, string outputDir, ImmutableDictionary<string, string> user, Encoding encoding, IReadOnlyDictionary<string, string> extraAttributes)
    {
        var ret = ImmutableArray.CreateBuilder<string>();
        foreach (var attachment in attachmentPatterns)
        {
            string patternPath = engine.Render(attachment.Source, user, extraAttributes);
            if (string.IsNullOrWhiteSpace(patternPath)) continue;

            string targetFile = engine.Render(attachment.Name, user, extraAttributes);
            if (string.IsNullOrWhiteSpace(targetFile)) continue;

            string outputPath = Path.Combine(outputDir, targetFile);

			switch((pattern: Path.GetExtension(patternPath).ToLower(), target: Path.GetExtension(outputPath).ToLower()))
			{
				// Render Typst to PDF
				case { pattern: ".typ", target: ".pdf" }:
					string renderedTyp = engine.Render(FileIo.ReadAllText(patternPath, encoding), user, extraAttributes);
					pdfProcessor.Convert(renderedTyp, Path.GetDirectoryName(patternPath) ?? "", outputPath);
					break;
				// Rename and passthru
				default:
					File.Copy(patternPath,outputPath);
					break;
			}
            ret.Add(outputPath);
        }
        return ret.ToImmutable();
    }

    public void Dispose()
    {
        Output.Dispose();
    }

    private void Emit(MailRunOutput output) => Output.OnNext(output);
}
