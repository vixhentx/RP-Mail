using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Extensions;
using RPMailCore.Models;
using RPMailCore.Resources;
using RPMailCore.Services;
using SmartFormat;

namespace RPMailCore.Processors;

public class MailRunProcessor : IDisposable
{
    public const string EmailColumn = "email";
	readonly DisposableBag _d = new();
	readonly Subject<MailRunOutput> _output = new();
	readonly ReactiveProperty<double> _progress = new(0);
	readonly ReactiveProperty<bool> _running = new(false);

    public Observable<MailRunOutput> Output => _output;
	public ReadOnlyReactiveProperty<double> Progress => _progress;
	public ReadOnlyReactiveProperty<bool> Running => _running;

	public MailRunProcessor()
	{
		_progress
			.Subscribe(p => 
				Emit(new ProgressOutput(Math.Min(p, 100))
			))
			.AddTo(ref _d);
	}
    public async ValueTask<RunResult> RunAsync(MailConfig config, string workspaceDirectory, CancellationToken ct = default)
    {
		_running.Value = true; // 本来想用响应式管道搞的, 暂时想不到优雅解法了, 先这么用着.
		using var _ = Disposable.Create(_running, static r => r.Value = false);

		workspaceDirectory = Path.GetFullPath(workspaceDirectory);
		string ResolvePath(string path) => Path.GetFullPath(path, workspaceDirectory);

        if (!config.Output.ConvertOnly && config.Sender is null)
        {
            string message = Strings.SenderConfigRequired;
            var result = new RunResult(false, 0, 0, 0, "", null, new InvalidDataException(message));
            Emit(new MessageOutput(message, LogLevel.Error, result.FatalException));
            Emit(new RunCompletedOutput(result));
            return result;
        }

        var encoding = EncodingResolver.Resolve(config.Template.CharSet);
		string realOutputDir = Path.Combine(ResolvePath(config.Output.OutputDir), $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
		TemplateEngine engine = new();

		ConcurrentQueue<ContentParsed> contents = [];
        ConcurrentQueue<FailedRow> failedRows = [];

		ImmutableArray<string> BuildAttachments(
			TypstPdfProcessor pdfProcessor,
			ImmutableArray<AttachmentPattern> attachmentPatterns,
			string outputDir,
			ImmutableDictionary<string, string> user,
			IReadOnlyDictionary<string, string> extraAttributes)
		{
			var ret = ImmutableArray.CreateBuilder<string>();
			foreach (var attachment in attachmentPatterns)
			{
				var patternPath = ResolvePath(engine.Render(attachment.Source, user, extraAttributes));
				if (string.IsNullOrWhiteSpace(patternPath)) continue;

				var targetFile = engine.Render(attachment.Name, user, extraAttributes);
				if (string.IsNullOrWhiteSpace(targetFile)) continue;

				var outputPath = Path.Combine(outputDir, targetFile);

				switch ((pattern: Path.GetExtension(patternPath).ToLower(), target: Path.GetExtension(outputPath).ToLower()))
				{
					case { pattern: ".typ", target: ".pdf" }:
						var renderedTyp = engine.Render(FileIo.ReadAllText(patternPath, encoding), user, extraAttributes);
						pdfProcessor.Convert(renderedTyp, Path.GetDirectoryName(patternPath) ?? "", outputPath);
						break;
					default:
						File.Copy(patternPath, outputPath);
						break;
				}
				ret.Add(outputPath);
			}
			return ret.ToImmutable();
		}

        try
        {
            Emit(new MessageOutput(Smart.FormatDict(Strings.ParsingContents, new(){  ["CsvPath"] = config.Template.CsvPath  })));
            FileIo.CreateDirectory(realOutputDir);

			var csv = CsvProcessor.Read(ResolvePath(config.Template.CsvPath), encoding);
            Emit(new RowsLoadedOutput(csv.Rows));

			var rowNumDigits = (int)Math.Floor(Math.Log10(csv.Rows.Length));

			async ValueTask PrepareForRow(int index, CancellationToken ct = default)
			{
                ct.ThrowIfCancellationRequested();
                var row = csv.Rows[index];
				using TypstPdfProcessor pdfProcessor = new();
                try
                {
                    string email = row[EmailColumn];
                    Emit(new TaskStateOutput(index, MailTaskStatus.Preparing, null));
                    Emit(new MessageOutput(Smart.FormatDict(Strings.ParsingEmail, new() { ["Email"] = email })));

                    string subject = engine.Render(config.Template.Subject, row, config.Template.ExtraAttributes);
					string bodyHtmlPath = ResolvePath(engine.Render(config.Template.BodyHtmlPath, row, config.Template.ExtraAttributes));
                    string htmlBody = engine.Render(FileIo.ReadAllText(bodyHtmlPath, encoding), row, config.Template.ExtraAttributes);

                    string outputDir = Path.Combine(realOutputDir, $"{index.ToString().PadLeft(rowNumDigits,'0')}-{email}");
					Directory.CreateDirectory(outputDir);

                    if (config.Output.SaveHtmlFile || config.Output.ConvertOnly)
                        FileIo.WriteAllText(Path.Combine(outputDir, "body.html"), htmlBody, encoding);

					var attachments = BuildAttachments(pdfProcessor, config.Template.Attachments, outputDir, row, config.Template.ExtraAttributes);

                    contents.Enqueue(new ()
                    {
                        RowIndex = index,
                        Email = email,
                        Subject = subject,
                        BodyHtml = htmlBody,
                        Attachments = attachments,
                        BodyHtmlPath = bodyHtmlPath,
                        OutputDir = outputDir,
                        UserAttributes = row,
                        ExtraAttributes = config.Template.ExtraAttributes,
                    });
                    Emit(new TaskStateOutput(index, MailTaskStatus.Pending, null));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    failedRows.Enqueue(new(row, e.Message));
                    Emit(new TaskStateOutput(index, MailTaskStatus.Failed, e.Message));
                    Emit(new MessageOutput(Smart.FormatDict(Strings.FailedToParseRow, new() { ["Index"] = index + 1 }) + $": {e.Message}", LogLevel.Error, e));
                }
            }

			// 全部准备, 先等待, 再发送
			await Parallel.ForEachAsync(
				Enumerable.Range(0, csv.Rows.Length),
				parallelOptions: new() { CancellationToken = ct },
				PrepareForRow
			);
            

            Emit(new MessageOutput(Smart.FormatDict(Strings.ParsedContents, new() { ["Count"] = contents.Count, ["CsvPath"] = config.Template.CsvPath })));
			int sentCount = 0;

            if (!config.Output.ConvertOnly && contents is { IsEmpty: false })
            {
				_progress.Value = 0;
                var mailSender = new MailSendProcessor(config.Sender);
                double step = 100.0 / contents.Count;

                foreach (var content in contents)
                {
                    ct.ThrowIfCancellationRequested();
                    Emit(new TaskStateOutput(content.RowIndex, MailTaskStatus.Running, Strings.StatusSending));
                    Emit(new MessageOutput(Smart.FormatDict(Strings.SendingEmailTo, new() { ["Email"] = content.Email, ["Subject"] = content.Subject })));
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
                        failedRows.Enqueue(new(content.UserAttributes, e.Message));
                        Emit(new TaskStateOutput(content.RowIndex, MailTaskStatus.Failed, e.Message));
                        Emit(new MessageOutput(Smart.FormatDict(Strings.FailedToSendEmail, new() { ["Email"] = content.Email }) + $": {e.Message}", LogLevel.Error, e));
                    }
                    _progress.Value = Math.Min(_progress.Value + step,100);
                }
            }

            string? failedCsvPath = null;
            if (!failedRows.IsEmpty)
            {
                failedCsvPath = Path.Combine(realOutputDir, "data_failed.csv");
                FileIo.WriteCsv(failedCsvPath, csv.Headers, failedRows.Select(f => f.Row), encoding);
                Emit(new MessageOutput(Smart.FormatDict(Strings.WrittenFailedList, new() { ["FailedCsvPath"] = failedCsvPath }), LogLevel.Warning));
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
    }

    public void Dispose()
    {
        _output.Dispose();
    }

	readonly Lock _emitLock = new();
    void Emit(MailRunOutput output)
	{
		lock(_emitLock)
			_output.OnNext(output);
	}
}
